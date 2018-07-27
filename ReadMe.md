# Jering.JavascriptUtils.NodeJS
[![Build status](https://ci.appveyor.com/api/projects/status/wawhrh1nvy5fae2s?svg=true)](https://ci.appveyor.com/project/JeremyTCD/javascriptutils-nodejs)
[![License](https://img.shields.io/badge/license-Apache%202.0-blue.svg)](https://github.com/Pkcs11Interop/Pkcs11Interop/blob/master/LICENSE.md)
[![NuGet](https://img.shields.io/nuget/vpre/Jering.JavascriptUtils.NodeJS.svg?label=nuget)](https://www.nuget.org/packages/Jering.JavascriptUtils.NodeJS/)
<!-- TODO tests badge, this service should work - https://github.com/monkey3310/appveyor-shields-badges/blob/master/README.md -->

## Table of Contents
[Overview](#Overview)  
[Prerequisites](#Prerequisites)  
[Installation](#Installation)  
[Concepts](#Concepts)  
[Usage](#Usage)  
[API](#api)  
[Extensibility](#Extensibility)  
[Performance](#Performance)  
[Building](#Building)  
[Contributing](#Contributing)  
[About](#About)  

## Overview
This library provides ways to invoke javascript in [NodeJS](https://nodejs.org/en/), from .Net applications. On top of providing a way to invoke javascript from `.js` files on disk,
this library provides ways to invoke in-memory Javascript in `string` or `Stream` form, as well as logic in the NodeJS cache.

## Prerequisites
NodeJS must be installed and node.exe's directory must be added to the `Path` environment variable.

## Installation
Using Package Manager:
```
PM> Install-Package Jering.JavascriptUtils.NodeJS
```
Using .Net CLI:
```
> dotnet add package Jering.JavascriptUtils.NodeJS
```
## Concepts
Familiarity with the following concepts will make it easier to use this library effectively. I've included this section for the benefit
of those who haven't had much experience with NodeJS, if you're already familiar with the following concepts, feel free to skip this section.

### What is NodeJS?
[NodeJS](https://nodejs.org/en/) is a javascript runtime. Essentially, it provides some built-in libraries and executes javascript. Similarities can be drawn to the
[Core Common Language Runtime (CoreCLR)](https://github.com/dotnet/coreclr), which provides a set of base libraries and executes C#.  

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

Running `node.exe printer.js` in the command line prints the following console output:
```
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

// This is an immediately invoked function expression shorthand for assigning the function to a variable then calling it - https://developer.mozilla.org/en-US/docs/Glossary/IIFE
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
We've successfully hidden `coolLibraryPrivateObject` from the global scope using a module-esque pattern. Apart from hiding private objects, this pattern also prevents the global namespace from being polluted.  

NodeJS modules serve the same purposes. By wrapping modules in functions, NodeJS creates a closure for each module so internal details
can be kept private.

## Usage
### Creating INodeJSService
This library uses depedency injection (DI) to facilitate [extensibility](#extensibility) and testability.
You can use any DI framework that has adapters for [Microsoft.Extensions.DependencyInjection](https://github.com/aspnet/DependencyInjection).
Here, we'll use the vanilla Microsoft.Extensions.DependencyInjection framework:
```csharp
var services = new ServiceCollection();
services.AddNodeJS();
ServiceProvider serviceProvider = services.BuildServiceProvider();
INodeJSService nodeJSService = serviceProvider.GetRequiredService<INodeJSService>();
```
`INodeJSService` is a singleton service and `INodeJSService`'s members are thread safe.
Where possible, inject `INodeJSService` into your types or keep a reference to a shared `INodeJSService` instance. 
Try to avoid creating multiple `INodeJSService` instances since each instance spawns a NodeJS process. 

When you're done, you can manually dispose of an `INodeJSService` instance by calling
```csharp
nodeJSService.Dispose();
```
or 
```csharp
serviceProvider.Dispose(); // Calls Dispose on objects it has instantiated that are disposable
```
`Dispose` kills the spawned NodeJS process.
Note that even if `Dispose` isn't called manually, `INodeJSService` will kill the 
NodeJS process when the application shuts down - if the application shuts down gracefully. If the application doesn't shutdown gracefully, the NodeJS process will kill 
itself when it detects that its parent has been killed. 
Essentially, manually disposing of `INodeJSService` instances is not mandatory.

### Configuring INodeJSService
TODO

### Using INodeJSService
TODO


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
The task object representing the asynchronous operation.
#### Exceptions
- `InvocationException`
  - Thrown if a NodeJS error occurs.
  - Thrown if the invocation request times out.
  - Thrown if NodeJS cannot be initialized.
#### Example
Using exampleModule.js (placed in `NodeJSProcessOptions.ProjectPath`):
```javascript
module.exports = (callback, message) => callback(null, { resultMessage: message });
```
And the class `Result`:
```csharp
public class Result
{
    public string ResultMessage { get; set; }
}
```
The following assertion will pass:
```csharp
Result result = await nodeJSService.InvokeFromFileAsync<Result>("exampleModule.js", args: new[] { "success" });

Assert.Equal("success", result.ResultMessage);
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
  - Description: The cache identifier for the module in the NodeJS module cache. If unspecified, the NodeJS module will not be cached.

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
The task object representing the asynchronous operation.
#### Exceptions
- `InvocationException`
  - Thrown if a NodeJS error occurs.
  - Thrown if the invocation request times out.
  - Thrown if NodeJS cannot be initialized.
#### Example
Using the class `Result`:
```csharp
public class Result
{
    public string ResultMessage { get; set; }
}
```
The following assertion will pass:
```csharp
Result result = await nodeJSService.InvokeFromStringAsync<Result>("module.exports = (callback, message) => callback(null, { resultMessage: message });", 
    args: new[] { "success" });

Assert.Equal("success", result.ResultMessage);
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
  - Description: The cache identifier for the module in the NodeJS module cache. If unspecified, the NodeJS module will not be cached.

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
The task object representing the asynchronous operation.
#### Exceptions
- `InvocationException`
  - Thrown if a NodeJS error occurs.
  - Thrown if the invocation request times out.
  - Thrown if NodeJS cannot be initialized.
#### Example
Using the class `Result`:
```csharp
public class Result
{
    public string ResultMessage { get; set; }
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
    
    Assert.Equal("success", result.ResultMessage);
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
The task object representing the asynchronous operation. On completion, the task returns a `(bool, T)` with the bool set to true on 
success and false otherwise.
#### Exceptions
- `InvocationException`
  - Thrown if a NodeJS error occurs.
  - Thrown if the invocation request times out.
  - Thrown if NodeJS cannot be initialized.
#### Example
Using the class `Result`:
```csharp
public class Result
{
    public string ResultMessage { get; set; }
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
Assert.Equal("success", result.ResultMessage);
```

## Extensibility
TODO

## Performance
This library is heavily inspired by [Microsoft.AspNetCore.NodeServices](https://github.com/aspnet/JavaScriptServices/tree/master/src/Microsoft.AspNetCore.NodeServices). While the main
addition to this library is ways to invoke in-memory javascript, this library does also provide better performance (note that INodeServices has only 1 benchmark because it 
only supports invoking javascript from a file):
<table>
<thead><tr><th>Method</th><th>Mean</th><th>Error</th><th>StdDev</th><th>Gen 0</th><th>Allocated</th></tr></thead>
<tbody>
<tr><td>INodeJSService_InvokeFromCache</td><td>0.1118 ms</td><td>0.001162 ms</td><td>0.001030 ms</td><td>2.3193</td><td>3.33 KB</td></tr>
<tr><td>INodeJSService_InvokeFromFile</td><td>0.1138 ms</td><td>0.001248 ms</td><td>0.001167 ms</td><td>2.1973</td><td>3.4 KB</td></tr>
<tr><td>INodeServices</td><td>0.1334 ms</td><td>0.001488 ms</td><td>0.001391 ms</td><td>1.9531</td><td>4.14 KB</td></tr>
</tbody>
</table>

The [benchmarks](https://github.com/JeremyTCD/JavascriptUtils.NodeJS/blob/master/test/NodeJS.Performance/Benchmarks.cs).

## Building
This project can be built using Visual Studio 2017.

## Related Projects
#### Projects Using this Library
[Jering.WebUtils.SyntaxHighlighters.HighlightJS](https://github.com/JeremyTCD/WebUtils.SyntaxHighlighters.HighlightJS) - A C# Wrapper for the Syntax Highlighter, HighlightJS.  
[Jering.WebUtils.SyntaxHighlighters.Prism](https://github.com/JeremyTCD/WebUtils.SyntaxHighlighters.Prism) - A C# Wrapper for the Syntax Highlighter, Prism.

## Contributing
Contributions are welcome!  

## About
Follow [@JeremyTCD](https://twitter.com/JeremyTCD) for updates and more.