# Jering.Javascript.NodeJS
[![Build Status](https://dev.azure.com/JeringTech/Javascript.NodeJS/_apis/build/status/Jering.Javascript.NodeJS-CI?branchName=master)](https://dev.azure.com/JeringTech/Javascript.NodeJS/_build/latest?definitionId=1?branchName=master)
[![codecov](https://codecov.io/gh/JeringTech/Javascript.NodeJS/branch/master/graph/badge.svg)](https://codecov.io/gh/JeringTech/Javascript.NodeJS)
[![License](https://img.shields.io/badge/license-Apache%202.0-blue.svg)](https://github.com/Pkcs11Interop/Pkcs11Interop/blob/master/LICENSE.md)
[![NuGet](https://img.shields.io/nuget/vpre/Jering.Javascript.NodeJS.svg?label=nuget)](https://www.nuget.org/packages/Jering.Javascript.NodeJS/)

## Table of Contents
[Overview](#overview)  
[Target Frameworks](#target-frameworks)  
[Prerequisites](#prerequisites)  
[Installation](#installation)  
[Usage](#usage)  
[API](#api)  
[Extensibility](#extensibility)  
[Performance](#performance)  
[Building and Testing](#building-and-testing)  
[Projects Using this Library](#projects-using-this-library)  
[Related Concepts](#related-concepts)  
[Contributing](#contributing)  
[About](#about)  

## Overview
Jering.Javascript.NodeJS enables you to invoke javascript in [NodeJS](https://nodejs.org/en/), from C#. With this ability, you can use javascript libraries and scripts from your C# projects.  

> You can use this library as a replacement for the recently obsoleted [Microsoft.AspNetCore.NodeServices](https://github.com/aspnet/JavaScriptServices/tree/master/src/Microsoft.AspNetCore.NodeServices).
[`InvokeFromFileAsync<T>`](#inodejsserviceinvokefromfileasync) replaces `INodeService`'s `InvokeAsync<T>` and `InvokeExportAsync<T>`.

This library is flexible; you can use a dependency injection (DI) based API or a static API, also, you can invoke both in-memory and on-disk javascript. 

Static API example:

```csharp
string javascriptModule = @"
module.exports = (callback, x, y) => {  // Module must export a function that takes a callback as its first parameter
    var result = x + y; // Your javascript logic
    callback(null /* If an error occurred, provide an error object or message */, result); // Call the callback when you're done.
}";

// Invoke javascript
int result = await StaticNodeJSService.InvokeFromStringAsync<int>(javascriptModule, args: new object[] { 3, 5 });

// result == 8
Assert.Equal(8, result);
```

DI based API example:

```csharp
string javascriptModule = @"
module.exports = (callback, x, y) => {  // Module must export a function that takes a callback as its first parameter
    var result = x + y; // Your javascript logic
    callback(null /* If an error occurred, provide an error object or message */, result); // Call the callback when you're done.
}";

// Create an INodeJSService
var services = new ServiceCollection();
services.AddNodeJS();
ServiceProvider serviceProvider = services.BuildServiceProvider();
INodeJSService nodeJSService = serviceProvider.GetRequiredService<INodeJSService>();

// Invoke javascript
int result = await nodeJSService.InvokeFromStringAsync<int>(javascriptModule, args: new object[] { 3, 5 });

// result == 8
Assert.Equal(8, result);
```

## Target Frameworks
- .NET Standard 2.0
- .NET Framework 4.6.1
 
## Prerequisites
You'll need to install [NodeJS](https://nodejs.org/en/) and add node.exe's directory to the `Path` environment variable (automatically done by the official installer). We've tested this library with
NodeJS 10.5.2 - 12.13.0.

## Installation
Using Package Manager:
```
PM> Install-Package Jering.Javascript.NodeJS
```
Using .Net CLI:
```
> dotnet add package Jering.Javascript.NodeJS
```

## Usage
### Creating INodeJSService
This library provides a DI based API to facilitate [extensibility](#extensibility) and testability.
You can use any DI framework that has adapters for [Microsoft.Extensions.DependencyInjection](https://github.com/aspnet/DependencyInjection).
Here, we'll use vanilla Microsoft.Extensions.DependencyInjection:
```csharp
var services = new ServiceCollection();
services.AddNodeJS();
ServiceProvider serviceProvider = services.BuildServiceProvider(); 
INodeJSService nodeJSService = serviceProvider.GetRequiredService<INodeJSService>();
```
The default implementation of `INodeJSService` is `HttpNodeJSService`, which manages a NodeJS process that it sends javascript invocations to via HTTP.
`INodeJSService` is a singleton service and `INodeJSService`'s members are thread safe.
Where possible, inject `INodeJSService` into your types or share an `INodeJSService`. 
This avoids the overhead of killing and creating NodeJS processes repeatedly.

When you're done, you can dispose of an `INodeJSService` by calling
```csharp
nodeJSService.Dispose();
```
or 
```csharp
serviceProvider.Dispose(); // Calls Dispose on objects it has instantiated that are disposable
```
Disposing of an `INodeJSService` kills its associated NodeJS process.
Note that even if `Dispose` isn't called, the NodeJS process is killed when the application shuts down - if the application shuts down gracefully.
If the application doesn't shutdown gracefully, the NodeJS process will kill itself when it detects that its parent has been killed.
Essentially, manually disposing of `INodeJSService`s isn't mandatory.

#### Static API
This library provides a static API as an alternative. The `StaticNodeJSService` type wraps an `INodeJSService`, exposing most of its [public members](#api).
Whether you use the static API or the DI based API depends on your development needs. If you're already using DI, if you want to mock 
out javascript invocations in your tests or if you want to [overwrite](#extensibility) services, use the DI based API. Otherwise,
use the static API. Example usage:

```csharp
string result = await StaticNodeJSService
    .InvokeFromStringAsync<Result>("module.exports = (callback, message) => callback(null, message);", args: new[] { "success" });

Assert.Equal("success", result);
```

### Using INodeJSService
#### Basics
To invoke javascript, you'll need a [NodeJS module](#nodejs-modules) that exports either a function or an object containing functions. Exported functions can be of two forms:

##### Function With Callback Parameter
These functions take a callback as their first argument, and call the callback when they're done.  

The callback takes two optional arguments:
- The first argument is an error or an error message. It must be of type [`Error`](https://nodejs.org/api/errors.html#errors_class_error) or `string`.
- The second argument is the result. It must be a JSON-serializable type, a `string`, or a [`stream.Readable`](https://nodejs.org/api/stream.html#stream_class_stream_readable). 
 
This is known as an [error-first callback](https://nodejs.org/api/errors.html#errors_error_first_callbacks).
Such callbacks are commonly used for [error handling](https://nodejs.org/api/errors.html#errors_error_propagation_and_interception) in NodeJS asynchronous code (check out [NodeJS Event Loop](https://nodejs.org/en/docs/guides/event-loop-timers-and-nexttick/)
for more information on asynchrony in NodeJS).

This is a module that exports a valid function:
```javascript
module.exports = (callback, arg1, arg2, arg3) => {
    ... // Do something with args

    callback(null, result);
}
```

This is a module that exports an object containing valid functions:
```javascript
module.exports = {
    doSomething: (callback, arg1) => {
        ... // Do something with arg

        callback(null, result);
    },
    doSomethingElse: (callback) => {
        ... // Do something else

        callback(null, result);
    }
}
```

##### Async Function
Async functions are syntactic sugar for [functions with callback parameters](#function-with-callback-parameter) (check out 
[Callbacks, Promises and Async/Await](https://medium.com/front-end-weekly/callbacks-promises-and-async-await-ad4756e01d90) for a summary on how callbacks, promises and async/await are related).

This is a module that exports a valid function:
```javascript
module.exports = async (arg1, arg2) => {
    ... // Do something with args

    return result;
}
```

And this is a module that exports an object containing valid functions:
```javascript
module.exports = {
    doSomething: async (arg1, arg2, arg3, arg4) => {
        ... // Do something with args

        // async functions can explicitly return promises
        return new Promise((resolve, reject) => {
            resolve(result);
        });
    },
    doSomethingElse: async (arg1) => {
        ... // Do something with arg
            
        return result;
    }
}
```

If an error is thrown in an async function, the error message is sent back to the calling .Net process, where an `InvocationException` is thrown:
```javascript
module.exports = async () => {
    throw new Error('error message');
}
```

#### Invoking Javascript From a File
If you have a javascript file named `exampleModule.js` (located in [`NodeJSProcessOptions.ProjectPath`](#nodejsprocessoptions)):
```javascript
module.exports = (callback, message) => callback(null, { resultMessage: message });
```
And a .Net class `Result`:
```csharp
public class Result
{
    public string Message { get; set; }
}
```
You can invoke the javascript using [`InvokeFromFileAsync<T>`](#inodejsserviceinvokefromfileasync):
```csharp
Result result = await nodeJSService.InvokeFromFileAsync<Result>("exampleModule.js", args: new[] { "success" });

Assert.Equal("success", result.Message);
```
If you change `exampleModule.js` to export an object containing functions:
```javascript
module.exports = {
    appendExclamationMark: (callback, message) => callback(null, { resultMessage: message + '!' }),
    appendFullStop: (callback, message) => callback(null, { resultMessage: message + '.' })
}
```
You can invoke a specific function by providing an export's name:
```csharp
Result result = await nodeJSService.InvokeFromFileAsync<Result>("exampleModule.js", "appendExclamationMark", args: new[] { "success" });

Assert.Equal("success!", result.Message);
```
When using `InvokeFromFileAsync`, NodeJS always caches the module using the `.js` file's absolute path as cache identifier. This is great for
performance, since the file will not be reread or recompiled on subsequent invocations.

#### Invoking Javascript in String Form
You can invoke javascript in string form using [`InvokeFromStringAsync<T>`](#inodejsserviceinvokefromstringasync):
```csharp
string module = "module.exports = (callback, message) => callback(null, { resultMessage: message });";

// Invoke javascript
Result result = await nodeJSService.InvokeFromStringAsync<Result>(module, args: new[] { "success" });

Assert.Equal("success", result.Message);
```

In the above example, the module string is sent to NodeJS and recompiled on every invocation. If you're going to invoke a module repeatedly, 
to avoid resending and recompiling, you'll want to have NodeJS cache the module.
To do this, you must specify a custom cache identifier, since unlike a file, a string has no "absolute file path" for NodeJS to use as cache identifier.
Once NodeJS has cached the module, invoke directly from the NodeJS cache: 

```csharp
string cacheIdentifier = "exampleModule";

// Try to invoke from the NodeJS cache
(bool success, Result result) = await nodeJSService.TryInvokeFromCacheAsync<Result>(cacheIdentifier, args: new[] { "success" });

// If the module hasn't been cached, cache it. If the NodeJS process dies and restarts, the cache will be invalidated, so always check whether success is false.
if(!success)
{
    // This is a trivialized example. In practice, to avoid holding large module strings in memory, you might retrieve the module 
    // string from an on-disk or remote source, like a file.
    string moduleString = "module.exports = (callback, message) => callback(null, { resultMessage: message });"; 

    // Send the module string to NodeJS where it's compiled, invoked and cached.
    result = await nodeJSService.InvokeFromStringAsync<Result>(moduleString, cacheIdentifier, args: new[] { "success" });
}

Assert.Equal("success", result.ResultMessage);
```

We recommend using the following [`InvokeFromStringAsync<T>`](#inodejsserviceinvokefromstringasync) overload to perform the above example's operations.
The above example is really there to explain what this overload does.
If you've enabled [concurrency](#concurrency), [you must use this overload](#concurrency):

```csharp
string module = "module.exports = (callback, message) => callback(null, { resultMessage: message });";
string cacheIdentifier = "exampleModule";

// This is a trivialized example. In practice, to avoid holding large module strings in memory, you might retrieve the module 
// string from an on-disk or remote source, like a file.
Func<string> moduleFactory = () => module;

// Initially, sends only cacheIdentifier to NodeJS, in an attempt to invoke from the NodeJS cache. If the module hasn't been cached, creates the module string using moduleFactory and
// sends it to NodeJS where it's compiled, invoked and cached. 
Result result = await nodeJSService.InvokeFromStringAsync<Result>(moduleFactory, cacheIdentifier, args: new[] { "success" });

Assert.Equal("success", result.Message);
```

Like when [invoking javascript form a file](#invoking-javascript-from-a-file), if the module exports an object containing functions, you can invoke a specific function by specifying
its name.  

#### Invoking Javascript in Stream Form
You can invoke javascript in stream form using [`InvokeFromStreamAsync<T>`](#inodejsserviceinvokefromstreamasync) :
```csharp
// Write the module to a MemoryStream for demonstration purposes.
streamWriter.Write("module.exports = (callback, message) => callback(null, {resultMessage: message});");
streamWriter.Flush();
memoryStream.Position = 0;

Result result = await nodeJSService.InvokeFromStreamAsync<Result>(memoryStream, args: new[] { "success" });
    
Assert.Equal("success", result.Message);
```

`InvokeFromStreamAsync` behaves in a similar manner to `InvokeFromStringAsync`, refer to [Invoking Javascript in String Form](#invoking-javascript-in-string-form) for details on caching and more. 
This method provides a way to avoid allocating a string if the source of the module is a stream. Avoiding `string` allocations can improve performance.

### Configuring INodeJSService
This library uses the [ASP.NET Core options pattern](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options?view=aspnetcore-2.1). While developed for ASP.NET Core,
this pattern can be used by other types of applications. The NodeJS process and the service that manages the process are both configurable, for example:

 ```csharp
var services = new ServiceCollection();
services.AddNodeJS();

// Options for the NodeJSProcess, here we enable debugging
services.Configure<NodeJSProcessOptions>(options => options.NodeAndV8Options = "--inspect-brk");

// Options for the service that manages the process, here we make its timeout infinite
services.Configure<OutOfProcessNodeJSServiceOptions>(options => options.TimeoutMS = -1);

ServiceProvider serviceProvider = services.BuildServiceProvider();
INodeJSService nodeJSService = serviceProvider.GetRequiredService<INodeJSService>();
```

#### Configuring Using the Static API
The static API exposes a method for configuring options:
```csharp
StaticNodeJSService.Configure<OutOfProcessNodeJSServiceOptions>(options => options.TimeoutMS = -1);
```
Configurations made using `StaticNodeJSService.Configure<T>` only apply to javascript invocations made using the static API. 
Ideally, such configurations should be done before the first javascript invocation.
Any existing NodeJS process is killed and a new one is created in the first javascript invocation after every `StaticNodeJSService.Configure<T>` call. 
Re-creating the NodeJS process is resource intensive. Also, if you're using the static API from multiple threads and 
the NodeJS process is performing invocations for other threads, you might get unexpected results.

The next two sections list all available options.

#### NodeJSProcessOptions
| Option | Type | Description | Default |  
| ------ | ---- | ----------- | ------- |
| ProjectPath | `string` | The base path for resolving paths of NodeJS modules on disk. If this value is `null`, whitespace or an empty string and the application is an ASP.NET Core application, project path is `IHostingEnvironment.ContentRootPath` | The current directory (value returned by `Directory.GetCurrentDirectory()`) |
| NodeAndV8Options | `string` | NodeJS and V8 options in the form "[NodeJS options] [V8 options]". The full list of NodeJS options can be found here: https://nodejs.org/api/cli.html#cli_options. | `null` |
| Port | `int` | The port that the server running on NodeJS will listen on. If set to 0, the OS will choose the port. | `0` |
| EnvironmentVariables | `IDictionary<string, string>` | The environment variables for the NodeJS process. The full list of NodeJS environment variables can be found here: https://nodejs.org/api/cli.html#cli_environment_variables. If this value doesn't contain an element with key "NODE_ENV" and the application is an ASP.NET Core application, an element with key "NODE_ENV" is added with value "development" if `IHostingEnvironment.EnvironmentName` is `EnvironmentName.Development` or "production" otherwise. | An Empty `IDictionary<string, string>`  |

#### OutOfProcessNodeJSServiceOptions
| Option | Type | Description | Default |  
| ------ | ---- | ----------- | ------- |
| TimeoutMS | `int` | The maximum duration to wait for the NodeJS process to connect and to wait for responses to invocations. If this value is negative, the maximum duration is infinite. | `60000` |
| NumRetries | `int` | The number of times an invocation is retried. If set to a negative value, invocations are retried indefinitely. If the module source of an invocation is an unseekable stream, the invocation isn't retried. If you require retries for such streams, copy their contents to a `MemoryStream`.| `1` |
| Concurrency | `Concurrency` | The concurrency mode for invocations.<br><br>By default, this value is `Concurrency.None` and invocations are executed synchronously by a single NodeJS process; mode pros: lower memory overhead and supports all modules, cons: less performant.<br><br>If this value is `Concurrency.MultiProcess`, `ConcurrencyDegree` NodeJS processes are created and invocations are distributed among them using round-robin load balancing; mode pros: more performant, cons: higher memory overhead and doesn't work with modules that have persistent state. | `Concurrency.None` |
| ConcurrencyDegree | `int` | The concurrency degree. If `Concurrency` is `Concurrency.MultiProcess`, this value is the number of NodeJS processes. If this value is less than or equal to 0, concurrency degree is the number of logical processors the current machine has. This value does nothing if `Concurrency` is `Concurrency.None`. | `0` |
| EnableFileWatching | `bool` | The value specifying whether file watching is enabled. If file watching is enabled, when a file in `WatchPath` with name matching a pattern in `WatchFileNamePatterns` changes, NodeJS is restarted. | `false` |
| WatchPath | `string` | The path of the directory to watch for file changes. If this value is `null`, the path `NodeJSProcessOptions.ProjectPath` is watched. This value does nothing if `EnableFileWatching` is `false`. | `null` |
| WatchSubdirectories | `bool` | The value specifying whether to watch subdirectories of `WatchPath`. This value does nothing if `EnableFileWatching` is `false`. | `true` |
| WatchFileNamePatterns | `IEnumerable<string>` | The file name patterns to watch. In a pattern, "*" represents 0 or more of any character and "?" represents 0 or 1 of any character. For example, "TestFile1.js" matches the pattern "*File?.js". This value does nothing if `EnableFileWatching` is `false`. | `["*.js", "*.jsx", "*.ts", "*.tsx", "*.json", "*.html"]` |
| WatchGracefulShutdown | `bool` | The value specifying whether NodeJS processes shutdown gracefully when a file changes. If this value is true, NodeJS processes shutdown gracefully. Otherwise they're killed immediately. This value does nothing if `EnableFileWatching` is `false`.<br><br>What's a graceful shutdown? When a file changes, a new NodeJS process is created and subsequent invocations are sent to it. The old NodeJS process might still be handling earlier invocations. If graceful shutdown is enabled, the old NodeJS process is killed after its invocations complete. If graceful shutdown is disabled, the old NodeJS process is killed immediately and invocations are retried in the new NodeJS process if retries remain (see `NumRetries`).<br><br>Should I use graceful shutdown? Shutting down gracefully is safer: chances of an invocation exhausting retries and failing is lower, also, you won't face issues from an invocation terminating midway. However, graceful shutdown does incur a tiny performance cost and invocations complete using the outdated version of your script. Weigh these factors for your script and use-case to decide whether to use graceful shutdown. | `true` |

### Debugging Javascript
These are the steps for debugging javascript invoked using INodeJSService:
1. Create an INodeJSService using the example options in the previous section (`NodeJSProcessOptions.NodeAndV8Options` = `--inspect-brk` and `OutOfProcessNodeJSServiceOptions.TimeoutMS` = `-1`).
2. Add [`debugger`](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Statements/debugger) statements to your javascript module.
3. Call a [javascript invoking method](#api). 
4. Navigate to `chrome://inspect/` in Chrome.
5. Click "Open dedicated DevTools for Node".
6. Click continue to advance to your `debugger` statements.

### Advanced Usage
#### Concurrency
To enable concurrency, set `OutOfProcessNodeJSServiceOptions.Concurrency` to `Concurrency.MultiProcess`:

```csharp
services.Configure<OutOfProcessNodeJSServiceOptions>(options => {
    options.Concurrency = Concurrency.MultiProcess; // Concurrency.None by default
    options.ConcurrencyDegree = 8; // Number of processes. Defaults to the number of logical processors on your machine.
);
```
(see [Configuring INodeJSService](#configuring-inodejsservice) for more information on configuring)  

All invocations will be distributed among multiple NodeJS processes using round-robin load balancing. 

##### Why Bother?
Enabling concurrency significantly speeds up CPU-bound workloads. For example, consider the following benchmarks:

<table>
<thead>
<tr><th>Method</th><th>Mean</th><th>Error</th><th>StdDev</th><th>Gen 0</th><th>Gen 1</th><th>Gen 2</th><th>Allocated</th></tr>
</thead>
<tbody>
<tr><td>INodeJSService_Concurrency_MultiProcess</td><td>400.3 ms</td><td>0.62 ms</td><td>0.58 ms</td><td>-</td><td>-</td><td>-</td><td>134.95 KB</td>
</tr><tr><td>INodeJSService_Concurrency_None</td><td>2,500.2 ms</td><td>0.51 ms</td><td>0.48 ms</td><td>-</td><td>-</td><td>-</td><td>135.13 KB</td>
</tr><tr><td>INodeServices_Concurrency</td><td>2,500.2 ms</td><td>0.49 ms</td><td>0.46 ms</td><td>-</td><td>-</td><td>-</td><td>246.98 KB</td>
</tr></tbody>
</table>

```
BenchmarkDotNet=v0.12.0, OS=Windows 10.0.18362
Intel Core i7-7700 CPU 3.60GHz (Kaby Lake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=3.0.100
  [Host]     : .NET Core 3.0.0 (CoreCLR 4.700.19.46205, CoreFX 4.700.19.46214), X64 RyuJIT
  DefaultJob : .NET Core 3.0.0 (CoreCLR 4.700.19.46205, CoreFX 4.700.19.46214), X64 RyuJIT
```

These benchmarks invoke javascript asynchronously, as most applications would (view complete source [here](https://github.com/JeringTech/Javascript.NodeJS/blob/master/perf/NodeJS/ConcurrencyBenchmarks.cs)):

```csharp
const int numTasks = 25;
var results = new Task<string>[numTasks];
for (int i = 0; i < numTasks; i++)
{
    results[i] = _nodeJSService.InvokeFromFileAsync<string>(DUMMY_CONCURRENCY_MODULE);
}

return await Task.WhenAll(results);
```

Where the `DUMMY_CONCURRENCY_MODULE` file contains:

```js
// Minimal processor blocking logic
module.exports = (callback) => {

    // Block processor
    var end = new Date().getTime() + 100; // 100ms block
    while (new Date().getTime() < end) { /* do nothing */ }

    callback(null);
};
```

For `INodeJSService` with `Concurrency.MultiProcessing`, multiple NodeJS processes perform invocations concurrently, so the benchmark takes ~400ms ((25 tasks x 100ms) / number-of-logical-processors + overhead-from-unrelated-processes).  

In the other two benchmarks, a single NodeJS process performs invocations synchronously, so those benchmarks take ~2500ms (25 tasks x 100ms).  

##### Limitations
1. You can't use concurrency if you persist data between invocations. For example, with concurrency enabled:

    ```csharp
    const string javascriptModule = @"
    var lastResult;

    module.exports = (callback, x) => {

        var result = x + (lastResult ? lastResult : 0); // Use persisted value here
        lastResult = result; // Persist
  
        callback(null, result);
    }";

    // result == 3
    int result = await StaticNodeJSService.InvokeFromStringAsync<int>(javascriptModule, "customIdentifier", args: new object[] { 3 });

    // expected 8, but result == 5 since different processes perform the invocations
    result = await StaticNodeJSService.InvokeFromStringAsync<int>(javascriptModule, "customIdentifier", args: new object[] { 5 });
    ```

    This should not be a problem in most cases.

2. Higher memory overhead. This isn't typically an issue - a standard workstation can host dozens of NodeJS processes, and in cloud scenarios you'll typically have memory proportional to 
  the number of logical processors.

3. Concurrency may not speed up workloads with lots of asynchronous operations. For example if your workload spends lots of time waiting on a databases, 
  more NodeJS processes will not speed things up significantly.
4. With concurrency enabled, you can't use the following [pattern](#invoking-javascript-in-string-form) to invoke from NodeJS's cache:

    ```csharp
    string cacheIdentifier = "exampleModule";

    // If you have an even number of NodeJS processes, success will always be false since the resulting caching attempt is
    // sent to the next NodeJS process.
    (bool success, Result result) = await nodeJSService.TryInvokeFromCacheAsync<Result>(cacheIdentifier, args: new[] { "success" });

    // False, so we attempt to cache
    if(!success)
    {
        string moduleString = "module.exports = (callback, message) => callback(null, { resultMessage: message });"; 

        // Because of round-robin load balancing, this caching attempt is sent to the next NodeJS process.
        result = await nodeJSService.InvokeFromStringAsync<Result>(moduleString, cacheIdentifier, args: new[] { "success" });
    }

    Assert.Equal("success", result.ResultMessage);
    ```

    Instead, call an overload that atomically handles caching and invoking:

    ```csharp
    string module = "module.exports = (callback, message) => callback(null, { resultMessage: message });";
    string cacheIdentifier = "exampleModule";

    // This is a trivialized example. In practice, to avoid holding large module strings in memory, you might retrieve the module 
    // string from an on-disk or remote source, like a file.
    Func<string> moduleFactory = () => module;

    // Initially, sends only cacheIdentifier to NodeJS, in an attempt to invoke from the NodeJS cache. If the module hasn't been cached, creates the module string using moduleFactory and
    // sends it to NodeJS where it's compiled, invoked and cached. 
    Result result = await nodeJSService.InvokeFromStringAsync<Result>(moduleFactory, cacheIdentifier, args: new[] { "success" });

    Assert.Equal("success", result.Message);
    ```
## API
### INodeJSService.InvokeFromFileAsync
#### Signature
```csharp
Task<T> InvokeFromFileAsync<T>(string modulePath, string exportName = null, object[] args = null, CancellationToken cancellationToken = default(CancellationToken));
```
#### Description
Invokes a function exported by a NodeJS module on disk.
#### Parameters
- `T`
  - Description: The type of object this method will return. It can be a JSON-serializable type, `string`, or `Stream`.
 
- `modulePath`
  - Type: `string`
  - Description: The path to the NodeJS module (i.e., JavaScript file) relative to `NodeJSProcessOptions.ProjectPath`.

- `exportName`
  - Type: `string`
  - Description: The function in the module's exports to be invoked. If unspecified, the module's exports object is assumed to be a function, and is invoked.

- `args`
  - Type: `object[]`
  - Description: The sequence of JSON-serializable and/or `string` arguments to be passed to the function to invoke.

- `cancellationToken`
  - Type: `CancellationToken`
  - Description: The cancellation token for the asynchronous operation.
#### Returns
The task representing the asynchronous operation.
#### Exceptions
- `ConnectionException`
  - Thrown if unable to connect to NodeJS.
- `InvocationException`
  - Thrown if a NodeJS error occurs.
  - Thrown if the invocation request times out.
- `ObjectDisposedException`
  - Thrown if this has been disposed or if it attempts to use one of its dependencies that has been disposed.
- `OperationCanceledException`
  - Thrown if `cancellationToken` is cancelled.
#### Example
If we have a file named `exampleModule.js` (located in `NodeJSProcessOptions.ProjectPath`), with contents:
```javascript
module.exports = (callback, message) => callback(null, { resultMessage: message });
```
And we have the class `Result`:
```csharp
public class Result
{
    public string Message { get; set; }
}
```
The following assertion will pass:
```csharp
Result result = await nodeJSService.InvokeFromFileAsync<Result>("exampleModule.js", args: new[] { "success" });

Assert.Equal("success", result.Message);
```

### INodeJSService.InvokeFromStringAsync
#### Signature
```csharp
Task<T> InvokeFromStringAsync<T>(string moduleString, string newCacheIdentifier = null, string exportName = null, object[] args = null, CancellationToken cancellationToken = default(CancellationToken));
```
#### Description
Invokes a function exported by a NodeJS module in string form.
#### Parameters
- `T`
  - Description: The type of object this method will return. It can be a JSON-serializable type, `string`, or `Stream`.
 
- `moduleString`
  - Type: `string`
  - Description: The module in `string` form.

- `newCacheIdentifier`
  - Type: `string`
  - Description: The modules's cache identifier in the NodeJS module cache. If unspecified, the module will not be cached.

- `exportName`
  - Type: `string`
  - Description: The function in the module's exports to be invoked. If unspecified, the module's exports object is assumed to be a function, and is invoked.

- `args`
  - Type: `object[]`
  - Description: The sequence of JSON-serializable and/or `string` arguments to be passed to the function to invoke.

- `cancellationToken`
  - Type: `CancellationToken`
  - Description: The cancellation token for the asynchronous operation.
#### Returns
The task representing the asynchronous operation.
#### Exceptions
- `ConnectionException`
  - Thrown if unable to connect to NodeJS.
- `InvocationException`
  - Thrown if a NodeJS error occurs.
  - Thrown if the invocation request times out.
- `ObjectDisposedException`
  - Thrown if this has been disposed or if it attempts to use one of its dependencies that has been disposed.
- `OperationCanceledException`
  - Thrown if `cancellationToken` is cancelled.
#### Example
Using the class `Result`:
```csharp
public class Result
{
    public string Message { get; set; }
}
```
The following assertion will pass:
```csharp
Result result = await nodeJSService.InvokeFromStringAsync<Result>("module.exports = (callback, message) => callback(null, { resultMessage: message });", 
    args: new[] { "success" });

Assert.Equal("success", result.Message);
```
### INodeJSService.InvokeFromStreamAsync
#### Signature
```csharp
Task<T> InvokeFromStreamAsync<T>(Stream moduleStream, string newCacheIdentifier = null, string exportName = null, object[] args = null, CancellationToken cancellationToken = default(CancellationToken));
```
#### Description
Invokes a function exported by a NodeJS module in Stream form.
#### Parameters
- `T`
  - Description: The type of object this method will return. It can be a JSON-serializable type, `string`, or `Stream`.
 
- `moduleStream`
  - Type: `Stream`
  - Description: The module in `Stream` form.

- `newCacheIdentifier`
  - Type: `string`
  - Description: The modules's cache identifier in the NodeJS module cache. If unspecified, the module will not be cached.

- `exportName`
  - Type: `string`
  - Description: The function in the module's exports to be invoked. If unspecified, the module's exports object is assumed to be a function, and is invoked.

- `args`
  - Type: `object[]`
  - Description: The sequence of JSON-serializable and/or `string` arguments to be passed to the function to invoke.

- `cancellationToken`
  - Type: `CancellationToken`
  - Description: The cancellation token for the asynchronous operation.
#### Returns
The task representing the asynchronous operation.
#### Exceptions
- `ConnectionException`
  - Thrown if unable to connect to NodeJS.
- `InvocationException`
  - Thrown if a NodeJS error occurs.
  - Thrown if the invocation request times out.
- `ObjectDisposedException`
  - Thrown if this has been disposed or if it attempts to use one of its dependencies that has been disposed.
- `OperationCanceledException`
  - Thrown if `cancellationToken` is cancelled.
#### Example
Using the class `Result`:
```csharp
public class Result
{
    public string Message { get; set; }
}
```
The following assertion will pass:
```csharp
using (var memoryStream = new MemoryStream())
using (var streamWriter = new StreamWriter(memoryStream))
{
    // Write the module to a MemoryStream for demonstration purposes.
    streamWriter.Write("module.exports = (callback, message) => callback(null, {resultMessage: message});");
    streamWriter.Flush();
    memoryStream.Position = 0;

    Result result = await nodeJSService.InvokeFromStreamAsync<Result>(memoryStream, args: new[] { "success" });
    
    Assert.Equal("success", result.Message);
}
```
### INodeJSService.TryInvokeFromCacheAsync
#### Signature
```csharp
Task<(bool, T)> TryInvokeFromCacheAsync<T>(string moduleCacheIdentifier, string exportName = null, object[] args = null, CancellationToken cancellationToken = default(CancellationToken));
```
#### Description
Attempts to invoke a function exported by a NodeJS module cached by NodeJS.
#### Parameters
- `T`
  - Description: The type of object this method will return. It can be a JSON-serializable type, `string`, or `Stream`.
 
- `moduleCacheIdentifier`
  - Type: `string`
  - Description: The cache identifier of the module.

- `exportName`
  - Type: `string`
  - Description: The function in the module's exports to be invoked. If unspecified, the module's exports object is assumed to be a function, and is invoked.

- `args`
  - Type: `object[]`
  - Description: The sequence of JSON-serializable and/or `string` arguments to be passed to the function to invoke.

- `cancellationToken`
  - Type: `CancellationToken`
  - Description: The cancellation token for the asynchronous operation.
#### Returns
The task representing the asynchronous operation. On completion, the task returns a `(bool, T)` with the bool set to true on 
success and false otherwise.
#### Exceptions
- `ConnectionException`
  - Thrown if unable to connect to NodeJS.
- `InvocationException`
  - Thrown if a NodeJS error occurs.
  - Thrown if the invocation request times out.
- `ObjectDisposedException`
  - Thrown if this has been disposed or if it attempts to use one of its dependencies that has been disposed.
- `OperationCanceledException`
  - Thrown if `cancellationToken` is cancelled.
#### Example
Using the class `Result`:
```csharp
public class Result
{
    public string Message { get; set; }
}
```
The following assertion will pass:
```csharp
// Cache the module
string cacheIdentifier = "exampleModule";
await nodeJSService.InvokeFromStringAsync<Result>("module.exports = (callback, message) => callback(null, { resultMessage: message });", 
    cacheIdentifier,
    args: new[] { "success" });

// Invoke from cache
(bool success, Result result) = await nodeJSService.TryInvokeFromCacheAsync<Result>(cacheIdentifier, args: new[] { "success" });

Assert.True(success);
Assert.Equal("success", result.Message);
```

## Extensibility
This library's behaviour can be customized by implementing public interfaces and overwriting their default DI services. For example, if we have objects that
can't be serialized using the default JSON serialization logic, we can implement `IJsonService`:
```csharp
// Create a custom implementation of IJsonService
public class MyJsonService : IJsonService
{
    public T Deserialize<T>(JsonReader jsonReader)
    {
        ... // Custom deserializetion logic
    }

    public void Serialize(JsonWriter jsonWriter, object value)
    {
        ... // Custom serialization logic
    }
}
```
And overwrite its default DI service:
```csharp
var services = new ServiceCollection();
services.AddNodeJS();

// Overwrite the default DI service
services.AddSingleton<IJsonService, MyJsonService>();

ServiceProvider serviceProvider = services.BuildServiceProvider();
INodeJSService nodeJSService = serviceProvider.GetRequiredService<INodeJSService>();
```
This is the list of implementable interfaces:

| Interface | Description |
| --------- | ----------- |
| `IJsonService` | An abstraction for JSON serialization/deserialization. |
| `IHttpClientService` | An abstraction for `HttpClient`. |
| `INodeJSProcessFactory` | An abstraction for NodeJS process creation. |
| `IHttpContentFactory` | An abstraction for `HttpContent` creation. |
| `INodeJSService` | An abstraction for invoking code in NodeJS. |
| `IEmbeddedResourcesService` | An abstraction for reading of embedded resources. |

## Performance
This library is heavily inspired by [Microsoft.AspNetCore.NodeServices](https://github.com/aspnet/JavaScriptServices/tree/master/src/Microsoft.AspNetCore.NodeServices). While the main
additions to this library are ways to invoke in-memory javascript, this library also provides better performance. 

### Latency
Inter-process communication latency benchmarks:

|                                                        Method |     Mean |   Error |  StdDev |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|-------------------------------------------------------------- |---------:|--------:|--------:|-------:|------:|------:|----------:|
|                         INodeJSService_Latency_InvokeFromFile | 104.0 us | 0.64 us | 0.56 us | 1.2207 |     - |     - |   5.69 KB |
| INodeJSService_Latency_InvokeFromFile_GracefulShutdownEnabled | 104.2 us | 0.65 us | 0.57 us | 1.2207 |     - |     - |   5.91 KB |
|                        INodeJSService_Latency_InvokeFromCache | 100.7 us | 0.47 us | 0.44 us | 1.2207 |     - |     - |   5.76 KB |
|                                         INodeServices_Latency | 114.8 us | 1.12 us | 0.99 us | 2.4414 |     - |     - |  10.25 KB |

```
NodeJS v12.13.0
BenchmarkDotNet=v0.12.0, OS=Windows 10.0.18362
Intel Core i7-7700 CPU 3.60GHz (Kaby Lake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=3.0.100
  [Host]     : .NET Core 3.0.0 (CoreCLR 4.700.19.46205, CoreFX 4.700.19.46214), X64 RyuJIT
  DefaultJob : .NET Core 3.0.0 (CoreCLR 4.700.19.46205, CoreFX 4.700.19.46214), X64 RyuJIT
```

View source [here](https://github.com/JeringTech/Javascript.NodeJS/blob/master/perf/NodeJS/LatencyBenchmarks.cs).

### Concurrency
<!-- TODO benchmark on how graceful shutdown (and thus invoke task tracking) affect concurrency -->
Asynchronous invocations benchmarks:

|                                  Method |       Mean |   Error |  StdDev | Gen 0 | Gen 1 | Gen 2 | Allocated |
|---------------------------------------- |-----------:|--------:|--------:|------:|------:|------:|----------:|
| INodeJSService_Concurrency_MultiProcess |   400.2 ms | 0.44 ms | 0.42 ms |     - |     - |     - | 134.76 KB |
|         INodeJSService_Concurrency_None | 2,500.4 ms | 0.45 ms | 0.42 ms |     - |     - |     - | 134.76 KB |
|               INodeServices_Concurrency | 2,500.2 ms | 0.49 ms | 0.46 ms |     - |     - |     - | 245.78 KB |

```
BenchmarkDotNet=v0.12.0, OS=Windows 10.0.18362
Intel Core i7-7700 CPU 3.60GHz (Kaby Lake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=3.0.100
  [Host]     : .NET Core 3.0.0 (CoreCLR 4.700.19.46205, CoreFX 4.700.19.46214), X64 RyuJIT
  DefaultJob : .NET Core 3.0.0 (CoreCLR 4.700.19.46205, CoreFX 4.700.19.46214), X64 RyuJIT
```

View source [here](https://github.com/JeringTech/Javascript.NodeJS/blob/master/perf/NodeJS/ConcurrencyBenchmarks.cs).

### Real Workload
Real world benchmarks. These use the syntax highlighter, Prism, to highlight C#:

|                      Method |     Mean |     Error |    StdDev |   Gen 0 |   Gen 1 | Gen 2 | Allocated |
|---------------------------- |---------:|----------:|----------:|--------:|--------:|------:|----------:|
| INodeJSService_RealWorkload | 1.269 ms | 0.0150 ms | 0.0140 ms | 54.6875 | 11.7188 |     - | 224.55 KB |
|  INodeServices_RealWorkload | 2.236 ms | 0.0148 ms | 0.0131 ms | 70.3125 |       - |     - | 283.93 KB |

```
NodeJS v12.13.0
BenchmarkDotNet=v0.12.0, OS=Windows 10.0.18362
Intel Core i7-7700 CPU 3.60GHz (Kaby Lake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=3.0.100
  [Host]     : .NET Core 3.0.0 (CoreCLR 4.700.19.46205, CoreFX 4.700.19.46214), X64 RyuJIT
  DefaultJob : .NET Core 3.0.0 (CoreCLR 4.700.19.46205, CoreFX 4.700.19.46214), X64 RyuJIT
```

View source [here](https://github.com/JeringTech/Javascript.NodeJS/blob/master/perf/NodeJS/RealWorkloadBenchmarks.cs).

### File Watching
<!-- TODO these don't consider situations with in-progess invocations -->
How long it takes for NodeJS to restart and begin processing invocations:

|                                                                   Method |     Mean |    Error |   StdDev | Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------------------------------------------------------------------- |---------:|---------:|---------:|------:|------:|------:|----------:|
|  HttpNodeJSService_FileWatching_GracefulShutdownEnabled_MoveToNewProcess | 48.64 ms | 0.943 ms | 0.882 ms |     - |     - |     - | 276.99 KB |
| HttpNodeJSService_FileWatching_GracefulShutdownDisabled_MoveToNewProcess | 49.75 ms | 0.987 ms | 1.416 ms |     - |     - |     - | 276.62 KB |

```
NodeJS v12.13.0
BenchmarkDotNet=v0.12.0, OS=Windows 10.0.18362
Intel Core i7-7700 CPU 3.60GHz (Kaby Lake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=3.0.100
  [Host]     : .NET Core 3.0.0 (CoreCLR 4.700.19.46205, CoreFX 4.700.19.46214), X64 RyuJIT
  DefaultJob : .NET Core 3.0.0 (CoreCLR 4.700.19.46205, CoreFX 4.700.19.46214), X64 RyuJIT
```

View source [here](https://github.com/JeringTech/Javascript.NodeJS/blob/master/perf/NodeJS/FileWatchingBenchmarks.cs).

## Building and Testing
You can build and test this project in Visual Studio 2017/2019.

## Projects Using this Library
[Jering.Web.SyntaxHighlighters.HighlightJS](https://github.com/JeringTech/Web.SyntaxHighlighters.HighlightJS) - Use the Syntax Highlighter, HighlightJS, from C#.
[Jering.Web.SyntaxHighlighters.Prism](https://github.com/JeringTech/Web.SyntaxHighlighters.Prism) - Use the Syntax Highlighter, Prism, from C#.  
[NodeReact.NET](https://github.com/DaniilSokolyuk/NodeReact.NET) - Library to render React library components on the server-side with C# as well as on the client.

## Related Concepts

### What is NodeJS?
[NodeJS](https://nodejs.org/en/) is a javascript runtime. Essentially, it provides some built-in libraries and executes javascript. Similarities can be drawn to the
[Core Common Language Runtime (CoreCLR)](https://github.com/dotnet/coreclr), which provides a set of base libraries and executes [.NET Intermediate Language](https://en.wikipedia.org/wiki/Common_Intermediate_Language) 
(typically generated by compiling C# or some other .NET language).  

Under the hood, NodeJS uses [V8](https://developers.google.com/v8/) to execute javascript. While this library could have been built to invoke javascript directly in V8,
invoking javascript in NodeJS affords both access to NodeJS's built-in modules and the ability to use most of the modules hosted by [npm](https://www.npmjs.com/).

### NodeJS Modules
NodeJS modules are a kind of javascript module. The concept of javascript modules can seem far more complicated than it really is,
not least because of the existence of competing specifications (CommonJS, AMD, ES6, ...), and the existence of multiple implementations of each specification (SystemJS, RequireJS, 
Dojo, NodeJS, ...). In reality, javascript modules such as NodeJS modules are really simple. In the following sections, we will go through the what, how and why of NodeJS modules.

#### What is a NodeJS Module?
The following is a valid NodeJS module. Lets imagine that it exists in a file, `flavours.js`:
```javascript
// Note that the module variable isn't declared (no "var module = {}")
module.exports = ['chocolate', 'strawberry', 'vanilla'];
```
The following is another valid NodeJS module, we will use it as an entry script (to be supplied to `node` on the command line). Lets imagine that it exists in a file, `printer.js`, 
in the same directory as `flavours.js`:
```javascript
var flavours = require('./flavours.js');

flavours.forEach((flavour) => console.log(flavour));
```
If we run `node printer.js` on the command line, the flavours get printed:
```powershell
PS C:\NodeJS_Modules_Example> node printer.js
chocolate
strawberry
vanilla
```

In general, a NodeJS module is simply a block of javascript with `module.exports` and/or `require` statements. These statements are explained in the next section.

#### How does a NodeJS Module Work?
NodeJS's logic for managing modules is contained in its `require` function. In the example above, `require('./flavours.js')` executes the following steps:
1. Resolves the absolute path of `flavours.js` to `C:/NodeJS_Modules_Example/flavours.js`.
2. Checks whether the NodeJS module cache (a simple javascript object) has a property with name `C:/NodeJS_Modules_Example/flavours.js`, and finds that it does not 
  (the module has not been cached).
3. Reads the contents of `C:/NodeJS_Modules_Example/flavours.js` into memory.
4. Wraps the contents of `C:/NodeJS_Modules_Example/flavour.js` in a function by appending and prepending strings. The resulting function looks like the following:
   ```javascript
   // Note how the require function and a module object are supplied by the wrapper.
   function (exports, require, module, __filename, __dirname){
       module.exports = ['chocolate', 'strawberry', 'vanilla'];
   }
   ```
5. Creates the `module` object and passes it to the generated function.
6. Adds the `module` object (now containing an array as its `exports` property) to the NodeJS module cache using the property name `C:/NodeJS_Modules_Example/flavours.js`.
7. Returns `module.exports`.

If the flavours module is required again, the cached `module` object is retrieved in step 2, and its exports object is returned. This means that module exports are not immutable, for example,
if we replace the contents of `printer.js` with the following:

```javascript
var flavours = require('./flavours.js');

flavours.forEach((flavour) => console.log(flavour));

// Clear the array
flavours.length = 0;

// Add three new flavours
flavours.push('apple');
flavours.push('green tea');
flavours.push('sea salt');

// Require the module again, turns out that require returns a reference to the same array
flavours = require('./flavours.js');

flavours.forEach((flavour) => console.log(flavour));
```

Running `node printer.js` on the command line prints the following flavours:
```powershell
PS C:\Users\Jeremy\Desktop\JSTest> node entry.js
chocolate
strawberry
vanilla
apple
green tea
sea salt
```

#### Why do NodeJS Modules exist?
To answer this question, lets consider the impetus for the creation of javascript modules in general. Web pages used to include scripts like so:
``` html
<html>
    ...
    <script type="text/javascript" src="path/to/coolLibrary.js"></script>
    <script type="text/javascript" src="path/to/myScript.js"></script>
    ...
</html>
```
Browsers would load the scripts like so:
```javascript
// Contents of coolLibrary.js
var coolLibraryPrivateObject = ...;

function CoolLibraryPublicFunction(){
    ... // Do something with coolLibraryPrivateObject, and return some value
}

// Contents of myScript.js
var myVar = CoolLibraryPublicFunction();

... // Do something with myVar
```
Note how everything in the example above is in the same scope. `coolLibraryPrivateObject` can be accessed in `myscript.js`. How
can we hide the private object? We can encapsulate cool library in a function:
```javascript
var module = {};

// This is an immediately invoked function expression, shorthand for assigning the function to a variable then calling it - https://developer.mozilla.org/en-US/docs/Glossary/IIFE
(function(module){
    // Contents of coolLibrary.js
    var coolLibraryPrivateObject = ...;

    function CoolLibraryPublicFunction(){
        ... // Do something with coolLibraryPrivateObject, and return some value
    }
    
    module.exports = CoolLibraryPublicFunction;
})(module)

// Contents of myScript.js
var myVar = module.exports(); // We assigned CoolLibraryPublicFunction to module.exports

... // Do something with myVar
```
We've successfully hidden `coolLibraryPrivateObject` from the global scope using a module-esque pattern. Apart from hiding private objects, this pattern also prevents global namespace pollution.  

NodeJS modules serve a similar purpose. By wrapping modules in functions, NodeJS creates a closure for each module so internal details
can be kept private.

## Contributing
Contributions are welcome!

### Contributors
- [JeremyTCD](https://github.com/JeremyTCD)
- [Daniil Sokolyuk](https://github.com/DaniilSokolyuk)
- [dustinsoftware](https://github.com/dustinsoftware)

## About
Follow [@JeringTech](https://twitter.com/JeringTech) for updates and more.
