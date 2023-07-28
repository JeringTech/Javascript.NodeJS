# Changelog
This project uses [semantic versioning](http://semver.org/spec/v2.0.0.html). Refer to 
*[Semantic Versioning in Practice](https://www.jering.tech/articles/semantic-versioning-in-practice)*
for an overview of semantic versioning.

## [Unreleased](https://github.com/JeringTech/Javascript.NodeJS/compare/7.0.0-beta.5...HEAD)

## [7.0.0-beta.5](https://github.com/JeringTech/Javascript.NodeJS/compare/7.0.0-beta.4...7.0.0-beta.5) - Jul 28, 2023
### Changes
- Changed server scripts from CommonJS to EcmaScript modules. This should improve the reliability of `.mjs` file invocations. ([#173](https://github.com/JeringTech/Javascript.NodeJS/pull/173)).

## [7.0.0-beta.4](https://github.com/JeringTech/Javascript.NodeJS/compare/7.0.0-beta.3...7.0.0-beta.4) - Apr 18, 2023
### Fixes
- Handle process outputs correctly. ([#166](https://github.com/JeringTech/Javascript.NodeJS/pull/166)).

## [7.0.0-beta.3](https://github.com/JeringTech/Javascript.NodeJS/compare/7.0.0-beta.2...7.0.0-beta.3) - Feb 14, 2023
### Additions
- Added `HttpResponseMessage` as a possible invocation result. ([#157](https://github.com/JeringTech/Javascript.NodeJS/pull/157)).
- Added a `responseAction` parameter to the [Javascript callback](https://github.com/JeringTech/Javascript.NodeJS#invoking-javascript). This action can be used to modify an invocation's
HTTP response before it is sent from Node.js to the .Net process. ([#157](https://github.com/JeringTech/Javascript.NodeJS/pull/157)).

## [7.0.0-beta.2](https://github.com/JeringTech/Javascript.NodeJS/compare/7.0.0-beta.1...7.0.0-beta.2) - Jan 19, 2023
### Additions
- `InvokeFromFileAsync` now supports `.mjs` files. ([#154](https://github.com/JeringTech/Javascript.NodeJS/pull/154)).
- Added net7.0 target. ([#153](https://github.com/JeringTech/Javascript.NodeJS/pull/153)
### Fixes
- `NodeJSProcess.Dispose` now waits for Node.js process to exit. Added `NodeJSProcess.DisposeAsync` (net5.0 and later only) which waits for the exit asynchronously. ([#155](https://github.com/JeringTech/Javascript.NodeJS/pull/155)).

## [7.0.0-beta.1](https://github.com/JeringTech/Javascript.NodeJS/compare/7.0.0-beta.0...7.0.0-beta.1) - Aug 26, 2022
### Changes
- **Breaking Changes**: 
    - Replaced `void INodeJSService.MoveToNewProcess` with `ValueTask MoveToNewProcessAsync()`. ([#144](https://github.com/JeringTech/Javascript.NodeJS/pull/144))
    - Replaced `OutOfProcessNodeJSServiceOptions.TimeoutMS` with `OutOfProcessNodeJSServiceOptions.ConnectionTimeoutMS` and `OutOfProcessNodeJSServiceOptions.InvocationTimeoutMS`. ([#144](https://github.com/JeringTech/Javascript.NodeJS/pull/144))

## [7.0.0-beta.0](https://github.com/JeringTech/Javascript.NodeJS/compare/6.3.1...7.0.0-beta.0) - Aug 25, 2022
### Changes
- **Breaking**: `OutOfProcessNodeJSService.OnConnectionEstablishedMessageReceived` now takes a `System.Text.RegularExpressions.Match` argument instead of a `string`. ([#146](https://github.com/JeringTech/Javascript.NodeJS/pull/146))
### Fixes
- Fixed handshake with Node.js not completing when external systems interfere with Node.js's stdout stream. ([#146](https://github.com/JeringTech/Javascript.NodeJS/pull/146))

## [6.3.1](https://github.com/JeringTech/Javascript.NodeJS/compare/6.3.0...6.3.1) - May 10, 2022
### Fixes
- Fixed infinite retries issue that occurs when `OutOfProcessNodeJSServiceOptions.NumProcessRetries` > 0 and `OutOfProcessNodeJSServiceOptions.NumProcessRetries` === 0. ([#135](https://github.com/JeringTech/Javascript.NodeJS/pull/135))

## [6.3.0](https://github.com/JeringTech/Javascript.NodeJS/compare/6.2.0...6.3.0) - Dec 27, 2021
### Additions
- Added net6.0 target. ([#128](https://github.com/JeringTech/Javascript.NodeJS/pull/128))
### Fixes
- Now supports HTTP2.0 for net6.0 and beyond. ([#128](https://github.com/JeringTech/Javascript.NodeJS/pull/128))

## [6.2.0](https://github.com/JeringTech/Javascript.NodeJS/compare/6.1.0...6.2.0) - Nov 26, 2021
### Additions
- Added `OutOfProcessNodeJSServiceOptions.EnableProcessRetriesForJavascriptErrors` option. Enables users to choose whether process retries occur for 
  invocations that fail due to Javascript errors. ([#124](https://github.com/JeringTech/Javascript.NodeJS/pull/124)).
### Fixes
- Fixed infinite process retries bug. ([#124](https://github.com/JeringTech/Javascript.NodeJS/pull/124)).
- Fixed missing log entry for last retry. ([#124](https://github.com/JeringTech/Javascript.NodeJS/pull/124)).

## [6.1.0](https://github.com/JeringTech/Javascript.NodeJS/compare/6.0.1...6.1.0) - Nov 4, 2021
### Additions
- Added `INodeJSService.MoveToNewProcess` method. ([#122](https://github.com/JeringTech/Javascript.NodeJS/pull/122)).

## [6.0.1](https://github.com/JeringTech/Javascript.NodeJS/compare/6.0.0...6.0.1) - May 24, 2021
### Fixes
- Fixed `InvocationException` message. ([#110](https://github.com/JeringTech/Javascript.NodeJS/pull/110)).

## [6.0.0](https://github.com/JeringTech/Javascript.NodeJS/compare/6.0.0-beta.3...6.0.0) - May 19, 2021
All additions and changes can be found here: [#108](https://github.com/JeringTech/Javascript.NodeJS/pull/108)

### Additions
- Added `HttpNodeJSServiceOptions.Version` option. Allows for selecting of HTTP version.
- Added source generators for `StaticNodeJSService`, `HttpNodeJSPoolService`, and API documentation.
### Changes
- Bumped dependencies.
- **Major Breaking Changes**:
  - Enabled nullable reference types.
  - Renamed `newCacheIdentifier` parameter in `INodeJSService`, `StaticNodeJSService` and `HttpNodeJSPoolService` methods to `cacheIdentifier`.
- **Minor Breaking Changes**:
  - Renamed `newCacheIdentifier` parameter in `OutOfProcessNodeJSService` methods to `cacheIdentifier`.
  - Removed obsolete `OutOfProcessNodeJSService` constructor.
  - Renamed `InvocationRequest.NewCacheIdentifier` to `CacheIdentifier`.
  - Added `httpNodeJSServiceOptionsAccessor` parameter to `HttpNodeJSService`.
  - Removed redundant `sender` parameter from `MessageReceivedEventHandler`.

## [6.0.0-beta.3](https://github.com/JeringTech/Javascript.NodeJS/compare/6.0.0-beta.2...6.0.0-beta.3) - Mar 31, 2021
### Additions
- Added `NodeJSProcessOptions.ExecutablePath` option. ([#106](https://github.com/JeringTech/Javascript.NodeJS/pull/106)).

## [6.0.0-beta.2](https://github.com/JeringTech/Javascript.NodeJS/compare/6.0.0-beta.1...6.0.0-beta.2) - Feb 24, 2021
### Additions
- Added `OutOfProcessNodeJSServiceOptions.NumProcessRetries` option. Allows for retrying of invocations in new processes. ([#101](https://github.com/JeringTech/Javascript.NodeJS/pull/101)).
### Changes
- `HttpNodeJSService` now logs endpoint on connect. Logged at `information` level. ([#101](https://github.com/JeringTech/Javascript.NodeJS/pull/101)).
- **Breaking**: Renamed `OutOfProcessNodeJSServiceOptions.WatchGracefulShutdown` to `GracefulProcessShutdown`. Option now affects
process shutdowns on-file-change *and* when retrying invocations. ([#101](https://github.com/JeringTech/Javascript.NodeJS/pull/101)).

## [6.0.0-beta.1](https://github.com/JeringTech/Javascript.NodeJS/compare/6.0.0-beta.0...6.0.0-beta.1) - Feb 22, 2021
### Fixes
- Fixed Http/2 for .Net 5.0. ([#100](https://github.com/JeringTech/Javascript.NodeJS/pull/100)).

## [6.0.0-beta.0](https://github.com/JeringTech/Javascript.NodeJS/compare/5.4.4...6.0.0-beta.0) - Feb 10, 2021
### Additions
- Added NetCoreApp3.0 as a target.
- Library uses HTTP/2 to communicate with Node.js when using NetCoreApp3.0 binaries. ([#97](https://github.com/JeringTech/Javascript.NodeJS/pull/97)).
### Changes
- **Breaking**: Simplified the surface area of `IHttpClientService`. Users can use DI to register a custom implementation of this service
to customize their `HttpClient`.

## [5.4.4](https://github.com/JeringTech/Javascript.NodeJS/compare/5.4.3...5.4.4) - Jan 18, 2021
### Fixes
- Fixed Node.js console window popping up. ([#93](https://github.com/JeringTech/Javascript.NodeJS/pull/93)).

## [5.4.3](https://github.com/JeringTech/Javascript.NodeJS/compare/5.4.2...5.4.3) - Aug 18, 2020
### Fixes
- Fixed incompatibilities with newer node versions. ([#90](https://github.com/JeringTech/Javascript.NodeJS/pull/90)).

## [5.4.2](https://github.com/JeringTech/Javascript.NodeJS/compare/5.4.1...5.4.2) - Jun 25, 2020
### Fixes
- Disabled unecessary Node.js HTTP timeouts, added logging for timeouts. ([#85](https://github.com/JeringTech/Javascript.NodeJS/pull/85)).

## [5.4.1](https://github.com/JeringTech/Javascript.NodeJS/compare/5.4.0...5.4.1) - Jun 23, 2020
### Fixes
- Fixed capturing of final output from Node.js. ([#84](https://github.com/JeringTech/Javascript.NodeJS/pull/84)).

## [5.4.0](https://github.com/JeringTech/Javascript.NodeJS/compare/5.3.2...5.4.0) - Mar 9, 2020
### Additions
- Added file watching. Refer to [ReadMe](https://github.com/JeringTech/Javascript.NodeJS#outofprocessnodejsserviceoptions) for more information. ([#69](https://github.com/JeringTech/Javascript.NodeJS/pull/69)).

## [5.3.2](https://github.com/JeringTech/Javascript.NodeJS/compare/5.3.1...5.3.2) - Feb 22, 2020
### Fixes
- `HttpNodeJSService` no longer disposes `Stream`s before returning them. ([#73](https://github.com/JeringTech/Javascript.NodeJS/pull/73)).

## [5.3.1](https://github.com/JeringTech/Javascript.NodeJS/compare/5.3.0...5.3.1) - Feb 12, 2020
### Fixes
- `ConfigureNodeJSProcessOptions` no longer overwrites user specified `NodeJSProcessOptions` options. ([#71](https://github.com/JeringTech/Javascript.NodeJS/pull/71)).

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
- Fixed `TypeError [ERR_INVALID_ARG_TYPE]: The �path� argument must be of type string` error when using newer NodeJS versions. ([6cd3b3f](https://github.com/JeringTech/Javascript.NodeJS/commit/6cd3b3f088321e02d51450aebe43a7b55ce3922d))
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
