import * as http from 'http';
import { AddressInfo } from 'net';

const server = http.createServer((request, response) => {
    console.log(request);
});

const requestedPortOrZero = 0;// args.port || 0; // 0 means 'let the OS decide'
server.listen(requestedPortOrZero, 'localhost', function () {
    // Signal to HttpNodeHost which loopback IP address (IPv4 or IPv6) and port it should make its HTTP connections on
    // and that we are ready to process invocations.
    let info = server.address() as AddressInfo;
    console.log(`[Jering.JavascriptUtils.Node: Listening on IP:${info.address} Port:${info.port}]`);
});