# Changelog
This project uses [semantic versioning](http://semver.org/spec/v2.0.0.html). Refer to 
*[Semantic Versioning in Practice](https://www.jering.tech/articles/semantic-versioning-in-practice)*
for an overview of semantic versioning.

## [Unreleased](https://github.com/JeringTech/Javascript.NodeJS/compare/5.3.0...HEAD)

## [5.3.0](https://github.com/JeringTech/Javascript.NodeJS/compare/5.2.1...5.3.0) - Dec 10, 2019
### Changes
- Jering.Javascript.NodeJS.dll is now strong named. ([#65](https://github.com/JeringTech/Javascript.NodeJS/pull/65)).
- `HttpNodeJSPoolService` round robin logic is now lock free. ([#63](https://github.com/JeringTech/Javascript.NodeJS/pull/63)).

## [5.2.1](https://github.com/JeringTech/Javascript.NodeJS/compare/5.2.0...5.2.1) - Dec 6, 2019
### Fixes
- Improved HTTP connection stability and error logging. ([#61](https://github.com/JeringTech/Javascript.NodeJS/pull/61)).

## [5.2.0](https://github.com/JeringTech/Javascript.NodeJS/compare/5.1.1...5.2.0) - Dec 4, 2019
### Additions
- Expanded API. ([#57](https://github.com/JeringTech/Javascript.NodeJS/pull/57)). Added `INodeJSService` members for invocations without return values and 
  atomic/simplified caching-invoking:
  - `Task InvokeFromFileAsync(string modulePath, string exportName = null, object[] args = null, CancellationToken cancellationToken = default);`
  - `Task InvokeFromStringAsync(string moduleString, string newCacheIdentifier = null, string exportName = null, object[] args = null, CancellationToken cancellationToken = default);`
  - `Task<T> InvokeFromStringAsync<T>(Func<string> moduleFactory, string cacheIdentifier, string exportName = null, object[] args = null, CancellationToken cancellationToken = default);`
  - `Task InvokeFromStringAsync(Func<string> moduleFactory, string cacheIdentifier, string exportName = null, object[] args = null, CancellationToken cancellationToken = default);`
  - `Task InvokeFromStreamAsync(Stream moduleStream, string newCacheIdentifier = null, string exportName = null, object[] args = null, CancellationToken cancellationToken = default);`
  - `Task<T> InvokeFromStreamAsync<T>(Func<Stream> moduleFactory, string cacheIdentifier, string exportName = null, object[] args = null, CancellationToken cancellationToken = default);`
  - `Task InvokeFromStreamAsync(Func<Stream> moduleFactory, string cacheIdentifier, string exportName = null, object[] args = null, CancellationToken cancellationToken = default);`
  - `Task<bool> TryInvokeFromCacheAsync(string moduleCacheIdentifier, string exportName = null, object[] args = null, CancellationToken cancellationToken = default);`

## [5.1.1](https://github.com/JeringTech/Javascript.NodeJS/compare/5.1.0...5.1.1) - Nov 29, 2019
### Fixes
- Fixed requiring of modules from modules in string/stream form. ([#59](https://github.com/JeringTech/Javascript.NodeJS/pull/59))

## [5.1.0](https://github.com/JeringTech/Javascript.NodeJS/compare/5.0.0...5.1.0) - Nov 28, 2019
### Additions
- Added out-of-the-box concurrency. ([#52](https://github.com/JeringTech/Javascript.NodeJS/pull/52))

## [5.0.0](https://github.com/JeringTech/Javascript.NodeJS/compare/4.4.1...5.0.0) - Nov 25, 2019
### Changes
- **Breaking**: We've replaced `IJsonService.Deserialize<T>` and `IJsonService.Serialize` with `IJsonService.DeserializeAsync<T>` and `IJsonService.SerializeAsync<T>` respectively. This
change only matters if you're using a custom implementation of `IJsonService`. ([#53](https://github.com/JeringTech/Javascript.NodeJS/pull/53))
- Switched from `Newtonsoft.Json` to `System.Text.Json` for performance gains. ([#53](https://github.com/JeringTech/Javascript.NodeJS/pull/53))

## [4.4.1](https://github.com/JeringTech/Javascript.NodeJS/compare/4.4.0...4.4.1) - Nov 25, 2019
### Fixes
- Fixed index out of bounds exception thrown when a Javascript log message contains an empty line. ([#55](https://github.com/JeringTech/Javascript.NodeJS/pull/55))

## [4.4.0](https://github.com/JeringTech/Javascript.NodeJS/compare/4.3.0...4.4.0) - Nov 19, 2019
### Additions
- Async Javascript methods are now supported, refer to [ReadMe](https://github.com/JeringTech/Javascript.NodeJS/tree/support-async-javascript-entry-methods#async-function) for more information. ([87bcacf](https://github.com/JeringTech/Javascript.NodeJS/commit/87bcacf1c4b6c170ea211ede162866055d8cd3be))

## [4.3.0](https://github.com/JeringTech/Javascript.NodeJS/compare/4.2.2...4.3.0) - Nov 16, 2019
### Fixes
- Fixed `TypeError [ERR_INVALID_ARG_TYPE]: The “path” argument must be of type string` error when using newer NodeJS versions. ([6cd3b3f](https://github.com/JeringTech/Javascript.NodeJS/commit/6cd3b3f088321e02d51450aebe43a7b55ce3922d))
### Changes
- Bumped several dependencies. ([f9e0dfc](https://github.com/JeringTech/Javascript.NodeJS/commit/f9e0dfcc8b0d6d808c52e8a550d85f81d53ed194))

## [4.2.2](https://github.com/JeringTech/Javascript.NodeJS/compare/4.1.2...4.2.2) - Apr 10, 2019
### Changes
- Bumped several dependencies. ([3a97203](https://github.com/JeringTech/Javascript.NodeJS/commit/3a97203fd25dc232202ff13d19a268df0f5e1a3f))

## [4.1.2](https://github.com/JeringTech/Javascript.NodeJS/compare/4.1.1...4.1.2) - Jan 27, 2019
### Changes
- Simplified NuGet package description. ([219a45c](https://github.com/JeringTech/Javascript.NodeJS/commit/219a45cf04943696afd2094e5866452dd4da7fe7))
### Fixes
- Fixed a `StaticNodeJSService` multi-threading bug. ([028173c](https://github.com/JeringTech/Javascript.NodeJS/commit/028173c26735471fa2158f513f4afbe2c19089d4))

## [4.1.1](https://github.com/JeringTech/Javascript.NodeJS/compare/4.1.0...4.1.1) - Jan 19, 2019
### Changes
- Bumped `Newtonsoft.Json` to `12.0.1`.
### Fixes
- Fixed NuGet package's `PackageLicenseUrl` metadata.

## [4.1.0](https://github.com/JeringTech/Javascript.NodeJS/compare/4.0.4...4.1.0) - Dec 3, 2018
### Additions
- Added `StaticNodeJSService.DisposeServiceProvider`.
### Fixes
- `StaticNodeJSService.Invoke*` methods are now thread-safe.

## [4.0.4](https://github.com/JeringTech/Javascript.NodeJS/compare/4.0.3...4.0.4) - Nov 30, 2018
### Changes
- Changed project URL (used by NuGet.org)  from `jering.tech/utilities/javascript.nodejs` to `jering.tech/utilities/jering.javascript.nodejs` for consistency with other Jering projects.

## [4.0.3](https://github.com/JeringTech/Javascript.NodeJS/compare/4.0.2...4.0.3) - Nov 29, 2018
### Fixes
- Fixed nuget package's `PackageProjectUrl`.

## [4.0.2](https://github.com/JeringTech/Javascript.NodeJS/compare/4.0.1...4.0.2) - Nov 28, 2018
### Fixes
- Fixed missing exception XML comments in `INodeJSService`.

## [4.0.1](https://github.com/JeringTech/Javascript.NodeJS/compare/4.0.0...4.0.1) - Nov 27, 2018
### Fixes
- Fixed retrying of invocation requests with stream module sources. Stream positions are now reset
before retries.

## [4.0.0](https://github.com/JeringTech/Javascript.NodeJS/compare/3.4.0...4.0.0) - Nov 22, 2018
### Additions
- Added `INodeJSProcess` interface. A wrapper for NodeJS `Process` instances.
### Changes
- **Breaking**: `INodeJSProcessFactory.Create` now returns an `INodeJSProcess` instead of a `Process`.
- Increased default `OutOfProcessNodeJSServiceOptions.TimeoutMS` from 10000ms to 60000ms.
- Overhauled logic for multi-threading. Added in depth tests for most multi-threaded use cases.

## [3.4.0](https://github.com/JeringTech/Javascript.NodeJS/compare/3.3.0...3.4.0) - Nov 17, 2018
### Additions
- Added automatic retries. Retries are configurable using the property `NumRetries` of `OutOfProcessNodeJSServiceOptions`. Its default
value is 1, so by default, every javascript invocation that fails is retried once.
### Fixes
- Fixed some thread safety issues in `OutOfProcessNodeJSServiceOptions`.

## [3.3.0](https://github.com/JeringTech/Javascript.NodeJS/compare/3.2.1...3.3.0) - Nov 16, 2018
### Additions
- Added `StaticNodeJSService` which exposes a static API alternative to the existing dependency injection based API.
### Changes
- `InvocationException` is now serializable.
### Fixes
- Added the SourceLink Github package required for source-linked symbols.

## [3.2.1](https://github.com/JeringTech/Javascript.NodeJS/compare/3.2.0...3.2.1) - Nov 14, 2018
### Changes
- Source-linked symbols now included in Nuget package.
- Now targets Netstandard2.0 and Net461. Removed Netstandard1.3 target.

## [3.2.0](https://github.com/JeringTech/Javascript.NodeJS/compare/3.1.0...3.2.0) - Oct 10, 2018
### Changes
- Added Nuget package title and improved description.

## [3.1.0](https://github.com/JeringTech/Javascript.NodeJS/compare/3.0.0...3.1.0) - Aug 9, 2018
### Changes
- Reduced memory consumption.

## [3.0.0](https://github.com/JeringTech/Javascript.NodeJS/compare/2.0.0...3.0.0) - Aug 6, 2018
### Changes
- Renamed project to `Jering.Javascript.NodeJS` for consistency with other `Jering` packages. Using statements must be updated to reference types from the
namespace `Jering.Javascript.NodeJS` instead of `Jering.JavascriptUtils.NodeJS`.

## [2.0.0](https://github.com/JeringTech/Javascript.NodeJS/compare/1.0.1...2.0.0) - Aug 4, 2018
### Changes
- Logging is now optional (previously, console logging was enabled by default). To make logging optional, 
the default `INodeJSService` implementation, `HttpNodeJSService`, now takes an 
`Microsoft.Extensions.Logging.ILoggerFactory` instead of an `Microsoft.Extensions.Logging.ILogger` 
as a constructor argument.
- Added .NET Standard 1.3 as a target framework.

## [1.0.1](https://github.com/JeringTech/Javascript.NodeJS/compare/1.0.0...1.0.1) - Aug 1, 2018
### Fixes
- Added some minor null checks in `InvocationContent`.

## [1.0.0](https://github.com/JeringTech/Javascript.NodeJS/compare/0.1.0...1.0.0) - Jul 28, 2018
### Changes
- Reduced default invocation/NodeJS initialization timeout.
- Improved comments for intellisense.

## [0.1.0](https://github.com/JeringTech/Javascript.NodeJS/compare/0.1.0...0.1.0) - Jul 24, 2018
Initial release.
