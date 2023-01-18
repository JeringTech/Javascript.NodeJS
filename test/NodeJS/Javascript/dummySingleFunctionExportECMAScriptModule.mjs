function dummyFunction(callback, x, y) {
    var result = x + y;
    callback(null, result);
}

export default dummyFunction;