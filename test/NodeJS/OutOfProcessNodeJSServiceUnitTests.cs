using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Jering.Javascript.NodeJS.Tests
{
    // OutOfProcessNodeJSService._processSemaphore and OutOfProcessNodeJSService._nodeJSProcess may use unmanaged resources. 
    // To be safe, dispose OutOfProcessNodeJSService instances.
    public class OutOfProcessNodeJSServiceUnitTests : IDisposable
    {
        private readonly MockRepository _mockRepository = new MockRepository(MockBehavior.Default);
        private readonly ITestOutputHelper _testOutputHelper;
        private IServiceProvider _serviceProvider;
        private const int TIMEOUT_MS = 60000;

        public OutOfProcessNodeJSServiceUnitTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async void InvokeFromFileAsync_WithTypeParameter_InvokesFromFile()
        {
            // Arrange
            const int dummyResult = 1;
            const string dummyModulePath = "dummyModulePath";
            const string dummyExportName = "dummyExportName";
            var dummyArgs = new object[0];
            var dummyCancellationToken = new CancellationToken();
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService();
            mockTestSubject.CallBase = true;
            mockTestSubject.
                Setup(t => t.TryInvokeCoreAsync<int>(It.Is<InvocationRequest>(
                        invocationRequest =>
                            invocationRequest.ModuleSourceType == ModuleSourceType.File &&
                            invocationRequest.ModuleSource == dummyModulePath &&
                            invocationRequest.NewCacheIdentifier == null &&
                            invocationRequest.ExportName == dummyExportName &&
                            invocationRequest.Args == dummyArgs &&
                            invocationRequest.ModuleStreamSource == null),
                    dummyCancellationToken)).
                ReturnsAsync((true, dummyResult));

            // Act
            int result = await mockTestSubject.Object.InvokeFromFileAsync<int>(dummyModulePath, dummyExportName, dummyArgs, dummyCancellationToken).ConfigureAwait(false);

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
            var dummyArgs = new object[0];
            var dummyCancellationToken = new CancellationToken();
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService();
            mockTestSubject.CallBase = true;
            mockTestSubject.
                Setup(t => t.InvokeFromFileAsync<Void>(dummyModulePath, dummyExportName, dummyArgs, dummyCancellationToken)).
                ReturnsAsync((Void)null);

            // Act
            await mockTestSubject.Object.InvokeFromFileAsync(dummyModulePath, dummyExportName, dummyArgs, dummyCancellationToken).ConfigureAwait(false);

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
            var dummyArgs = new object[0];
            var dummyCancellationToken = new CancellationToken();
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService();
            mockTestSubject.CallBase = true;
            mockTestSubject.
                Setup(t => t.TryInvokeCoreAsync<int>(It.Is<InvocationRequest>(
                        invocationRequest =>
                            invocationRequest.ModuleSourceType == ModuleSourceType.String &&
                            invocationRequest.ModuleSource == dummyModuleString &&
                            invocationRequest.NewCacheIdentifier == dummyNewCacheIdentifier &&
                            invocationRequest.ExportName == dummyExportName &&
                            invocationRequest.Args == dummyArgs &&
                            invocationRequest.ModuleStreamSource == null),
                    dummyCancellationToken)).
                ReturnsAsync((true, dummyResult));

            // Act
            int result = await mockTestSubject.Object.InvokeFromStringAsync<int>(dummyModuleString, dummyNewCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken).ConfigureAwait(false);

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
            var dummyArgs = new object[0];
            var dummyCancellationToken = new CancellationToken();
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService();
            mockTestSubject.CallBase = true;
            mockTestSubject.
                Setup(t => t.InvokeFromStringAsync<Void>(dummyModuleString, dummyNewCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken)).
                ReturnsAsync((Void)null);

            // Act
            await mockTestSubject.Object.InvokeFromStringAsync(dummyModuleString, dummyNewCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken).ConfigureAwait(false);

            // Assert
            _mockRepository.VerifyAll();
        }

        [Fact]
        public async void InvokeFromStringAsync_WithTypeParameter_WithModuleFactory_ThrowsArgumentNullExceptionIfModuleIsNotCachedButModuleFactoryIsNull()
        {
            // Arrange
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService();
            mockTestSubject.CallBase = true;
            mockTestSubject.
                Setup(t => t.TryInvokeFromCacheAsync<int>("dummyCacheIdentifier", null, null, default)).
                ReturnsAsync((false, 0));

            // Act and assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await mockTestSubject.Object.InvokeFromStringAsync<int>((Func<string>)null, "dummyCacheIdentifier").ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async void InvokeFromStringAsync_WithTypeParameter_WithModuleFactory_InvokesFromCacheIfModuleIsCached()
        {
            // Arrange
            const int dummyResult = 1;
            const string dummyCacheIdentifier = "dummyCacheIdentifier";
            const string dummyExportName = "dummyExportName";
            var dummyArgs = new object[0];
            var dummyCancellationToken = new CancellationToken();
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService();
            mockTestSubject.CallBase = true;
            mockTestSubject.
                Setup(t => t.TryInvokeFromCacheAsync<int>(dummyCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken)).
                ReturnsAsync((true, dummyResult));

            // Act
            int result = await mockTestSubject.Object.InvokeFromStringAsync<int>(() => "dummyModule", dummyCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken).ConfigureAwait(false);

            // Assert
            _mockRepository.VerifyAll();
            Assert.Equal(dummyResult, result);
        }

        [Fact]
        public async void InvokeFromStringAsync_WithTypeParameter_WithModuleFactory_InvokesFromStringAndCachesModuleIfModuleIsNotCached()
        {
            // Arrange
            const int dummyResult = 1;
            const string dummyCacheIdentifier = "dummyCacheIdentifier";
            const string dummyExportName = "dummyExportName";
            const string dummyModule = "dummyModule";
            var dummyArgs = new object[0];
            var dummyCancellationToken = new CancellationToken();
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService();
            mockTestSubject.CallBase = true;
            mockTestSubject.
                Setup(t => t.TryInvokeFromCacheAsync<int>(dummyCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken)).
                ReturnsAsync((false, 0));
            mockTestSubject.
                Setup(t => t.TryInvokeCoreAsync<int>(It.Is<InvocationRequest>(
                        invocationRequest =>
                            invocationRequest.ModuleSourceType == ModuleSourceType.String &&
                            invocationRequest.ModuleSource == dummyModule &&
                            invocationRequest.NewCacheIdentifier == dummyCacheIdentifier &&
                            invocationRequest.ExportName == dummyExportName &&
                            invocationRequest.Args == dummyArgs &&
                            invocationRequest.ModuleStreamSource == null),
                    dummyCancellationToken)).
                ReturnsAsync((true, dummyResult));

            // Act
            int result = await mockTestSubject.Object.InvokeFromStringAsync<int>(() => dummyModule, dummyCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken).ConfigureAwait(false);

            // Assert
            _mockRepository.VerifyAll();
            Assert.Equal(dummyResult, result);
        }

        [Fact]
        public async void InvokeFromStringAsync_WithoutTypeParameter_WithModuleFactory_IfModuleIsCachedInvokesFromCacheOtherwiseInvokesFromString()
        {
            // Arrange
            Func<string> dummyFactory = () => "dummyModule";
            const string dummyCacheIdentifier = "dummyCacheIdentifier";
            const string dummyExportName = "dummyExportName";
            var dummyArgs = new object[0];
            var dummyCancellationToken = new CancellationToken();
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService();
            mockTestSubject.CallBase = true;
            mockTestSubject.
                Setup(t => t.InvokeFromStringAsync<Void>(dummyFactory, dummyCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken)).
                ReturnsAsync((Void)null);

            // Act
            await mockTestSubject.Object.InvokeFromStringAsync(dummyFactory, dummyCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken).ConfigureAwait(false);

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
            var dummyArgs = new object[0];
            var dummyCancellationToken = new CancellationToken();
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService();
            mockTestSubject.CallBase = true;
            mockTestSubject.
                Setup(t => t.TryInvokeCoreAsync<int>(It.Is<InvocationRequest>(
                        invocationRequest =>
                            invocationRequest.ModuleSourceType == ModuleSourceType.Stream &&
                            invocationRequest.ModuleSource == null &&
                            invocationRequest.NewCacheIdentifier == dummyNewCacheIdentifier &&
                            invocationRequest.ExportName == dummyExportName &&
                            invocationRequest.Args == dummyArgs &&
                            invocationRequest.ModuleStreamSource == dummyModuleStream),
                    dummyCancellationToken)).
                ReturnsAsync((true, dummyResult));

            // Act
            int result = await mockTestSubject.Object.InvokeFromStreamAsync<int>(dummyModuleStream, dummyNewCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken).ConfigureAwait(false);

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
            var dummyArgs = new object[0];
            var dummyCancellationToken = new CancellationToken();
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService();
            mockTestSubject.CallBase = true;
            mockTestSubject.
                Setup(t => t.InvokeFromStreamAsync<Void>(dummyModuleStream, dummyNewCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken)).
                ReturnsAsync((Void)null);

            // Act
            await mockTestSubject.Object.InvokeFromStreamAsync(dummyModuleStream, dummyNewCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken).ConfigureAwait(false);

            // Assert
            _mockRepository.VerifyAll();
        }

        [Fact]
        public async void InvokeFromStreamAsync_WithTypeParameter_WithModuleFactory_ThrowsArgumentNullExceptionIfModuleIsNotCachedButModuleFactoryIsNull()
        {
            // Arrange
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService();
            mockTestSubject.CallBase = true;
            mockTestSubject.
                Setup(t => t.TryInvokeFromCacheAsync<int>("dummyCacheIdentifier", null, null, default)).
                ReturnsAsync((false, 0));

            // Act and assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await mockTestSubject.Object.InvokeFromStreamAsync<int>((Func<Stream>)null, "dummyCacheIdentifier").ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async void InvokeFromStreamAsync_WithTypeParameter_WithModuleFactory_InvokesFromCacheIfModuleIsCached()
        {
            // Arrange
            const int dummyResult = 1;
            const string dummyCacheIdentifier = "dummyCacheIdentifier";
            const string dummyExportName = "dummyExportName";
            var dummyArgs = new object[0];
            var dummyCancellationToken = new CancellationToken();
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService();
            mockTestSubject.CallBase = true;
            mockTestSubject.
                Setup(t => t.TryInvokeFromCacheAsync<int>(dummyCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken)).
                ReturnsAsync((true, dummyResult));

            // Act
            int result = await mockTestSubject.Object.InvokeFromStreamAsync<int>(() => new MemoryStream(), dummyCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken).ConfigureAwait(false);

            // Assert
            _mockRepository.VerifyAll();
            Assert.Equal(dummyResult, result);
        }

        [Fact]
        public async void InvokeFromStreamAsync_WithTypeParameter_WithModuleFactory_InvokesFromStreamIfModuleIsNotCached()
        {
            // Arrange
            const int dummyResult = 1;
            const string dummyCacheIdentifier = "dummyCacheIdentifier";
            const string dummyExportName = "dummyExportName";
#pragma warning disable IDE0067
            var dummyModule = new MemoryStream();
#pragma warning disable IDE0067
            var dummyArgs = new object[0];
            var dummyCancellationToken = new CancellationToken();
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService();
            mockTestSubject.CallBase = true;
            mockTestSubject.
                Setup(t => t.TryInvokeFromCacheAsync<int>(dummyCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken)).
                ReturnsAsync((false, 0));
            mockTestSubject.
                Setup(t => t.TryInvokeCoreAsync<int>(It.Is<InvocationRequest>(
                        invocationRequest =>
                            invocationRequest.ModuleSourceType == ModuleSourceType.Stream &&
                            invocationRequest.ModuleSource == null &&
                            invocationRequest.NewCacheIdentifier == dummyCacheIdentifier &&
                            invocationRequest.ExportName == dummyExportName &&
                            invocationRequest.Args == dummyArgs &&
                            invocationRequest.ModuleStreamSource == dummyModule),
                    dummyCancellationToken)).
                ReturnsAsync((true, dummyResult));

            // Act
            int result = await mockTestSubject.Object.InvokeFromStreamAsync<int>(() => dummyModule, dummyCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken).ConfigureAwait(false);

            // Assert
            _mockRepository.VerifyAll();
            Assert.Equal(dummyResult, result);
        }

        [Fact]
        public async void InvokeFromStreamAsync_WithoutTypeParameter_WithModuleFactory_IfModuleIsCachedInvokesFromCacheOtherwiseInvokesFromStream()
        {
            // Arrange
            Func<Stream> dummyFactory = () => new MemoryStream();
            const string dummyCacheIdentifier = "dummyCacheIdentifier";
            const string dummyExportName = "dummyExportName";
            var dummyArgs = new object[0];
            var dummyCancellationToken = new CancellationToken();
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService();
            mockTestSubject.CallBase = true;
            mockTestSubject.
                Setup(t => t.InvokeFromStreamAsync<Void>(dummyFactory, dummyCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken)).ReturnsAsync((Void)null);

            // Act
            await mockTestSubject.Object.InvokeFromStreamAsync(dummyFactory, dummyCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken).ConfigureAwait(false);

            // Assert
            _mockRepository.VerifyAll();
        }

        [Fact]
        public async void TryInvokeFromCacheAsync_WithTypeParameter_InvokesFromCache()
        {
            // Arrange
            const int dummyResult = 1;
            const string dummyModuleCacheIdentifier = "dummyModuleCacheIdentifier";
            const string dummyExportName = "dummyExportName";
            var dummyArgs = new object[0];
            var dummyCancellationToken = new CancellationToken();
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService();
            mockTestSubject.CallBase = true;
            mockTestSubject.
                Setup(t => t.TryInvokeCoreAsync<int>(It.Is<InvocationRequest>(
                        invocationRequest =>
                            invocationRequest.ModuleSourceType == ModuleSourceType.Cache &&
                            invocationRequest.ModuleSource == dummyModuleCacheIdentifier &&
                            invocationRequest.NewCacheIdentifier == null &&
                            invocationRequest.ExportName == dummyExportName &&
                            invocationRequest.Args == dummyArgs &&
                            invocationRequest.ModuleStreamSource == null),
                    dummyCancellationToken)).
                ReturnsAsync((true, dummyResult));

            // Act
            (bool success, int result) = await mockTestSubject.Object.TryInvokeFromCacheAsync<int>(dummyModuleCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken).ConfigureAwait(false);

            // Assert
            Assert.True(success);
            Assert.Equal(dummyResult, result);
            _mockRepository.VerifyAll();
        }

        [Fact]
        public async void TryInvokeFromCacheAsync_WithoutTypeParameter_InvokesFromCache()
        {
            // Arrange
            const string dummyModuleCacheIdentifier = "dummyModuleCacheIdentifier";
            const string dummyExportName = "dummyExportName";
            var dummyArgs = new object[0];
            var dummyCancellationToken = new CancellationToken();
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService();
            mockTestSubject.CallBase = true;
            mockTestSubject.
                Setup(t => t.TryInvokeFromCacheAsync<Void>(dummyModuleCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken)).
                ReturnsAsync((true, null));

            // Act
            bool success = await mockTestSubject.Object.TryInvokeFromCacheAsync(dummyModuleCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken).ConfigureAwait(false);

            // Assert
            Assert.True(success);
            _mockRepository.VerifyAll();
        }

        [Fact]
        public async void TryInvokeCoreAsync_ThrowsObjectDisposedExceptionIfObjectHasBeenDisposedOf()
        {
            // Arrange
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService();
            mockTestSubject.CallBase = true;

            // Act and assert
            mockTestSubject.Object.Dispose();
            ObjectDisposedException result = await Assert.ThrowsAsync<ObjectDisposedException>(async () => await mockTestSubject.Object.TryInvokeCoreAsync<string>(null, CancellationToken.None).ConfigureAwait(false)).ConfigureAwait(false);
            Assert.Equal($"Cannot access a disposed object.\nObject name: '{nameof(OutOfProcessNodeJSService)}'.", result.Message, ignoreLineEndingDifferences: true);
        }

        [Fact(Timeout = TIMEOUT_MS)]
        public void TryInvokeCoreAsync_FirstThreadLazilyCreatesNodeJSProcessBeforeInvoking()
        {
            // Arrange
            const string dummyServerScript = "dummyServerScript";
            const string dummyServerScriptName = "dummyServerScriptName";
            bool dummyIsConnected = false;
            var dummyCancellationToken = new CancellationToken();
            var dummyServerScriptAssembly = new DummyAssembly();
            (bool, int) dummyReturnValue = (true, 1);
            Mock<IEmbeddedResourcesService> mockEmbeddedResourcesService = _mockRepository.Create<IEmbeddedResourcesService>();
            mockEmbeddedResourcesService.Setup(e => e.ReadAsString(dummyServerScriptAssembly, dummyServerScriptName)).Returns(dummyServerScript);
            Mock<INodeJSProcess> mockNodeJSProcess = _mockRepository.Create<INodeJSProcess>();
            mockNodeJSProcess.
                Setup(n => n.AddOutputDataReceivedHandler(It.IsAny<DataReceivedEventHandler>())).
                Callback<DataReceivedEventHandler>(dataReceivedEventHandler => dataReceivedEventHandler(null, CreateDataReceivedEventArgs(OutOfProcessNodeJSService.CONNECTION_ESTABLISHED_MESSAGE_START)));
            mockNodeJSProcess.Setup(n => n.AddErrorDataReceivedHandler(It.IsAny<DataReceivedEventHandler>()));
            mockNodeJSProcess.Setup(n => n.BeginOutputReadLine());
            mockNodeJSProcess.Setup(n => n.BeginErrorReadLine());
            mockNodeJSProcess.Setup(n => n.SetConnected()).Callback(() => dummyIsConnected = true);
            mockNodeJSProcess.Setup(n => n.Connected).Returns(() => dummyIsConnected);
            Mock<INodeJSProcessFactory> mockNodeJSProcessFactory = _mockRepository.Create<INodeJSProcessFactory>();
            mockNodeJSProcessFactory.Setup(n => n.Create(dummyServerScript)).Returns(mockNodeJSProcess.Object);
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(mockNodeJSProcessFactory.Object,
                embeddedResourcesService: mockEmbeddedResourcesService.Object,
                serverScriptAssembly: dummyServerScriptAssembly,
                serverScriptName: dummyServerScriptName);
            mockTestSubject.CallBase = true;
            mockTestSubject.Setup(t => t.CreateCts(dummyCancellationToken)).Returns((dummyCancellationToken, null));
            mockTestSubject.Protected().As<IOutOfProcessNodeJSServiceProtectedMembers>()
                .Setup(t => t.TryInvokeAsync<int>(null, dummyCancellationToken)).
                ReturnsAsync(dummyReturnValue);
            OutOfProcessNodeJSService testSubject = mockTestSubject.Object;

            // Act
            var results = new ConcurrentQueue<(bool, int)>();
            const int numThreads = 5;
            var threads = new List<Thread>();
            for (int i = 0; i < numThreads; i++)
            {
                var thread = new Thread(() => results.Enqueue(testSubject.TryInvokeCoreAsync<int>(null, dummyCancellationToken).GetAwaiter().GetResult()));
                threads.Add(thread);
                thread.Start();
            }
            foreach (Thread thread in threads)
            {
                thread.Join();
            }

            // Assert
            _mockRepository.VerifyAll();
            mockTestSubject.Verify(t => t.CreateAndConnectToNodeJSProcess(It.IsAny<EventWaitHandle>()), Times.Once); // Only creates and connects once
            mockTestSubject.Protected().As<IOutOfProcessNodeJSServiceProtectedMembers>().
                Verify(t => t.TryInvokeAsync<int>(null, dummyCancellationToken), Times.Exactly(numThreads)); // Invokes javascript for each invocation request
            foreach ((bool success, int value) in results)
            {
                Assert.Equal(success, dummyReturnValue.Item1);
                Assert.Equal(value, dummyReturnValue.Item2);
            }
        }

        [Fact(Timeout = TIMEOUT_MS)]
        public async void TryInvokeCoreAsync_IfNodeJSProcessIsNotConnectedFirstThreadCreatesNodeJSProcessBeforeInvoking()
        {
            // Arrange
            const string dummyServerScript = "dummyServerScript";
            const string dummyServerScriptName = "dummyServerScriptName";
            bool dummyIsConnected = false;
            var dummyCancellationToken = new CancellationToken();
            (bool, int) dummyReturnValue = (true, 1);
            var dummyServerScriptAssembly = new DummyAssembly();
            Mock<IEmbeddedResourcesService> mockEmbeddedResourcesService = _mockRepository.Create<IEmbeddedResourcesService>();
            mockEmbeddedResourcesService.Setup(e => e.ReadAsString(dummyServerScriptAssembly, dummyServerScriptName)).Returns(dummyServerScript);
            Mock<INodeJSProcess> mockNodeJSProcess = _mockRepository.Create<INodeJSProcess>();
            mockNodeJSProcess.
                Setup(n => n.AddOutputDataReceivedHandler(It.IsAny<DataReceivedEventHandler>())).
                Callback<DataReceivedEventHandler>(dataReceivedEventHandler => dataReceivedEventHandler(null, CreateDataReceivedEventArgs(OutOfProcessNodeJSService.CONNECTION_ESTABLISHED_MESSAGE_START)));
            mockNodeJSProcess.Setup(n => n.AddErrorDataReceivedHandler(It.IsAny<DataReceivedEventHandler>()));
            mockNodeJSProcess.Setup(n => n.BeginOutputReadLine());
            mockNodeJSProcess.Setup(n => n.BeginErrorReadLine());
            mockNodeJSProcess.Setup(n => n.SetConnected()).Callback(() => dummyIsConnected = true);
            mockNodeJSProcess.Setup(n => n.Connected).Returns(() => dummyIsConnected);
            mockNodeJSProcess.Setup(n => n.Dispose());
            Mock<INodeJSProcessFactory> mockNodeJSProcessFactory = _mockRepository.Create<INodeJSProcessFactory>();
            mockNodeJSProcessFactory.Setup(n => n.Create(dummyServerScript)).Returns(mockNodeJSProcess.Object);
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(mockNodeJSProcessFactory.Object,
                embeddedResourcesService: mockEmbeddedResourcesService.Object,
                serverScriptAssembly: dummyServerScriptAssembly,
                serverScriptName: dummyServerScriptName);
            mockTestSubject.CallBase = true;
            mockTestSubject.Setup(t => t.CreateCts(dummyCancellationToken)).Returns((dummyCancellationToken, null));
            mockTestSubject.Protected().As<IOutOfProcessNodeJSServiceProtectedMembers>().
                Setup(t => t.TryInvokeAsync<int>(null, dummyCancellationToken)).ReturnsAsync(dummyReturnValue);
            OutOfProcessNodeJSService testSubject = mockTestSubject.Object;

            // Act
            await testSubject.TryInvokeCoreAsync<int>(null, dummyCancellationToken).ConfigureAwait(false); // Set _nodeJSProcess
            dummyIsConnected = false; // Set _nodeJSProcess.Connected to false
            var results = new ConcurrentQueue<(bool, int)>();
            const int numThreads = 5;
            var threads = new List<Thread>();
            for (int i = 0; i < numThreads; i++)
            {
                var thread = new Thread(() => results.Enqueue(testSubject.TryInvokeCoreAsync<int>(null, dummyCancellationToken).GetAwaiter().GetResult()));
                threads.Add(thread);
                thread.Start();
            }
            foreach (Thread thread in threads)
            {
                thread.Join();
            }

            // Assert
            _mockRepository.VerifyAll();
            mockTestSubject.Verify(t => t.CreateAndConnectToNodeJSProcess(It.IsAny<EventWaitHandle>()), Times.Exactly(2)); // Creates NodeJS process twice
            mockTestSubject.Protected().As<IOutOfProcessNodeJSServiceProtectedMembers>().
                Verify(t => t.TryInvokeAsync<int>(null, dummyCancellationToken), Times.Exactly(numThreads + 1)); // Invokes javascript for each invocation request
            foreach ((bool success, int value) in results)
            {
                Assert.Equal(success, dummyReturnValue.Item1);
                Assert.Equal(value, dummyReturnValue.Item2);
            }
        }

        [Fact(Timeout = TIMEOUT_MS)]
        public void TryInvokeCoreAsync_RetriesConnectionIfConnectionAttemptTimesoutAndThrowsInvocationExceptionIfNoRetriesRemain()
        {
            // Arrange
            const string dummyExitCode = "dummyExitCode";
            const bool dummyHasExited = false;
            const int dummyTimeoutMS = 200; // Arbitrary
            const string dummyServerScript = "dummyServerScript";
            const string dummyServerScriptName = "dummyServerScriptName";
            var dummyCancellationToken = new CancellationToken();
            var dummyServerScriptAssembly = new DummyAssembly();
            Mock<IEmbeddedResourcesService> mockEmbeddedResourcesService = _mockRepository.Create<IEmbeddedResourcesService>();
            mockEmbeddedResourcesService.Setup(e => e.ReadAsString(dummyServerScriptAssembly, dummyServerScriptName)).Returns(dummyServerScript);
            Mock<INodeJSProcess> mockNodeJSProcess = _mockRepository.Create<INodeJSProcess>();
            mockNodeJSProcess.Setup(n => n.AddOutputDataReceivedHandler(It.IsAny<DataReceivedEventHandler>()));
            mockNodeJSProcess.Setup(n => n.AddErrorDataReceivedHandler(It.IsAny<DataReceivedEventHandler>()));
            mockNodeJSProcess.Setup(n => n.BeginOutputReadLine());
            mockNodeJSProcess.Setup(n => n.BeginErrorReadLine());
            mockNodeJSProcess.Setup(n => n.Connected).Returns(false);
            mockNodeJSProcess.Setup(n => n.HasExited).Returns(dummyHasExited);
            mockNodeJSProcess.Setup(n => n.ExitStatus).Returns(dummyExitCode);
            Mock<INodeJSProcessFactory> mockNodeJSProcessFactory = _mockRepository.Create<INodeJSProcessFactory>();
            mockNodeJSProcessFactory.Setup(n => n.Create(dummyServerScript)).Returns(mockNodeJSProcess.Object);
            var dummyOptions = new OutOfProcessNodeJSServiceOptions { TimeoutMS = dummyTimeoutMS };
            Mock<IOptions<OutOfProcessNodeJSServiceOptions>> mockOptionsAccessor = _mockRepository.Create<IOptions<OutOfProcessNodeJSServiceOptions>>();
            mockOptionsAccessor.Setup(o => o.Value).Returns(dummyOptions);
            var loggerStringBuilder = new StringBuilder();
            var dummyInvocationRequest = new InvocationRequest(ModuleSourceType.String, "dummyModuleSource");
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(mockNodeJSProcessFactory.Object,
                mockOptionsAccessor.Object,
                mockEmbeddedResourcesService.Object,
                dummyServerScriptAssembly,
                dummyServerScriptName,
                loggerStringBuilder);
            mockTestSubject.CallBase = true;
            OutOfProcessNodeJSService testSubject = mockTestSubject.Object;

            // Act
            var results = new ConcurrentQueue<Exception>();
            const int numThreads = 5;
            var threads = new List<Thread>();
            for (int i = 0; i < numThreads; i++)
            {
                var thread = new Thread(() =>
                {
                    try
                    {
                        testSubject.TryInvokeCoreAsync<int>(dummyInvocationRequest, dummyCancellationToken).GetAwaiter().GetResult();
                    }
                    catch (Exception exception)
                    {
                        results.Enqueue(exception);
                    }
                });
                threads.Add(thread);
                thread.Start();
            }
            foreach (Thread thread in threads)
            {
                thread.Join();
            }

            // Assert
            _mockRepository.VerifyAll();
            string logResult = loggerStringBuilder.ToString();
            Assert.Equal(numThreads, Regex.Matches(logResult, $"{nameof(LogLevel.Warning)}: ").Count); // Each thread must log a timed out connection attempt once
            mockTestSubject.Verify(t => t.CreateAndConnectToNodeJSProcess(It.IsAny<EventWaitHandle>()), Times.Exactly(numThreads * 2)); // Each thread tries to create and connect to NodeJS process twice
            Assert.Equal(numThreads, results.Count);
            foreach (Exception exception in results)
            {
                // Each thread must throw a connection attempt timed out invocation exception
                Assert.IsType<InvocationException>(exception);
                Assert.Equal(string.Format(Strings.InvocationException_OutOfProcessNodeJSService_ConnectionAttemptTimedOut, dummyTimeoutMS, dummyHasExited, dummyExitCode), exception.Message);
            }
        }

        [Fact(Timeout = TIMEOUT_MS)]
        public void TryInvokeCoreAsync_DoesNotRetryInvocationsThatAreCanceledAndThrowsOperationCanceledException()
        {
            // Arrange
            const string dummyServerScript = "dummyServerScript";
            bool dummyIsConnected = false;
            const string dummyServerScriptName = "dummyServerScriptName";
            var dummyException = new OperationCanceledException();
            using (var dummyCancellationTokenSource = new CancellationTokenSource())
            {
                CancellationToken dummyCancellationToken = dummyCancellationTokenSource.Token;
                var dummyServerScriptAssembly = new DummyAssembly();
                Mock<IEmbeddedResourcesService> mockEmbeddedResourcesService = _mockRepository.Create<IEmbeddedResourcesService>();
                mockEmbeddedResourcesService.Setup(e => e.ReadAsString(dummyServerScriptAssembly, dummyServerScriptName)).Returns(dummyServerScript);
                Mock<INodeJSProcess> mockNodeJSProcess = _mockRepository.Create<INodeJSProcess>();
                mockNodeJSProcess.
                    Setup(n => n.AddOutputDataReceivedHandler(It.IsAny<DataReceivedEventHandler>())).
                    Callback<DataReceivedEventHandler>(dataReceivedEventHandler => dataReceivedEventHandler(null, CreateDataReceivedEventArgs(OutOfProcessNodeJSService.CONNECTION_ESTABLISHED_MESSAGE_START)));
                mockNodeJSProcess.Setup(n => n.AddErrorDataReceivedHandler(It.IsAny<DataReceivedEventHandler>()));
                mockNodeJSProcess.Setup(n => n.BeginOutputReadLine());
                mockNodeJSProcess.Setup(n => n.BeginErrorReadLine());
                mockNodeJSProcess.Setup(n => n.SetConnected()).Callback(() => dummyIsConnected = true);
                mockNodeJSProcess.Setup(n => n.Connected).Returns(() => dummyIsConnected);
                Mock<INodeJSProcessFactory> mockNodeJSProcessFactory = _mockRepository.Create<INodeJSProcessFactory>();
                mockNodeJSProcessFactory.Setup(n => n.Create(dummyServerScript)).Returns(mockNodeJSProcess.Object);
                Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(mockNodeJSProcessFactory.Object,
                    embeddedResourcesService: mockEmbeddedResourcesService.Object,
                    serverScriptAssembly: dummyServerScriptAssembly,
                    serverScriptName: dummyServerScriptName);
                mockTestSubject.CallBase = true;
                mockTestSubject.Protected().As<IOutOfProcessNodeJSServiceProtectedMembers>().
                    Setup(t => t.TryInvokeAsync<int>(null, dummyCancellationToken)).ThrowsAsync(dummyException);
                mockTestSubject.Setup(t => t.CreateCts(dummyCancellationToken)).Returns((dummyCancellationToken, null));
                OutOfProcessNodeJSService testSubject = mockTestSubject.Object;

                // Act
                dummyCancellationTokenSource.Cancel(); // Cancel token
                var results = new ConcurrentQueue<Exception>();
                const int numThreads = 5;
                var threads = new List<Thread>();
                for (int i = 0; i < numThreads; i++)
                {
                    var thread = new Thread(() =>
                    {
                        try
                        {
                            testSubject.TryInvokeCoreAsync<int>(null, dummyCancellationToken).GetAwaiter().GetResult();
                        }
                        catch (Exception exception)
                        {
                            results.Enqueue(exception);
                        }
                    });
                    threads.Add(thread);
                    thread.Start();
                }
                foreach (Thread thread in threads)
                {
                    thread.Join();
                }

                // Assert
                _mockRepository.VerifyAll();
                mockTestSubject.Verify(t => t.CreateAndConnectToNodeJSProcess(It.IsAny<EventWaitHandle>()), Times.Once); // Only connects once
                mockTestSubject.Protected().As<IOutOfProcessNodeJSServiceProtectedMembers>().
                    Verify(t => t.TryInvokeAsync<int>(null, It.IsAny<CancellationToken>()), Times.Exactly(numThreads)); // No retries
                Assert.Equal(numThreads, results.Count);
                foreach (Exception exception in results)
                {
                    // Each thread must throw an OperationCanceledException
                    Assert.Same(dummyException, exception);
                }
            }
        }

        [Fact(Timeout = TIMEOUT_MS)]
        public void TryInvokeCoreAsync_RetriesInvocationsThatTimeoutAndThrowsInvocationExceptionIfNoRetriesRemain()
        {
            // Arrange
            const string dummyServerScript = "dummyServerScript";
            bool dummyIsConnected = false;
            const string dummyServerScriptName = "dummyServerScriptName";
            var dummyException = new OperationCanceledException();
            const int dummyTimeoutMS = 100;
            var dummyServerScriptAssembly = new DummyAssembly();
            Mock<IEmbeddedResourcesService> mockEmbeddedResourcesService = _mockRepository.Create<IEmbeddedResourcesService>();
            mockEmbeddedResourcesService.Setup(e => e.ReadAsString(dummyServerScriptAssembly, dummyServerScriptName)).Returns(dummyServerScript);
            Mock<INodeJSProcess> mockNodeJSProcess = _mockRepository.Create<INodeJSProcess>();
            mockNodeJSProcess.
                Setup(n => n.AddOutputDataReceivedHandler(It.IsAny<DataReceivedEventHandler>())).
                Callback<DataReceivedEventHandler>(dataReceivedEventHandler => dataReceivedEventHandler(null, CreateDataReceivedEventArgs(OutOfProcessNodeJSService.CONNECTION_ESTABLISHED_MESSAGE_START)));
            mockNodeJSProcess.Setup(n => n.AddErrorDataReceivedHandler(It.IsAny<DataReceivedEventHandler>()));
            mockNodeJSProcess.Setup(n => n.BeginOutputReadLine());
            mockNodeJSProcess.Setup(n => n.BeginErrorReadLine());
            mockNodeJSProcess.Setup(n => n.SetConnected()).Callback(() => dummyIsConnected = true);
            mockNodeJSProcess.Setup(n => n.Connected).Returns(() => dummyIsConnected);
            var dummyOptions = new OutOfProcessNodeJSServiceOptions { TimeoutMS = dummyTimeoutMS };
            Mock<IOptions<OutOfProcessNodeJSServiceOptions>> mockOptionsAccessor = _mockRepository.Create<IOptions<OutOfProcessNodeJSServiceOptions>>();
            mockOptionsAccessor.Setup(o => o.Value).Returns(dummyOptions);
            Mock<INodeJSProcessFactory> mockNodeJSProcessFactory = _mockRepository.Create<INodeJSProcessFactory>();
            mockNodeJSProcessFactory.Setup(n => n.Create(dummyServerScript)).Returns(mockNodeJSProcess.Object);
            var loggerStringBuilder = new StringBuilder();
            var dummyInvocationRequest = new InvocationRequest(ModuleSourceType.String, "dummyModuleSource");
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(mockNodeJSProcessFactory.Object,
                mockOptionsAccessor.Object,
                mockEmbeddedResourcesService.Object,
                dummyServerScriptAssembly,
                dummyServerScriptName,
                loggerStringBuilder: loggerStringBuilder);
            mockTestSubject.CallBase = true;
            mockTestSubject.Protected().As<IOutOfProcessNodeJSServiceProtectedMembers>().
                Setup(t => t.TryInvokeAsync<int>(dummyInvocationRequest, It.IsAny<CancellationToken>())).ThrowsAsync(dummyException);
            OutOfProcessNodeJSService testSubject = mockTestSubject.Object;

            // Act
            var results = new ConcurrentQueue<Exception>();
            const int numThreads = 5;
            var threads = new List<Thread>();
            for (int i = 0; i < numThreads; i++)
            {
                var thread = new Thread(() =>
                {
                    try
                    {
                        testSubject.TryInvokeCoreAsync<int>(dummyInvocationRequest, CancellationToken.None).GetAwaiter().GetResult();
                    }
                    catch (Exception exception)
                    {
                        results.Enqueue(exception);
                    }
                });
                threads.Add(thread);
                thread.Start();
            }
            foreach (Thread thread in threads)
            {
                thread.Join();
            }

            // Assert
            _mockRepository.VerifyAll();
            string logResult = loggerStringBuilder.ToString();
            Assert.Equal(numThreads, Regex.Matches(logResult, $"{nameof(LogLevel.Warning)}: ").Count); // Each thread must log a timed out connection attempt once
            mockTestSubject.Protected().As<IOutOfProcessNodeJSServiceProtectedMembers>().
                Verify(t => t.TryInvokeAsync<int>(dummyInvocationRequest, It.IsAny<CancellationToken>()), Times.Exactly(numThreads * 2)); // Each thread tries twice
            Assert.Equal(numThreads, results.Count);
            foreach (Exception exception in results)
            {
                Assert.IsType<InvocationException>(exception);
                Assert.Equal(string.Format(Strings.InvocationException_OutOfProcessNodeJSService_InvocationTimedOut,
                        dummyTimeoutMS,
                        nameof(OutOfProcessNodeJSServiceOptions.TimeoutMS),
                        nameof(OutOfProcessNodeJSServiceOptions)),
                    exception.Message);
            }
        }

        [Fact(Timeout = TIMEOUT_MS)]
        public void TryInvokeCoreAsync_RetriesInvocationsThatThrowExceptionsAndThrowsExceptionIfNoRetriesRemain()
        {
            // Arrange
            const string dummyServerScript = "dummyServerScript";
            bool dummyIsConnected = false;
            const string dummyServerScriptName = "dummyServerScriptName";
            var dummyException = new InvocationException();
            var dummyServerScriptAssembly = new DummyAssembly();
            Mock<IEmbeddedResourcesService> mockEmbeddedResourcesService = _mockRepository.Create<IEmbeddedResourcesService>();
            mockEmbeddedResourcesService.Setup(e => e.ReadAsString(dummyServerScriptAssembly, dummyServerScriptName)).Returns(dummyServerScript);
            Mock<INodeJSProcess> mockNodeJSProcess = _mockRepository.Create<INodeJSProcess>();
            mockNodeJSProcess.
                Setup(n => n.AddOutputDataReceivedHandler(It.IsAny<DataReceivedEventHandler>())).
                Callback<DataReceivedEventHandler>(dataReceivedEventHandler => dataReceivedEventHandler(null, CreateDataReceivedEventArgs(OutOfProcessNodeJSService.CONNECTION_ESTABLISHED_MESSAGE_START)));
            mockNodeJSProcess.Setup(n => n.AddErrorDataReceivedHandler(It.IsAny<DataReceivedEventHandler>()));
            mockNodeJSProcess.Setup(n => n.BeginOutputReadLine());
            mockNodeJSProcess.Setup(n => n.BeginErrorReadLine());
            mockNodeJSProcess.Setup(n => n.SetConnected()).Callback(() => dummyIsConnected = true);
            mockNodeJSProcess.Setup(n => n.Connected).Returns(() => dummyIsConnected);
            Mock<INodeJSProcessFactory> mockNodeJSProcessFactory = _mockRepository.Create<INodeJSProcessFactory>();
            mockNodeJSProcessFactory.Setup(n => n.Create(dummyServerScript)).Returns(mockNodeJSProcess.Object);
            var loggerStringBuilder = new StringBuilder();
            var dummyInvocationRequest = new InvocationRequest(ModuleSourceType.String, "dummyModuleSource");
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(mockNodeJSProcessFactory.Object,
                embeddedResourcesService: mockEmbeddedResourcesService.Object,
                serverScriptAssembly: dummyServerScriptAssembly,
                serverScriptName: dummyServerScriptName,
                loggerStringBuilder: loggerStringBuilder);
            mockTestSubject.CallBase = true;
            mockTestSubject.Protected().As<IOutOfProcessNodeJSServiceProtectedMembers>().
                Setup(t => t.TryInvokeAsync<int>(dummyInvocationRequest, It.IsAny<CancellationToken>())).ThrowsAsync(dummyException);
            OutOfProcessNodeJSService testSubject = mockTestSubject.Object;

            // Act
            var results = new ConcurrentQueue<Exception>();
            const int numThreads = 5;
            var threads = new List<Thread>();
            for (int i = 0; i < numThreads; i++)
            {
                var thread = new Thread(() =>
                {
                    try
                    {
                        testSubject.TryInvokeCoreAsync<int>(dummyInvocationRequest, CancellationToken.None).GetAwaiter().GetResult();
                    }
                    catch (Exception exception)
                    {
                        results.Enqueue(exception);
                    }
                });
                threads.Add(thread);
                thread.Start();
            }
            foreach (Thread thread in threads)
            {
                thread.Join();
            }

            // Assert
            _mockRepository.VerifyAll();
            string logResult = loggerStringBuilder.ToString();
            Assert.Equal(numThreads, Regex.Matches(logResult, $"{nameof(LogLevel.Warning)}: ").Count); // Each thread must log a timed out connection attempt once
            mockTestSubject.Protected().As<IOutOfProcessNodeJSServiceProtectedMembers>().
                Verify(t => t.TryInvokeAsync<int>(dummyInvocationRequest, It.IsAny<CancellationToken>()), Times.Exactly(numThreads * 2)); // Each thread tries twice
            Assert.Equal(numThreads, results.Count);
            foreach (Exception exception in results)
            {
                Assert.Same(dummyException, exception);
            }
        }

        [Fact(Timeout = TIMEOUT_MS)]
        public void TryInvokeCoreAsync_DoesNotRetryInvocationIfModuleSourceIsAnUnseekableStream()
        {
            // Arrange
            const string dummyServerScript = "dummyServerScript";
            const string dummyServerScriptName = "dummyServerScriptName";
            var dummyException = new InvocationException();
            var dummyServerScriptAssembly = new DummyAssembly();
            Mock<IEmbeddedResourcesService> mockEmbeddedResourcesService = _mockRepository.Create<IEmbeddedResourcesService>();
            mockEmbeddedResourcesService.Setup(e => e.ReadAsString(dummyServerScriptAssembly, dummyServerScriptName)).Returns(dummyServerScript);
            Mock<INodeJSProcess> mockNodeJSProcess = _mockRepository.Create<INodeJSProcess>();
            mockNodeJSProcess.
                Setup(n => n.AddOutputDataReceivedHandler(It.IsAny<DataReceivedEventHandler>())).
                Callback<DataReceivedEventHandler>(dataReceivedEventHandler => dataReceivedEventHandler(null, CreateDataReceivedEventArgs(OutOfProcessNodeJSService.CONNECTION_ESTABLISHED_MESSAGE_START)));
            mockNodeJSProcess.Setup(n => n.AddErrorDataReceivedHandler(It.IsAny<DataReceivedEventHandler>()));
            mockNodeJSProcess.Setup(n => n.BeginOutputReadLine());
            mockNodeJSProcess.Setup(n => n.BeginErrorReadLine());
            mockNodeJSProcess.Setup(n => n.SetConnected());
            Mock<INodeJSProcessFactory> mockNodeJSProcessFactory = _mockRepository.Create<INodeJSProcessFactory>();
            mockNodeJSProcessFactory.Setup(n => n.Create(dummyServerScript)).Returns(mockNodeJSProcess.Object);
            Mock<Stream> mockStream = _mockRepository.Create<Stream>();
            mockStream.Setup(s => s.CanSeek).Returns(false);
            var dummyInvocationRequest = new InvocationRequest(ModuleSourceType.Stream, moduleStreamSource: mockStream.Object);
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(mockNodeJSProcessFactory.Object,
                embeddedResourcesService: mockEmbeddedResourcesService.Object,
                serverScriptAssembly: dummyServerScriptAssembly,
                serverScriptName: dummyServerScriptName);
            mockTestSubject.CallBase = true;
            mockTestSubject.Protected().As<IOutOfProcessNodeJSServiceProtectedMembers>().
                Setup(t => t.TryInvokeAsync<int>(dummyInvocationRequest, It.IsAny<CancellationToken>())).ThrowsAsync(dummyException);

            // Act and assert
            InvocationException result = Assert.Throws<InvocationException>(() => mockTestSubject.Object.TryInvokeCoreAsync<int>(dummyInvocationRequest, CancellationToken.None).GetAwaiter().GetResult());
            Assert.Equal(dummyException, result);
            _mockRepository.VerifyAll();
            mockTestSubject.
                Protected().
                As<IOutOfProcessNodeJSServiceProtectedMembers>().
                Verify(t => t.TryInvokeAsync<int>(dummyInvocationRequest, It.IsAny<CancellationToken>()), Times.Once()); // No retries
        }

        [Fact(Timeout = TIMEOUT_MS)]
        public void TryInvokeCoreAsync_ResetsStreamPositionAndRetriesInvocationIfModuleSourceIsASeekableStreamThatIsNotAtItsInitialPosition()
        {
            // Arrange
            const string dummyServerScript = "dummyServerScript";
            const string dummyServerScriptName = "dummyServerScriptName";
            var dummyException = new InvocationException();
            var dummyServerScriptAssembly = new DummyAssembly();
            Mock<IEmbeddedResourcesService> mockEmbeddedResourcesService = _mockRepository.Create<IEmbeddedResourcesService>();
            mockEmbeddedResourcesService.Setup(e => e.ReadAsString(dummyServerScriptAssembly, dummyServerScriptName)).Returns(dummyServerScript);
            Mock<INodeJSProcess> mockNodeJSProcess = _mockRepository.Create<INodeJSProcess>();
            mockNodeJSProcess.
                Setup(n => n.AddOutputDataReceivedHandler(It.IsAny<DataReceivedEventHandler>())).
                Callback<DataReceivedEventHandler>(dataReceivedEventHandler => dataReceivedEventHandler(null, CreateDataReceivedEventArgs(OutOfProcessNodeJSService.CONNECTION_ESTABLISHED_MESSAGE_START)));
            mockNodeJSProcess.Setup(n => n.AddErrorDataReceivedHandler(It.IsAny<DataReceivedEventHandler>()));
            mockNodeJSProcess.Setup(n => n.BeginOutputReadLine());
            mockNodeJSProcess.Setup(n => n.BeginErrorReadLine());
            mockNodeJSProcess.Setup(n => n.SetConnected());
            Mock<INodeJSProcessFactory> mockNodeJSProcessFactory = _mockRepository.Create<INodeJSProcessFactory>();
            mockNodeJSProcessFactory.Setup(n => n.Create(dummyServerScript)).Returns(mockNodeJSProcess.Object);
            const int dummyStreamInitialPosition = 1;
            Mock<Stream> mockStream = _mockRepository.Create<Stream>();
            mockStream.Setup(s => s.CanSeek).Returns(true);
            mockStream.SetupSequence(s => s.Position).Returns(dummyStreamInitialPosition).Returns(2); // Position changes after caching of initial position
            var dummyInvocationRequest = new InvocationRequest(ModuleSourceType.Stream, moduleStreamSource: mockStream.Object);
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(mockNodeJSProcessFactory.Object,
                embeddedResourcesService: mockEmbeddedResourcesService.Object,
                serverScriptAssembly: dummyServerScriptAssembly,
                serverScriptName: dummyServerScriptName);
            mockTestSubject.CallBase = true;
            mockTestSubject.Protected().As<IOutOfProcessNodeJSServiceProtectedMembers>().
                Setup(t => t.TryInvokeAsync<int>(dummyInvocationRequest, It.IsAny<CancellationToken>())).ThrowsAsync(dummyException);

            // Act and assert
            InvocationException result = Assert.Throws<InvocationException>(() => mockTestSubject.Object.TryInvokeCoreAsync<int>(dummyInvocationRequest, CancellationToken.None).GetAwaiter().GetResult());
            Assert.Equal(dummyException, result);
            _mockRepository.VerifyAll();
            mockTestSubject.
                Protected().
                As<IOutOfProcessNodeJSServiceProtectedMembers>().
                Verify(t => t.TryInvokeAsync<int>(dummyInvocationRequest, It.IsAny<CancellationToken>()), Times.Exactly(2)); // Retries
            mockStream.VerifySet(s => s.Position = dummyStreamInitialPosition);
        }

        [Fact(Timeout = TIMEOUT_MS)]
        public void TryInvokeCoreAsync_RetriesInvocationIfModuleSourceIsASeekableStream()
        {
            // Arrange
            const string dummyServerScript = "dummyServerScript";
            const string dummyServerScriptName = "dummyServerScriptName";
            var dummyException = new InvocationException();
            var dummyServerScriptAssembly = new DummyAssembly();
            Mock<IEmbeddedResourcesService> mockEmbeddedResourcesService = _mockRepository.Create<IEmbeddedResourcesService>();
            mockEmbeddedResourcesService.Setup(e => e.ReadAsString(dummyServerScriptAssembly, dummyServerScriptName)).Returns(dummyServerScript);
            Mock<INodeJSProcess> mockNodeJSProcess = _mockRepository.Create<INodeJSProcess>();
            mockNodeJSProcess.
                Setup(n => n.AddOutputDataReceivedHandler(It.IsAny<DataReceivedEventHandler>())).
                Callback<DataReceivedEventHandler>(dataReceivedEventHandler => dataReceivedEventHandler(null, CreateDataReceivedEventArgs(OutOfProcessNodeJSService.CONNECTION_ESTABLISHED_MESSAGE_START)));
            mockNodeJSProcess.Setup(n => n.AddErrorDataReceivedHandler(It.IsAny<DataReceivedEventHandler>()));
            mockNodeJSProcess.Setup(n => n.BeginOutputReadLine());
            mockNodeJSProcess.Setup(n => n.BeginErrorReadLine());
            mockNodeJSProcess.Setup(n => n.SetConnected());
            Mock<INodeJSProcessFactory> mockNodeJSProcessFactory = _mockRepository.Create<INodeJSProcessFactory>();
            mockNodeJSProcessFactory.Setup(n => n.Create(dummyServerScript)).Returns(mockNodeJSProcess.Object);
            Mock<Stream> mockStream = _mockRepository.Create<Stream>();
            mockStream.Setup(s => s.CanSeek).Returns(true);
            mockStream.SetupSequence(s => s.Position).Returns(1).Returns(1); // Stays at initial position
            var dummyInvocationRequest = new InvocationRequest(ModuleSourceType.Stream, moduleStreamSource: mockStream.Object);
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(mockNodeJSProcessFactory.Object,
                embeddedResourcesService: mockEmbeddedResourcesService.Object,
                serverScriptAssembly: dummyServerScriptAssembly,
                serverScriptName: dummyServerScriptName);
            mockTestSubject.CallBase = true;
            mockTestSubject.Protected().As<IOutOfProcessNodeJSServiceProtectedMembers>().
                Setup(t => t.TryInvokeAsync<int>(dummyInvocationRequest, It.IsAny<CancellationToken>())).ThrowsAsync(dummyException);

            // Act and assert
            InvocationException result = Assert.Throws<InvocationException>(() => mockTestSubject.Object.TryInvokeCoreAsync<int>(dummyInvocationRequest, CancellationToken.None).GetAwaiter().GetResult());
            Assert.Equal(dummyException, result);
            _mockRepository.VerifyAll();
            mockTestSubject.
                Protected().
                As<IOutOfProcessNodeJSServiceProtectedMembers>().
                Verify(t => t.TryInvokeAsync<int>(dummyInvocationRequest, It.IsAny<CancellationToken>()), Times.Exactly(2)); // Retries
            mockStream.VerifySet(s => s.Position = It.IsAny<int>(), Times.Never);
        }

        // Note for the following (CreateCts) tests: CancellationToken1.Equal(CancellationToken2) returns CancellationToken1._source == CancellationToken2._source where source is 
        // the cancellation token's parent CancellationTokenSource. CancellationToken is a struct while CancellationTokenSource is a class.
        [Theory]
        [MemberData(nameof(CreateCts_ReturnsCancellationTokenAndCancellationTokenSourceIfTimeoutIsNotInfinite_Data))]
        public void CreateCts_ReturnsCancellationTokenAndCancellationTokenSourceIfTimeoutIsNotInfinite(CancellationToken dummyCancellationToken)
        {
            // Arrange
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService();
            mockTestSubject.CallBase = true;

            // Act
            (CancellationToken resultCancellationToken, CancellationTokenSource resultCancellationTokenSource) = mockTestSubject.Object.CreateCts(dummyCancellationToken);

            // Assert
            Assert.NotNull(resultCancellationTokenSource);
            Assert.Equal(resultCancellationToken, resultCancellationTokenSource.Token);
        }

        public static IEnumerable<object[]> CreateCts_ReturnsCancellationTokenAndCancellationTokenSourceIfTimeoutIsNotInfinite_Data()
        {
            var dummyCancellationTokenSource = new CancellationTokenSource();

            return new object[][]{
                new object[]{CancellationToken.None},
                new object[]{dummyCancellationTokenSource.Token}
            };
        }

        [Fact]
        public void CreateCts_ReturnsOriginalCancellationTokenIfTimeoutIsInfinite()
        {
            // Arrange
            var dummyOptions = new OutOfProcessNodeJSServiceOptions { TimeoutMS = -1 };
            Mock<IOptions<OutOfProcessNodeJSServiceOptions>> mockOptionsAccessor = _mockRepository.Create<IOptions<OutOfProcessNodeJSServiceOptions>>();
            mockOptionsAccessor.Setup(o => o.Value).Returns(dummyOptions);
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(optionsAccessor: mockOptionsAccessor.Object);
            mockTestSubject.CallBase = true;
            using (var dummyCancellationTokenSource = new CancellationTokenSource())
            {
                // Act
                (CancellationToken resultCancellationToken, CancellationTokenSource resultCancellationTokenSource) = mockTestSubject.Object.CreateCts(dummyCancellationTokenSource.Token);

                // Assert
                Assert.Null(resultCancellationTokenSource);
                Assert.Equal(dummyCancellationTokenSource.Token, resultCancellationToken);
            }
        }

        [Fact]
        public void CreateAndConnectNodeJSProcess_ThrowsObjectDisposedExceptionIfObjectHasBeenDisposed()
        {
            // Arrange
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService();
            mockTestSubject.CallBase = true;

            // Act and assert
            mockTestSubject.Object.Dispose();
            Assert.Throws<ObjectDisposedException>(() => mockTestSubject.Object.CreateAndConnectToNodeJSProcess(It.IsAny<EventWaitHandle>()));
        }

        [Fact]
        public void CreateAndConnectNodeJSProcess_DisposesOfExistingNodeJSProcess()
        {
            // Arrange
            const string dummyServerScript = "dummyServerScript";
            const string dummyServerScriptName = "dummyServerScriptName";
            var dummyServerScriptAssembly = new DummyAssembly();
            Mock<IEmbeddedResourcesService> mockEmbeddedResourcesService = _mockRepository.Create<IEmbeddedResourcesService>();
            mockEmbeddedResourcesService.Setup(e => e.ReadAsString(dummyServerScriptAssembly, dummyServerScriptName)).Returns(dummyServerScript);
            Mock<INodeJSProcess> mockNodeJSProcess = _mockRepository.Create<INodeJSProcess>();
            mockNodeJSProcess.Setup(n => n.AddOutputDataReceivedHandler(It.IsAny<DataReceivedEventHandler>()));
            mockNodeJSProcess.Setup(n => n.AddErrorDataReceivedHandler(It.IsAny<DataReceivedEventHandler>()));
            mockNodeJSProcess.Setup(n => n.BeginOutputReadLine());
            mockNodeJSProcess.Setup(n => n.BeginErrorReadLine());
            Mock<INodeJSProcessFactory> mockNodeJSProcessFactory = _mockRepository.Create<INodeJSProcessFactory>();
            mockNodeJSProcessFactory.Setup(n => n.Create(dummyServerScript)).Returns(mockNodeJSProcess.Object);
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(mockNodeJSProcessFactory.Object,
                embeddedResourcesService: mockEmbeddedResourcesService.Object,
                serverScriptAssembly: dummyServerScriptAssembly,
                serverScriptName: dummyServerScriptName);
            mockTestSubject.CallBase = true;

            // Act
            mockTestSubject.Object.CreateAndConnectToNodeJSProcess(It.IsAny<EventWaitHandle>()); // Create NodeJSProcess
            mockTestSubject.Object.CreateAndConnectToNodeJSProcess(It.IsAny<EventWaitHandle>()); // This should dispose of original NodeJSProcess

            // Assert
            _mockRepository.VerifyAll();
            mockNodeJSProcess.Verify(n => n.Dispose(), Times.Once());
        }

        [Fact]
        public void OutputDataReceivedHandler_DoesNothingIfEventDataIsNull()
        {
            // Arrange
            Mock<IEmbeddedResourcesService> mockEmbeddedResourcesService = _mockRepository.Create<IEmbeddedResourcesService>();
            Mock<INodeJSProcess> mockNodeJSProcess = _mockRepository.Create<INodeJSProcess>();
            Mock<INodeJSProcessFactory> mockNodeJSProcessFactory = _mockRepository.Create<INodeJSProcessFactory>();
            mockNodeJSProcessFactory.Setup(n => n.Create(null)).Returns(mockNodeJSProcess.Object);
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(mockNodeJSProcessFactory.Object,
                embeddedResourcesService: mockEmbeddedResourcesService.Object);
            mockTestSubject.CallBase = true;
            mockTestSubject.Object.CreateAndConnectToNodeJSProcess(It.IsAny<EventWaitHandle>()); // Set _nodeJSProcess

            // Act
            mockTestSubject.Object.OutputDataReceivedHandler(null, CreateDataReceivedEventArgs(null), null);

            // Assert
            _mockRepository.VerifyAll();
            mockNodeJSProcess.Verify(n => n.Connected, Times.Never());
        }

        [Fact]
        public void OutputDataReceivedHandler_IfNodeJSProcessIsNotConnectedAndEventDataIsConnectionEstablishedMessageEstablishesConnection()
        {
            // Arrange
            const string dummyData = OutOfProcessNodeJSService.CONNECTION_ESTABLISHED_MESSAGE_START;
            Mock<IEmbeddedResourcesService> mockEmbeddedResourcesService = _mockRepository.Create<IEmbeddedResourcesService>();
            Mock<INodeJSProcess> mockNodeJSProcess = _mockRepository.Create<INodeJSProcess>();
            mockNodeJSProcess.Setup(n => n.Connected).Returns(false);
            Mock<INodeJSProcessFactory> mockNodeJSProcessFactory = _mockRepository.Create<INodeJSProcessFactory>();
            mockNodeJSProcessFactory.Setup(n => n.Create(null)).Returns(mockNodeJSProcess.Object);
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(mockNodeJSProcessFactory.Object,
                embeddedResourcesService: mockEmbeddedResourcesService.Object);
            mockTestSubject.CallBase = true;
            using (var dummyWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset))
            {
                mockTestSubject.Object.CreateAndConnectToNodeJSProcess(It.IsAny<EventWaitHandle>()); // Set _nodeJSProcess

                // Act
                mockTestSubject.Object.OutputDataReceivedHandler(null, CreateDataReceivedEventArgs(dummyData), dummyWaitHandle);

                // Assert
                _mockRepository.VerifyAll();
                Assert.True(dummyWaitHandle.WaitOne(0)); // Ensure that it gets signaled
                mockTestSubject.Protected().As<IOutOfProcessNodeJSServiceProtectedMembers>().Verify(t => t.OnConnectionEstablishedMessageReceived(dummyData), Times.Once());
            }
        }

        [Theory]
        [MemberData(nameof(OutputDataReceivedHandler_IfNodeJSProcessIsConnectedOrDataIsNotConnectionEstablishedMessageAccumulatesAndLogsMessages_Data))]
        public void OutputDataReceivedHandler_IfNodeJSProcessIsConnectedOrDataIsNotConnectionEstablishedMessageAccumulatesAndLogsMessages(bool dummyConnected, string dummyData)
        {
            // Arrange
            Mock<IEmbeddedResourcesService> mockEmbeddedResourcesService = _mockRepository.Create<IEmbeddedResourcesService>();
            Mock<INodeJSProcess> mockNodeJSProcess = _mockRepository.Create<INodeJSProcess>();
            mockNodeJSProcess.Setup(n => n.Connected).Returns(dummyConnected);
            Mock<INodeJSProcessFactory> mockNodeJSProcessFactory = _mockRepository.Create<INodeJSProcessFactory>();
            mockNodeJSProcessFactory.Setup(n => n.Create(null)).Returns(mockNodeJSProcess.Object);
            var loggerStringBuilder = new StringBuilder();
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(mockNodeJSProcessFactory.Object,
                embeddedResourcesService: mockEmbeddedResourcesService.Object,
                loggerStringBuilder: loggerStringBuilder,
                logLevel: LogLevel.Information);
            mockTestSubject.CallBase = true;
            string dummyOutMessage = "dummyOutMessage";
            mockTestSubject.Setup(t => t.TryCreateMessage(It.IsAny<StringBuilder>(), dummyData, out dummyOutMessage)).Returns(true);
            mockTestSubject.Object.CreateAndConnectToNodeJSProcess(It.IsAny<EventWaitHandle>()); // Set _nodeJSProcess

            // Act
            mockTestSubject.Object.OutputDataReceivedHandler(null, CreateDataReceivedEventArgs(dummyData), null);

            // Assert
            _mockRepository.VerifyAll();
            string logResult = loggerStringBuilder.ToString();
            Assert.Contains(dummyOutMessage, logResult);
        }

        public static IEnumerable<object[]> OutputDataReceivedHandler_IfNodeJSProcessIsConnectedOrDataIsNotConnectionEstablishedMessageAccumulatesAndLogsMessages_Data()
        {
            return new object[][]
            {
                new object[]{true, OutOfProcessNodeJSService.CONNECTION_ESTABLISHED_MESSAGE_START},
                new object[]{false, "dummyData"},
            };
        }

        [Fact]
        public void ErrorDataReceivedHandler_DoesNothingIfEventDataIsNull()
        {
            // Arrange
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService();
            mockTestSubject.CallBase = true;

            // Act
            mockTestSubject.Object.ErrorDataReceivedHandler(null, CreateDataReceivedEventArgs(null));

            // Assert
            string dummyOutMessage = null;
            mockTestSubject.Verify(t => t.TryCreateMessage(It.IsAny<StringBuilder>(), null, out dummyOutMessage), Times.Never());
        }

        [Fact]
        public void ErrorDataReceivedHandler_IfDataIsNotNullLogsMessages()
        {
            // Arrange
            const string dummyData = "dummyData";
            var loggerStringBuilder = new StringBuilder();
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(loggerStringBuilder: loggerStringBuilder);
            mockTestSubject.CallBase = true;
            string dummyOutMessage = null;
            mockTestSubject.Setup(t => t.TryCreateMessage(It.IsAny<StringBuilder>(), dummyData, out dummyOutMessage)).Returns(true);

            // Act
            mockTestSubject.Object.ErrorDataReceivedHandler(null, CreateDataReceivedEventArgs(dummyData));

            // Assert
            _mockRepository.VerifyAll();
            string logResult = loggerStringBuilder.ToString();
            Assert.Single(Regex.Matches(logResult, $"{nameof(LogLevel.Error)}: "));
        }

        [Fact]
        public void TryCreateMessage_AppendsDataToStringBuilderAndReturnsFalseIfTheDataDoesNotEndWithANullTerminatingCharacter()
        {
            // Arrange
            var dummyStringBuilder = new StringBuilder();
            const string dummyData = "dummyData";
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService();
            mockTestSubject.CallBase = true;

            // Act
            bool result = mockTestSubject.Object.TryCreateMessage(dummyStringBuilder, dummyData, out string resultMessage);

            // Assert
            Assert.False(result);
            Assert.Null(resultMessage);
            Assert.Equal(dummyData + "\n", dummyStringBuilder.ToString(), ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void TryCreate_ResetsStringBuilderReturnsTrueAndAMessageIfTheDataEndsWithANullTerminatingCharacter()
        {
            // Arrange
            var dummyStringBuilder = new StringBuilder();
            const string dummyData = "dummyData";
            string dummyDataWithNullTerminatingCharacter = $"{dummyData}\0";
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService();
            mockTestSubject.CallBase = true;

            // Act
            bool result = mockTestSubject.Object.TryCreateMessage(dummyStringBuilder, dummyDataWithNullTerminatingCharacter, out string resultMessage);

            // Assert
            Assert.True(result);
            Assert.Equal(dummyData, resultMessage);
            Assert.Equal(0, dummyStringBuilder.Length);
        }

        // Mocking protected members: https://github.com/Moq/moq4/wiki/Quickstart#miscellaneous
        private interface IOutOfProcessNodeJSServiceProtectedMembers
        {
            void OnConnectionEstablishedMessageReceived(string connectionEstablishedMessage);
            Task<(bool, T)> TryInvokeAsync<T>(InvocationRequest invocationRequest, CancellationToken cancellationToken);
            void Dispose(bool disposing);
        }

        private interface INodeJSProcessProtectedMembers
        {
            void Dispose(bool disposing);
        }

        private Mock<OutOfProcessNodeJSService> CreateMockOutOfProcessNodeJSService(INodeJSProcessFactory nodeProcessFactory = null,
            IOptions<OutOfProcessNodeJSServiceOptions> optionsAccessor = null,
            IEmbeddedResourcesService embeddedResourcesService = null,
            Assembly serverScriptAssembly = null,
            string serverScriptName = null,
            StringBuilder loggerStringBuilder = null,
            LogLevel logLevel = LogLevel.Warning)
        {
            // Log output from all tests
            var services = new ServiceCollection();
            services.AddLogging(lb =>
            {
                lb.
                    AddProvider(new TestOutputProvider(_testOutputHelper)).
                    AddFilter<TestOutputProvider>((LogLevel loglevel) => loglevel >= LogLevel.Debug);

                if (loggerStringBuilder != null)
                {
                    lb.
                        AddProvider(new StringBuilderProvider(loggerStringBuilder)).
                        AddFilter<StringBuilderProvider>((LogLevel LogLevel) => LogLevel >= logLevel);
                }
            });

            _serviceProvider = services.BuildServiceProvider();
            ILogger logger = _serviceProvider.GetRequiredService<ILogger<OutOfProcessNodeJSService>>();

            return _mockRepository.Create<OutOfProcessNodeJSService>(nodeProcessFactory,
                logger,
                optionsAccessor,
                embeddedResourcesService,
                serverScriptAssembly,
                serverScriptName);
        }

        private class DummyAssembly : Assembly { }

        // https://stackoverflow.com/questions/1354308/how-to-instantiate-datareceivedeventargs-or-be-able-to-fill-it-with-data
        private DataReceivedEventArgs CreateDataReceivedEventArgs(string TestData)
        {
            var MockEventArgs =
                (DataReceivedEventArgs)System.Runtime.Serialization.FormatterServices
                 .GetUninitializedObject(typeof(DataReceivedEventArgs));

            FieldInfo[] EventFields = typeof(DataReceivedEventArgs)
                .GetFields(
                    BindingFlags.NonPublic |
                    BindingFlags.Instance |
                    BindingFlags.DeclaredOnly);

            EventFields[0].SetValue(MockEventArgs, TestData);

            return MockEventArgs;
        }

        public void Dispose()
        {
            ((IDisposable)_serviceProvider).Dispose();
        }
    }
}
