function add(callback, x, y) {
    var result = x + y;
    callback(null, result);
}

function subtract(callback, x, y) {
    var result = x - y;
    callback(null, result);
}

export { add, subtract };