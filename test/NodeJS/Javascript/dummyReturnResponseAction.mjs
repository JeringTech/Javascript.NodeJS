function responseActionWithHeader(callback, header, headerValue, result) {
    callback(null, result, (res) => {
        res.setHeader(header, headerValue);
        res.writeHead(200, { 'Content-Type': 'text/plain' });
        return false;
    });
}

function responseActionWithHeaderAndReturn(callback, header, headerValue, result) {
    callback(null, null, (res) => {
        res.setHeader(header, headerValue);
        res.writeHead(200, { 'Content-Type': 'text/plain' });
        res.end(result)
    });
}

export { responseActionWithHeader, responseActionWithHeaderAndReturn };