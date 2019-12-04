// Used by HttpNodeJSServiceIntegrationTests
var persistentString;
var persistentNumber = 0;

module.exports = {
    setString: (callback, value) => {
        persistentString = value;
        callback();
    },
    getString: (callback) => callback(null, { result: persistentString }),
    incrementNumber: (callback) => {
        persistentNumber++;
        callback();
    },
    getNumber: (callback) => callback(null, persistentNumber),
    incrementAndGetNumber: (callback) => {
        callback(null, ++persistentNumber);
    }
};
