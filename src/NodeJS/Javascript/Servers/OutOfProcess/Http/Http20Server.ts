// The typings for module are incomplete and can't be augmented, so import as any.
const Module = require('module');
import * as http2 from 'http2';
import { AddressInfo } from 'net';
import * as path from 'path';
import * as stream from 'stream';
import InvocationRequest from '../../../InvocationData/InvocationRequest';
import ModuleSourceType from '../../../InvocationData/ModuleSourceType';
import { getTempIdentifier, respondWithError, setup } from './Shared';

// Setup
const [args, projectDir, moduleResolutionPaths] = setup();

// Start
startServer();

function startServer(): void {
    // Create server
    const server: http2.Http2Server = http2.createServer(serverOnRequestListener);

    // By default, on older versions of Node.js, request handling times out after 120 seconds.
    // This timeout is disabled by default on Node.js v13+. 
    // Becuase of the older versions, we explicitly disable it.
    server.setTimeout(0);

    // Log timed out connections for debugging
    server.on('timeout', serverOnTimeout);

    // Send client error details to client for debugging
    server.on('sessionError', serverOnSessionError); // Provides a bit more specificity than the error event below
    server.on('error', serverOnError);

    // Start server
    server.listen(parseInt(args.port), 'localhost', () => {
        // Signal to HttpNodeHost which loopback IP address (IPv4 or IPv6) and port it should make its HTTP connections on
        // and that we are ready to process invocations.
        let info = server.address() as AddressInfo;
        console.log(`[Jering.Javascript.NodeJS: HttpVersion - HTTP/2.0 Listening on IP - ${info.address} Port - ${info.port}]`);
    });
}

function serverOnRequestListener(req: http2.Http2ServerRequest, res: http2.Http2ServerResponse) {
    const bodyChunks = [];
    req.
        on('data', chunk => bodyChunks.push(chunk)).
        on('end', async () => {
            try {
                // Create InvocationRequest
                const body: string = Buffer.concat(bodyChunks).toString();
                let invocationRequest: InvocationRequest;
                if (req.headers['content-type'] === 'multipart/mixed') {
                    const parts: string[] = body.split('--Uiw6+hXl3k+5ia0cUYGhjA==');
                    invocationRequest = JSON.parse(parts[0]);
                    invocationRequest.moduleSource = parts[1];
                } else {
                    invocationRequest = JSON.parse(body);
                }

                // Get exports of module specified by InvocationRequest.moduleSource
                let exports: any;
                if (invocationRequest.moduleSourceType === ModuleSourceType.Cache) {
                    const cachedModule = Module._cache[invocationRequest.moduleSource];

                    // Cache miss
                    if (cachedModule == null) {
                        res.statusCode = 404;
                        res.end();
                        return;
                    }

                    exports = cachedModule.exports;
                } else if (invocationRequest.moduleSourceType === ModuleSourceType.Stream ||
                    invocationRequest.moduleSourceType === ModuleSourceType.String) {
                    // Check if already cached
                    if (invocationRequest.cacheIdentifier != null) {
                        const cachedModule = Module._cache[invocationRequest.cacheIdentifier];
                        if (cachedModule != null) {
                            exports = cachedModule.exports;
                        }
                    }

                    // Not cached
                    if (exports == null) {
                        const newModule = new Module('', null);

                        // Specify paths where child modules may be.
                        newModule.paths = moduleResolutionPaths;

                        // Node.js exposes a method for loading a module from a file: Module.load - https://github.com/nodejs/node/blob/6726246dbb83e3251f080fc4729154d492f7e340/lib/internal/modules/cjs/loader.js#L942.
                        // Since we're loading a module in string form, we can't use it. Instead we call Module._compile - https://github.com/nodejs/node/blob/6726246dbb83e3251f080fc4729154d492f7e340/lib/internal/modules/cjs/loader.js#L1043,
                        // which Module.load calls internally.
                        newModule._compile(invocationRequest.moduleSource, 'anonymous');

                        if (invocationRequest.cacheIdentifier != null) {
                            // Notes on module caching:
                            // When a module is required using require, it is cached in Module._cache using its absolute file path as its key.
                            // When Module._load tries to load the same module again, it first resolves the absolute file path of the module, then it 
                            // checks if the module exists in the cache. Custom keys for in memory modules cause an error at the file resolution step.
                            // To make modules with custom keys requirable by other modules, require must be monkey patched.
                            Module._cache[invocationRequest.cacheIdentifier] = newModule;
                        }

                        exports = newModule.exports;
                    }
                } else if (invocationRequest.moduleSourceType === ModuleSourceType.File) {
                    const resolvedPath = path.resolve(projectDir, invocationRequest.moduleSource);
                    if (resolvedPath.endsWith('.mjs')) {
                        exports = await import(/* webpackIgnore: true */ 'file:///' + resolvedPath.replaceAll('\\', '/'));
                    } else {
                        exports = __non_webpack_require__(resolvedPath);
                    }
                } else {
                    respondWithError(res, `Invalid module source type: ${invocationRequest.moduleSourceType}.`);
                    return;
                }
                if (exports == null || typeof exports === 'object' && Object.keys(exports).length === 0) {
                    respondWithError(res, `The module ${getTempIdentifier(invocationRequest)} has no exports. Ensure that the module assigns a function or an object containing functions to module.exports.`);
                    return;
                }

                // Get function to invoke
                let functionToInvoke: Function;
                if (invocationRequest.exportName != null) {
                    functionToInvoke = exports[invocationRequest.exportName];
                    if (functionToInvoke == null) {
                        respondWithError(res, `The module ${getTempIdentifier(invocationRequest)} has no export named ${invocationRequest.exportName}.`);
                        return;
                    }
                    if (!(typeof functionToInvoke === 'function')) {
                        respondWithError(res, `The export named ${invocationRequest.exportName} from module ${getTempIdentifier(invocationRequest)} is not a function.`);
                        return;
                    }
                } else if (typeof exports === 'function') {
                    functionToInvoke = exports;
                } else if (typeof exports.default === 'function') { // .mjs default export
                    functionToInvoke = exports.default;
                } else {
                    respondWithError(res, `The module ${getTempIdentifier(invocationRequest)} does not export a function.`);
                    return;
                }

                let callbackCalled = false;
                const callback = (error: Error | string, result: any) => {
                    if (callbackCalled) {
                        return;
                    }
                    callbackCalled = true;

                    if (error != null) {
                        respondWithError(res, error);
                    } else if (result instanceof stream.Readable) {
                        // By default, res is ended when result ends - https://nodejs.org/api/stream.html#stream_readable_pipe_destination_options
                        result.pipe(res.stream);
                    } else if (typeof result === 'string') {
                        // String - can bypass JSON-serialization altogether
                        res.end(result);
                    } else {
                        // Arbitrary object/number/etc - JSON-serialize it
                        let responseJson: string;
                        try {
                            responseJson = JSON.stringify(result);
                        } catch (err) {
                            // JSON serialization error - pass it back to .NET
                            respondWithError(res, err);
                            return;
                        }
                        res.end(responseJson);
                    }
                }

                // Invoke function 
                if (functionToInvoke.constructor.name === "AsyncFunction") {
                    callback(null, await functionToInvoke.apply(null, invocationRequest.args));
                } else {
                    const args: object[] = [callback];
                    functionToInvoke.apply(null, args.concat(invocationRequest.args));
                }
            } catch (error) {
                respondWithError(res, error);
            }
        });
}

// Send error details to client for debugging
function serverOnSessionError(error: Error) {
    console.log(`[Node.js HTTP server] Session error: ${error.stack}`);
}

function serverOnError(error: Error) {
    console.log(`[Node.js HTTP server] Error: ${error.stack}`);
}

// Send timeout details to client for debugging - this shouldn't fire but there have been various node http server timeout issues in the past.
// The socket won't actually get closed (the timeout function needs to do that manually).
function serverOnTimeout() {
    console.log(`[Node.js HTTP server] Ignoring unexpected socket timeout`);
}