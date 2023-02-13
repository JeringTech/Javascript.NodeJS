function resActionWithHeader(callback, header, headerValue, result) {
    callback(null, result, (res) => {
        res.setHeader(header, headerValue);
        res.writeHead(200, { 'Content-Type': 'text/plain' });
        return false;
    });
}

function resActionWithHeaderAndReturn(callback, header, headerValue, result) {
    callback(null, null, (res) => {
        res.setHeader(header, headerValue);
        res.writeHead(200, { 'Content-Type': 'text/plain' });
        res.end(result)
    });
}

export { resActionWithHeader, resActionWithHeaderAndReturn };