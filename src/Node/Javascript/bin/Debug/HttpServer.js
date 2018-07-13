module.exports =
/******/ (function(modules) { // webpackBootstrap
/******/ 	// The module cache
/******/ 	var installedModules = {};
/******/
/******/ 	// The require function
/******/ 	function __webpack_require__(moduleId) {
/******/
/******/ 		// Check if module is in cache
/******/ 		if(installedModules[moduleId]) {
/******/ 			return installedModules[moduleId].exports;
/******/ 		}
/******/ 		// Create a new module (and put it into the cache)
/******/ 		var module = installedModules[moduleId] = {
/******/ 			i: moduleId,
/******/ 			l: false,
/******/ 			exports: {}
/******/ 		};
/******/
/******/ 		// Execute the module function
/******/ 		modules[moduleId].call(module.exports, module, module.exports, __webpack_require__);
/******/
/******/ 		// Flag the module as loaded
/******/ 		module.l = true;
/******/
/******/ 		// Return the exports of the module
/******/ 		return module.exports;
/******/ 	}
/******/
/******/
/******/ 	// expose the modules object (__webpack_modules__)
/******/ 	__webpack_require__.m = modules;
/******/
/******/ 	// expose the module cache
/******/ 	__webpack_require__.c = installedModules;
/******/
/******/ 	// define getter function for harmony exports
/******/ 	__webpack_require__.d = function(exports, name, getter) {
/******/ 		if(!__webpack_require__.o(exports, name)) {
/******/ 			Object.defineProperty(exports, name, { enumerable: true, get: getter });
/******/ 		}
/******/ 	};
/******/
/******/ 	// define __esModule on exports
/******/ 	__webpack_require__.r = function(exports) {
/******/ 		if(typeof Symbol !== 'undefined' && Symbol.toStringTag) {
/******/ 			Object.defineProperty(exports, Symbol.toStringTag, { value: 'Module' });
/******/ 		}
/******/ 		Object.defineProperty(exports, '__esModule', { value: true });
/******/ 	};
/******/
/******/ 	// create a fake namespace object
/******/ 	// mode & 1: value is a module id, require it
/******/ 	// mode & 2: merge all properties of value into the ns
/******/ 	// mode & 4: return value when already ns object
/******/ 	// mode & 8|1: behave like require
/******/ 	__webpack_require__.t = function(value, mode) {
/******/ 		if(mode & 1) value = __webpack_require__(value);
/******/ 		if(mode & 8) return value;
/******/ 		if((mode & 4) && typeof value === 'object' && value && value.__esModule) return value;
/******/ 		var ns = Object.create(null);
/******/ 		__webpack_require__.r(ns);
/******/ 		Object.defineProperty(ns, 'default', { enumerable: true, value: value });
/******/ 		if(mode & 2 && typeof value != 'string') for(var key in value) __webpack_require__.d(ns, key, function(key) { return value[key]; }.bind(null, key));
/******/ 		return ns;
/******/ 	};
/******/
/******/ 	// getDefaultExport function for compatibility with non-harmony modules
/******/ 	__webpack_require__.n = function(module) {
/******/ 		var getter = module && module.__esModule ?
/******/ 			function getDefault() { return module['default']; } :
/******/ 			function getModuleExports() { return module; };
/******/ 		__webpack_require__.d(getter, 'a', getter);
/******/ 		return getter;
/******/ 	};
/******/
/******/ 	// Object.prototype.hasOwnProperty.call
/******/ 	__webpack_require__.o = function(object, property) { return Object.prototype.hasOwnProperty.call(object, property); };
/******/
/******/ 	// __webpack_public_path__
/******/ 	__webpack_require__.p = "";
/******/
/******/
/******/ 	// Load entry module and return exports
/******/ 	return __webpack_require__(__webpack_require__.s = "./NodeServiceImplementations/OutOfProcess/Http/HttpServer.ts");
/******/ })
/************************************************************************/
/******/ ({

/***/ "./NodeInvocationData/ModuleSourceType.ts":
/*!************************************************!*\
  !*** ./NodeInvocationData/ModuleSourceType.ts ***!
  \************************************************/
/*! no static exports found */
/***/ (function(module, exports, __webpack_require__) {

"use strict";
eval("\r\nObject.defineProperty(exports, \"__esModule\", { value: true });\r\nvar ModuleSourceType;\r\n(function (ModuleSourceType) {\r\n    ModuleSourceType[ModuleSourceType[\"Cache\"] = 0] = \"Cache\";\r\n    ModuleSourceType[ModuleSourceType[\"File\"] = 1] = \"File\";\r\n    ModuleSourceType[ModuleSourceType[\"String\"] = 2] = \"String\";\r\n    ModuleSourceType[ModuleSourceType[\"Stream\"] = 3] = \"Stream\";\r\n})(ModuleSourceType || (ModuleSourceType = {}));\r\nexports.default = ModuleSourceType;\r\n\n\n//# sourceURL=webpack:///./NodeInvocationData/ModuleSourceType.ts?");

/***/ }),

/***/ "./NodeServiceImplementations/OutOfProcess/Http/HttpServer.ts":
/*!********************************************************************!*\
  !*** ./NodeServiceImplementations/OutOfProcess/Http/HttpServer.ts ***!
  \********************************************************************/
/*! no static exports found */
/***/ (function(module, exports, __webpack_require__) {

"use strict";
eval("\r\nObject.defineProperty(exports, \"__esModule\", { value: true });\r\n// The typings for module are incomplete and can't be augmented, so import as any.\r\nvar Module = __webpack_require__(/*! module */ \"module\");\r\nconst path = __webpack_require__(/*! path */ \"path\");\r\nconst http = __webpack_require__(/*! http */ \"http\");\r\nconst ModuleSourceType_1 = __webpack_require__(/*! ../../../NodeInvocationData/ModuleSourceType */ \"./NodeInvocationData/ModuleSourceType.ts\");\r\n// Parse arguments\r\nconst args = parseArgs(process.argv);\r\n// Start auto-termination loop\r\nexitWhenParentExits(parseInt(args.parentPid), true, 1000);\r\n// Start server\r\nconst server = http.createServer((req, res) => {\r\n    let bodyChunks = [];\r\n    req.\r\n        on('data', chunk => bodyChunks.push(chunk)).\r\n        on('end', () => {\r\n        debugger;\r\n        try {\r\n            // Create InvocationRequest\r\n            let body = Buffer.concat(bodyChunks).toString();\r\n            let invocationRequest;\r\n            if (req.headers[\"content-type\"] == 'application/json') {\r\n                invocationRequest = JSON.parse(body);\r\n            }\r\n            else if (req.headers[\"content-type\"] == 'multipart/mixed') {\r\n                let parts = body.split('--Jering.JavascriptUtils.Node');\r\n                invocationRequest = JSON.parse(parts[0]);\r\n                invocationRequest.moduleSource = parts[1];\r\n            }\r\n            // Get exports of module specified by InvocationRequest.moduleSource\r\n            let exports;\r\n            if (invocationRequest.moduleSourceType === ModuleSourceType_1.default.Cache) {\r\n                var cachedModule = Module._cache[invocationRequest.moduleSource];\r\n                // Cache miss\r\n                if (cachedModule == null) {\r\n                    res.statusCode = 404;\r\n                    res.end();\r\n                }\r\n                exports = cachedModule.exports;\r\n            }\r\n            else if (invocationRequest.moduleSourceType === ModuleSourceType_1.default.Stream ||\r\n                invocationRequest.moduleSourceType === ModuleSourceType_1.default.String) {\r\n                let module = new Module(null, null);\r\n                module._compile(invocationRequest.moduleSource, \"anonymous\");\r\n                if (invocationRequest.newCacheIdentifier != null) {\r\n                    module._cache[invocationRequest.newCacheIdentifier] = module;\r\n                }\r\n                exports = module.exports;\r\n            }\r\n            else if (invocationRequest.moduleSourceType === ModuleSourceType_1.default.File) {\r\n                const resolvedPath = path.resolve(process.cwd(), invocationRequest.moduleSource);\r\n                let module = require(resolvedPath);\r\n                exports = module.exports;\r\n            }\r\n            else {\r\n                respondWithError(res, `Invalid module source type: ${invocationRequest.moduleSourceType}`);\r\n            }\r\n            if (exports == null) {\r\n                respondWithError(res, `The module ${invocationRequest.newCacheIdentifier == null ? invocationRequest.moduleSource : invocationRequest.newCacheIdentifier} \r\n                    has no exports. Ensure that the module assigns a function or an object containing functions to module.exports.`);\r\n            }\r\n            // Get function to invoke\r\n            let functionToInvoke;\r\n            if (invocationRequest.exportName != null) {\r\n                functionToInvoke = exports[invocationRequest.exportName];\r\n                if (functionToInvoke == null) {\r\n                    respondWithError(res, `The module ${invocationRequest.newCacheIdentifier == null ? invocationRequest.moduleSource : invocationRequest.newCacheIdentifier} \r\n                        has no export named ${invocationRequest.exportName}`);\r\n                }\r\n                if (!(typeof functionToInvoke === 'function')) {\r\n                    respondWithError(res, `The export named ${invocationRequest.exportName} from module ${invocationRequest.newCacheIdentifier == null ? invocationRequest.moduleSource : invocationRequest.newCacheIdentifier} \r\n                        is not a function`);\r\n                }\r\n            }\r\n            else {\r\n                if (!(typeof exports === 'function')) {\r\n                    respondWithError(res, `The module ${invocationRequest.newCacheIdentifier == null ? invocationRequest.moduleSource : invocationRequest.newCacheIdentifier} \r\n                        does not export a function`);\r\n                }\r\n                functionToInvoke = exports;\r\n            }\r\n            let callbackCalled = false;\r\n            const callback = (error, result) => {\r\n                if (callbackCalled) {\r\n                    return;\r\n                }\r\n                callbackCalled = true;\r\n                if (error != null) {\r\n                    respondWithError(res, error);\r\n                }\r\n                else if (typeof result !== 'string') {\r\n                    // Arbitrary object/number/etc - JSON-serialize it\r\n                    let responseJson;\r\n                    try {\r\n                        responseJson = JSON.stringify(result);\r\n                    }\r\n                    catch (err) {\r\n                        // JSON serialization error - pass it back to .NET\r\n                        respondWithError(res, err);\r\n                        return;\r\n                    }\r\n                    res.setHeader('Content-Type', 'application/json');\r\n                    res.end(responseJson);\r\n                }\r\n                else {\r\n                    // String - can bypass JSON-serialization altogether\r\n                    res.setHeader('Content-Type', 'text/plain');\r\n                    res.end(result);\r\n                }\r\n            };\r\n            // Invoke function \r\n            functionToInvoke.apply(null, invocationRequest.args.unshift(callback));\r\n        }\r\n        catch (synchronousError) {\r\n            respondWithError(res, synchronousError);\r\n        }\r\n    });\r\n}).listen(parseInt(args.port), 'localhost', function () {\r\n    // Signal to HttpNodeHost which loopback IP address (IPv4 or IPv6) and port it should make its HTTP connections on\r\n    // and that we are ready to process invocations.\r\n    let info = server.address();\r\n    console.log(`[Jering.JavascriptUtils.Node: Listening on IP - ${info.address} Port - ${info.port}]`);\r\n});\r\nfunction respondWithError(res, error) {\r\n    let errorIsString = typeof error === 'string';\r\n    res.statusCode = 500;\r\n    res.end(JSON.stringify({\r\n        errorMessage: errorIsString ? error : error.message,\r\n        errorDetails: errorIsString ? null : error.stack\r\n    }));\r\n}\r\n// https://github.com/aspnet/JavaScriptServices/blob/0dc570a0c8725e3031ce5a884d7df3cfb75545ba/src/Microsoft.AspNetCore.NodeServices/TypeScript/Util/ArgsUtil.ts\r\nfunction parseArgs(args) {\r\n    let currentKey = null;\r\n    const result = {};\r\n    args.forEach(arg => {\r\n        if (arg.indexOf('--') === 0) {\r\n            const argName = arg.substring(2);\r\n            result[argName] = undefined;\r\n            currentKey = argName;\r\n        }\r\n        else if (currentKey !== null) {\r\n            result[currentKey] = arg;\r\n            currentKey = null;\r\n        }\r\n    });\r\n    return result;\r\n}\r\n// https://github.com/aspnet/JavaScriptServices/blob/0dc570a0c8725e3031ce5a884d7df3cfb75545ba/src/Microsoft.AspNetCore.NodeServices/TypeScript/Util/ExitWhenParentExits.ts\r\nfunction exitWhenParentExits(parentPid, ignoreSigint, pollIntervalMS) {\r\n    setInterval(() => {\r\n        if (!processExists(parentPid)) {\r\n            process.exit();\r\n        }\r\n    }, pollIntervalMS);\r\n    if (ignoreSigint) {\r\n        process.on('SIGINT', () => {\r\n            console.log('Received SIGINT. Waiting for .NET process to exit...');\r\n        });\r\n    }\r\n}\r\n// https://github.com/aspnet/JavaScriptServices/blob/0dc570a0c8725e3031ce5a884d7df3cfb75545ba/src/Microsoft.AspNetCore.NodeServices/TypeScript/Util/ExitWhenParentExits.ts\r\nfunction processExists(pid) {\r\n    try {\r\n        process.kill(pid, 0);\r\n        return true;\r\n    }\r\n    catch (ex) {\r\n        if (ex.code === 'EPERM') {\r\n            throw new Error(`Attempted to check whether process ${pid} was running, but got a permissions error.`);\r\n        }\r\n        return false;\r\n    }\r\n}\r\n\n\n//# sourceURL=webpack:///./NodeServiceImplementations/OutOfProcess/Http/HttpServer.ts?");

/***/ }),

/***/ "http":
/*!***********************!*\
  !*** external "http" ***!
  \***********************/
/*! no static exports found */
/***/ (function(module, exports) {

eval("module.exports = require(\"http\");\n\n//# sourceURL=webpack:///external_%22http%22?");

/***/ }),

/***/ "module":
/*!*************************!*\
  !*** external "module" ***!
  \*************************/
/*! no static exports found */
/***/ (function(module, exports) {

eval("module.exports = require(\"module\");\n\n//# sourceURL=webpack:///external_%22module%22?");

/***/ }),

/***/ "path":
/*!***********************!*\
  !*** external "path" ***!
  \***********************/
/*! no static exports found */
/***/ (function(module, exports) {

eval("module.exports = require(\"path\");\n\n//# sourceURL=webpack:///external_%22path%22?");

/***/ })

/******/ });