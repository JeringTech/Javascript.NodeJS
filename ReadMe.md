# Jering.JavascriptUtils.NodeJS
[![Build status](https://ci.appveyor.com/api/projects/status/wawhrh1nvy5fae2s?svg=true)](https://ci.appveyor.com/project/JeremyTCD/javascriptutils-nodejs)
[![License](https://img.shields.io/badge/license-Apache%202.0-blue.svg)](https://github.com/Pkcs11Interop/Pkcs11Interop/blob/master/LICENSE.md)
[![NuGet](https://img.shields.io/nuget/vpre/Jering.JavascriptUtils.NodeJS.svg?label=nuget)](https://www.nuget.org/packages/Jering.JavascriptUtils.NodeJS/)
<!-- TODO tests badge, this service should work - https://github.com/monkey3310/appveyor-shields-badges/blob/master/README.md -->

Invoke Both In-Memory and File Based Javascript from .Net Applications.

## Table of Contents
[Overview](#Overview)  
[Prerequisites](#Prerequisites)  
[Installation](#Installation)  
[Concepts](#Concepts)  
[Usage](#Usage)  
[Extensibility](#Extensibility)  
[Performance](#Performance)  
[Building](#Building)  
[Contributing](#Contributing)  
[About](#About)  

## Overview
This library provides ways to invoke javascript in [NodeJS](https://nodejs.org/en/), from .Net applications. Javascript in `string` or 
`Stream` form (in-memory) or in `.js` files on disk can be invoked.

## Prerequisites
[NodeJS](https://nodejs.org/en/) must be installed and node.exe's directory must be added to the `Path` environment variable.

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
NodeJS is a javascript runtime. Essentially, it provides some built-in libraries and executes javascript. Similarities can be drawn to the
[Core Common Language Runtime (CoreCLR)](https://github.com/dotnet/coreclr), which provides a set of base libraries and executes C#. Under the hood,
NodeJS uses [V8](https://developers.google.com/v8/) to execute javascript. While this library could have been built to invoke javascript directly in V8,
invoking javascript in NodeJS affords a host of extra built-in libraries as well as compatibility with many libraries hosted by [npm](https://www.npmjs.com/).

### NodeJS Modules
A javascript module is a unit of javascript. Javascript modules can seem far more complicated than they really are. This is due in part to the existence of multiple competing specifications (CommonJS,
AMD, ES6), as well as the existence of multiple implementations of each specification (SystemJS, RequireJS, Dojo, NodeJS). We will be looking at NodeJS modules,
an implementation of the CommonJS standard used by NodeJS, and we will see just how simple javascript modules really are.

#### Basic NodeJS Modules
The following is a basic NodeJS module. Lets imagine that it exists in a file, `flavours.js`:
```javascript
module.exports = ['chocolate', 'strawberry', 'vanilla'];
```
We will use the following as an entry module (to be supplied to `node.exe`). Lets imagine that it exists in a file, `entry.js`, in the same directory as `flavours.js`:
```javascript
var flavours = require('./flavours.js');

flavours.forEach((flavour) => console.log(flavour));
```
If we run `node.exe entry.js` in the command line, we get the following console output:
```powershell
PS C:\NodeJS_Modules_Example> node entry.js
chocolate
strawberry
vanilla
```

<!-- what are nodejs modules? any logic + require/module.exports -->

#### The `require` Function
In the example above, `require('./flavours.js')` executes the following steps:
- Resolves the absolute path of `flavours.js` by combining `C:/NodeJS_Modules_Example/entry.js` with `./flavours.js`.
- Checks whether the NodeJS module cache has a key-value pair with key `C:/NodeJS_Modules_Example/entry.js`, and finds that the module has not been cached
  (elaborated on in [Module Caching](#module-caching)).
- Reads `C:/NodeJS_Modules_Example/flavours.js` into memory.
- Wraps the contents of `C:/NodeJS_Modules_Example/flavour.js` in a function by appending and prepending strings. The resulting function looks like the following:
  ```javascript
  // Note how the require function and a module object are supplied by the wrapper.
  function (exports, require, module, __filename, __dirname){
      module.exports = ['chocolate', 'strawberry', 'vanilla'];
  }
  ```
- Creats the `module` object and passes it to the generated function, thereby defining `module.exports`.
- Caches the `module` object using the key `C:/NodeJS_Modules_Example/flavours.js` (the absolute path of the javascript file).
- Returns `module.exports`.

#### Module Caching
```javascript
var flavours = require('./flavours.js');

flavours.forEach((flavour) => console.log(flavour));
flavours.add('apple');

var flavours = require('./flavours.js');

flavours.forEach((flavour) => console.log(flavour));
```


<!-- module exports are cached and aren't immutable -->
<!-- require flavours, edit it, require it again -->

### What is the Point of Modules?

in the early days of the web, people just included scripts. this meant that the combined scripts in a page could look like the following: *
so people decided, lets use closures to make stuff private, and export only public stuff, and they called their creation modules. so modules
are basically for encapsulation. the end.

<!-- we now know how modules work, but what is the point of modules? -->
<!-- what is the point of this? lets make printer a module that chooses the flavour based on day of the week, new entry, now notice how the full array of flavours and the logic required to choose
the flavour are encapsulated? this is what modules allow for,  encapsulation through the use of closures. -->

## Usage
### Creating INodeJSService

In non-ASP.NET projects, you'll have to create your own DI container. For example, using [Microsoft.Extensions.DependencyInjection](https://github.com/aspnet/DependencyInjection):
```csharp
var services = new ServiceCollection();
services.AddHighlightJS();
ServiceProvider serviceProvider = services.BuildServiceProvider();
IHighlightJSService highlightJSService = serviceProvider.GetRequiredService<IHighlightJSService>();
```
`IHighlightJSService` is a singleton service and `IHighlightJSService`'s members are thread safe.
Where possible, inject `IHighlightJSService` into your types or keep a reference to a shared `IHighlightJSService` instance. 
Try to avoid creating multiple `IHighlightJSService` instances, since each instance spawns a NodeJS process. 

When you're done, you can manually dispose of an `IHighlightJSService` instance by calling
```csharp
highlightJSService.Dispose();
```
or 
```csharp
serviceProvider.Dispose(); // Calls Dispose on objects it has instantiated that are disposable
```
`Dispose` kills the spawned NodeJS process.
Note that even if `Dispose` isn't called manually, the service that manages the NodeJS process, `INodeJSService` from [Jering.JavascriptUtils.NodeJS](https://github.com/JeremyTCD/JavascriptUtils.NodeJS), will kill the 
NodeJS process when the application shuts down - if the application shuts down gracefully. If the application does not shutdown gracefully, the NodeJS process will kill 
itself when it detects that its parent has been killed. 
Essentially, manually disposing of `IHighlightJSService` instances is not mandatory.

### API
#### IHighlightJSService.HighlightAsync
##### Signature
```csharp
Task<string> HighlightAsync(string code, string languageAlias, string classPrefix = "hljs-")
```
##### Description
Highlights code of a specified language.
##### Parameters
- `code`
  - Type: `string`
  - Description: Code to highlight.
- `languageAlias`
  - Type: `string`
  - Description: A HighlightJS language alias. Visit http://highlightjs.readthedocs.io/en/latest/css-classes-reference.html#language-names-and-aliases for the list of valid language aliases.
- `classPrefix`
  - Type: `string`
  - Description: If not null or whitespace, this string will be appended to HighlightJS classes. Defaults to `hljs-`.
##### Returns
Highlighted code.
##### Exceptions
- `ArgumentNullException`
  - Thrown if `code` is null.
- `ArgumentException`
  - Thrown if `languageAlias` is not a valid HighlightJS language alias.
- `InvocationException`
  - Thrown if a NodeJS error occurs.
##### Example
```csharp
string code = @"public string ExampleFunction(string arg)
{
    // Example comment
    return arg + ""dummyString"";
}";

string highlightedCode = await highlightJSService.HighlightAsync(code, "csharp");
```
#### IHighlightJSService.IsValidLanguageAliasAsync
##### Signature
```csharp
ValueTask<bool> IsValidLanguageAliasAsync(string languageAlias)
```
##### Description
Determines whether a language alias is valid.
##### Parameters
- `languageAlias`
  - Type: `string`
  - Description: Language alias to validate. Visit http://highlightjs.readthedocs.io/en/latest/css-classes-reference.html#language-names-and-aliases for the list of valid language aliases.
##### Returns
`true` if `languageAlias` is a valid HighlightJS language alias. Otherwise, `false`.
##### Exceptions
- `InvocationException`
  - Thrown if a NodeJS error occurs.
##### Example
```csharp
bool isValid = await highlightJSService.IsValidLanguageAliasAsync("csharp");
```

## Extensibility
<!-- overwritable services-->

## Performance
<!-- benchmark results -->

## Building
This project can be built using Visual Studio 2017.

## Contributing
Contributions are welcome!  

## About
Follow [@JeremyTCD](https://twitter.com/JeremyTCD) for updates and more.