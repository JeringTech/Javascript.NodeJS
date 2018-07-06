// Limit dependencies to core Node modules. This means the code in this file has to be very low-level and unattractive,
// but simplifies things for the consumer of this module.
import './Util/PatchModuleResolutionLStat';
import './Util/OverrideStdOutputs';
import * as http from 'http';
import * as path from 'path';
//import { parseArgs } from './Util/ArgsUtil';
//import { exitWhenParentExits } from './Util/ExitWhenParentExits';

// Webpack doesn't support dynamic requires for files not present at compile time, so grab a direct
// reference to Node's runtime 'require' function.
const dynamicRequire: (name: string) => any = eval('require');

const server = http.createServer((request, response) => {
    // TODO move routing to its own file, rename this file to server and keep it simple
    let requestUrl = new URL(request.url);

    if (requestUrl.pathname === 'cache') { // /cache?source=string|stream|file
        // - cache from string/stream request
        //      - return 200/400
        // - cache from file request
        //      - return 200 / 400

        let source = requestUrl.searchParams.get('source');

        // TODO extract content of module
        if (source === 'string' || source === 'stream') {

        } else if (source === 'file') {

        }

        // TODO use module loader to load exports

    } else if (requestUrl.pathname === 'invoke') { // /invoke?source=string|cache|file
        // - invoke from file
        // - invoke from string
        // - invoke from cache
        let source = requestUrl.searchParams.get('source');
        if (source === 'cache') {

        } else if (source === 'string') {

        } else if (source === 'file') {

        }
    }

    // TODO if we get here, return bad request


    readRequestBodyAsJson(request, bodyJson => {
        let hasSentResult = false;
        const callback = (errorValue, successValue) => {
            if (!hasSentResult) {
                hasSentResult = true;
                if (errorValue) {
                    respondWithError(response, errorValue);
                } else if (typeof successValue !== 'string') {
                    // Arbitrary object/number/etc - JSON-serialize it
                    let successValueJson: string;
                    try {
                        successValueJson = JSON.stringify(successValue);
                    } catch (ex) {
                        // JSON serialization error - pass it back to .NET
                        respondWithError(response, ex);
                        return;
                    }
                    response.setHeader('Content-Type', 'application/json');
                    response.end(successValueJson);
                } else {
                    // String - can bypass JSON-serialization altogether
                    response.setHeader('Content-Type', 'text/plain');
                    response.end(successValue);
                }
            }
        };

        // Support streamed responses
        Object.defineProperty(callback, 'stream', {
            enumerable: true,
            get: function () {
                if (!hasSentResult) {
                    hasSentResult = true;
                    response.setHeader('Content-Type', 'application/octet-stream');
                }

                return response;
            }
        });

        try {
            const resolvedPath = path.resolve(process.cwd(), bodyJson.moduleName);
            const invokedModule = dynamicRequire(resolvedPath);
            const func = bodyJson.exportedFunctionName ? invokedModule[bodyJson.exportedFunctionName] : invokedModule;
            if (!func) {
                throw new Error('The module "' + resolvedPath + '" has no export named "' + bodyJson.exportedFunctionName + '"');
            }

            func.apply(null, [callback].concat(bodyJson.args));
        } catch (synchronousException) {
            callback(synchronousException, null);
        }
    });
});

const parsedArgs = parseArgs(process.argv);
const requestedPortOrZero = parsedArgs.port || 0; // 0 means 'let the OS decide'
server.listen(requestedPortOrZero, 'localhost', function () {
    // Signal to HttpNodeHost which loopback IP address (IPv4 or IPv6) and port it should make its HTTP connections on
    console.log('[Jering.JavascriptUtils.Node.HttpNodeHost:Listening on {' + server.address().address + '} port ' + server.address().port + '\]');

    // Signal to the NodeServices base class that we're ready to accept invocations
    console.log('[Jering.JavascriptUtils.Node:Listening]');
});

exitWhenParentExits(parseInt(parsedArgs.parentPid), /* ignoreSigint */ true);

function readRequestBodyAsJson(request, callback) {
    let requestBodyAsString = '';
    request.on('data', chunk => { requestBodyAsString += chunk; });
    request.on('end', () => { callback(JSON.parse(requestBodyAsString)); });
}

function respondWithError(res: http.ServerResponse, errorValue: any) {
    res.statusCode = 500;
    res.end(JSON.stringify({
        errorMessage: errorValue.message || errorValue,
        errorDetails: errorValue.stack || null
    }));
}
