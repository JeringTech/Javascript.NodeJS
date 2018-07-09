# JavascriptUtils.Node
This project is a work in progress. It is based on [Microsoft.AspNetCore.NodeServices](https://github.com/aspnet/JavaScriptServices/tree/master/src/Microsoft.AspNetCore.NodeServices).
This project will have the following additional features:

- [X] Allow for running "in memory" Javascript. In other words, instead of specifying a file, you can pass javascript as a string or stream. 

- [X] Allow for any Node.js option. This will enable debugging using `--inspect-brk`.

- [x] Allow for no-timeout mode to facilitate debugging.

- [ ] Expose convenience static methods on top of DI support.

And some changes under the hood:
- [X] Start the Http server using a string instead of a temp file to improve security.

- [ ] Avoid redundant allocations when dealing with JSON.

- [X] Add ConfigureAwait(false) to Tasks to mitigate the chance of deadlock for consuming packages.

- [ ] Proper target for generating javascript bundle.

- [ ] Clean up DI architecture https://github.com/aspnet/Options/pull/219

- [ ] Use pipes instead of HTTP for lower latency.