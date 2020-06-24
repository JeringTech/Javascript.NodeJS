// The typings for module are incomplete and can't be augmented, so import as any.
var Module = require('module');
import * as path from 'path';
import * as http from 'http';
import * as stream from 'stream';
import { AddressInfo, Socket } from 'net';
import InvocationRequest from '../../../InvocationData/InvocationRequest';
import ModuleSourceType from '../../../InvocationData/ModuleSourceType';

// Parse arguments
const args: { [key: string]: string } = parseArgs(process.argv);

// Overwrite writing to output streams
demarcateMessageEndings(process.stdout);
demarcateMessageEndings(process.stderr);

// Start auto-termination loop
exitWhenParentExits(parseInt(args.parentPid), true, 1000);

// Patch lstat - issue explained in this comment: https://github.com/aspnet/JavaScriptServices/issues/1101#issue-241971678
patchLStat();

// Set by NodeJSProcessFactory
let projectDir = process.cwd();

// Create server
const server = http.createServer(serverOnRequestListener);

// In Node.js v13+ this is the default, however for earlier versions it is 120 seconds.
server.setTimeout(0);
server.keepAliveTimeout = 0;
// This is fixed so that 0 disables the timeout in node 12.17+ -- for earlier versions 0 times out immediately, so set it to 10 years
server.headersTimeout = 10 * 365 * 24 * 60 * 60 * 1000;

// Log timed out connections for debugging
server.on('timeout', serverOnTimeout);

// Send client error details to client for debugging
server.on('clientError', serverOnClientError);

// Start server
server.listen(parseInt(args.port), 'localhost', serverOnListeningListener);

function serverOnRequestListener(req, res) {
    let bodyChunks = [];
    req.
        on('data', chunk => bodyChunks.push(chunk)).
        on('end', async () => {
            try {
                // Create InvocationRequest
                let body: string = Buffer.concat(bodyChunks).toString();
                let invocationRequest: InvocationRequest;
                if (req.headers['content-type'] == 'multipart/mixed') {
                    let parts: string[] = body.split('--Uiw6+hXl3k+5ia0cUYGhjA==');
                    invocationRequest = JSON.parse(parts[0]);
                    invocationRequest.moduleSource = parts[1];
                } else {
                    invocationRequest = JSON.parse(body);
                }

                // Get exports of module specified by InvocationRequest.moduleSource
                let exports: any;
                if (invocationRequest.moduleSourceType === ModuleSourceType.Cache) {
                    var cachedModule = Module._cache[invocationRequest.moduleSource];

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
                    if (invocationRequest.newCacheIdentifier != null) {
                        cachedModule = Module._cache[invocationRequest.newCacheIdentifier];
                        if (cachedModule != null) {
                            exports = cachedModule.exports;
                        }
                    }

                    // Not cached
                    if (exports == null) {
                        // global is analagous to window in browsers (the global namespace) - https://nodejs.org/api/globals.html#globals_global
                        // module is a reference to the current module - https://nodejs.org/api/modules.html#modules_module
                        let parent = global.module;
                        let module = new Module('', parent);
                        // This is typically done by Module._resolveLookupPaths which is called by Module._resolveFilename which in turn is called by Module._load.
                        // Since we're loading a module in string form - not an actual file with a filename - we can't use Module._load. So we do this manually.
                        module.paths = parent.paths;
                        module._compile(invocationRequest.moduleSource, 'anonymous');

                        if (invocationRequest.newCacheIdentifier != null) {
                            // Notes on module caching:
                            // When a module is required using require, it is cached in Module._cache using its absolute file path as its key.
                            // When Module._load tries to load the same module again, it first resolves the absolute file path of the module, then it 
                            // checks if the module exists in the cache. Custom keys for in memory modules cause an error at the file resolution step.
                            // To make modules with custom keys requirable by other modules, require must be monkey patched.
                            Module._cache[invocationRequest.newCacheIdentifier] = module;
                        }

                        exports = module.exports;
                    }
                } else if (invocationRequest.moduleSourceType === ModuleSourceType.File) {
                    const resolvedPath = path.resolve(projectDir, invocationRequest.moduleSource);
                    exports = __non_webpack_require__(resolvedPath);
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
                } else {
                    if (!(typeof exports === 'function')) {
                        respondWithError(res, `The module ${getTempIdentifier(invocationRequest)} does not export a function.`);
                        return;
                    }
                    functionToInvoke = exports;
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
                    let args: object[] = [callback];
                    functionToInvoke.apply(null, args.concat(invocationRequest.args));
                }
            } catch (error) {
                respondWithError(res, error);
            }
        });
}

// Send error details to client for debugging - https://nodejs.org/api/http.html#http_event_clienterror
function serverOnClientError(error: Error, socket: stream.Duplex) {
    let errorJson = JSON.stringify({
        errorMessage: error.message,
        errorStack: error.stack
    });

    let httpResponseMessage = `HTTP/1.1 500 Internal Server Error\r\nContent-Length: ${Buffer.byteLength(errorJson, 'utf8')}\r\nContent-Type: text/html\r\n\r\n${errorJson}`;
    socket.end(httpResponseMessage);
}

// Send timeout details to client for debugging - this shouldn't fire but there have been various node http server timeout issues in the past
// The socket won't actually get closed (the timeout function needs to do that manually)
function serverOnTimeout(socket: Socket) {
    console.error(`Ignoring unexpected socket timeout for address ${socket.remoteAddress}, port ${socket.remotePort}`);
}

function serverOnListeningListener() {
    // Signal to HttpNodeHost which loopback IP address (IPv4 or IPv6) and port it should make its HTTP connections on
    // and that we are ready to process invocations.
    let info = server.address() as AddressInfo;
    console.log(`[Jering.Javascript.NodeJS: Listening on IP - ${info.address} Port - ${info.port}]`);
}

function getTempIdentifier(invocationRequest: InvocationRequest): string {
    if (invocationRequest.newCacheIdentifier == null) {
        return `"${invocationRequest.moduleSource.substring(0, 25)}..."`;
    } else {
        return invocationRequest.newCacheIdentifier;
    }
}

function respondWithError(res: http.ServerResponse, error: Error | string) {
    let errorIsString: boolean = typeof error === 'string';

    res.statusCode = 500;
    res.end(JSON.stringify({
        errorMessage: errorIsString ? error : (error as Error).message,
        errorStack: errorIsString ? null : (error as Error).stack
    }));
}

// https://github.com/aspnet/JavaScriptServices/blob/0dc570a0c8725e3031ce5a884d7df3cfb75545ba/src/Microsoft.AspNetCore.NodeServices/TypeScript/Util/ArgsUtil.ts
function parseArgs(args: string[]) {
    let currentKey = null;
    const result: any = {};
    args.forEach(arg => {
        if (arg.indexOf('--') === 0) {
            const argName = arg.substring(2);
            result[argName] = undefined;
            currentKey = argName;
        } else if (currentKey !== null) {
            result[currentKey] = arg;
            currentKey = null;
        }
    })

    return result;
}

// https://github.com/aspnet/JavaScriptServices/blob/0dc570a0c8725e3031ce5a884d7df3cfb75545ba/src/Microsoft.AspNetCore.NodeServices/TypeScript/Util/ExitWhenParentExits.ts
function exitWhenParentExits(parentPid: number, ignoreSigint: boolean, pollIntervalMS: number) {
    setInterval(() => {
        if (!processExists(parentPid)) {
            console.log(`Parent process (pid: ${parentPid}) exited. Exiting this process...`);
            process.exit();
        }
    }, pollIntervalMS);

    if (ignoreSigint) {
        process.on('SIGINT', () => {
            console.log('Received SIGINT. Waiting for .NET process to exit...');
        });
    }
}

// https://github.com/aspnet/JavaScriptServices/blob/0dc570a0c8725e3031ce5a884d7df3cfb75545ba/src/Microsoft.AspNetCore.NodeServices/TypeScript/Util/ExitWhenParentExits.ts
function processExists(pid: number) {
    try {
        process.kill(pid, 0);
        return true;
    } catch (ex) {
        if (ex.code === 'EPERM') {
            throw new Error(`Attempted to check whether process ${pid} was running, but got a permissions error.`);
        }
        return false;
    }
}

// https://github.com/aspnet/JavaScriptServices/blob/4763ad5b8c0575f030a3cac8518767f4bd192c9b/src/Microsoft.AspNetCore.NodeServices/TypeScript/Util/PatchModuleResolutionLStat.ts
function patchLStat() {
    function patchedLStat(pathToStatLong: string, fsReqWrap?: any) {
        try {
            // If the lstat completes without errors, we don't modify its behavior at all
            return origLStat.apply(this, arguments);
        } catch (ex) {
            const shouldOverrideError =
                ex.message.startsWith('EPERM')                     // It's a permissions error
                && typeof appRootDirLong === 'string'
                && appRootDirLong.startsWith(pathToStatLong)       // ... for an ancestor directory
                && ex.stack.indexOf('Object.realpathSync ') >= 0;   // ... during symlink resolution
            if (shouldOverrideError) {
                // Fake the result to give the same result as an 'lstat' on the app root dir.
                // This stops Node failing to load modules just because it doesn't know whether
                // ancestor directories are symlinks or not. If there's a genuine file
                // permissions issue, it will still surface later when Node actually
                // tries to read the file.
                return origLStat.call(this, projectDir, fsReqWrap);
            } else {
                // In any other case, preserve the original error
                throw ex;
            }
        }
    }

    // It's only necessary to apply this workaround on Windows
    let appRootDirLong: string = null;
    let origLStat: Function = null;
    if (/^win/.test(process.platform)) {
        try {
            // Get the app's root dir in Node's internal "long" format (e.g., \\?\C:\dir\subdir)
            appRootDirLong = (path as any)._makeLong(projectDir);

            // Actually apply the patch, being as defensive as possible
            const bindingFs = (process as any).binding('fs');
            origLStat = bindingFs.lstat;
            if (typeof origLStat === 'function') {
                bindingFs.lstat = patchedLStat;
            }
        } catch (ex) {
            // If some future version of Node throws (e.g., to prevent use of process.binding()),
            // don't apply the patch, but still let the application run.
        }
    }
}

function demarcateMessageEndings(outputStream: NodeJS.WritableStream) {
    const origWriteFunction = outputStream.write;
    outputStream.write = <any>function (value: any) {
        // Only interfere with the write if it's definitely a string
        if (typeof value === 'string') {
            // Node appends a new line character at the end of the message. This facilitates reading of the stream: the process reading it reads messages line by line -
            // characters stay in its buffer until a new line character is received. This means that the null terminating character must appear before the last
            // new line character of the message. This approach is inefficent since it allocates 2 strings.
            //
            // TODO consider sending '\0\n' as a demarcator after sending value (profile). Also need to check if logging from worker threads might cause
            // concurrency issues.
            arguments[0] = value.slice(0, value.length - 1) + '\0\n';
        }

        origWriteFunction.apply(this, arguments);
    };
}