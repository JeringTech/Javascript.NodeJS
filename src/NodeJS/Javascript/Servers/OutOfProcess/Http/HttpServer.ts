// The typings for module are incomplete and can't be augmented, so import as any.
var Module = require('module');
import * as path from 'path';
import * as http from 'http';
import * as stream from 'stream';
import { AddressInfo } from 'net';
import InvocationRequest from '../../../NodeInvocationData/InvocationRequest';
import ModuleSourceType from '../../../NodeInvocationData/ModuleSourceType';

// Parse arguments
const args: { [key: string]: string } = parseArgs(process.argv);

// Start auto-termination loop
exitWhenParentExits(parseInt(args.parentPid), true, 1000);

// Start server
const server = http.createServer((req, res) => {
    let bodyChunks = [];
    req.
        on('data', chunk => bodyChunks.push(chunk)).
        on('end', () => {
            try {
                // Create InvocationRequest
                let body: string = Buffer.concat(bodyChunks).toString();
                let invocationRequest: InvocationRequest;
                if (req.headers['content-type'] == 'application/json') {
                    invocationRequest = JSON.parse(body);
                } else if (req.headers['content-type'] == 'multipart/mixed') {
                    let parts: string[] = body.split('--Jering.JavascriptUtils.Node');
                    invocationRequest = JSON.parse(parts[0]);
                    invocationRequest.moduleSource = parts[1];
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
                    let module = new Module(null, null);
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
                } else if (invocationRequest.moduleSourceType === ModuleSourceType.File) {
                    const resolvedPath = path.resolve(process.cwd(), invocationRequest.moduleSource);
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
                let args: object[] = [callback];
                functionToInvoke.apply(null, args.concat(invocationRequest.args));
            } catch (synchronousError) {
                respondWithError(res, synchronousError);
            }
        });
}).listen(parseInt(args.port), 'localhost', function () {
    // Signal to HttpNodeHost which loopback IP address (IPv4 or IPv6) and port it should make its HTTP connections on
    // and that we are ready to process invocations.
    let info = server.address() as AddressInfo;
    console.log(`[Jering.JavascriptUtils.Node: Listening on IP - ${info.address} Port - ${info.port}]`);
});

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
