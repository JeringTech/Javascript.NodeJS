var Module = require('module');
var path = require('path');

function loadModuleFromString(code, identifier, parent) {
    var cachedExports = tryGetCachedExports(identifier);
    if (cachedExports) {
        return cachedExports;
    }

    var module = new Module(identifier, parent);

    Module._cache[identifier] = module;
    module.identifier = identifier;
    module.paths = Module._nodeModulePaths(path.dirname(identifier));
    module._compile(code, identifier)

    return module.exports;
}

function tryGetCachedExports(identifier) {
    var cachedModule = Module._cache[identifier];
    if (cachedModule) {
        return cachedModule.exports;
    }
}