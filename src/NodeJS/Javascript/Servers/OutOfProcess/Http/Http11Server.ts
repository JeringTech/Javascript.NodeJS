// The typings for module are incomplete and can't be augmented, so import as any.
const Module = require('module');
import * as http from 'http';
import { AddressInfo, Socket } from 'net';
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
    const server: http.Server = http.createServer(serverOnRequestListener);

    // The timeouts below are designed to manage network instability. Since we're using the HTTP protocol on a local machine, we can disable them
    // to avoid their overhead and stability issues.

    // By default, on older versions of Node.js, request handling times out after 120 seconds.
    // This timeout is disabled by default on Node.js v13+. 
    // Becuase of the older versions, we explicitly disable it.
    server.setTimeout(0);

    // By default, a socket is destroyed if it receives no incoming data for 5 seconds: https://nodejs.org/api/http.html#http_server_keepalivetimeout. 
    // This is good practice when making external requests because DNS records may change: https://github.com/dotnet/runtime/issues/18348.
    // Since we're using the HTTP protocol on a local machine, it's safe and more efficient to keep sockets alive indefinitely.
    server.keepAliveTimeout = 0;

    // By default, a socket is destroyed if its incoming headers take longer than 60 seconds: https://nodejs.org/api/http.html#http_server_headerstimeout.
    // In early versions of Node.js, even if setTimeout() was specified with a non-zero value, the server would wait indefinitely for headers. 
    // This timeout was added to deal with that issue. We specify setTimeout(0), so this timeout is of no use to us.
    //
    // Note that while 0 disables this timeout in node 12.17+, in earlier versions it causes requests to time out immediately, so set to max positive int 32.
    server.headersTimeout = 2147483647;

    // Log timed out connections for debugging
    server.on('timeout', serverOnTimeout);

    // Send client error details to client for debugging
    server.on('clientError', serverOnClientError);

    // Start server
    server.listen(parseInt(args.port), 'localhost', () => {
        // Signal to HttpNodeHost which loopback IP address (IPv4 or IPv6) and port it should make its HTTP connections on
        // and that we are ready to process invocations.
        let info = server.address() as AddressInfo;
        console.log(`[Jering.Javascript.NodeJS: HttpVersion - HTTP/1.1 Listening on IP - ${info.address} Port - ${info.port}]`);
    });
}

function serverOnRequestListener(req: http.IncomingMessage, res: http.ServerResponse): void {
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
                        exports = await import(/* webpackIgnore: true */ 'file:///' + resolvedPath.replaceAll('\\', '/'));
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
                    functionToInvoke = exports[invocationRequest.exportName] ?? exports.default?.[invocationRequest.exportName];
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
                const callback = (error: Error | string, result: any, responseAction?: (response: http.ServerResponse) => boolean) => {
                    if (callbackCalled) {
                        return;
                    }
                    callbackCalled = true;

                    if (error != null) {
                        respondWithError(res, error);
                    }

                    if (responseAction?.(res)) {
                        return;
                    }

                    if (result instanceof stream.Readable) {
                        // By default, res is ended when result ends - https://nodejs.org/api/stream.html#stream_readable_pipe_destination_options
                        result.pipe(res);
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


// Send error details to client for debugging - https://nodejs.org/api/http.html#http_event_clienterror
function serverOnClientError(error: Error, socket: stream.Duplex): void {
    let errorJson = JSON.stringify({
        errorMessage: error.message,
        errorStack: error.stack
    });

    let httpResponseMessage = `HTTP/1.1 500 Internal Server Error\r\nContent-Length: ${Buffer.byteLength(errorJson, 'utf8')}\r\nContent-Type: text/html\r\n\r\n${errorJson}`;
    socket.end(httpResponseMessage);
}

// Send timeout details to client for debugging - this shouldn't fire but there have been various node http server timeout issues in the past.
// The socket won't actually get closed (the timeout function needs to do that manually).
function serverOnTimeout(socket: Socket): void {
    console.log(`[Node.js HTTP server] Ignoring unexpected socket timeout for address ${socket.remoteAddress}, port ${socket.remotePort}`);
}