using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using Xunit;

namespace Jering.JavascriptUtils.NodeJS.Tests
{
    /// <summary>
    /// These tests are de facto tests for HttpServer.ts. They serve the additional role of verifying that IPC works.
    /// </summary>
    public class HttpNodeJSServiceIntegrationTests : IDisposable
    {
        private ServiceProvider _serviceProvider;

        [Fact]
        public async void TryInvokeFromCacheAsync_InvokesJavascriptIfModuleIsCached()
        {
            // Arrange
            const string dummyResultString = "success";
            const string dummyCacheIdentifier = "dummyCacheIdentifier";
            HttpNodeJSService httpNodeService = CreateHttpNodeService();
            // Cache
            await httpNodeService.
                InvokeFromStringAsync<DummyResult>("module.exports = (callback, resultString) => callback(null, {result: resultString});",
                    dummyCacheIdentifier,
                    args: new[] { dummyResultString }).
                ConfigureAwait(false);

            // Act
            (bool success, DummyResult value) = await httpNodeService.TryInvokeFromCacheAsync<DummyResult>(dummyCacheIdentifier, args: new[] { dummyResultString }).ConfigureAwait(false);

            // Assert
            Assert.True(success);
            Assert.Equal(dummyResultString, value.Result);
        }

        [Fact]
        public async void TryInvokeFromCacheAsync_ReturnsFalseIfModuleIsNotCached()
        {
            // Arrange
            const string dummyResultString = "success";
            const string dummyCacheIdentifier = "dummyCacheIdentifier";
            HttpNodeJSService httpNodeService = CreateHttpNodeService();

            // Act
            (bool success, DummyResult value) = await httpNodeService.TryInvokeFromCacheAsync<DummyResult>(dummyCacheIdentifier, args: new[] { dummyResultString }).ConfigureAwait(false);

            // Assert
            Assert.False(success);
            Assert.Null(value);
        }

        [Fact]
        public async void InvokeFromStreamAsync_InvokesJavascript()
        {
            // Arrange
            const string dummyResultString = "success";
            HttpNodeJSService httpNodeService = CreateHttpNodeService();

            DummyResult result;
            using (var memoryStream = new MemoryStream())
            using (var streamWriter = new StreamWriter(memoryStream))
            {
                streamWriter.Write("module.exports = (callback, resultString) => callback(null, {result: resultString});");
                streamWriter.Flush();
                memoryStream.Position = 0;

                // Act
                result = await httpNodeService.InvokeFromStreamAsync<DummyResult>(memoryStream, args: new[] { dummyResultString }).ConfigureAwait(false);
            }

            // Assert
            Assert.Equal(dummyResultString, result.Result);
        }

        [Fact]
        public async void InvokeFromStringAsync_InvokesJavascript()
        {
            // Arrange
            const string dummyResultString = "success";
            HttpNodeJSService httpNodeService = CreateHttpNodeService();

            // Act
            DummyResult result = await httpNodeService.
                InvokeFromStringAsync<DummyResult>("module.exports = (callback, resultString) => callback(null, {result: resultString});", args: new[] { dummyResultString }).ConfigureAwait(false);

            // Assert
            Assert.Equal(dummyResultString, result.Result);
        }

        [Fact]
        public async void InvokeFromFileAsync_InvokesJavascript()
        {
            const string dummyResultString = "success";
            HttpNodeJSService httpNodeService = CreateHttpNodeService();

            // Act
            DummyResult result = await httpNodeService.
                InvokeFromFileAsync<DummyResult>("dummyModule.js", args: new[] { dummyResultString }).ConfigureAwait(false);

            // Assert
            Assert.Equal(dummyResultString, result.Result);
        }

        [Fact]
        public async void AllInvokeMethods_ThrowInvocationExceptionIfModuleHasNoExports()
        {
            // Arrange
            const string dummyModule = "return null;";
            HttpNodeJSService httpNodeService = CreateHttpNodeService();

            // Act
            InvocationException result = await Assert.ThrowsAsync<InvocationException>(() =>
                httpNodeService.InvokeFromStringAsync<DummyResult>(dummyModule)).
                ConfigureAwait(false);

            // Assert
            Assert.StartsWith($"The module \"{dummyModule}...\" has no exports. Ensure that the module assigns a function or an object containing functions to module.exports.", result.Message);
        }

        [Fact]
        public async void AllInvokeMethods_ThrowInvocationExceptionIfThereIsNoModuleExportWithSpecifiedExportName()
        {
            // Arrange
            const string dummyExportName = "dummyExportName";
            const string dummyCacheIdentifier = "dummyCacheIdentifier";
            HttpNodeJSService httpNodeService = CreateHttpNodeService();

            // Act
            InvocationException result = await Assert.ThrowsAsync<InvocationException>(() =>
                httpNodeService.InvokeFromStringAsync<DummyResult>("module.exports = (callback) => callback(null, {result: 'success'});", dummyCacheIdentifier, dummyExportName)).
                ConfigureAwait(false);

            // Assert
            Assert.StartsWith($"The module {dummyCacheIdentifier} has no export named {dummyExportName}.", result.Message);
        }

        [Fact]
        public async void AllInvokeMethods_ThrowInvocationExceptionIfModuleExportWithSpecifiedExportNameIsNotAFunction()
        {
            // Arrange
            const string dummyExportName = "dummyExportName";
            HttpNodeJSService httpNodeService = CreateHttpNodeService();

            // Act
            InvocationException result = await Assert.ThrowsAsync<InvocationException>(() =>
                httpNodeService.InvokeFromStringAsync<DummyResult>($"module.exports = {{{dummyExportName}: 'not a function'}};", exportName: dummyExportName)).
                ConfigureAwait(false);

            // Assert
            Assert.StartsWith($"The export named {dummyExportName} from module \"module.exports = {{dummyEx...\" is not a function.", result.Message);
        }

        [Fact]
        public async void AllInvokeMethods_ThrowInvocationExceptionIfNoExportNameSpecifiedAndModuleExportsIsNotAFunction()
        {
            // Arrange
            HttpNodeJSService httpNodeService = CreateHttpNodeService();

            // Act
            InvocationException result = await Assert.ThrowsAsync<InvocationException>(() =>
                httpNodeService.InvokeFromStringAsync<DummyResult>("module.exports = {result: 'not a function'};")).
                ConfigureAwait(false);

            // Assert
            Assert.StartsWith("The module \"module.exports = {result:...\" does not export a function.", result.Message);
        }

        [Fact]
        public async void AllInvokeMethods_ThrowInvocationExceptionIfInvokedMethodCallsCallbackWithError()
        {
            // Arrange
            const string dummyErrorString = "error";
            HttpNodeJSService httpNodeService = CreateHttpNodeService();

            // Act
            InvocationException result = await Assert.ThrowsAsync<InvocationException>(() =>
                httpNodeService.InvokeFromStringAsync<DummyResult>("module.exports = (callback, errorString) => callback(new Error(errorString));", args: new[] { dummyErrorString })).
                ConfigureAwait(false);

            // Assert
            Assert.StartsWith(dummyErrorString, result.Message); // Complete message includes the stack
        }

        [Fact]
        public async void AllInvokeMethods_InvokeASpecificExportIfExportNameIsProvided()
        {
            // Arrange
            const string dummyResultString = "success";
            const string dummyExportName = "dummyExportName";
            HttpNodeJSService httpNodeService = CreateHttpNodeService();

            // Act
            DummyResult result = await httpNodeService.
                InvokeFromStringAsync<DummyResult>($"module.exports = {{ {dummyExportName}: (callback, resultString) => callback(null, {{result: resultString}}) }};", exportName: dummyExportName, args: new[] { dummyResultString }).ConfigureAwait(false);

            // Assert
            Assert.Equal(dummyResultString, result.Result);
        }

        private HttpNodeJSService CreateHttpNodeService()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddNode(); // Default INodeService is HttpNodeService

            if (Debugger.IsAttached)
            {
                services.Configure<NodeJSProcessOptions>(options => options.NodeAndV8Options = "--inspect-brk");
                services.Configure<OutOfProcessNodeJSServiceOptions>(options => options.TimeoutMS = -1);
            }
            _serviceProvider = services.BuildServiceProvider();

            ILoggerFactory loggerFactory = _serviceProvider.GetRequiredService<ILoggerFactory>();
            loggerFactory.AddDebug();

            return _serviceProvider.GetRequiredService<INodeJSService>() as HttpNodeJSService;
        }

        private class DummyResult
        {
            public string Result { get; set; }
        }

        public void Dispose()
        {
            _serviceProvider?.Dispose();
        }
    }
}
