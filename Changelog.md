# Changelog
This project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html). Refer to 
[The Semantic Versioning Lifecycle](https://www.jeremytcd.com/articles/the-semantic-versioning-lifecycle)
for an overview of semantic versioning.

## [Unreleased](https://github.com/JeremyTCD/Javascript.NodeJS/compare/3.2.0...HEAD)

## [3.2.0](https://github.com/JeremyTCD/Javascript.NodeJS/compare/3.1.0...3.2.0) - Oct 10, 2018
### Changes
- Added Nuget package title and improved description.

## [3.1.0](https://github.com/JeremyTCD/Javascript.NodeJS/compare/3.0.0...3.1.0) - Aug 9, 2018
### Changes
- Reduced memory consumption.

## [3.0.0](https://github.com/JeremyTCD/Javascript.NodeJS/compare/2.0.0...3.0.0) - Aug 6, 2018
### Changes
- Renamed project to `Jering.Javascript.NodeJS` for consistency with other `Jering` packages. Using statements must be updated to reference types from the
namespace `Jering.Javascript.NodeJS` instead of `Jering.JavascriptUtils.NodeJS`.

## [2.0.0](https://github.com/JeremyTCD/Javascript.NodeJS/compare/1.0.1...2.0.0) - Aug 4, 2018
### Changes
- Logging is now optional (previously, console logging was enabled by default). To make logging optional, 
the default `INodeJSService` implementation, `HttpNodeJSService`, now takes an 
`Microsoft.Extensions.Logging.ILoggerFactory` instead of an `Microsoft.Extensions.Logging.ILogger` 
as a constructor argument.
- Added .NET Standard 1.3 as a target framework.

## [1.0.1](https://github.com/JeremyTCD/Javascript.NodeJS/compare/1.0.0...1.0.1) - Aug 1, 2018
### Fixes
- Added some minor null checks in `InvocationContent`.

## [1.0.0](https://github.com/JeremyTCD/Javascript.NodeJS/compare/0.1.0...1.0.0) - Jul 28, 2018
### Changes
- Reduced default invocation/NodeJS initialization timeout.
- Improved comments for intellisense.

## [0.1.0](https://github.com/JeremyTCD/Javascript.NodeJS/compare/0.1.0...0.1.0) - Jul 24, 2018
Initial release.