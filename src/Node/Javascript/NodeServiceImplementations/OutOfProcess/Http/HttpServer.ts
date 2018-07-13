// The typings for module are incomplete and can't be augmented, so import as any.
var Module = require('module'); 
import * as path from 'path';
import * as http from 'http';
import { AddressInfo } from 'net';
import InvocationRequest from '../../../NodeInvocationData/InvocationRequest';
import ModuleSourceType from '../../../NodeInvocationData/ModuleSourceType';

// TODO read through original, make sure nothing missed out, e.g stream

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
            debugger;

            try {
                // Create InvocationRequest
                let body: string = Buffer.concat(bodyChunks).toString();
                let invocationRequest: InvocationRequest;
                if (req.headers["content-type"] == 'application/json') {
                    invocationRequest = JSON.parse(body);
                } else if (req.headers["content-type"] == 'multipart/mixed') {
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
                    }

                    exports = cachedModule.exports;
                } else if (invocationRequest.moduleSourceType === ModuleSourceType.Stream ||
                    invocationRequest.moduleSourceType === ModuleSourceType.String) {
                    let module = new Module(null, null);
                    module._compile(invocationRequest.moduleSource, "anonymous");

                    if (invocationRequest.newCacheIdentifier != null) {
                        module._cache[invocationRequest.newCacheIdentifier] = module;
                    }

                    exports = module.exports;
                } else if (invocationRequest.moduleSourceType === ModuleSourceType.File) {
                    const resolvedPath = path.resolve(process.cwd(), invocationRequest.moduleSource);
                    let module = __non_webpack_require__(resolvedPath);

                    exports = module.exports;
                } else {
                    respondWithError(res, `Invalid module source type: ${invocationRequest.moduleSourceType}`);
                }
                if (exports == null) {
                    respondWithError(res, `The module ${invocationRequest.newCacheIdentifier == null ? invocationRequest.moduleSource : invocationRequest.newCacheIdentifier} 
                    has no exports. Ensure that the module assigns a function or an object containing functions to module.exports.`);
                }

                // Get function to invoke
                let functionToInvoke: Function;
                if (invocationRequest.exportName != null) {
                    functionToInvoke = exports[invocationRequest.exportName];
                    if (functionToInvoke == null) {
                        respondWithError(res, `The module ${invocationRequest.newCacheIdentifier == null ? invocationRequest.moduleSource : invocationRequest.newCacheIdentifier} 
                        has no export named ${invocationRequest.exportName}`);
                    }
                    if (!(typeof functionToInvoke === 'function')) {
                        respondWithError(res, `The export named ${invocationRequest.exportName} from module ${invocationRequest.newCacheIdentifier == null ? invocationRequest.moduleSource : invocationRequest.newCacheIdentifier} 
                        is not a function`);
                    }
                } else {
                    if (!(typeof exports === 'function')) {
                        respondWithError(res, `The module ${invocationRequest.newCacheIdentifier == null ? invocationRequest.moduleSource : invocationRequest.newCacheIdentifier} 
                        does not export a function`);
                    }
                    functionToInvoke = exports;
                }

                let callbackCalled = false;
                const callback = (error : Error | string, result: any) => {
                    if (callbackCalled) {
                        return;
                    }
                    callbackCalled = true;

                    if (error != null) {
                        respondWithError(res, error);
                    } else if (typeof result !== 'string') {
                        // Arbitrary object/number/etc - JSON-serialize it
                        let responseJson: string;
                        try {
                            responseJson = JSON.stringify(result);
                        } catch (err) {
                            // JSON serialization error - pass it back to .NET
                            respondWithError(res, err);
                            return;
                        }
                        res.setHeader('Content-Type', 'application/json');
                        res.end(responseJson);
                    } else {
                        // String - can bypass JSON-serialization altogether
                        res.setHeader('Content-Type', 'text/plain');
                        res.end(result);
                    }
                }

                // Invoke function 
                functionToInvoke.apply(null, invocationRequest.args.unshift(callback));
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

function respondWithError(res: http.ServerResponse, error: Error | string) {
    let errorIsString: boolean = typeof error === 'string';

    res.statusCode = 500;
    res.end(JSON.stringify({
        errorMessage: errorIsString ? error : (error as Error).message,
        errorDetails: errorIsString ? null : (error as Error).stack
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
