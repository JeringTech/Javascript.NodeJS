// Minimal processor blocking logic
module.exports = (callback) => {

    // Block processor
    var end = new Date().getTime() + 100; // 100ms block
    while (new Date().getTime() < end) { /* do nothing */ }

    callback();
};
