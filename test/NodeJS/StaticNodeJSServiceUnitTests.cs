using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using System.IO;
using System.Threading;
using Xunit;

namespace Jering.Javascript.NodeJS.Tests
{
    [Collection(nameof(StaticNodeJSService))]
    public class StaticNodeJSServiceUnitTests
    {
        private readonly MockRepository _mockRepository = new(MockBehavior.Default);

        [Fact]
        public async void InvokeFromFileAsync_WithTypeParameter_InvokesFromFile()
        {
            // Arrange
            const int dummyResult = 1;
            const string dummyModulePath = "dummyModulePath";
            const string dummyExportName = "dummyExportName";
            object[] dummyArgs = Array.Empty<object>();
            var dummyCancellationToken = new CancellationToken();
            Mock<INodeJSService> mockNodeJSService = _mockRepository.Create<INodeJSService>();
            mockNodeJSService.
                Setup(t => t.InvokeFromFileAsync<int>(dummyModulePath, dummyExportName, dummyArgs, dummyCancellationToken)).
                ReturnsAsync(dummyResult);
            var dummyServices = new ServiceCollection();
            dummyServices.AddSingleton(typeof(INodeJSService), mockNodeJSService.Object);
            StaticNodeJSService.SetServices(dummyServices);

            // Act
            int result = await StaticNodeJSService.InvokeFromFileAsync<int>(dummyModulePath, dummyExportName, dummyArgs, dummyCancellationToken).ConfigureAwait(false);

            // Assert
            _mockRepository.VerifyAll();
            Assert.Equal(dummyResult, result);
        }

        [Fact]
        public async void InvokeFromFileAsync_WithoutTypeParameter_InvokesFromFile()
        {
            // Arrange
            const string dummyModulePath = "dummyModulePath";
            const string dummyExportName = "dummyExportName";
            object[] dummyArgs = Array.Empty<object>();
            var dummyCancellationToken = new CancellationToken();
            Mock<INodeJSService> mockNodeJSService = _mockRepository.Create<INodeJSService>();
            mockNodeJSService.
                Setup(t => t.InvokeFromFileAsync(dummyModulePath, dummyExportName, dummyArgs, dummyCancellationToken));
            var dummyServices = new ServiceCollection();
            dummyServices.AddSingleton(typeof(INodeJSService), mockNodeJSService.Object);
            StaticNodeJSService.SetServices(dummyServices);

            // Act
            await StaticNodeJSService.InvokeFromFileAsync(dummyModulePath, dummyExportName, dummyArgs, dummyCancellationToken).ConfigureAwait(false);

            // Assert
            _mockRepository.VerifyAll();
        }

        [Fact]
        public async void InvokeFromStringAsync_WithTypeParameter_WithRawStringModule_InvokesFromString()
        {
            // Arrange
            const int dummyResult = 1;
            const string dummyModuleString = "dummyModuleString";
            const string dummyNewCacheIdentifier = "dummyNewCacheIdentifier";
            const string dummyExportName = "dummyExportName";
            object[] dummyArgs = Array.Empty<object>();
            var dummyCancellationToken = new CancellationToken();
            Mock<INodeJSService> mockNodeJSService = _mockRepository.Create<INodeJSService>();
            mockNodeJSService.
                Setup(t => t.InvokeFromStringAsync<int>(dummyModuleString, dummyNewCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken)).
                ReturnsAsync(dummyResult);
            var dummyServices = new ServiceCollection();
            dummyServices.AddSingleton(typeof(INodeJSService), mockNodeJSService.Object);
            StaticNodeJSService.SetServices(dummyServices);

            // Act
            int result = await StaticNodeJSService.InvokeFromStringAsync<int>(dummyModuleString, dummyNewCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken).ConfigureAwait(false);

            // Assert
            Assert.Equal(dummyResult, result);
            _mockRepository.VerifyAll();
        }

        [Fact]
        public async void InvokeFromStringAsync_WithoutTypeParameter_WithRawStringModule_InvokesFromString()
        {
            // Arrange
            const string dummyModuleString = "dummyModuleString";
            const string dummyNewCacheIdentifier = "dummyNewCacheIdentifier";
            const string dummyExportName = "dummyExportName";
            object[] dummyArgs = Array.Empty<object>();
            var dummyCancellationToken = new CancellationToken();
            Mock<INodeJSService> mockNodeJSService = _mockRepository.Create<INodeJSService>();
            mockNodeJSService.Setup(t => t.InvokeFromStringAsync(dummyModuleString, dummyNewCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken));
            var dummyServices = new ServiceCollection();
            dummyServices.AddSingleton(typeof(INodeJSService), mockNodeJSService.Object);
            StaticNodeJSService.SetServices(dummyServices);

            // Act
            await StaticNodeJSService.InvokeFromStringAsync(dummyModuleString, dummyNewCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken).ConfigureAwait(false);

            // Assert
            _mockRepository.VerifyAll();
        }

        [Fact]
        public async void InvokeFromStringAsync_WithTypeParameter_WithModuleFactory_IfModuleIsCachedInvokesFromCacheOtherwiseInvokesFromString()
        {
            // Arrange
            const int dummyResult = 1;
            Func<string> dummyModuleFactory = () => "dummyModule";
            const string dummyCacheIdentifier = "dummyCacheIdentifier";
            const string dummyExportName = "dummyExportName";
            object[] dummyArgs = Array.Empty<object>();
            var dummyCancellationToken = new CancellationToken();
            Mock<INodeJSService> mockNodeJSService = _mockRepository.Create<INodeJSService>();
            mockNodeJSService.
                Setup(t => t.InvokeFromStringAsync<int>(dummyModuleFactory, dummyCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken)).
                ReturnsAsync(dummyResult);
            var dummyServices = new ServiceCollection();
            dummyServices.AddSingleton(typeof(INodeJSService), mockNodeJSService.Object);
            StaticNodeJSService.SetServices(dummyServices);

            // Act
            int result = await StaticNodeJSService.InvokeFromStringAsync<int>(dummyModuleFactory, dummyCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken).ConfigureAwait(false);

            // Assert
            _mockRepository.VerifyAll();
            Assert.Equal(dummyResult, result);
        }

        [Fact]
        public async void InvokeFromStringAsync_WithoutTypeParameter_WithModuleFactory_IfModuleIsCachedInvokesFromCacheOtherwiseInvokesFromString()
        {
            // Arrange
            Func<string> dummyModuleFactory = () => "dummyModule";
            const string dummyCacheIdentifier = "dummyCacheIdentifier";
            const string dummyExportName = "dummyExportName";
            object[] dummyArgs = Array.Empty<object>();
            var dummyCancellationToken = new CancellationToken();
            Mock<INodeJSService> mockNodeJSService = _mockRepository.Create<INodeJSService>();
            mockNodeJSService.
                Setup(t => t.InvokeFromStringAsync(dummyModuleFactory, dummyCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken));
            var dummyServices = new ServiceCollection();
            dummyServices.AddSingleton(typeof(INodeJSService), mockNodeJSService.Object);
            StaticNodeJSService.SetServices(dummyServices);

            // Act
            await StaticNodeJSService.InvokeFromStringAsync(dummyModuleFactory, dummyCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken).ConfigureAwait(false);

            // Assert
            _mockRepository.VerifyAll();
        }

        [Fact]
        public async void InvokeFromStreamAsync_WithTypeParameter_WithRawStreamModule_InvokesFromStream()
        {
            // Arrange
            const int dummyResult = 1;
            var dummyModuleStream = new MemoryStream();
            const string dummyNewCacheIdentifier = "dummyNewCacheIdentifier";
            const string dummyExportName = "dummyExportName";
            object[] dummyArgs = Array.Empty<object>();
            var dummyCancellationToken = new CancellationToken();
            Mock<INodeJSService> mockNodeJSService = _mockRepository.Create<INodeJSService>();
            mockNodeJSService.
                Setup(t => t.InvokeFromStreamAsync<int>(dummyModuleStream, dummyNewCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken)).
                ReturnsAsync(dummyResult);
            var dummyServices = new ServiceCollection();
            dummyServices.AddSingleton(typeof(INodeJSService), mockNodeJSService.Object);
            StaticNodeJSService.SetServices(dummyServices);

            // Act
            int result = await StaticNodeJSService.InvokeFromStreamAsync<int>(dummyModuleStream, dummyNewCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken).ConfigureAwait(false);

            // Assert
            Assert.Equal(dummyResult, result);
            _mockRepository.VerifyAll();
        }

        [Fact]
        public async void InvokeFromStreamAsync_WithoutTypeParameter_WithRawStreamModule_InvokesFromStream()
        {
            // Arrange
            var dummyModuleStream = new MemoryStream();
            const string dummyNewCacheIdentifier = "dummyNewCacheIdentifier";
            const string dummyExportName = "dummyExportName";
            object[] dummyArgs = Array.Empty<object>();
            var dummyCancellationToken = new CancellationToken();
            Mock<INodeJSService> mockNodeJSService = _mockRepository.Create<INodeJSService>();
            mockNodeJSService.Setup(t => t.InvokeFromStreamAsync(dummyModuleStream, dummyNewCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken));
            var dummyServices = new ServiceCollection();
            dummyServices.AddSingleton(typeof(INodeJSService), mockNodeJSService.Object);
            StaticNodeJSService.SetServices(dummyServices);

            // Act
            await StaticNodeJSService.InvokeFromStreamAsync(dummyModuleStream, dummyNewCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken).ConfigureAwait(false);

            // Assert
            _mockRepository.VerifyAll();
        }

        [Fact]
        public async void InvokeFromStreamAsync_WithTypeParameter_WithModuleFactory_IfModuleIsCachedInvokesFromCacheOtherwiseInvokesFromStream()
        {
            // Arrange
            const int dummyResult = 1;
            Func<Stream> dummyModuleFactory = () => new MemoryStream();
            const string dummyCacheIdentifier = "dummyCacheIdentifier";
            const string dummyExportName = "dummyExportName";
            object[] dummyArgs = Array.Empty<object>();
            var dummyCancellationToken = new CancellationToken();
            Mock<INodeJSService> mockNodeJSService = _mockRepository.Create<INodeJSService>();
            mockNodeJSService.
                Setup(t => t.InvokeFromStreamAsync<int>(dummyModuleFactory, dummyCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken)).
                ReturnsAsync(dummyResult);
            var dummyServices = new ServiceCollection();
            dummyServices.AddSingleton(typeof(INodeJSService), mockNodeJSService.Object);
            StaticNodeJSService.SetServices(dummyServices);

            // Act
            int result = await StaticNodeJSService.InvokeFromStreamAsync<int>(dummyModuleFactory, dummyCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken).ConfigureAwait(false);

            // Assert
            _mockRepository.VerifyAll();
            Assert.Equal(dummyResult, result);
        }

        [Fact]
        public async void InvokeFromStreamAsync_WithoutTypeParameter_WithModuleFactory_IfModuleIsCachedInvokesFromCacheOtherwiseInvokesFromStream()
        {
            // Arrange
            Func<Stream> dummyModuleFactory = () => new MemoryStream();
            const string dummyCacheIdentifier = "dummyCacheIdentifier";
            const string dummyExportName = "dummyExportName";
            object[] dummyArgs = Array.Empty<object>();
            var dummyCancellationToken = new CancellationToken();
            Mock<INodeJSService> mockNodeJSService = _mockRepository.Create<INodeJSService>();
            mockNodeJSService.
                Setup(t => t.InvokeFromStreamAsync(dummyModuleFactory, dummyCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken));
            var dummyServices = new ServiceCollection();
            dummyServices.AddSingleton(typeof(INodeJSService), mockNodeJSService.Object);
            StaticNodeJSService.SetServices(dummyServices);

            // Act
            await StaticNodeJSService.InvokeFromStreamAsync(dummyModuleFactory, dummyCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken).ConfigureAwait(false);

            // Assert
            _mockRepository.VerifyAll();
        }

        [Fact]
        public async void TryInvokeFromCacheAsync_WithTypeParameter_CreatesInvocationRequestAndCallsTryInvokeCoreAsync()
        {
            // Arrange
            const int dummyResult = 1;
            const string dummyModuleCacheIdentifier = "dummyModuleCacheIdentifier";
            const string dummyExportName = "dummyExportName";
            object[] dummyArgs = Array.Empty<object>();
            var dummyCancellationToken = new CancellationToken();
            Mock<INodeJSService> mockNodeJSService = _mockRepository.Create<INodeJSService>();
            mockNodeJSService.
                Setup(t => t.TryInvokeFromCacheAsync<int>(dummyModuleCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken)).
                ReturnsAsync((true, dummyResult));
            var dummyServices = new ServiceCollection();
            dummyServices.AddSingleton(typeof(INodeJSService), mockNodeJSService.Object);
            StaticNodeJSService.SetServices(dummyServices);

            // Act
            (bool success, int result) = await StaticNodeJSService.TryInvokeFromCacheAsync<int>(dummyModuleCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken).ConfigureAwait(false);

            // Assert
            Assert.True(success);
            Assert.Equal(dummyResult, result);
            _mockRepository.VerifyAll();
        }

        [Fact]
        public async void TryInvokeFromCacheAsync_WithoutTypeParameter_CreatesInvocationRequestAndCallsTryInvokeCoreAsync()
        {
            // Arrange
            const string dummyModuleCacheIdentifier = "dummyModuleCacheIdentifier";
            const string dummyExportName = "dummyExportName";
            object[] dummyArgs = Array.Empty<object>();
            var dummyCancellationToken = new CancellationToken();
            Mock<INodeJSService> mockNodeJSService = _mockRepository.Create<INodeJSService>();
            mockNodeJSService.
                Setup(t => t.TryInvokeFromCacheAsync(dummyModuleCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken)).
                ReturnsAsync(true);
            var dummyServices = new ServiceCollection();
            dummyServices.AddSingleton(typeof(INodeJSService), mockNodeJSService.Object);
            StaticNodeJSService.SetServices(dummyServices);

            // Act
            bool success = await StaticNodeJSService.TryInvokeFromCacheAsync(dummyModuleCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken).ConfigureAwait(false);

            // Assert
            Assert.True(success);
            _mockRepository.VerifyAll();
        }
    }
}
