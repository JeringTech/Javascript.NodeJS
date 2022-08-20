# Jering.Javascript.NodeJS
[![Build Status](https://dev.azure.com/JeringTech/Javascript.NodeJS/_apis/build/status/Jering.Javascript.NodeJS-CI?branchName=master)](https://dev.azure.com/JeringTech/Javascript.NodeJS/_build/latest?definitionId=1?branchName=master)
[![codecov](https://codecov.io/gh/JeringTech/Javascript.NodeJS/branch/master/graph/badge.svg)](https://codecov.io/gh/JeringTech/Javascript.NodeJS)
[![License](https://img.shields.io/badge/license-Apache%202.0-blue.svg)](https://github.com/JeringTech/Javascript.NodeJS/blob/master/License.md)
[![NuGet](https://img.shields.io/nuget/vpre/Jering.Javascript.NodeJS.svg?label=nuget)](https://www.nuget.org/packages/Jering.Javascript.NodeJS/)

## Table of Contents
[Overview](#overview)  
[Target Frameworks](#target-frameworks)  
[Platforms](#platforms)  
[Prerequisites](#prerequisites)  
[Installation](#installation)  
[Usage](#usage)  
[API](#api)  
[Performance](#performance)  
[Building and Testing](#building-and-testing)  
[Projects Using this Library](#projects-using-this-library)  
[Related Concepts](#related-concepts)  
[Contributing](#contributing)  
[About](#about)  

## Overview
Jering.Javascript.NodeJS enables you to invoke javascript in [NodeJS](https://nodejs.org/en/), from C#. With this ability, you can use Node.js-javascript libraries and scripts from your C# projects.  

> You can use this library as a replacement for the obsoleted [Microsoft.AspNetCore.NodeServices](https://github.com/aspnet/JavaScriptServices/tree/master/src/Microsoft.AspNetCore.NodeServices).
[`InvokeFromFileAsync<T>`](#inodejsserviceinvokefromfileasynctstring-string-object-cancellationtoken) replaces `INodeService`'s `InvokeAsync<T>` and `InvokeExportAsync<T>`.

This library is flexible - it provides both a dependency injection (DI) based API and a static API. Also, it supports invoking both in-memory and on-disk javascript. 

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

DI-based API example:

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
- .NET Core 3.1
- .NET 5.0
- .NET 6.0

## Platforms
- Windows
- macOS
- Linux
 
## Prerequisites
You'll need to install [NodeJS](https://nodejs.org/en/) and add the NodeJS executable's directory to the `Path` environment variable.

## Installation
Using Package Manager:
```
PM> Install-Package Jering.Javascript.NodeJS
```
Using .NET CLI:
```
> dotnet add package Jering.Javascript.NodeJS
```

## Usage
This section explains how to use this library. Topics:

[Using the DI-Based API](#using-the-di-based-api)  
[Using the Static API](#using-the-static-api)  
[Invoking Javascript](#invoking-javascript)  
[Debugging Javascript](#debugging-javascript)  
[Configuring](#configuring)  
[Customizing Logic](#customizing-logic)  
[Enabling Multi-Process Concurrency](#enabling-multi-process-concurrency)

### Using the DI-Based API
First, create an `INodeJSService`. You can use any DI framework that has adapters for Microsoft.Extensions.DependencyInjection.
Here, we'll use vanilla Microsoft.Extensions.DependencyInjection:

```csharp
var services = new ServiceCollection();
services.AddNodeJS();
ServiceProvider serviceProvider = services.BuildServiceProvider(); 
INodeJSService nodeJSService = serviceProvider.GetRequiredService<INodeJSService>();
```

Once you've got an `INodeJSService`, you can invoke javascript using its invoke methods. All invoke methods are thread-safe.
Here's one of its invoke-from-string methods:

```csharp
string? result = nodeJSService.InvokeFromStringAsync<Result>("module.exports = (callback, message) => callback(null, message);", args: new[] { "success" });
Assert.Equal("success", result);
```

We describe all of the invoke methods in detail [later on](#invoking-javascript).  

No clean up is required when you're done:
the NodeJS process `INodeJSService` sends javascript invocations to kills itself when it detects that its parent process has died.

If you'd like to manually kill the NodeJS process, you can call `INodeJSService.Dispose()`.
Once the instance is disposed, all invoke methods throw `ObjectDisposedException`.
This is important to keep in mind since `services.AddNodeJS()` registers `INodeJSService` as a singleton (same instance injected every where).

### Using the Static API
This library provides a static alternative to the DI-based API. `StaticNodeJSService` wraps an `INodeJSService`, exposing most of its [public members](#api).  

With the static API, you don't need to worry about creating or managing `INodeJSService`. Example usage;

```csharp
string result = await StaticNodeJSService
    .InvokeFromStringAsync<Result>("module.exports = (callback, message) => callback(null, message);", args: new[] { "success" });

Assert.Equal("success", result);
```

`StaticNodeJSService`'s invoke methods are thread-safe.  

Clean-up wise, `StaticNodeJSService.DisposeServiceProvider()` kills the NodeJS process immediately.
Alternatively, the NodeJS process kills itself when it detects that its parent process has died.

Whether you use the static API or the DI-based API depends on your development needs. If you're already using DI and/or you want to mock 
out `INodeJSService` in your tests and/or you want to [customize](#customizing-logic) services, use the DI-based API. Otherwise,
the static API works fine. 

### Invoking Javascript
We'll begin with the javascript side of things. You'll need a [NodeJS module](#nodejs-modules) that exports either a function or an object containing functions. Exported functions can be of two forms:

#### Function With Callback Parameter
These functions take a callback as their first argument, and call the callback when they're done.  

The callback takes two optional arguments:
- The first argument is an error or an error message. It must be of type [`Error`](https://nodejs.org/api/errors.html#errors_class_error) or `string`.
- The second argument is the result. It must be a JSON-serializable type, a `string`, or a [`stream.Readable`](https://nodejs.org/api/stream.html#stream_class_stream_readable). 
 
Note: this is known as an [error-first callback](https://nodejs.org/api/errors.html#errors_error_first_callbacks).
Such callbacks are used for [error handling](https://nodejs.org/api/errors.html#errors_error_propagation_and_interception) in NodeJS asynchronous code (check out [NodeJS Event Loop](https://nodejs.org/en/docs/guides/event-loop-timers-and-nexttick/) for more information on asynchrony in NodeJS).

As mentioned before, you'll need a module that exports either a function or an object containing functions.
This is a module that exports a valid function:
```javascript
module.exports = (callback, arg1, arg2, arg3) => {
    ... // Do something with args

    callback(null /* error */, result /* result */);
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

If an error or error message is passed to the callback, it's sent back to the calling .NET process, where an `InvocationException` is thrown.

#### Async Function
Async functions are the second valid function form. They're syntactic sugar for the function form described in the previous section (check out 
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

If an error is thrown in an async function, the error message is sent back to the calling .NET process, where an `InvocationException` is thrown:
```javascript
module.exports = async () => {
    throw new Error('error message');
}
```

#### Invoking Javascript From a File
Now that we've covered the javascript side of things, let's invoke some javascript from C#.

If you have a javascript file named `exampleModule.js` (located in [`NodeJSProcessOptions.ProjectPath`](#nodejsprocessoptionsprojectpath)):
```javascript
module.exports = (callback, message) => callback(null, { message: message });
```
And a .NET class `Result`:
```csharp
public class Result
{
    public string? Message { get; set; }
}
```
You can invoke the javascript using [`InvokeFromFileAsync<T>`](#inodejsserviceinvokefromfileasynctstring-string-object-cancellationtoken):
```csharp
Result? result = await nodeJSService.InvokeFromFileAsync<Result>("exampleModule.js", args: new[] { "success" });

Assert.Equal("success", result?.Message);
```
If you change `exampleModule.js` to export an object containing functions:
```javascript
module.exports = {
    appendExclamationMark: (callback, message) => callback(null, { message: message + '!' }),
    appendFullStop: (callback, message) => callback(null, { message: message + '.' })
}
```
You can invoke a specific function by specifying its name:
```csharp
// Invoke appendExclamationMark
Result? result = await nodeJSService.InvokeFromFileAsync<Result>("exampleModule.js", "appendExclamationMark", args: new[] { "success" });

Assert.Equal("success!", result?.Message);
```
When using `InvokeFromFileAsync`, NodeJS always caches the module using the `.js` file's absolute path as cache identifier. This is great for
performance, since the file will not be re-read or recompiled on subsequent invocations.

#### Invoking Javascript in String Form
You can invoke javascript in string form using [`InvokeFromStringAsync<T>`](#inodejsserviceinvokefromstringasynctstring-string-string-object-cancellationtoken):
```csharp
string module = "module.exports = (callback, message) => callback(null, { message: message });";

// Invoke javascript
Result? result = await nodeJSService.InvokeFromStringAsync<Result>(module, args: new[] { "success" });

Assert.Equal("success", result?.Message);
```

In the above example, the module string is sent to NodeJS and recompiled on every invocation.  

If you're planning to invoke a module repeatedly, to avoid resending and recompiling, you'll want NodeJS to cache the module.  

For that, you'll have to specify a custom cache identifier, since unlike a file, a string has no "absolute file path" for NodeJS to identify it by.
Once NodeJS has cached the module, you can invoke from the NodeJS cache: 

```csharp
string cacheIdentifier = "exampleModule";

// Try to invoke from the NodeJS cache
(bool success, Result? result) = await nodeJSService.TryInvokeFromCacheAsync<Result>(cacheIdentifier, args: new[] { "success" });

// If the module hasn't been cached, cache it. If the NodeJS process dies and restarts, the cache will be invalidated, so always check whether success is false.
if(!success)
{
    // This is a trivialized example. In practice, to avoid holding large module strings in memory, you might retrieve the module 
    // string from an on-disk or remote source.
    string moduleString = "module.exports = (callback, message) => callback(null, { message: message });"; 

    // Send the module string to NodeJS where it's compiled, invoked and cached.
    result = await nodeJSService.InvokeFromStringAsync<Result>(moduleString, cacheIdentifier, args: new[] { "success" });
}

Assert.Equal("success", result?.Message);
```

The following [`InvokeFromStringAsync<T>`](#inodejsserviceinvokefromstringasync) overload abstracts away the above example's operations for you.
We recommend it over the logic in the above example. If you've enabled [multi-process concurrency](#enabling-multi-process-concurrency), you must use this overload:

```csharp
string module = "module.exports = (callback, message) => callback(null, { message: message });";
string cacheIdentifier = "exampleModule";

// This is a trivialized example. In practice, to avoid holding large module strings in memory, you might retrieve the module 
// string from an on-disk or remote source.
Func<string> moduleFactory = () => module;

// Initially, sends only cacheIdentifier to NodeJS. If the module hasn't been cached, NodeJS lets the .NET process know.
// The .NET process then creates the module string using moduleFactory and sends it to NodeJS where it's compiled, invoked and cached. 
Result? result = await nodeJSService.InvokeFromStringAsync<Result>(moduleFactory, cacheIdentifier, args: new[] { "success" });

Assert.Equal("success", result?.Message);
```

Like when [invoking javascript from a file](#invoking-javascript-from-a-file), if the module exports an object containing functions, you can invoke a specific function by specifying its name.  

#### Invoking Javascript in Stream Form
You can invoke javascript in stream form using [`InvokeFromStreamAsync<T>`](#inodejsserviceinvokefromstreamasynctstream-string-string-object-cancellationtoken) :
```csharp
// Write the module to a MemoryStream for demonstration purposes.
streamWriter.Write("module.exports = (callback, message) => callback(null, {message: message});");
streamWriter.Flush();
memoryStream.Position = 0;

Result? result = await nodeJSService.InvokeFromStreamAsync<Result>(memoryStream, args: new[] { "success" });
    
Assert.Equal("success", result?.Message);
```

`InvokeFromStreamAsync` behaves like `InvokeFromStringAsync` with regard to caching, refer to [Invoking Javascript in String Form](#invoking-javascript-in-string-form) for details.  

Why bother invoking from streams? If your module is in stream form to begin with, for example, a `NetworkStream`, you avoid allocating a string. Avoiding `string` allocations can improve performance.

### Configuring
If you're using the DI-based API, configure `INodeJSService` using the [.NET options pattern](https://docs.microsoft.com/en-us/dotnet/core/extensions/options). For example:

 ```csharp
var services = new ServiceCollection();
services.AddNodeJS();

// Options for the NodeJS process, here we enable debugging
services.Configure<NodeJSProcessOptions>(options => options.NodeAndV8Options = "--inspect-brk");

// Options for the INodeJSService implementation
// - HttpNodeJSService is the default INodeJSService implementation. It communicates with the NodeJS process via HTTP. Below, we set the HTTP version it uses to HTTP/2.0.
// - HttpNodeJSService extends OutOfProcessNodeJSService, an abstraction for NodeJS process management. Below we set the timeout for connecting to the NodeJS process and for invocations to -1 (infinite).
services.Configure<OutOfProcessNodeJSServiceOptions>(options => options.TimeoutMS = -1);
services.Configure<HttpNodeJSServiceOptions>(options => options.Version = HttpVersion.Version20);

ServiceProvider serviceProvider = services.BuildServiceProvider();
INodeJSService nodeJSService = serviceProvider.GetRequiredService<INodeJSService>(); // Configured INodeJSService
```

You can find the full list of options in the [API](#api) section:

- [NodeJSProcessOptions](#nodejsprocessoptions-class)
- [OutOfProcessNodeJSServiceOptions](#outofprocessnodejsserviceoptions-class)
- [HttpNodeJSServiceOptions](#httpnodejsserviceoptions-class)

#### Configure Using the Static API
Use `StaticNodeJSService.Configure<T>` to configure `StaticNodeJSService`:

```csharp
// Options for the NodeJS process, here we enable debugging
StaticNodeJSService.Configure<NodeJSProcessOptions>(options => options.NodeAndV8Options = "--inspect-brk");

// Options for the INodeJSService implementation
// - HttpNodeJSService is the default INodeJSService implementation. It communicates with the NodeJS process via HTTP. Below, we set the HTTP version it uses to HTTP/2.0.
// - HttpNodeJSService extends OutOfProcessNodeJSService, an abstraction for NodeJS process management. Below we set the timeout for connecting to the NodeJS process and for invocations to -1 (infinite).
StaticNodeJSService.Configure<OutOfProcessNodeJSServiceOptions>(options => options.TimeoutMS = -1);
StaticNodeJSService.Configure<HttpNodeJSServiceOptions>(options => options.Version = HttpVersion.Version20);
```

Configurations made using `StaticNodeJSService.Configure<T>` only apply to javascript invocations made using the static API.  

We recommend making these configurations at application startup since:

- `StaticNodeJSService.Configure<T>` is not thread-safe.
- The NodeJS process is recreated after every `StaticNodeJSService.Configure<T>` call.  

### Debugging Javascript
Follow these steps to debug javascript invoked using `INodeJSService`:
1. Add [`debugger`](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Statements/debugger) statements to your javascript module.
2. Configure the following options: `NodeJSProcessOptions.NodeAndV8Options` = `--inspect-brk` and `OutOfProcessNodeJSServiceOptions.TimeoutMS` = `-1`.
3. Create an `INodeJSService` (or use `StaticNodeJSService`).
4. Call a [javascript invoking method](#methods). 
5. Navigate to `chrome://inspect/` in Chrome.
6. Click "Open dedicated DevTools for Node".
7. Click continue to advance to your `debugger` statements.

### Customizing Logic
You can customize logic by overwriting DI services.  

For example, if you'd like to customize how data sent to NodeJS is serialized/deserialized, create a custom `IJsonService` implementation:

```csharp
// Create a custom implementation of IJsonService
public class MyJsonService : IJsonService
{
    public ValueTask<T?> DeserializeAsync<T>(Stream stream, CancellationToken cancellationToken = default)
    {
        ... // Custom deserializetion logic
    }

    public Task SerializeAsync<T>(Stream stream, T value, CancellationToken cancellationToken = default)
    {
        ... // Custom serialization logic
    }
}
```
And overwrite `IJsonService`'s DI service:
```csharp
var services = new ServiceCollection();
services.AddNodeJS();

// Overwrite the DI service
services.AddSingleton<IJsonService, MyJsonService>();

ServiceProvider serviceProvider = services.BuildServiceProvider();
INodeJSService nodeJSService = serviceProvider.GetRequiredService<INodeJSService>();
```

These are some of the services you can overwrite:

| Interface | Description |
| --------- | ----------- |
| `IJsonService` | An abstraction for JSON serialization/deserialization. |
| `IHttpClientService` | An abstraction for `HttpClient`. |
| `INodeJSProcessFactory` | An abstraction for NodeJS process creation. |
| `IHttpContentFactory` | An abstraction for `HttpContent` creation. |
| `INodeJSService` | An abstraction for invoking code in NodeJS. |
| `IEmbeddedResourcesService` | An abstraction for reading of embedded resources. |

You can find the full list of services in [`NodeJSServiceCollectionExtensions.cs`](https://github.com/JeringTech/Javascript.NodeJS/blob/master/src/NodeJS/NodeJSServiceCollectionExtensions.cs).

#### Customizing Logic Using the Static API
Use `StaticNodeJSService.SetServices` to customize the logic executed by `StaticNodeJSService`'s underlying `INodeJSService`:

```csharp
var services = new ServiceCollection();
services.AddNodeJS();

// Overwrite the DI service
services.AddSingleton<IJsonService, MyJsonService>();

StaticNodeJSService.SetServices(services);
```

We recommend only calling `StaticNodeJSService.SetServices` at application startup since:

- `StaticNodeJSService.SetServices` is not thread-safe.
- The NodeJS process is recreated after every `StaticNodeJSService.SetServices` call.  

### Enabling Multi-Process Concurrency
To enable multi-process concurrency, set `OutOfProcessNodeJSServiceOptions.Concurrency` to `Concurrency.MultiProcess`:

```csharp
services.Configure<OutOfProcessNodeJSServiceOptions>(options => {
    options.Concurrency = Concurrency.MultiProcess; // Concurrency.None by default
    options.ConcurrencyDegree = 8; // Number of processes. Defaults to the number of logical processors on your machine.
);
```
(see [Configuring](#configuring) for more information on configuring)  

Invocations will be distributed among multiple NodeJS processes using round-robin load balancing. 

#### Why Enable Multi-Process Concurrency?
Multi-process concurrency speeds up CPU-bound workloads. We ran a benchmark executing the following logic 25-times, concurrently in NodeJS:

```js
// Minimal CPU-bound operation
module.exports = (callback) => {
    // Block CPU
    var end = new Date().getTime() + 100; // 100ms block
    while (new Date().getTime() < end) { /* do nothing */ }

    callback(null);
};
```

The logic fully utilizes a CPU for 100ms.  

With multi-process concurrency disabled, a single NodeJS process performs invocations synchronously, so the benchmark takes ~2500ms (25 tasks x 100ms).  

With multi-process concurrency enabled, on an 8-core machine, the benchmark takes ~400ms ((25 tasks x 100ms) / 8 + overhead).  

View the full results of our multi-process concurrency benchmark [here](#multi-process-concurrency-1).

#### Limitations
1. You can't use multi-process concurrency if your logic persists data between invocations. For example:

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

    // Intended for result == 8, but result == 5 since different processes perform the invocations
    result = await StaticNodeJSService.InvokeFromStringAsync<int>(javascriptModule, "customIdentifier", args: new object[] { 5 });
    ```

2. With concurrency enabled, you can't use the following caching pattern (previously described in [Inoke Javascript in String Form](#invoke-javascript-in-string-form)):

    ```csharp
    string cacheIdentifier = "exampleModule";

    // If you have an even number of NodeJS processes, success will always be false since the resulting caching attempt is
    // sent to the next NodeJS process.
    (bool success, Result? result) = await nodeJSService.TryInvokeFromCacheAsync<Result>(cacheIdentifier, args: new[] { "success" });

    // False, so we attempt to cache
    if(!success)
    {
        string moduleString = "module.exports = (callback, message) => callback(null, { message: message });"; 

        // Because of round-robin load balancing, this caching attempt is sent to the next NodeJS process.
        result = await nodeJSService.InvokeFromStringAsync<Result>(moduleString, cacheIdentifier, args: new[] { "success" });
    }

    Assert.Equal("success", result?.Message);
    ```

    Instead, call an overload that takes a `moduleFactory` argument. These overloads atomically handle caching and invoking:

    ```csharp
    string module = "module.exports = (callback, message) => callback(null, { message: message });";
    string cacheIdentifier = "exampleModule";

    // This is a trivialized example. In practice, to avoid holding large module strings in memory, you might retrieve the module 
    // string from an on-disk or remote source.
    Func<string> moduleFactory = () => module;

    // Initially, sends only cacheIdentifier to NodeJS. If the module hasn't been cached, NodeJS lets the .NET process know.
    // The .NET process then creates the module string using moduleFactory and sends it to *the same* NodeJS process where it's compiled, invoked and cached. 
    Result? result = await nodeJSService.InvokeFromStringAsync<Result>(moduleFactory, cacheIdentifier, args: new[] { "success" });

    Assert.Equal("success", result?.Message);
    ```

## API

<!-- INodeJSService generated docs -->

### INodeJSService Interface
#### Methods
##### INodeJSService.InvokeFromFileAsync&lt;T&gt;(string, string, object[], CancellationToken)
Invokes a function from a NodeJS module on disk.  
```csharp
Task<T?> InvokeFromFileAsync<T>(string modulePath, [string? exportName = null], [object?[]? args = null], [CancellationToken cancellationToken = default(CancellationToken)])
```
###### Type Parameters
`T`  
The type of value returned. This may be a JSON-serializable type, `string`, or `Stream`.  
###### Parameters
modulePath `string`  
The path to the module relative to `NodeJSProcessOptions.ProjectPath`. This value must not be `null`, whitespace or an empty string.  

exportName `string`  
The name of the function in `module.exports` to invoke. If this value is `null`, `module.exports` is assumed to be a function and is invoked.  

args `object[]`  
The sequence of JSON-serializable arguments to pass to the function to invoke. If this value is `null`, no arguments are passed.  

cancellationToken `CancellationToken`  
The cancellation token for the asynchronous operation.  
###### Returns
The `Task` representing the asynchronous operation.  
###### Exceptions
`ArgumentException`  
Thrown if `modulePath` is `null`, whitespace or an empty string.  

`ConnectionException`  
Thrown if unable to connect to NodeJS.  

`InvocationException`  
Thrown if the invocation request times out.  

`InvocationException`  
Thrown if a NodeJS error occurs.  

`ObjectDisposedException`  
Thrown if this instance is disposed or if it attempts to use a disposed dependency.  

`OperationCanceledException`  
Thrown if `cancellationToken` is cancelled.  

###### Remarks
To avoid rereads and recompilations on subsequent invocations, NodeJS caches the module using the its absolute path as cache identifier.  
###### Example

If we have a file named exampleModule.js (located in `NodeJSProcessOptions.ProjectPath`), with contents:
```javascript
module.exports = (callback, message) => callback(null, { resultMessage: message });
```

Using the class `Result`:
```csharp
public class Result
{
    public string? Message { get; set; }
}
```

The following assertion will pass:
```csharp
Result? result = await nodeJSService.InvokeFromFileAsync<Result>("exampleModule.js", args: new[] { "success" });

Assert.Equal("success", result?.Message);
```  
##### INodeJSService.InvokeFromFileAsync(string, string, object[], CancellationToken)
Invokes a function from a NodeJS module on disk.  
```csharp
Task InvokeFromFileAsync(string modulePath, [string? exportName = null], [object?[]? args = null], [CancellationToken cancellationToken = default(CancellationToken)])
```
###### Parameters
modulePath `string`  
The path to the module relative to `NodeJSProcessOptions.ProjectPath`. This value must not be `null`, whitespace or an empty string.  

exportName `string`  
The name of the function in `module.exports` to invoke. If this value is `null`, `module.exports` is assumed to be a function and is invoked.  

args `object[]`  
The sequence of JSON-serializable arguments to pass to the function to invoke. If this value is `null`, no arguments are passed.  

cancellationToken `CancellationToken`  
The cancellation token for the asynchronous operation.  
###### Exceptions
`ArgumentException`  
Thrown if `modulePath` is `null`, whitespace or an empty string.  

`ConnectionException`  
Thrown if unable to connect to NodeJS.  

`InvocationException`  
Thrown if the invocation request times out.  

`InvocationException`  
Thrown if a NodeJS error occurs.  

`ObjectDisposedException`  
Thrown if this instance is disposed or if it attempts to use a disposed dependency.  

`OperationCanceledException`  
Thrown if `cancellationToken` is cancelled.  

###### Remarks
To avoid rereads and recompilations on subsequent invocations, NodeJS caches the module using the its absolute path as cache identifier.  
##### INodeJSService.InvokeFromStringAsync&lt;T&gt;(string, string, string, object[], CancellationToken)
Invokes a function from a NodeJS module in string form.  
```csharp
Task<T?> InvokeFromStringAsync<T>(string moduleString, [string? cacheIdentifier = null], [string? exportName = null], [object?[]? args = null], [CancellationToken cancellationToken = default(CancellationToken)])
```
###### Type Parameters
`T`  
The type of value returned. This may be a JSON-serializable type, `string`, or `Stream`.  
###### Parameters
moduleString `string`  
The module in string form. This value must not be `null`, whitespace or an empty string.  

cacheIdentifier `string`  
The module's cache identifier. If this value is `null`, NodeJS ignores its module cache..  

exportName `string`  
The name of the function in `module.exports` to invoke. If this value is `null`, `module.exports` is assumed to be a function and is invoked.  

args `object[]`  
The sequence of JSON-serializable arguments to pass to the function to invoke. If this value is `null`, no arguments are passed.  

cancellationToken `CancellationToken`  
The cancellation token for the asynchronous operation.  
###### Returns
The `Task` representing the asynchronous operation.  
###### Exceptions
`ArgumentException`  
Thrown if `moduleString` is `null`, whitespace or an empty string.  

`ConnectionException`  
Thrown if unable to connect to NodeJS.  

`InvocationException`  
Thrown if the invocation request times out.  

`InvocationException`  
Thrown if a NodeJS error occurs.  

`ObjectDisposedException`  
Thrown if this instance is disposed or if it attempts to use a disposed dependency.  

`OperationCanceledException`  
Thrown if `cancellationToken` is cancelled.  

###### Remarks
If `cacheIdentifier` is `null`, sends `moduleString` to NodeJS where it's compiled it for one-time use.  

If `cacheIdentifier` isn't `null`, sends both `moduleString` and `cacheIdentifier` to NodeJS. NodeJS reuses the module if it's already cached. Otherwise, it compiles and caches the module.  

Once the module is cached, you may use `INodeJSService.TryInvokeFromCacheAsync<T>` to invoke directly from the cache, avoiding the overhead of sending `moduleString`.  
###### Example

Using the class `Result`:
```csharp
public class Result
{
    public string? Message { get; set; }
}
```

The following assertion will pass:
```csharp
Result? result = await nodeJSService.InvokeFromStringAsync<Result>("module.exports = (callback, message) => callback(null, { resultMessage: message });", 
    args: new[] { "success" });

Assert.Equal("success", result?.Message);
```  
##### INodeJSService.InvokeFromStringAsync(string, string, string, object[], CancellationToken)
Invokes a function from a NodeJS module in string form.  
```csharp
Task InvokeFromStringAsync(string moduleString, [string? cacheIdentifier = null], [string? exportName = null], [object?[]? args = null], [CancellationToken cancellationToken = default(CancellationToken)])
```
###### Parameters
moduleString `string`  
The module in string form. This value must not be `null`, whitespace or an empty string.  

cacheIdentifier `string`  
The module's cache identifier. If this value is `null`, NodeJS ignores its module cache..  

exportName `string`  
The name of the function in `module.exports` to invoke. If this value is `null`, `module.exports` is assumed to be a function and is invoked.  

args `object[]`  
The sequence of JSON-serializable arguments to pass to the function to invoke. If this value is `null`, no arguments are passed.  

cancellationToken `CancellationToken`  
The cancellation token for the asynchronous operation.  
###### Returns
The `Task` representing the asynchronous operation.  
###### Exceptions
`ArgumentException`  
Thrown if `moduleString` is `null`, whitespace or an empty string.  

`ConnectionException`  
Thrown if unable to connect to NodeJS.  

`InvocationException`  
Thrown if the invocation request times out.  

`InvocationException`  
Thrown if a NodeJS error occurs.  

`ObjectDisposedException`  
Thrown if this instance is disposed or if it attempts to use a disposed dependency.  

`OperationCanceledException`  
Thrown if `cancellationToken` is cancelled.  

###### Remarks
If `cacheIdentifier` is `null`, sends `moduleString` to NodeJS where it's compiled for one-time use.  

If `cacheIdentifier` isn't `null`, sends both `moduleString` and `cacheIdentifier` to NodeJS. NodeJS reuses the module if it's already cached. Otherwise, it compiles and caches the module.  

Once the module is cached, you may use `INodeJSService.TryInvokeFromCacheAsync<T>` to invoke directly from the cache, avoiding the overhead of sending `moduleString`.  
##### INodeJSService.InvokeFromStringAsync&lt;T&gt;(Func&lt;string&gt;, string, string, object[], CancellationToken)
Invokes a function from a NodeJS module in string form.  
```csharp
Task<T?> InvokeFromStringAsync<T>(Func<string> moduleFactory, string cacheIdentifier, [string? exportName = null], [object?[]? args = null], [CancellationToken cancellationToken = default(CancellationToken)])
```
###### Type Parameters
`T`  
The type of value returned. This may be a JSON-serializable type, `string`, or `Stream`.  
###### Parameters
moduleFactory `Func<string>`  
The factory that creates the module string. This value must not be `null` and it must not return `null`, whitespace or an empty string.  

cacheIdentifier `string`  
The module's cache identifier. This value must not be `null`.  

exportName `string`  
The name of the function in `module.exports` to invoke. If this value is `null`, `module.exports` is assumed to be a function and is invoked.  

args `object[]`  
The sequence of JSON-serializable arguments to pass to the function to invoke. If this value is `null`, no arguments are passed.  

cancellationToken `CancellationToken`  
The cancellation token for the asynchronous operation.  
###### Returns
The `Task` representing the asynchronous operation.  
###### Exceptions
`ArgumentNullException`  
Thrown if module is not cached but `moduleFactory` is `null`.  

`ArgumentNullException`  
Thrown if `cacheIdentifier` is `null`.  

`ArgumentException`  
Thrown if `moduleFactory` returns `null`, whitespace or an empty string.  

`ConnectionException`  
Thrown if unable to connect to NodeJS.  

`InvocationException`  
Thrown if the invocation request times out.  

`InvocationException`  
Thrown if a NodeJS error occurs.  

`ObjectDisposedException`  
Thrown if this instance is disposed or if it attempts to use a disposed dependency.  

`OperationCanceledException`  
Thrown if `cancellationToken` is cancelled.  

###### Remarks
Initially, sends only `cacheIdentifier` to NodeJS. NodeJS reuses the module if it's already cached. Otherwise, it informs the .NET process that the module isn't cached. 
The .NET process then creates the module string using `moduleFactory` and send it to NodeJS where it's compiled, invoked and cached.  

If `exportName` is `null`, `module.exports` is assumed to be a function and is invoked. Otherwise, invokes the function named `exportName` in `module.exports`.  
##### INodeJSService.InvokeFromStringAsync(Func&lt;string&gt;, string, string, object[], CancellationToken)
Invokes a function from a NodeJS module in string form.  
```csharp
Task InvokeFromStringAsync(Func<string> moduleFactory, string cacheIdentifier, [string? exportName = null], [object?[]? args = null], [CancellationToken cancellationToken = default(CancellationToken)])
```
###### Parameters
moduleFactory `Func<string>`  
The factory that creates the module string. This value must not be `null` and it must not return `null`, whitespace or an empty string.  

cacheIdentifier `string`  
The module's cache identifier. This value must not be `null`.  

exportName `string`  
The name of the function in `module.exports` to invoke. If this value is `null`, `module.exports` is assumed to be a function and is invoked.  

args `object[]`  
The sequence of JSON-serializable arguments to pass to the function to invoke. If this value is `null`, no arguments are passed.  

cancellationToken `CancellationToken`  
The cancellation token for the asynchronous operation.  
###### Returns
The `Task` representing the asynchronous operation.  
###### Exceptions
`ArgumentNullException`  
Thrown if module is not cached but `moduleFactory` is `null`.  

`ArgumentNullException`  
Thrown if `cacheIdentifier` is `null`.  

`ArgumentException`  
Thrown if `moduleFactory` returns `null`, whitespace or an empty string.  

`ConnectionException`  
Thrown if unable to connect to NodeJS.  

`InvocationException`  
Thrown if the invocation request times out.  

`InvocationException`  
Thrown if a NodeJS error occurs.  

`ObjectDisposedException`  
Thrown if this instance is disposed or if it attempts to use a disposed dependency.  

`OperationCanceledException`  
Thrown if `cancellationToken` is cancelled.  

###### Remarks
Initially, sends only `cacheIdentifier` to NodeJS. NodeJS reuses the module if it's already cached. Otherwise, it informs the .NET process that the module isn't cached. 
The .NET process then creates the module string using `moduleFactory` and send it to NodeJS where it's compiled, invoked and cached.  

If `exportName` is `null`, `module.exports` is assumed to be a function and is invoked. Otherwise, invokes the function named `exportName` in `module.exports`.  
##### INodeJSService.InvokeFromStreamAsync&lt;T&gt;(Stream, string, string, object[], CancellationToken)
Invokes a function from a NodeJS module in stream form.  
```csharp
Task<T?> InvokeFromStreamAsync<T>(Stream moduleStream, [string? cacheIdentifier = null], [string? exportName = null], [object?[]? args = null], [CancellationToken cancellationToken = default(CancellationToken)])
```
###### Type Parameters
`T`  
The type of value returned. This may be a JSON-serializable type, `string`, or `Stream`.  
###### Parameters
moduleStream `Stream`  
The module in stream form. This value must not be `null`.  

cacheIdentifier `string`  
The module's cache identifier. If this value is `null`, NodeJS ignores its module cache..  

exportName `string`  
The name of the function in `module.exports` to invoke. If this value is `null`, `module.exports` is assumed to be a function and is invoked.  

args `object[]`  
The sequence of JSON-serializable arguments to pass to the function to invoke. If this value is `null`, no arguments are passed.  

cancellationToken `CancellationToken`  
The cancellation token for the asynchronous operation.  
###### Returns
The `Task` representing the asynchronous operation.  
###### Exceptions
`ArgumentException`  
Thrown if `moduleStream` is `null`.  

`ConnectionException`  
Thrown if unable to connect to NodeJS.  

`InvocationException`  
Thrown if the invocation request times out.  

`InvocationException`  
Thrown if a NodeJS error occurs.  

`ObjectDisposedException`  
Thrown if this instance is disposed or if it attempts to use a disposed dependency.  

`OperationCanceledException`  
Thrown if `cancellationToken` is cancelled.  

###### Remarks
If `cacheIdentifier` is `null`, sends the stream to NodeJS where it's compiled for one-time use.  

If `cacheIdentifier` isn't `null`, sends both the stream and `cacheIdentifier` to NodeJS. NodeJS reuses the module if it's already cached. Otherwise, it compiles and caches the module.  

Once the module is cached, you may use `INodeJSService.TryInvokeFromCacheAsync<T>` to invoke directly from the cache, avoiding the overhead of sending the module stream.  
###### Example

Using the class `Result`:
```csharp
public class Result
{
    public string? Message { get; set; }
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

    Result? result = await nodeJSService.InvokeFromStreamAsync<Result>(memoryStream, args: new[] { "success" });
    
    Assert.Equal("success", result?.Message);
}
```  
##### INodeJSService.InvokeFromStreamAsync(Stream, string, string, object[], CancellationToken)
Invokes a function from a NodeJS module in stream form.  
```csharp
Task InvokeFromStreamAsync(Stream moduleStream, [string? cacheIdentifier = null], [string? exportName = null], [object?[]? args = null], [CancellationToken cancellationToken = default(CancellationToken)])
```
###### Parameters
moduleStream `Stream`  
The module in stream form. This value must not be `null`.  

cacheIdentifier `string`  
The module's cache identifier. If this value is `null`, NodeJS ignores its module cache..  

exportName `string`  
The name of the function in `module.exports` to invoke. If this value is `null`, `module.exports` is assumed to be a function and is invoked.  

args `object[]`  
The sequence of JSON-serializable arguments to pass to the function to invoke. If this value is `null`, no arguments are passed.  

cancellationToken `CancellationToken`  
The cancellation token for the asynchronous operation.  
###### Returns
The `Task` representing the asynchronous operation.  
###### Exceptions
`ArgumentException`  
Thrown if `moduleStream` is `null`.  

`ConnectionException`  
Thrown if unable to connect to NodeJS.  

`InvocationException`  
Thrown if the invocation request times out.  

`InvocationException`  
Thrown if a NodeJS error occurs.  

`ObjectDisposedException`  
Thrown if this instance is disposed or if it attempts to use a disposed dependency.  

`OperationCanceledException`  
Thrown if `cancellationToken` is cancelled.  

###### Remarks
If `cacheIdentifier` is `null`, sends the stream to NodeJS where it's compiled for one-time use.  

If `cacheIdentifier` isn't `null`, sends both the stream and `cacheIdentifier` to NodeJS. NodeJS reuses the module if it's already cached. Otherwise, it compiles and caches the module.  

Once the module is cached, you may use `INodeJSService.TryInvokeFromCacheAsync<T>` to invoke directly from the cache, avoiding the overhead of sending the module stream.  
##### INodeJSService.InvokeFromStreamAsync&lt;T&gt;(Func&lt;Stream&gt;, string, string, object[], CancellationToken)
Invokes a function from a NodeJS module in stream form.  
```csharp
Task<T?> InvokeFromStreamAsync<T>(Func<Stream> moduleFactory, string cacheIdentifier, [string? exportName = null], [object?[]? args = null], [CancellationToken cancellationToken = default(CancellationToken)])
```
###### Type Parameters
`T`  
The type of value returned. This may be a JSON-serializable type, `string`, or `Stream`.  
###### Parameters
moduleFactory `Func<Stream>`  
The factory that creates the module stream. This value must not be `null` and it must not return `null`.  

cacheIdentifier `string`  
The module's cache identifier. This value must not be `null`.  

exportName `string`  
The name of the function in `module.exports` to invoke. If this value is `null`, `module.exports` is assumed to be a function and is invoked.  

args `object[]`  
The sequence of JSON-serializable arguments to pass to the function to invoke. If this value is `null`, no arguments are passed.  

cancellationToken `CancellationToken`  
The cancellation token for the asynchronous operation.  
###### Returns
The `Task` representing the asynchronous operation.  
###### Exceptions
`ArgumentNullException`  
Thrown if module is not cached but `moduleFactory` is `null`.  

`ArgumentNullException`  
Thrown if `cacheIdentifier` is `null`.  

`ArgumentException`  
Thrown if `moduleFactory` returns `null`.  

`ConnectionException`  
Thrown if unable to connect to NodeJS.  

`InvocationException`  
Thrown if the invocation request times out.  

`InvocationException`  
Thrown if a NodeJS error occurs.  

`ObjectDisposedException`  
Thrown if this instance is disposed or if it attempts to use a disposed dependency.  

`OperationCanceledException`  
Thrown if `cancellationToken` is cancelled.  

###### Remarks
Initially, sends only `cacheIdentifier` to NodeJS. NodeJS reuses the module if it's already cached. Otherwise, it informs the .NET process that the module isn't cached. 
The .NET process then creates the module stream using `moduleFactory` and send it to NodeJS where it's compiled, invoked and cached.  

If `exportName` is `null`, `module.exports` is assumed to be a function and is invoked. Otherwise, invokes the function named `exportName` in `module.exports`.  
##### INodeJSService.InvokeFromStreamAsync(Func&lt;Stream&gt;, string, string, object[], CancellationToken)
Invokes a function from a NodeJS module in stream form.  
```csharp
Task InvokeFromStreamAsync(Func<Stream> moduleFactory, string cacheIdentifier, [string? exportName = null], [object?[]? args = null], [CancellationToken cancellationToken = default(CancellationToken)])
```
###### Parameters
moduleFactory `Func<Stream>`  
The factory that creates the module stream. This value must not be `null` and it must not return `null`.  

cacheIdentifier `string`  
The module's cache identifier. This value must not be `null`.  

exportName `string`  
The name of the function in `module.exports` to invoke. If this value is `null`, `module.exports` is assumed to be a function and is invoked.  

args `object[]`  
The sequence of JSON-serializable arguments to pass to the function to invoke. If this value is `null`, no arguments are passed.  

cancellationToken `CancellationToken`  
The cancellation token for the asynchronous operation.  
###### Returns
The `Task` representing the asynchronous operation.  
###### Exceptions
`ArgumentNullException`  
Thrown if module is not cached but `moduleFactory` is `null`.  

`ArgumentNullException`  
Thrown if `cacheIdentifier` is `null`.  

`ArgumentException`  
Thrown if `moduleFactory` returns `null`.  

`ConnectionException`  
Thrown if unable to connect to NodeJS.  

`InvocationException`  
Thrown if the invocation request times out.  

`InvocationException`  
Thrown if a NodeJS error occurs.  

`ObjectDisposedException`  
Thrown if this instance is disposed or if it attempts to use a disposed dependency.  

`OperationCanceledException`  
Thrown if `cancellationToken` is cancelled.  

###### Remarks
Initially, sends only `cacheIdentifier` to NodeJS. NodeJS reuses the module if it's already cached. Otherwise, it informs the .NET process that the module isn't cached. 
The .NET process then creates the module stream using `moduleFactory` and send it to NodeJS where it's compiled, invoked and cached.  

If `exportName` is `null`, `module.exports` is assumed to be a function and is invoked. Otherwise, invokes the function named `exportName` in `module.exports`.  
##### INodeJSService.TryInvokeFromCacheAsync&lt;T&gt;(string, string, object[], CancellationToken)
Attempts to invoke a function from a module in NodeJS's cache.  
```csharp
Task<(bool, T?)> TryInvokeFromCacheAsync<T>(string cacheIdentifier, [string? exportName = null], [object?[]? args = null], [CancellationToken cancellationToken = default(CancellationToken)])
```
###### Type Parameters
`T`  
The type of value returned. This may be a JSON-serializable type, `string`, or `Stream`.  
###### Parameters
cacheIdentifier `string`  
The module's cache identifier. This value must not be `null`.  

exportName `string`  
The name of the function in `module.exports` to invoke. If this value is `null`, `module.exports` is assumed to be a function and is invoked.  

args `object[]`  
The sequence of JSON-serializable arguments to pass to the function to invoke. If this value is `null`, no arguments are passed.  

cancellationToken `CancellationToken`  
The cancellation token for the asynchronous operation.  
###### Returns
The `Task` representing the asynchronous operation. On completion, the task returns a (bool, T) with the bool set to true on 
 success and false otherwise.  
###### Exceptions
`ArgumentNullException`  
Thrown if `cacheIdentifier` is `null`.  

`ConnectionException`  
Thrown if unable to connect to NodeJS.  

`InvocationException`  
Thrown if the invocation request times out.  

`InvocationException`  
Thrown if a NodeJS error occurs.  

`ObjectDisposedException`  
Thrown if this instance is disposed or if it attempts to use a disposed dependency.  

`OperationCanceledException`  
Thrown if `cancellationToken` is cancelled.  

###### Example

 Using the class `Result`:
 ```csharp
public class Result
 {
     public string? Message { get; set; }
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
 (bool success, Result? result) = await nodeJSService.TryInvokeFromCacheAsync<Result>(cacheIdentifier, args: new[] { "success" });

 Assert.True(success);
 Assert.Equal("success", result?.Message);
```  
##### INodeJSService.TryInvokeFromCacheAsync(string, string, object[], CancellationToken)
Attempts to invoke a function from a module in NodeJS's cache.  
```csharp
Task<bool> TryInvokeFromCacheAsync(string cacheIdentifier, [string? exportName = null], [object?[]? args = null], [CancellationToken cancellationToken = default(CancellationToken)])
```
###### Parameters
cacheIdentifier `string`  
The module's cache identifier. This value must not be `null`.  

exportName `string`  
The name of the function in `module.exports` to invoke. If this value is `null`, `module.exports` is assumed to be a function and is invoked.  

args `object[]`  
The sequence of JSON-serializable arguments to pass to the function to invoke. If this value is `null`, no arguments are passed.  

cancellationToken `CancellationToken`  
The cancellation token for the asynchronous operation.  
###### Returns
The `Task` representing the asynchronous operation. On completion, the task returns true on success and false otherwise.  
###### Exceptions
`ArgumentNullException`  
Thrown if `cacheIdentifier` is `null`.  

`ConnectionException`  
Thrown if unable to connect to NodeJS.  

`InvocationException`  
Thrown if the invocation request times out.  

`InvocationException`  
Thrown if a NodeJS error occurs.  

`ObjectDisposedException`  
Thrown if this instance is disposed or if it attempts to use a disposed dependency.  

`OperationCanceledException`  
Thrown if `cancellationToken` is cancelled.  

##### INodeJSService.MoveToNewProcess()
Moves subsequent invocations to a new NodeJS process.  
```csharp
void MoveToNewProcess()
```
###### Remarks
This method exposes the system used by file watching (see `OutOfProcessNodeJSServiceOptions.EnableFileWatching`) and process retries 
(see `OutOfProcessNodeJSServiceOptions.NumProcessRetries`) to move to new processes.  

When is access to this system useful? Consider the situation where your application uses file watching.
If your application knows when files change (e.g. your application is the actor changing files) you can manually invoke this method instead of using file 
watching. This enables you to avoid the overhead of file watching.  

The method respects `OutOfProcessNodeJSServiceOptions.GracefulProcessShutdown`.  
<!-- INodeJSService generated docs -->
<!-- NodeJSProcessOptions generated docs -->

### NodeJSProcessOptions Class
#### Constructors
##### NodeJSProcessOptions()
```csharp
public NodeJSProcessOptions()
```
#### Properties
##### NodeJSProcessOptions.ProjectPath
The base path for resolving NodeJS module paths.  
```csharp
public string ProjectPath { get; set; }
```
###### Remarks
If this value is `null`, whitespace or an empty string and the application is an ASP.NET Core application, 
project path is `IHostingEnvironment.ContentRootPath`.  
##### NodeJSProcessOptions.ExecutablePath
The value used to locate the NodeJS executable.  
```csharp
public string? ExecutablePath { get; set; }
```
###### Remarks
This value may be an absolute path, a relative path, or a file name.  

If this value is a relative path, the executable's path is resolved relative to `Directory.GetCurrentDirectory`.  

If this value is a file name, the executable's path is resolved using the path environment variable.  

If this value is `null`, whitespace or an empty string, it is overridden with the file name "node".  

Defaults to `null`.  
##### NodeJSProcessOptions.NodeAndV8Options
NodeJS and V8 options in the form &lt;NodeJS options&gt; &lt;V8 options&gt;.  
```csharp
public string? NodeAndV8Options { get; set; }
```
###### Remarks
You can find the full list of NodeJS options [here](https://nodejs.org/api/cli.html#cli_options).  
##### NodeJSProcessOptions.Port
The NodeJS server will listen on this port.  
```csharp
public int Port { get; set; }
```
###### Remarks
If this value is 0, the OS will choose the port.  

Defaults to 0.  
##### NodeJSProcessOptions.EnvironmentVariables
The NodeJS process's environment variables.  
```csharp
public IDictionary<string, string> EnvironmentVariables { get; set; }
```
###### Remarks
You can configure NodeJS by specifying environment variables for it. Find the full list of environment variables [here](https://nodejs.org/api/cli.html#cli_environment_variables).  

If this value doesn't contain an element with key "NODE_ENV" and the application is an ASP.NET Core application,
an element with key "NODE_ENV" is added. The added element's value is "development" if `IHostingEnvironment.EnvironmentName` is `EnvironmentName.Development`,
and "production" otherwise.  
<!-- NodeJSProcessOptions generated docs -->
<!-- OutOfProcessNodeJSServiceOptions generated docs -->

### OutOfProcessNodeJSServiceOptions Class
#### Constructors
##### OutOfProcessNodeJSServiceOptions()
```csharp
public OutOfProcessNodeJSServiceOptions()
```
#### Properties
##### OutOfProcessNodeJSServiceOptions.TimeoutMS
The maximum duration to wait for the NodeJS process to connect and to wait for responses to invocations.  
```csharp
public int TimeoutMS { get; set; }
```
###### Remarks
If this value is negative, the maximum duration is infinite.  

Defaults to 3000.  
##### OutOfProcessNodeJSServiceOptions.NumRetries
The number of times a NodeJS process retries an invocation.  
```csharp
public int NumRetries { get; set; }
```
###### Remarks
If this value is negative, invocations are retried indefinitely.  

If an invocation's module source is an unseekable stream, the invocation is not retried.
If you require retries for such streams, copy their contents to a `MemoryStream`.  

Defaults to 1.  
##### OutOfProcessNodeJSServiceOptions.NumProcessRetries
The number of NodeJS processes created to retry an invocation.  
```csharp
public int NumProcessRetries { get; set; }
```
###### Remarks
A NodeJS process retries invocations `OutOfProcessNodeJSServiceOptions.NumRetries` times. Once a process's retries are exhausted,
if any retry-processes remain, the library creates a new process and retries invocations `OutOfProcessNodeJSServiceOptions.NumRetries` times.  

For example, consider the situation where `OutOfProcessNodeJSServiceOptions.NumRetries` and this value are both 1. The existing process first attempts the invocation.
If it fails, it retries the invocation once. If it fails again, the library creates a new process that retries the invocation once. In total, the library
attempts the invocation 3 times.  

If this value is negative, the library creates new NodeJS processes indefinitely.  

If this value is larger than 0 and `OutOfProcessNodeJSServiceOptions.NumRetries` is 0, the invocation is retried once in each new process.  

By default, process retries are disabled for invocation failures caused by javascript errors. See `OutOfProcessNodeJSServiceOptions.EnableProcessRetriesForJavascriptErrors` for more information.  

If the module source of an invocation is an unseekable stream, the invocation is not retried.
If you require retries for such streams, copy their contents to a `MemoryStream`.  

Defaults to 1.  
##### OutOfProcessNodeJSServiceOptions.EnableProcessRetriesForJavascriptErrors
Whether invocation failures caused by Javascript errors are retried in new processes.  
```csharp
public bool EnableProcessRetriesForJavascriptErrors { get; set; }
```
###### Remarks
Process retries were introduced to deal with process-level issues. For example, when a NodeJS process becomes unresponsive the only solution is to start a new process.  

If this value is `true`, process retries also occur on Javascript errors. If it is `false`, they only occur for process-level issues.  

Defaults to `false`.  
##### OutOfProcessNodeJSServiceOptions.NumConnectionRetries
Number of times the library retries NodeJS connection attempts.  
```csharp
public int NumConnectionRetries { get; set; }
```
###### Remarks
If this value is negative, connection attempts are retried indefinitely.  

Defaults to 2.  
##### OutOfProcessNodeJSServiceOptions.Concurrency
The concurrency mode for invocations.  
```csharp
public Concurrency Concurrency { get; set; }
```
###### Remarks
By default, this value is `Concurrency.None`. In this mode, a single NodeJS process executes invocations synchronously. 
This mode has the benefit of lower memory overhead and it supports all modules. However, it is less performant.  

If this value is `Concurrency.MultiProcess`, `OutOfProcessNodeJSServiceOptions.Concurrency` NodeJS processes are created and invocations are
distributed among them using round robin load balancing. This mode is more performant. However, it has higher memory overhead and doesn't work with modules that 
have persistent state.  

Defaults to `Concurrency.None`.  
##### OutOfProcessNodeJSServiceOptions.ConcurrencyDegree
The concurrency degree.  
```csharp
public int ConcurrencyDegree { get; set; }
```
###### Remarks
If `OutOfProcessNodeJSServiceOptions.Concurrency` is `Concurrency.MultiProcess`, this value is the number of NodeJS processes.  

If this value is less than or equal to 0, concurrency degree is the number of logical processors the current machine has.  

This value does nothing if `OutOfProcessNodeJSServiceOptions.Concurrency` is `Concurrency.None`.  

Defaults to 0.  
##### OutOfProcessNodeJSServiceOptions.EnableFileWatching
The value specifying whether file watching is enabled.  
```csharp
public bool EnableFileWatching { get; set; }
```
###### Remarks
If file watching is enabled, the library watches files in `OutOfProcessNodeJSServiceOptions.WatchPath` with file name matching a pattern in `OutOfProcessNodeJSServiceOptions.WatchFileNamePatterns`. 
The library restarts NodeJS when a watched file changes.  

Works with all `OutOfProcessNodeJSServiceOptions.Concurrency` modes.  

Defaults to `false`.  
##### OutOfProcessNodeJSServiceOptions.WatchPath
The directory to watch for file changes.  
```csharp
public string? WatchPath { get; set; }
```
###### Remarks
If this value is `null`, the path `NodeJSProcessOptions.ProjectPath` is watched.  

This value does nothing if `OutOfProcessNodeJSServiceOptions.EnableFileWatching` is `false`.  

Defaults to `null`  
##### OutOfProcessNodeJSServiceOptions.WatchSubdirectories
The value specifying whether subdirectories of `OutOfProcessNodeJSServiceOptions.WatchPath` are watched.  
```csharp
public bool WatchSubdirectories { get; set; }
```
###### Remarks
This value does nothing if `OutOfProcessNodeJSServiceOptions.EnableFileWatching` is `false`.  

Defaults to `true`.  
##### OutOfProcessNodeJSServiceOptions.WatchFileNamePatterns
The file name patterns to watch.  
```csharp
public IEnumerable<string> WatchFileNamePatterns { get; set; }
```
###### Remarks
In a pattern, "*" represents 0 or more of any character and "?" represents 0 or 1 of any character. For example,
"TestFile1.js" matches the pattern "*File?.js".  

This value does nothing if `OutOfProcessNodeJSServiceOptions.EnableFileWatching` is `false`.  

Defaults to "*.js", "*.jsx", "*.ts", "*.tsx", "*.json" and "*.html".  
##### OutOfProcessNodeJSServiceOptions.GracefulProcessShutdown
The value specifying whether NodeJS processes shutdown gracefully when moving to a new process.  
```csharp
public bool GracefulProcessShutdown { get; set; }
```
###### Remarks
If this value is true, NodeJS processes shutdown gracefully. Otherwise they're killed immediately.  

What's a graceful shutdown? When the library creates a new NodeJS process, the old NodeJS process
might still be handling earlier invocations. If graceful shutdown is enabled, the old NodeJS process is killed after its
invocations complete. If graceful shutdown is disabled, the old NodeJS process is killed immediately and existing
invocations are retried in the new NodeJS process (assuming they have remaining retries, see `OutOfProcessNodeJSServiceOptions.NumRetries`).  

Should I use graceful shutdown? Shutting down gracefully is safer: chances of an invocation exhausting retries and failing is lower, also,
you won't face issues from an invocation terminating midway. However, graceful shutdown does incur a small performance cost.
Also, invocations complete using the outdated version of your script. Weigh these factors for your script and use-case to decide whether to use graceful shutdown.  

This value does nothing if `OutOfProcessNodeJSServiceOptions.EnableFileWatching` is `false` and `OutOfProcessNodeJSServiceOptions.NumProcessRetries` is 0.  

Defaults to `true`.  
<!-- OutOfProcessNodeJSServiceOptions generated docs -->
<!-- HttpNodeJSServiceOptions generated docs -->

### HttpNodeJSServiceOptions Class
#### Constructors
##### HttpNodeJSServiceOptions()
```csharp
public HttpNodeJSServiceOptions()
```
#### Properties
##### HttpNodeJSServiceOptions.Version
The HTTP version to use.  
```csharp
public Version Version { get; set; }
```
###### Remarks
This value can be `HttpVersion.Version11` or `HttpVersion.Version20`. `HttpVersion.Version11` is faster than `HttpVersion.Version20`, 
but `HttpVersion.Version20` may be more stable (unverified).  

If this value is not `HttpVersion.Version11` or `HttpVersion.Version20`, `HttpVersion.Version11` is used.  

This option is not available for the net461 and netstandard2.0 versions of this library because those framework versions do not support HTTP/2.0.  

Defaults to `HttpVersion.Version11`.  
<!-- HttpNodeJSServiceOptions generated docs -->

## Performance
These benchmarks compare modes offered by this library and Microsoft's [`INodeServices`](https://github.com/aspnet/JavaScriptServices/tree/master/src/Microsoft.AspNetCore.NodeServices).

### Latency
Inter-process communication latency benchmarks (1 invocation per iteration):

|                                                        Method |     Mean |   Error |  StdDev |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|-------------------------------------------------------------- |---------:|--------:|--------:|-------:|------:|------:|----------:|
|                         INodeJSService_Latency_InvokeFromFile | 105.7 μs | 1.59 μs | 1.48 μs | 1.2207 |     - |     - |   5.18 KB |
| INodeJSService_Latency_InvokeFromFile_GracefulShutdownEnabled | 106.9 μs | 0.54 μs | 0.43 μs | 1.2207 |     - |     - |    5.4 KB |
|                        INodeJSService_Latency_InvokeFromCache | 103.8 μs | 0.56 μs | 0.53 μs | 1.2207 |     - |     - |   5.25 KB |
|                                         INodeServices_Latency | 117.4 μs | 1.73 μs | 1.54 μs | 2.4414 |     - |     - |   9.66 KB |

```
NodeJS v12.18.3
BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19041.985 (2004/?/20H1)
Intel Core i7-7700 CPU 3.60GHz (Kaby Lake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=5.0.300-preview.21180.15
  [Host]     : .NET Core 5.0.6 (CoreCLR 5.0.621.22011, CoreFX 5.0.621.22011), X64 RyuJIT
  DefaultJob : .NET Core 5.0.6 (CoreCLR 5.0.621.22011, CoreFX 5.0.621.22011), X64 RyuJIT
```

View source [here](https://github.com/JeringTech/Javascript.NodeJS/blob/master/perf/NodeJS/LatencyBenchmarks.cs).

### Multi-Process Concurrency
<!-- TODO benchmark on how graceful shutdown (and thus invoke task tracking) affects concurrency -->
Asynchronous invocations benchmarks (25 invocations per iteration):

|                                  Method |       Mean |   Error |  StdDev | Gen 0 | Gen 1 | Gen 2 | Allocated |
|---------------------------------------- |-----------:|--------:|--------:|------:|------:|------:|----------:|
| INodeJSService_Concurrency_MultiProcess |   400.3 ms | 0.60 ms | 0.47 ms |     - |     - |     - | 120.75 KB |
|         INodeJSService_Concurrency_None | 2,500.0 ms | 1.66 ms | 1.55 ms |     - |     - |     - | 123.38 KB |
|               INodeServices_Concurrency | 2,500.3 ms | 0.48 ms | 0.40 ms |     - |     - |     - | 237.77 KB |

```
NodeJS v12.18.3
BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19041.985 (2004/?/20H1)
Intel Core i7-7700 CPU 3.60GHz (Kaby Lake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=5.0.300-preview.21180.15
  [Host]     : .NET Core 5.0.6 (CoreCLR 5.0.621.22011, CoreFX 5.0.621.22011), X64 RyuJIT
  Job-DXCSVX : .NET Core 5.0.6 (CoreCLR 5.0.621.22011, CoreFX 5.0.621.22011), X64 RyuJIT
InvocationCount=1  UnrollFactor=1  
```

View source [here](https://github.com/JeringTech/Javascript.NodeJS/blob/master/perf/NodeJS/ConcurrencyBenchmarks.cs).

### Real Workload
Real world benchmarks. These use the syntax highlighter, Prism, to highlight C# (25 invocations per iteration):

|                      Method |     Mean |     Error |    StdDev |   Median | Gen 0 | Gen 1 | Gen 2 | Allocated |
|---------------------------- |---------:|----------:|----------:|---------:|------:|------:|------:|----------:|
| INodeJSService_RealWorkload | 2.269 ms | 0.1627 ms | 0.4535 ms | 2.133 ms |     - |     - |     - | 213.17 KB |
|  INodeServices_RealWorkload | 5.352 ms | 0.3976 ms | 1.1343 ms | 5.252 ms |     - |     - |     - | 270.98 KB |

```
NodeJS v12.18.3
BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19041.985 (2004/?/20H1)
Intel Core i7-7700 CPU 3.60GHz (Kaby Lake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=5.0.300-preview.21180.15
  [Host]     : .NET Core 5.0.6 (CoreCLR 5.0.621.22011, CoreFX 5.0.621.22011), X64 RyuJIT
  Job-DXJFJI : .NET Core 5.0.6 (CoreCLR 5.0.621.22011, CoreFX 5.0.621.22011), X64 RyuJIT
```

View source [here](https://github.com/JeringTech/Javascript.NodeJS/blob/master/perf/NodeJS/RealWorkloadBenchmarks.cs).

### File Watching
<!-- TODO these don't consider situations with in-progess invocations -->
How long it takes for NodeJS to restart and begin processing invocations (1 process swap per iteration):

|                                                                   Method |     Mean |    Error |   StdDev | Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------------------------------------------------------------------- |---------:|---------:|---------:|------:|------:|------:|----------:|
|  HttpNodeJSService_FileWatching_GracefulShutdownEnabled_MoveToNewProcess | 64.96 ms | 0.253 ms | 0.224 ms |     - |     - |     - | 253.43 KB |
| HttpNodeJSService_FileWatching_GracefulShutdownDisabled_MoveToNewProcess | 64.99 ms | 0.191 ms | 0.160 ms |     - |     - |     - | 252.95 KB |

```
NodeJS v12.18.3
BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19041.985 (2004/?/20H1)
Intel Core i7-7700 CPU 3.60GHz (Kaby Lake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=5.0.300-preview.21180.15
  [Host]     : .NET Core 5.0.6 (CoreCLR 5.0.621.22011, CoreFX 5.0.621.22011), X64 RyuJIT
  DefaultJob : .NET Core 5.0.6 (CoreCLR 5.0.621.22011, CoreFX 5.0.621.22011), X64 RyuJIT
```

View source [here](https://github.com/JeringTech/Javascript.NodeJS/blob/master/perf/NodeJS/FileWatchingBenchmarks.cs).

## Building and Testing
You can build and test this project in Visual Studio 2019/2022.  

This project uses [source generators](https://devblogs.microsoft.com/dotnet/introducing-c-source-generators/). They have a critical limitation - Visual Studio only loads 
source generator dlls once, at startup.  

This means for builds to succeed, you have to: 

1. Build the solution once (outputs the source generator project dlls)
2. Restart Visual Studio 
3. Rebuild (this should succeed)

Thereafter, if you make changes to the source generator projects, you'll have to build, restart Visual Studio, and rebuild.

## Projects Using this Library
[Jering.Web.SyntaxHighlighters.HighlightJS](https://github.com/JeringTech/Web.SyntaxHighlighters.HighlightJS) - Use the Syntax Highlighter, HighlightJS, from C#.
[Jering.Web.SyntaxHighlighters.Prism](https://github.com/JeringTech/Web.SyntaxHighlighters.Prism) - Use the Syntax Highlighter, Prism, from C#.  
[NodeReact.NET](https://github.com/DaniilSokolyuk/NodeReact.NET) - Library to render React library components on the server-side with C# as well as on the client.

## Related Concepts

### What is NodeJS?
[NodeJS](https://nodejs.org/en/) is a javascript runtime. Essentially, it provides built-in libraries for interfacing with the operating system (OS) and it executes javascript.
Built-in ibraries include [fs](https://nodejs.org/api/fs.html) for interfacing with the file system and [http](https://nodejs.org/api/http.html) for interfacing with 
with sockets.

Similarities can be drawn to the [Core Common Language Runtime (CoreCLR)](https://github.com/dotnet/coreclr), which provides a set of base libraries and executes [.NET Intermediate Language](https://en.wikipedia.org/wiki/Common_Intermediate_Language) (typically generated by compiling C# or some other .NET language).  

### When Should I Use NodeJS?
Use NodeJS when you're writing javascript that interfaces with the OS. This includes when you use a library, e.g. from [npm](https://www.npmjs.com/), that interfaces with the OS. 

Under the hood, NodeJS uses [V8](https://developers.google.com/v8/) to execute javascript. If you logic doesn't interface with the OS, you can use V8 directly through
an alternative library.

### NodeJS Modules
*Javascript* modules can seem like a complicated topic,
not least because of the existence of competing specifications (CommonJS, AMD, ES6, ...), and the existence of multiple implementations of each specification (SystemJS, RequireJS, 
Dojo, NodeJS, ...). In reality, javascript modules are simple.  

In the following sections, we'll explain the basics of javascript modules. In particular, we'll look at NodeJS modules, a type of javascript module.

#### What is a NodeJS Module?
The following line is a valid NodeJS module:
```javascript
// Note that the module variable isn't declared (no "var module = ...")
module.exports = ['chocolate', 'strawberry', 'vanilla'];
```
Let's imagine that the module above exists in the file C:/NodeJS_Modules_Example/flavours.js.

The following is another valid NodeJS module:
```javascript
var flavours = require('./flavours.js');

flavours.forEach((flavour) => console.log(flavour));
```
Let's imagine that it exists in C:/NodeJS_Modules_Example/printer.js:

If we run `node printer.js` on the command line, the flavours are printed:
```powershell
PS C:\NodeJS_Modules_Example> node printer.js
chocolate
strawberry
vanilla
```

A NodeJS module is simply a block of javascript with `module.exports` and/or `require` statements. These statements are explained in the next section.

#### How does a NodeJS Module Work?
Consider the first module we described above. To *load* it, NodeJS first wraps it:

```javascript
// Note how the module object is supplied by the wrapper.
function (exports, require, module, __filename, __dirname) {
    module.exports = ['chocolate', 'strawberry', 'vanilla'];
}
```

Next, NodeJS invokes the generated function, passing a newly created `module` object (plain javascript object) to it.  
The module sets `module.exports` to `['chocolate', 'strawberry', 'vanilla']` and returns.  

After the function returns, NodeJS caches the `module` object in a simple map, using the module's absolute path, C:/NodeJS_Modules_Example/flavours.js, as cache identifier.
Once the `module` object is cached, the module is considered to be *loaded*.

Consider the second module we described above. To *load* it, NodeJS first wraps it:

```javascript
function (exports, require, module, __filename, __dirname) {
    // Note how the require function is supplied by the wrapper.
    var flavours = require('./flavours.js');

    flavours.forEach((flavour) => console.log(flavour));
}
```

Next, NodeJS invokes the generated function, passing a `require` function to it.
`require('./flavours.js')` does the following:

- Resolves the path ./flavours.js to C:/NodeJS_Modules_Example/flavours.js.
- Looks for a `module` object with cache identifier C:/NodeJS_Modules_Example/flavours.js in its module cache.
- If the flavours.js module is already cached, returns `module.exports`.
- Otherwise, loads the flavours.js module and returns `module.exports`.

`require('./flavours.js')` eventually returns `['chocolate', 'strawberry', 'vanilla']`. The printer.js module then prints the contents
of the array and returns. Note that the printer.js module receives a `module` object but does not set its `exports` property.
The `module` object is still cached, at which point the printer.js module is considered to be *loaded*.  

To further illustrate caching of `module` objects, consider the following example:

```javascript
var flavours = require('./flavours.js');

flavours.forEach((flavour) => console.log(flavour));

// Clear the array
flavours.length = 0;

// Add three new flavours
flavours.push('apple');
flavours.push('green tea');
flavours.push('sea salt');

// Require the module again, require returns a reference to the same array (module only ever runs once)
flavours = require('./flavours.js');

flavours.forEach((flavour) => console.log(flavour));
```

Running `node printer.js` on the command line prints all of the flavours since `require` returns the same array both times:

```powershell
PS C:\Users\Jeremy\Desktop\JSTest> node entry.js
chocolate
strawberry
vanilla
apple
green tea
sea salt
```

In summary, NodeJS modules work by creating closures around logic. Why do that? We'll explain in the next section.  

#### Why do NodeJS Modules exist?
To answer this question, let's consider the impetus for the creation of javascript modules in general. Web pages used to include scripts like so:
``` html
<html>
    ...
    <script type="text/javascript" src="path/to/coolLibrary.js"></script>
    <script type="text/javascript" src="path/to/myScript.js"></script>
    ...
</html>
```
Browsers loaded the scripts like so:
```javascript
// Contents of coolLibrary.js
var somePrivateObject = ...;
var usefulFunction = function() {
    ...
}

// Contents of myScript.js
var somePrivateObject = ...;
usefulFunction();
```

Note how the variable `somePrivateObject` collides. How can we prevent the collision? We can wrap the scripts in functions:

```javascript
var module = {};

// This is an immediately invoked function expression, shorthand for assigning the function to a variable then calling it - https://developer.mozilla.org/en-US/docs/Glossary/IIFE
(function(module){
    // Contents of coolLibrary.js
    var somePrivateObject = ...;
    var usefulFunction = function() {
        ...
    }
    
    module.exports = usefulFunction;
})(module)

// Contents of myScript.js
var somePrivateObject = ...;
module.usefulFunction();
```
We've successfully hidden coolLibrary's `somePrivateObject` variable from the global scope using a module-esque pattern.  

NodeJS modules exist to serve a similar purpose. By wrapping modules in functions, NodeJS creates a closure for each module so internal details
can be kept private.  

## Contributing
Contributions are welcome!

### Contributors
- [JeremyTCD](https://github.com/JeremyTCD)
- [Daniil Sokolyuk](https://github.com/DaniilSokolyuk)
- [dustinsoftware](https://github.com/dustinsoftware)
- [blushingpenguin](https://github.com/blushingpenguin)
- [flcdrg](https://github.com/flcdrg)
- [samcic](https://github.com/samcic)
- [johnrom](https://github.com/johnrom)
- [aKzenT](https://github.com/aKzenT)

## About
Follow [@JeringTech](https://twitter.com/JeringTech) for updates and more.
