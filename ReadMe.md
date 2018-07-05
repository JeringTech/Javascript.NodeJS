# JavascriptUtils.Node
This project is a work in progress. It is based on [Microsoft.AspNetCore.NodeServices](https://github.com/aspnet/JavaScriptServices/tree/master/src/Microsoft.AspNetCore.NodeServices).
I made a [pull request](https://github.com/aspnet/JavaScriptServices/pull/1669) for exposing Node.js options, unfortunately, it doesn't look like it will be accepted.
This project will have the following features:

- Allow for running "in memory" Javascript. In other words, instead of specifying a file, you can pass javascript as a string. 
- Allow for any Node.js option. This will enable debugging using `--inspect-brk`.
- Allow for no-timeout mode to facilitate debugging.
- Exposes easy to use static methods on top of DI support.

And some minor changes under the hood:
- Start the Http server using a string instead of a temp file to improve security.
- Avoid redundant allocations when dealing with JSON.
- Add ConfigureAwait(false) to Tasks to mitigate the chance of deadlock for consuming packages.
- Proper target for generating javascript bundle.
- Clean up DI architecture https://github.com/aspnet/Options/pull/219