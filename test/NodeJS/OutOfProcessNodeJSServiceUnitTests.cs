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
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Jering.Javascript.NodeJS.Tests
{
    // OutOfProcessNodeJSService._processSemaphore and OutOfProcessNodeJSService._nodeJSProcess may use unmanaged resources. 
    // To be safe, dispose OutOfProcessNodeJSService instances.
    public class OutOfProcessNodeJSServiceUnitTests
    {
        private readonly MockRepository _mockRepository = new MockRepository(MockBehavior.Default);

        [Fact]
        public async void InvokeFromFileAsync_CreatesInvocationRequestAndCallsTryInvokeCoreAsync()
        {
            // Arrange
            const int dummyResult = 1;
            const string dummyModulePath = "dummyModulePath";
            const string dummyExportName = "dummyExportName";
            var dummyArgs = new object[0];
            var dummyCancellationToken = new CancellationToken();
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService();
            mockTestSubject.CallBase = true;
            mockTestSubject.Setup(t => t.TryInvokeCoreAsync<int>(It.IsAny<InvocationRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync((true, dummyResult));
            using (OutOfProcessNodeJSService testSubject = mockTestSubject.Object)
            {
                // Act
                int result = await testSubject.InvokeFromFileAsync<int>(dummyModulePath, dummyExportName, dummyArgs, dummyCancellationToken).ConfigureAwait(false);

                // Assert
                Assert.Equal(dummyResult, result);
                _mockRepository.VerifyAll();
                mockTestSubject.Verify(t => t.TryInvokeCoreAsync<int>(
                    It.Is<InvocationRequest>(
                        invocationRequest =>
                        invocationRequest.ModuleSourceType == ModuleSourceType.File &&
                        invocationRequest.ModuleSource == dummyModulePath &&
                        invocationRequest.ExportName == dummyExportName &&
                        invocationRequest.Args == dummyArgs),
                    dummyCancellationToken));
            }
        }

        [Fact]
        public async void InvokeFromStringAsync_CreatesInvocationRequestAndCallsTryInvokeCoreAsync()
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
            mockTestSubject.Setup(t => t.TryInvokeCoreAsync<int>(It.IsAny<InvocationRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync((true, dummyResult));
            using (OutOfProcessNodeJSService testSubject = mockTestSubject.Object)
            {
                // Act
                int result = await testSubject.InvokeFromStringAsync<int>(dummyModuleString, dummyNewCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken).ConfigureAwait(false);

                // Assert
                Assert.Equal(dummyResult, result);
                _mockRepository.VerifyAll();
                mockTestSubject.Verify(t => t.TryInvokeCoreAsync<int>(
                    It.Is<InvocationRequest>(
                        invocationRequest =>
                        invocationRequest.ModuleSourceType == ModuleSourceType.String &&
                        invocationRequest.ModuleSource == dummyModuleString &&
                        invocationRequest.NewCacheIdentifier == dummyNewCacheIdentifier &&
                        invocationRequest.ExportName == dummyExportName &&
                        invocationRequest.Args == dummyArgs),
                    dummyCancellationToken));
            }
        }

        [Fact]
        public async void InvokeFromStreamAsync_CreatesInvocationRequestAndCallsTryInvokeCoreAsync()
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
            mockTestSubject.Setup(t => t.TryInvokeCoreAsync<int>(It.IsAny<InvocationRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync((true, dummyResult));
            using (OutOfProcessNodeJSService testSubject = mockTestSubject.Object)
            {
                // Act
                int result = await testSubject.InvokeFromStreamAsync<int>(dummyModuleStream, dummyNewCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken).ConfigureAwait(false);

                // Assert
                Assert.Equal(dummyResult, result);
                _mockRepository.VerifyAll();
                mockTestSubject.Verify(t => t.TryInvokeCoreAsync<int>(
                    It.Is<InvocationRequest>(
                        invocationRequest =>
                        invocationRequest.ModuleSourceType == ModuleSourceType.Stream &&
                        invocationRequest.ModuleStreamSource == dummyModuleStream &&
                        invocationRequest.NewCacheIdentifier == dummyNewCacheIdentifier &&
                        invocationRequest.ExportName == dummyExportName &&
                        invocationRequest.Args == dummyArgs),
                    dummyCancellationToken));
            }
        }

        [Fact]
        public async void TryInvokeFromCacheAsync_CreatesInvocationRequestAndCallsTryInvokeCoreAsync()
        {
            // Arrange
            const int dummyResult = 1;
            const string dummyModuleCacheIdentifier = "dummyModuleCacheIdentifier";
            const string dummyExportName = "dummyExportName";
            var dummyArgs = new object[0];
            var dummyCancellationToken = new CancellationToken();
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService();
            mockTestSubject.CallBase = true;
            mockTestSubject.Setup(t => t.TryInvokeCoreAsync<int>(It.IsAny<InvocationRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync((true, dummyResult));
            using (OutOfProcessNodeJSService testSubject = mockTestSubject.Object)
            {
                // Act
                (bool success, int result) = await testSubject.TryInvokeFromCacheAsync<int>(dummyModuleCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken).ConfigureAwait(false);

                // Assert
                Assert.True(success);
                Assert.Equal(dummyResult, result);
                _mockRepository.VerifyAll();
                mockTestSubject.Verify(t => t.TryInvokeCoreAsync<int>(
                    It.Is<InvocationRequest>(
                        invocationRequest =>
                        invocationRequest.ModuleSourceType == ModuleSourceType.Cache &&
                        invocationRequest.ModuleSource == dummyModuleCacheIdentifier &&
                        invocationRequest.ExportName == dummyExportName &&
                        invocationRequest.Args == dummyArgs),
                    dummyCancellationToken));
            }
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

        [Fact]
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
            mockNodeJSProcess.Setup(n => n.Lock).Returns(new object());
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

            using (OutOfProcessNodeJSService testSubject = mockTestSubject.Object)
            {
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
                mockTestSubject.Verify(t => t.CreateAndConnectToNodeJSProcess(), Times.Once); // Only creates and connects once
                mockTestSubject.Protected().As<IOutOfProcessNodeJSServiceProtectedMembers>().
                    Verify(t => t.TryInvokeAsync<int>(null, dummyCancellationToken), Times.Exactly(numThreads)); // Invokes javascript for each invocation request
                foreach ((bool success, int value) in results)
                {
                    Assert.Equal(success, dummyReturnValue.Item1);
                    Assert.Equal(value, dummyReturnValue.Item2);
                }
            }
        }

        [Fact]
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
            mockNodeJSProcess.Setup(n => n.Lock).Returns(new object());
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
            using (OutOfProcessNodeJSService testSubject = mockTestSubject.Object)
            {
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
                mockTestSubject.Verify(t => t.CreateAndConnectToNodeJSProcess(), Times.Exactly(2)); // Creates NodeJS process twice
                mockTestSubject.Protected().As<IOutOfProcessNodeJSServiceProtectedMembers>().
                    Verify(t => t.TryInvokeAsync<int>(null, dummyCancellationToken), Times.Exactly(numThreads + 1)); // Invokes javascript for each invocation request
                foreach ((bool success, int value) in results)
                {
                    Assert.Equal(success, dummyReturnValue.Item1);
                    Assert.Equal(value, dummyReturnValue.Item2);
                }
            }
        }

        [Fact]
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
            Mock<ILogger> mockLogger = _mockRepository.Create<ILogger>();
            mockLogger.Setup(l => l.IsEnabled(LogLevel.Warning)).Returns(true);
            mockLogger.Setup(l => l.IsEnabled(LogLevel.Debug)).Returns(false);
            mockLogger.Setup(l => l.Log(LogLevel.Warning, 0, It.IsAny<object>(), null, It.IsAny<Func<object, Exception, string>>()));
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(mockNodeJSProcessFactory.Object,
                mockLogger.Object,
                mockOptionsAccessor.Object,
                mockEmbeddedResourcesService.Object,
                dummyServerScriptAssembly,
                dummyServerScriptName);
            mockTestSubject.CallBase = true;
            using (OutOfProcessNodeJSService testSubject = mockTestSubject.Object)
            {
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
                mockLogger.Verify(l => l.Log(LogLevel.Warning, 0, It.IsAny<object>(), null, It.IsAny<Func<object, Exception, string>>()), Times.Exactly(numThreads)); // Each thread must log a timed out connection attempt once
                mockTestSubject.Verify(t => t.CreateAndConnectToNodeJSProcess(), Times.Exactly(numThreads * 2)); // Each thread tries to create and connect to NodeJS process twice
                Assert.Equal(5, results.Count);
                foreach (Exception exception in results)
                {
                    // Each thread must throw a connection attempt timed out invocation exception
                    Assert.IsType<InvocationException>(exception);
                    Assert.Equal(string.Format(Strings.InvocationException_OutOfProcessNodeJSService_ConnectionAttemptTimedOut, dummyTimeoutMS, dummyHasExited, dummyExitCode), exception.Message);
                }
            }
        }

        [Fact]
        public void TryInvokeCoreAsync_DoesNotRetryInvocationsThatAreCanceledAndThrowsOperationCanceledException()
        {
            // Arrange
            const string dummyServerScript = "dummyServerScript";
            bool dummyIsConnected = false;
            const string dummyServerScriptName = "dummyServerScriptName";
            var dummyException = new OperationCanceledException();
            var dummyCancellationTokenSource = new CancellationTokenSource();
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
            mockNodeJSProcess.Setup(n => n.Lock).Returns(new object());
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
            using (OutOfProcessNodeJSService testSubject = mockTestSubject.Object)
            {
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
                mockTestSubject.Verify(t => t.CreateAndConnectToNodeJSProcess(), Times.Once); // Only connects once
                mockTestSubject.Protected().As<IOutOfProcessNodeJSServiceProtectedMembers>().
                    Verify(t => t.TryInvokeAsync<int>(null, It.IsAny<CancellationToken>()), Times.Exactly(numThreads)); // No retries
                Assert.Equal(5, results.Count);
                foreach (Exception exception in results)
                {
                    // Each thread must throw an OperationCanceledException
                    Assert.Same(dummyException, exception);
                }
            }
        }

        [Fact]
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
            mockNodeJSProcess.Setup(n => n.Lock).Returns(new object());
            var dummyOptions = new OutOfProcessNodeJSServiceOptions { TimeoutMS = dummyTimeoutMS };
            Mock<IOptions<OutOfProcessNodeJSServiceOptions>> mockOptionsAccessor = _mockRepository.Create<IOptions<OutOfProcessNodeJSServiceOptions>>();
            mockOptionsAccessor.Setup(o => o.Value).Returns(dummyOptions);
            Mock<INodeJSProcessFactory> mockNodeJSProcessFactory = _mockRepository.Create<INodeJSProcessFactory>();
            mockNodeJSProcessFactory.Setup(n => n.Create(dummyServerScript)).Returns(mockNodeJSProcess.Object);
            Mock<ILogger> mockLogger = _mockRepository.Create<ILogger>();
            mockLogger.Setup(l => l.IsEnabled(LogLevel.Warning)).Returns(true);
            mockLogger.Setup(l => l.IsEnabled(LogLevel.Debug)).Returns(false);
            mockLogger.Setup(l => l.Log(LogLevel.Warning, 0, It.IsAny<object>(), null, It.IsAny<Func<object, Exception, string>>()));
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(mockNodeJSProcessFactory.Object,
                mockLogger.Object,
                mockOptionsAccessor.Object,
                mockEmbeddedResourcesService.Object,
                dummyServerScriptAssembly,
                dummyServerScriptName);
            mockTestSubject.CallBase = true;
            mockTestSubject.Protected().As<IOutOfProcessNodeJSServiceProtectedMembers>().
                Setup(t => t.TryInvokeAsync<int>(null, It.IsAny<CancellationToken>())).ThrowsAsync(dummyException);
            using (OutOfProcessNodeJSService testSubject = mockTestSubject.Object)
            {
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
                            testSubject.TryInvokeCoreAsync<int>(null, CancellationToken.None).GetAwaiter().GetResult();
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
                mockLogger.Verify(l => l.Log(LogLevel.Warning, 0, It.IsAny<object>(), null, It.IsAny<Func<object, Exception, string>>()), Times.Exactly(numThreads)); // Each thread must log a timed out connection attempt once
                mockTestSubject.Protected().As<IOutOfProcessNodeJSServiceProtectedMembers>().
                    Verify(t => t.TryInvokeAsync<int>(null, It.IsAny<CancellationToken>()), Times.Exactly(numThreads * 2)); // Each thread tries twice
                Assert.Equal(5, results.Count);
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
        }

        [Fact]
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
            mockNodeJSProcess.Setup(n => n.Lock).Returns(new object());
            Mock<INodeJSProcessFactory> mockNodeJSProcessFactory = _mockRepository.Create<INodeJSProcessFactory>();
            mockNodeJSProcessFactory.Setup(n => n.Create(dummyServerScript)).Returns(mockNodeJSProcess.Object);
            Mock<ILogger> mockLogger = _mockRepository.Create<ILogger>();
            mockLogger.Setup(l => l.IsEnabled(LogLevel.Warning)).Returns(true);
            mockLogger.Setup(l => l.IsEnabled(LogLevel.Debug)).Returns(false);
            mockLogger.Setup(l => l.Log(LogLevel.Warning, 0, It.IsAny<object>(), null, It.IsAny<Func<object, Exception, string>>()));
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(mockNodeJSProcessFactory.Object,
                mockLogger.Object,
                embeddedResourcesService: mockEmbeddedResourcesService.Object,
                serverScriptAssembly: dummyServerScriptAssembly,
                serverScriptName: dummyServerScriptName);
            mockTestSubject.CallBase = true;
            mockTestSubject.Protected().As<IOutOfProcessNodeJSServiceProtectedMembers>().
                Setup(t => t.TryInvokeAsync<int>(null, It.IsAny<CancellationToken>())).ThrowsAsync(dummyException);
            using (OutOfProcessNodeJSService testSubject = mockTestSubject.Object)
            {
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
                            testSubject.TryInvokeCoreAsync<int>(null, CancellationToken.None).GetAwaiter().GetResult();
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
                mockLogger.Verify(l => l.Log(LogLevel.Warning, 0, It.IsAny<object>(), null, It.IsAny<Func<object, Exception, string>>()), Times.Exactly(numThreads)); // Each thread must log a timed out connection attempt once
                mockTestSubject.Protected().As<IOutOfProcessNodeJSServiceProtectedMembers>().
                    Verify(t => t.TryInvokeAsync<int>(null, It.IsAny<CancellationToken>()), Times.Exactly(numThreads * 2)); // Each thread tries twice
                Assert.Equal(5, results.Count);
                foreach (Exception exception in results)
                {
                    Assert.Same(dummyException, exception);
                }
            }
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
            using (OutOfProcessNodeJSService testSubject = mockTestSubject.Object)
            {
                // Act
                (CancellationToken resultCancellationToken, CancellationTokenSource resultCancellationTokenSource) = testSubject.CreateCts(dummyCancellationToken);

                // Assert
                Assert.NotNull(resultCancellationTokenSource);
                Assert.Equal(resultCancellationToken, resultCancellationTokenSource.Token);
            }
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
            var dummyCancellationTokenSource = new CancellationTokenSource();
            using (OutOfProcessNodeJSService testSubject = mockTestSubject.Object)
            {
                // Act
                (CancellationToken resultCancellationToken, CancellationTokenSource resultCancellationTokenSource) = testSubject.CreateCts(dummyCancellationTokenSource.Token);

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
            Assert.Throws<ObjectDisposedException>(() => mockTestSubject.Object.CreateAndConnectToNodeJSProcess());
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
            using (OutOfProcessNodeJSService testSubject = mockTestSubject.Object)
            {
                // Act
                testSubject.CreateAndConnectToNodeJSProcess(); // Create NodeJSProcess
                testSubject.CreateAndConnectToNodeJSProcess(); // This should dispose of original NodeJSProcess

                // Assert
                _mockRepository.VerifyAll();
                mockNodeJSProcess.Verify(n => n.Dispose(), Times.Once());
            }
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
            mockTestSubject.Object.CreateAndConnectToNodeJSProcess(); // Set _nodeJSProcess
            using (OutOfProcessNodeJSService testSubject = mockTestSubject.Object)
            {
                // Act
                testSubject.OutputDataReceivedHandler(null, CreateDataReceivedEventArgs(null));

                // Assert
                _mockRepository.VerifyAll();
                mockNodeJSProcess.Verify(n => n.Connected, Times.Never());
            }
        }

        [Fact]
        public void OutputDataReceivedHandler_IfNodeJSProcessIsNotConnectedAndEventDataIsConnectionEstablishedMessageEstablishesConnection()
        {
            // Arrange
            const string dummyData = OutOfProcessNodeJSService.CONNECTION_ESTABLISHED_MESSAGE_START;
            Mock<IEmbeddedResourcesService> mockEmbeddedResourcesService = _mockRepository.Create<IEmbeddedResourcesService>();
            Mock<INodeJSProcess> mockNodeJSProcess = _mockRepository.Create<INodeJSProcess>();
            mockNodeJSProcess.Setup(n => n.Connected).Returns(false);
            mockNodeJSProcess.Setup(n => n.Lock).Returns(new object());
            Mock<INodeJSProcessFactory> mockNodeJSProcessFactory = _mockRepository.Create<INodeJSProcessFactory>();
            mockNodeJSProcessFactory.Setup(n => n.Create(null)).Returns(mockNodeJSProcess.Object);
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(mockNodeJSProcessFactory.Object,
                embeddedResourcesService: mockEmbeddedResourcesService.Object);
            mockTestSubject.CallBase = true;
            using (OutOfProcessNodeJSService testSubject = mockTestSubject.Object)
            {
                testSubject.CreateAndConnectToNodeJSProcess(); // Set _nodeJSProcess

                // Act
                testSubject.OutputDataReceivedHandler(null, CreateDataReceivedEventArgs(dummyData));

                // Assert
                _mockRepository.VerifyAll();
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
            Mock<ILogger> mockLogger = _mockRepository.Create<ILogger>();
            mockLogger.Setup(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<object>(), It.IsAny<Exception>(), It.IsAny<Func<object, Exception, string>>()));
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(mockNodeJSProcessFactory.Object,
                logger: mockLogger.Object,
                embeddedResourcesService: mockEmbeddedResourcesService.Object);
            mockTestSubject.CallBase = true;
            string dummyOutMessage = null;
            mockTestSubject.Setup(t => t.TryCreateMessage(It.IsAny<StringBuilder>(), dummyData, out dummyOutMessage)).Returns(true);
            using (OutOfProcessNodeJSService testSubject = mockTestSubject.Object)
            {
                testSubject.CreateAndConnectToNodeJSProcess(); // Set _nodeJSProcess

                // Act
                testSubject.OutputDataReceivedHandler(null, CreateDataReceivedEventArgs(dummyData));

                // Assert
                _mockRepository.VerifyAll();
            }
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
            using (OutOfProcessNodeJSService testSubject = mockTestSubject.Object)
            {
                // Act
                testSubject.ErrorDataReceivedHandler(null, CreateDataReceivedEventArgs(null));

                // Assert
                string dummyOutMessage = null;
                mockTestSubject.Verify(t => t.TryCreateMessage(It.IsAny<StringBuilder>(), null, out dummyOutMessage), Times.Never());
            }
        }

        [Fact]
        public void ErrorDataReceivedHandler_IfDataIsNotNullLogsMessages()
        {
            // Arrange
            const string dummyData = "dummyData";
            Mock<ILogger> mockLogger = _mockRepository.Create<ILogger>();
            mockLogger.Setup(l => l.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<object>(), It.IsAny<Exception>(), It.IsAny<Func<object, Exception, string>>()));
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(logger: mockLogger.Object);
            mockTestSubject.CallBase = true;
            string dummyOutMessage = null;
            mockTestSubject.Setup(t => t.TryCreateMessage(It.IsAny<StringBuilder>(), dummyData, out dummyOutMessage)).Returns(true);
            using (OutOfProcessNodeJSService testSubject = mockTestSubject.Object)
            {
                // Act
                testSubject.ErrorDataReceivedHandler(null, CreateDataReceivedEventArgs(dummyData));

                // Assert
                _mockRepository.VerifyAll();
            }
        }

        [Fact]
        public void TryCreateMessage_AppendsDataToStringBuilderAndReturnsFalseIfTheDataDoesNotEndWithANullTerminatingCharacter()
        {
            // Arrange
            var dummyStringBuilder = new StringBuilder();
            const string dummyData = "dummyData";
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService();
            mockTestSubject.CallBase = true;
            using (OutOfProcessNodeJSService testSubject = mockTestSubject.Object)
            {
                // Act
                bool result = testSubject.TryCreateMessage(dummyStringBuilder, dummyData, out string resultMessage);

                // Assert
                Assert.False(result);
                Assert.Null(resultMessage);
                Assert.Equal(dummyData + "\n", dummyStringBuilder.ToString(), ignoreLineEndingDifferences: true);
            }
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
            using (OutOfProcessNodeJSService testSubject = mockTestSubject.Object)
            {
                // Act
                bool result = testSubject.TryCreateMessage(dummyStringBuilder, dummyDataWithNullTerminatingCharacter, out string resultMessage);

                // Assert
                Assert.True(result);
                Assert.Equal(dummyData, resultMessage);
                Assert.Equal(0, dummyStringBuilder.Length);
            }
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
            ILogger logger = null,
            IOptions<OutOfProcessNodeJSServiceOptions> optionsAccessor = null,
            IEmbeddedResourcesService embeddedResourcesService = null,
            Assembly serverScriptAssembly = null,
            string serverScriptName = null)
        {
            if (logger == null)
            {
                Mock<ILogger> mockLogger = _mockRepository.Create<ILogger>();
                mockLogger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(false);
                logger = mockLogger.Object;
            }

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
    }
}
