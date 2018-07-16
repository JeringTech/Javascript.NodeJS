using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using Xunit;

namespace Jering.JavascriptUtils.Node.Tests
{
    public class HttpNodeServiceIntegrationTests : IDisposable
    {
        private ServiceProvider _serviceProvider;

        [Fact]
        public async void InvokeFromFileAsync_InvokesJavascript()
        {
            const string dummyResultString = "success";
            HttpNodeService httpNodeService = CreateHttpNodeService();

            // Act
            DummyResult result = await httpNodeService.
                InvokeFromFileAsync<DummyResult>("dummyModule.js", args: new[] { dummyResultString }).ConfigureAwait(false);

            // Assert
            Assert.Equal(dummyResultString, result.Result);
        }

        [Fact]
        public async void InvokeFromStringAsync_InvokesJavascript()
        {
            // Arrange
            const string dummyResultString = "success";
            HttpNodeService httpNodeService = CreateHttpNodeService();

            // Act
            DummyResult result = await httpNodeService.
                InvokeFromStringAsync<DummyResult>("module.exports = (callback, resultString) => callback(null, {result: resultString});", args: new[] { dummyResultString }).ConfigureAwait(false);

            // Assert
            Assert.Equal(dummyResultString, result.Result);
        }

        [Fact]
        public async void InvokeFromStreamAsync_InvokesJavascript()
        {
            // Arrange
            const string dummyResultString = "success";
            HttpNodeService httpNodeService = CreateHttpNodeService();

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
        public async void TryInvokeFromCacheAsync_InvokesJavascriptIfModuleIsCached()
        {
            // Arrange
            const string dummyResultString = "success";
            const string dummyCacheIdentifier = "dummyCacheIdentifier";
            HttpNodeService httpNodeService = CreateHttpNodeService();
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
            HttpNodeService httpNodeService = CreateHttpNodeService();

            // Act
            (bool success, DummyResult value) = await httpNodeService.TryInvokeFromCacheAsync<DummyResult>(dummyCacheIdentifier, args: new[] { dummyResultString }).ConfigureAwait(false);

            // Assert
            Assert.False(success);
            Assert.Null(value);
        }

        // TryInvokeAsync is called by TryInvokeCoreAsync, which is private. We test it through InvokeFromStringAsync, since it is the simplest public invoke method.
        [Fact]
        public async void TryInvokeAsync_ReturnsStreamValueIfTypeParameterIsStream()
        {
            // Arrange
            const string dummyResultString = "success";
            HttpNodeService httpNodeService = CreateHttpNodeService();

            // Act
            using (Stream result = await httpNodeService.InvokeFromStringAsync<Stream>("module.exports = (callback, resultString) => callback(null, resultString);", args: new[] { dummyResultString }).ConfigureAwait(false))
            using (var streamReader = new StreamReader(result))
            {
                // Assert
                Assert.Equal(dummyResultString, await streamReader.ReadToEndAsync().ConfigureAwait(false));
            }
        }

        [Fact]
        public async void TryInvokeCoreAsync_ReturnsStringValueIfTypeParameterIsString()
        {
            // Arrange
            const string dummyResultString = "success";
            HttpNodeService httpNodeService = CreateHttpNodeService();

            // Act
            string result = await httpNodeService.
                InvokeFromStringAsync<string>("module.exports = (callback, resultString) => callback(null, resultString);", args: new[] { dummyResultString }).ConfigureAwait(false);

            // Assert
            Assert.Equal(dummyResultString, result);
        }

        [Fact]
        public async void TryInvokeCoreAsync_InvokesSpecificExportIfExportNameIsProvided()
        {
            // Arrange
            const string dummyResultString = "success";
            const string dummyExportName = "dummyExportName";
            HttpNodeService httpNodeService = CreateHttpNodeService();

            // Act
            DummyResult result = await httpNodeService.
                InvokeFromStringAsync<DummyResult>($"module.exports = {{ {dummyExportName}: (callback, resultString) => callback(null, {{result: resultString}}) }};", exportName: dummyExportName, args: new[] { dummyResultString }).ConfigureAwait(false);

            // Assert
            Assert.Equal(dummyResultString, result.Result);
        }

        [Fact]
        public async void TryInvokeCoreAsync_ThrowsInvocationExceptionIfModuleHasNoExports()
        {
            // Arrange
            const string dummyModule = "return null;";
            HttpNodeService httpNodeService = CreateHttpNodeService();

            // Act
            InvocationException result = await Assert.ThrowsAsync<InvocationException>(() =>
                httpNodeService.InvokeFromStringAsync<DummyResult>(dummyModule)).
                ConfigureAwait(false);

            // Assert
            Assert.StartsWith($"The module \"{dummyModule}...\" has no exports. Ensure that the module assigns a function or an object containing functions to module.exports.", result.Message);
        }

        [Fact]
        public async void TryInvokeCoreAsync_ThrowsInvocationExceptionIfThereIsNoModuleExportWithSpecifiedExportName()
        {
            // Arrange
            const string dummyExportName = "dummyExportName";
            const string dummyCacheIdentifier = "dummyCacheIdentifier";
            HttpNodeService httpNodeService = CreateHttpNodeService();

            // Act
            InvocationException result = await Assert.ThrowsAsync<InvocationException>(() =>
                httpNodeService.InvokeFromStringAsync<DummyResult>("module.exports = (callback) => callback(null, {result: 'success'});", dummyCacheIdentifier, dummyExportName)).
                ConfigureAwait(false);

            // Assert
            Assert.StartsWith($"The module {dummyCacheIdentifier} has no export named {dummyExportName}.", result.Message);
        }

        [Fact]
        public async void TryInvokeCoreAsync_ThrowsInvocationExceptionIfModuleExportWithSpecifiedExportNameIsNotAFunction()
        {
            // Arrange
            const string dummyExportName = "dummyExportName";
            HttpNodeService httpNodeService = CreateHttpNodeService();

            // Act
            InvocationException result = await Assert.ThrowsAsync<InvocationException>(() =>
                httpNodeService.InvokeFromStringAsync<DummyResult>($"module.exports = {{{dummyExportName}: 'not a function'}};", exportName: dummyExportName)).
                ConfigureAwait(false);

            // Assert
            Assert.StartsWith($"The export named {dummyExportName} from module \"module.exports = {{dummyEx...\" is not a function.", result.Message);
        }

        [Fact]
        public async void TryInvokeCoreAsync_ThrowsInvocationExceptionIfNoExportNameSpecifiedAndModuleExportsIsNotAFunction()
        {
            // Arrange
            HttpNodeService httpNodeService = CreateHttpNodeService();

            // Act
            InvocationException result = await Assert.ThrowsAsync<InvocationException>(() =>
                httpNodeService.InvokeFromStringAsync<DummyResult>("module.exports = {result: 'not a function'};")).
                ConfigureAwait(false);

            // Assert
            Assert.StartsWith("The module \"module.exports = {result:...\" does not export a function.", result.Message);
        }

        [Fact]
        public async void TryInvokeCoreAsync_ThrowsInvocationExceptionIfInvokedMethodCallsCallbackWithError()
        {
            // Arrange
            const string dummyErrorString = "error";
            HttpNodeService httpNodeService = CreateHttpNodeService();

            // Act
            InvocationException result = await Assert.ThrowsAsync<InvocationException>(() =>
                httpNodeService.InvokeFromStringAsync<DummyResult>("module.exports = (callback, errorString) => callback(new Error(errorString));", args: new[] { dummyErrorString })).
                ConfigureAwait(false);

            // Assert
            Assert.StartsWith(dummyErrorString, result.Message); // Complete message includes the stack
        }

        private HttpNodeService CreateHttpNodeService()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddNode(); // Default INodeService is HttpNodeService

            if (Debugger.IsAttached)
            {
                services.Configure<NodeProcessOptions>(options => options.NodeAndV8Options = "--inspect-brk");
                services.Configure<OutOfProcessNodeServiceOptions>(options => options.TimeoutMS = -1);
            }
            _serviceProvider = services.BuildServiceProvider();

            ILoggerFactory loggerFactory = _serviceProvider.GetRequiredService<ILoggerFactory>();
            loggerFactory.AddDebug();

            return _serviceProvider.GetRequiredService<INodeService>() as HttpNodeService;
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
