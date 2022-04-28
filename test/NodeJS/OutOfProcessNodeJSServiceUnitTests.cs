using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
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
    public sealed class OutOfProcessNodeJSServiceUnitTests : IDisposable
    {
        private readonly MockRepository _mockRepository = new(MockBehavior.Default);
        private readonly ITestOutputHelper _testOutputHelper;
        private IServiceProvider? _serviceProvider;
        private const int TIMEOUT_MS = 60000;

        public OutOfProcessNodeJSServiceUnitTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void Constructor_InitializesFileWatching()
        {
            // Arrange
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService();
            mockTestSubject.Setup(t => t.InitializeFileWatching());

            // Act
            OutOfProcessNodeJSService testSubject = mockTestSubject.Object;

            // Assert
            mockTestSubject.VerifyAll();
        }

        [Fact]
        public async void InvokeFromFileAsync_WithTypeParameter_InvokesFromFile()
        {
            // Arrange
            const int dummyResult = 1;
            const string dummyModulePath = "dummyModulePath";
            const string dummyExportName = "dummyExportName";
            object[] dummyArgs = Array.Empty<object>();
            var dummyCancellationToken = new CancellationToken();
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService();
            mockTestSubject.CallBase = true;
            mockTestSubject.
                Setup(t => t.TryInvokeCoreAsync<int>(It.Is<InvocationRequest>(
                        invocationRequest =>
                            invocationRequest.ModuleSourceType == ModuleSourceType.File &&
                            invocationRequest.ModuleSource == dummyModulePath &&
                            invocationRequest.CacheIdentifier == null &&
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
            object[] dummyArgs = Array.Empty<object>();
            var dummyCancellationToken = new CancellationToken();
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService();
            mockTestSubject.CallBase = true;
            mockTestSubject.
                Setup(t => t.InvokeFromFileAsync<Void>(dummyModulePath, dummyExportName, dummyArgs, dummyCancellationToken)).
                ReturnsAsync((Void?)null);

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
            const string dummyCacheIdentifier = "dummyCacheIdentifier";
            const string dummyExportName = "dummyExportName";
            object[] dummyArgs = Array.Empty<object>();
            var dummyCancellationToken = new CancellationToken();
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService();
            mockTestSubject.CallBase = true;
            mockTestSubject.
                Setup(t => t.TryInvokeCoreAsync<int>(It.Is<InvocationRequest>(
                        invocationRequest =>
                            invocationRequest.ModuleSourceType == ModuleSourceType.String &&
                            invocationRequest.ModuleSource == dummyModuleString &&
                            invocationRequest.CacheIdentifier == dummyCacheIdentifier &&
                            invocationRequest.ExportName == dummyExportName &&
                            invocationRequest.Args == dummyArgs &&
                            invocationRequest.ModuleStreamSource == null),
                    dummyCancellationToken)).
                ReturnsAsync((true, dummyResult));

            // Act
            int result = await mockTestSubject.Object.InvokeFromStringAsync<int>(dummyModuleString, dummyCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken).ConfigureAwait(false);

            // Assert
            Assert.Equal(dummyResult, result);
            _mockRepository.VerifyAll();
        }

        [Fact]
        public async void InvokeFromStringAsync_WithoutTypeParameter_WithRawStringModule_InvokesFromString()
        {
            // Arrange
            const string dummyModuleString = "dummyModuleString";
            const string dummyCacheIdentifier = "dummyCacheIdentifier";
            const string dummyExportName = "dummyExportName";
            object[] dummyArgs = Array.Empty<object>();
            var dummyCancellationToken = new CancellationToken();
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService();
            mockTestSubject.CallBase = true;
            mockTestSubject.
                Setup(t => t.InvokeFromStringAsync<Void>(dummyModuleString, dummyCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken)).
                ReturnsAsync((Void?)null);

            // Act
            await mockTestSubject.Object.InvokeFromStringAsync(dummyModuleString, dummyCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken).ConfigureAwait(false);

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
            await Assert.
                ThrowsAsync<ArgumentNullException>(async () => await mockTestSubject.Object.InvokeFromStringAsync<int>((Func<string>)null!, "dummyCacheIdentifier").ConfigureAwait(false)).ConfigureAwait(false); // Testing situation where nullable reference type warnings are ignored
        }

        [Fact]
        public async void InvokeFromStringAsync_WithTypeParameter_WithModuleFactory_InvokesFromCacheIfModuleIsCached()
        {
            // Arrange
            const int dummyResult = 1;
            const string dummyCacheIdentifier = "dummyCacheIdentifier";
            const string dummyExportName = "dummyExportName";
            object[] dummyArgs = Array.Empty<object>();
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
            object[] dummyArgs = Array.Empty<object>();
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
                            invocationRequest.CacheIdentifier == dummyCacheIdentifier &&
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
            object[] dummyArgs = Array.Empty<object>();
            var dummyCancellationToken = new CancellationToken();
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService();
            mockTestSubject.CallBase = true;
            mockTestSubject.
                Setup(t => t.InvokeFromStringAsync<Void>(dummyFactory, dummyCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken)).
                ReturnsAsync((Void?)null);

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
            const string dummyCacheIdentifier = "dummyCacheIdentifier";
            const string dummyExportName = "dummyExportName";
            object[] dummyArgs = Array.Empty<object>();
            var dummyCancellationToken = new CancellationToken();
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService();
            mockTestSubject.CallBase = true;
            mockTestSubject.
                Setup(t => t.TryInvokeCoreAsync<int>(It.Is<InvocationRequest>(
                        invocationRequest =>
                            invocationRequest.ModuleSourceType == ModuleSourceType.Stream &&
                            invocationRequest.ModuleSource == null &&
                            invocationRequest.CacheIdentifier == dummyCacheIdentifier &&
                            invocationRequest.ExportName == dummyExportName &&
                            invocationRequest.Args == dummyArgs &&
                            invocationRequest.ModuleStreamSource == dummyModuleStream),
                    dummyCancellationToken)).
                ReturnsAsync((true, dummyResult));

            // Act
            int result = await mockTestSubject.Object.InvokeFromStreamAsync<int>(dummyModuleStream, dummyCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken).ConfigureAwait(false);

            // Assert
            Assert.Equal(dummyResult, result);
            _mockRepository.VerifyAll();
        }

        [Fact]
        public async void InvokeFromStreamAsync_WithoutTypeParameter_WithRawStreamModule_InvokesFromStream()
        {
            // Arrange
            var dummyModuleStream = new MemoryStream();
            const string dummyCacheIdentifier = "dummyCacheIdentifier";
            const string dummyExportName = "dummyExportName";
            object[] dummyArgs = Array.Empty<object>();
            var dummyCancellationToken = new CancellationToken();
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService();
            mockTestSubject.CallBase = true;
            mockTestSubject.
                Setup(t => t.InvokeFromStreamAsync<Void>(dummyModuleStream, dummyCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken)).
                ReturnsAsync((Void?)null);

            // Act
            await mockTestSubject.Object.InvokeFromStreamAsync(dummyModuleStream, dummyCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken).ConfigureAwait(false);

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
            await Assert.
                ThrowsAsync<ArgumentNullException>(async () => await mockTestSubject.Object.InvokeFromStreamAsync<int>((Func<Stream>)null!, "dummyCacheIdentifier").ConfigureAwait(false)).ConfigureAwait(false); // Testing situation where nullable reference type warnings are ignored
        }

        [Fact]
        public async void InvokeFromStreamAsync_WithTypeParameter_WithModuleFactory_InvokesFromCacheIfModuleIsCached()
        {
            // Arrange
            const int dummyResult = 1;
            const string dummyCacheIdentifier = "dummyCacheIdentifier";
            const string dummyExportName = "dummyExportName";
            object[] dummyArgs = Array.Empty<object>();
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
            var dummyModule = new MemoryStream();
            object[] dummyArgs = Array.Empty<object>();
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
                            invocationRequest.CacheIdentifier == dummyCacheIdentifier &&
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
            object[] dummyArgs = Array.Empty<object>();
            var dummyCancellationToken = new CancellationToken();
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService();
            mockTestSubject.CallBase = true;
            mockTestSubject.
                Setup(t => t.InvokeFromStreamAsync<Void>(dummyFactory, dummyCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken)).ReturnsAsync((Void?)null);

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
            object[] dummyArgs = Array.Empty<object>();
            var dummyCancellationToken = new CancellationToken();
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService();
            mockTestSubject.CallBase = true;
            mockTestSubject.
                Setup(t => t.TryInvokeCoreAsync<int>(It.Is<InvocationRequest>(
                        invocationRequest =>
                            invocationRequest.ModuleSourceType == ModuleSourceType.Cache &&
                            invocationRequest.ModuleSource == dummyModuleCacheIdentifier &&
                            invocationRequest.CacheIdentifier == null &&
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
            object[] dummyArgs = Array.Empty<object>();
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
            ObjectDisposedException result = await Assert.
                ThrowsAsync<ObjectDisposedException>(async () => await mockTestSubject.Object.TryInvokeCoreAsync<string>(new InvocationRequest(ModuleSourceType.String, "dummyModuleSource"), CancellationToken.None).ConfigureAwait(false)).ConfigureAwait(false);
            Assert.Equal($"Cannot access a disposed object.\nObject name: '{nameof(OutOfProcessNodeJSService)}'.", result.Message, ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void TryInvokeCoreAsync_DoesNotRetryInvocationIfConnectionAttemptFailsAndThrowsConnectionException()
        {
            // Arrange
            var dummyException = new ConnectionException();
            var dummyInvocationRequest = new InvocationRequest(ModuleSourceType.String, "dummyModuleSource");
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService();
            mockTestSubject.CallBase = true;
            mockTestSubject.Setup(t => t.ConnectIfNotConnected()).Throws(dummyException);

            // Act and assert
            Assert.ThrowsAsync<ConnectionException>(async () => await mockTestSubject.Object.TryInvokeCoreAsync<int>(dummyInvocationRequest, CancellationToken.None).ConfigureAwait(false));
            _mockRepository.VerifyAll();
            mockTestSubject.Verify(t => t.ConnectIfNotConnected(), Times.Exactly(1)); // No retries
        }

        [Fact]
        public void TryInvokeCoreAsync_DoesNotRetryInvocationsThatAreCanceledAndThrowsOperationCanceledException()
        {
            // Arrange
            using var dummyCancellationTokenSource = new CancellationTokenSource();
            var dummyException = new OperationCanceledException();
            var dummyInvocationRequest = new InvocationRequest(ModuleSourceType.String, "dummyModuleSource");
            CancellationToken dummyCancellationToken = dummyCancellationTokenSource.Token;
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService();
            mockTestSubject.CallBase = true;
            mockTestSubject.Setup(t => t.ConnectIfNotConnected());
            mockTestSubject.
                Protected().
                As<IOutOfProcessNodeJSServiceProtectedMembers>().
                Setup(t => t.TryInvokeAsync<int>(dummyInvocationRequest, dummyCancellationToken)).
                ThrowsAsync(dummyException);
            mockTestSubject.Setup(t => t.CreateCancellationToken(dummyCancellationToken)).Returns((dummyCancellationToken, null));
            dummyCancellationTokenSource.Cancel(); // Cancel token

            // Act and assert
            Assert.ThrowsAsync<OperationCanceledException>(async () => await mockTestSubject.Object.TryInvokeCoreAsync<int>(dummyInvocationRequest, dummyCancellationToken).ConfigureAwait(false));
            _mockRepository.VerifyAll();
            mockTestSubject.
                Protected().
                As<IOutOfProcessNodeJSServiceProtectedMembers>().
                Verify(t => t.TryInvokeAsync<int>(dummyInvocationRequest, It.IsAny<CancellationToken>()), Times.Exactly(1)); // No retries
        }

        [Fact]
        public async void TryInvokeCoreAsync_RetriesInvocationsThatTimeoutAndThrowsInvocationExceptionIfNoRetriesRemain()
        {
            // Arrange
            var dummyException = new OperationCanceledException();
            const int dummyTimeoutMS = 100;
            const int dummyNumRetries = 2;
            const int dummyNumProcessRetries = 2;
            var dummyOptions = new OutOfProcessNodeJSServiceOptions { TimeoutMS = dummyTimeoutMS, NumRetries = dummyNumRetries, NumProcessRetries = dummyNumProcessRetries };
            Mock<IOptions<OutOfProcessNodeJSServiceOptions>> mockOptionsAccessor = _mockRepository.Create<IOptions<OutOfProcessNodeJSServiceOptions>>();
            mockOptionsAccessor.Setup(o => o.Value).Returns(dummyOptions);
            var loggerStringBuilder = new StringBuilder();
            var dummyInvocationRequest = new InvocationRequest(ModuleSourceType.String, "dummyModuleSource");
            var dummyCancellationToken = new CancellationToken();
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(optionsAccessor: mockOptionsAccessor.Object,
                loggerStringBuilder: loggerStringBuilder);
            mockTestSubject.CallBase = true;
            mockTestSubject.Setup(t => t.ConnectIfNotConnected());
            mockTestSubject.
                Protected().
                As<IOutOfProcessNodeJSServiceProtectedMembers>().
                Setup(t => t.TryInvokeAsync<int>(dummyInvocationRequest, dummyCancellationToken)).
                ThrowsAsync(dummyException);
            mockTestSubject.Setup(t => t.CreateCancellationToken(dummyCancellationToken)).Returns((dummyCancellationToken, null));
            mockTestSubject.Setup(t => t.MoveToNewProcess(false));

            // Act and assert
            InvocationException result = await Assert.
                ThrowsAsync<InvocationException>(async () => await mockTestSubject.Object.TryInvokeCoreAsync<int>(dummyInvocationRequest, dummyCancellationToken).ConfigureAwait(false)).
                ConfigureAwait(false);
            _mockRepository.VerifyAll();
            // Verify log
            string resultLog = loggerStringBuilder.ToString();
            Assert.Equal(dummyNumRetries * (dummyNumProcessRetries + 1), Regex.Matches(resultLog, Strings.LogWarning_InvocationAttemptFailed.Substring(0, 30)).Count); // Logs after each retry
            Assert.Equal(dummyNumProcessRetries, Regex.Matches(resultLog, Strings.LogWarning_RetriesInExistingProcessExhausted.Substring(0, 30)).Count); // Logs before each process swap
            // Verify calls
            mockTestSubject.
                Protected().
                As<IOutOfProcessNodeJSServiceProtectedMembers>().
                Verify(t => t.TryInvokeAsync<int>(dummyInvocationRequest, dummyCancellationToken), Times.Exactly(dummyNumRetries * (dummyNumProcessRetries + 1) + 1));
            Assert.Equal(string.Format(Strings.InvocationException_OutOfProcessNodeJSService_InvocationTimedOut,
                    dummyTimeoutMS,
                    nameof(OutOfProcessNodeJSServiceOptions.TimeoutMS),
                    nameof(OutOfProcessNodeJSServiceOptions)),
                result.Message);
            mockTestSubject.Verify(t => t.MoveToNewProcess(false), times: Times.Exactly(2));
        }

        [Fact]
        public async void TryInvokeCoreAsync_WithRetriesAndNoProcessRetries_RetriesOnlyOnCurrentProcess()
        {
            // Arrange
            var dummyException = new OperationCanceledException();
            const int dummyTimeoutMS = 100;
            const int dummyNumRetries = 2;
            const int dummyNumProcessRetries = 0;
            var dummyOptions = new OutOfProcessNodeJSServiceOptions { 
                TimeoutMS = dummyTimeoutMS, 
                NumRetries = dummyNumRetries, 
                NumProcessRetries = dummyNumProcessRetries 
            };
            Mock<IOptions<OutOfProcessNodeJSServiceOptions>> mockOptionsAccessor = _mockRepository.Create<IOptions<OutOfProcessNodeJSServiceOptions>>();
            mockOptionsAccessor.Setup(o => o.Value).Returns(dummyOptions);
            var loggerStringBuilder = new StringBuilder();
            var dummyInvocationRequest = new InvocationRequest(ModuleSourceType.String, "dummyModuleSource");
            var dummyCancellationToken = new CancellationToken();
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(optionsAccessor: mockOptionsAccessor.Object,
                loggerStringBuilder: loggerStringBuilder);
            mockTestSubject.CallBase = true;
            mockTestSubject.Setup(t => t.ConnectIfNotConnected());
            mockTestSubject.
                Protected().
                As<IOutOfProcessNodeJSServiceProtectedMembers>().
                Setup(t => t.TryInvokeAsync<int>(dummyInvocationRequest, dummyCancellationToken)).
                ThrowsAsync(dummyException);
            mockTestSubject.Setup(t => t.CreateCancellationToken(dummyCancellationToken)).Returns((dummyCancellationToken, null));

            // Act and assert
            InvocationException result = await Assert.
                ThrowsAsync<InvocationException>(async () => await mockTestSubject.Object.TryInvokeCoreAsync<int>(dummyInvocationRequest, dummyCancellationToken).ConfigureAwait(false)).
                ConfigureAwait(false);
            _mockRepository.VerifyAll();
            // Verify log
            string resultLog = loggerStringBuilder.ToString();
            Assert.Equal(dummyNumRetries * (dummyNumProcessRetries + 1), Regex.Matches(resultLog, Strings.LogWarning_InvocationAttemptFailed.Substring(0, 30)).Count); // Logs after each retry
            Assert.Equal(dummyNumProcessRetries, Regex.Matches(resultLog, Strings.LogWarning_RetriesInExistingProcessExhausted.Substring(0, 30)).Count); // Logs before each process swap
            // Verify calls
            mockTestSubject.
                Protected().
                As<IOutOfProcessNodeJSServiceProtectedMembers>().
                Verify(t => t.TryInvokeAsync<int>(dummyInvocationRequest, dummyCancellationToken), Times.Exactly(dummyNumRetries * (dummyNumProcessRetries + 1) + 1));
            Assert.Equal(string.Format(Strings.InvocationException_OutOfProcessNodeJSService_InvocationTimedOut,
                    dummyTimeoutMS,
                    nameof(OutOfProcessNodeJSServiceOptions.TimeoutMS),
                    nameof(OutOfProcessNodeJSServiceOptions)),
                result.Message);
            mockTestSubject.Verify(t => t.MoveToNewProcess(false), times: Times.Exactly(0));
        }

        [Fact]
        public async void TryInvokeCoreAsync_WithProcessRetriesAndNoRetries_RetriesOnlyOnNewProcess()
        {
            // Arrange
            var dummyException = new OperationCanceledException();
            const int dummyTimeoutMS = 100;
            const int dummyNumRetries = 0;
            const int dummyNumProcessRetries = 2;
            var dummyOptions = new OutOfProcessNodeJSServiceOptions { 
                TimeoutMS = dummyTimeoutMS, 
                NumRetries = dummyNumRetries, 
                NumProcessRetries = dummyNumProcessRetries 
            };
            Mock<IOptions<OutOfProcessNodeJSServiceOptions>> mockOptionsAccessor = _mockRepository.Create<IOptions<OutOfProcessNodeJSServiceOptions>>();
            mockOptionsAccessor.Setup(o => o.Value).Returns(dummyOptions);
            var loggerStringBuilder = new StringBuilder();
            var dummyInvocationRequest = new InvocationRequest(ModuleSourceType.String, "dummyModuleSource");
            var dummyCancellationToken = new CancellationToken();
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(optionsAccessor: mockOptionsAccessor.Object,
                loggerStringBuilder: loggerStringBuilder);
            mockTestSubject.CallBase = true;
            mockTestSubject.Setup(t => t.ConnectIfNotConnected());
            mockTestSubject.
                Protected().
                As<IOutOfProcessNodeJSServiceProtectedMembers>().
                Setup(t => t.TryInvokeAsync<int>(dummyInvocationRequest, dummyCancellationToken)).
                ThrowsAsync(dummyException);
            mockTestSubject.Setup(t => t.CreateCancellationToken(dummyCancellationToken)).Returns((dummyCancellationToken, null));
            mockTestSubject.Setup(t => t.MoveToNewProcess(false));

            // Act and assert
            InvocationException result = await Assert.
                ThrowsAsync<InvocationException>(async () => await mockTestSubject.Object.TryInvokeCoreAsync<int>(dummyInvocationRequest, dummyCancellationToken).ConfigureAwait(false)).
                ConfigureAwait(false);
            _mockRepository.VerifyAll();
            // Verify log
            string resultLog = loggerStringBuilder.ToString();
            Assert.Equal(dummyNumProcessRetries, Regex.Matches(resultLog, Strings.LogWarning_InvocationAttemptFailed.Substring(0, 30)).Count); // Logs after each retry
            Assert.Equal(dummyNumProcessRetries, Regex.Matches(resultLog, Strings.LogWarning_RetriesInExistingProcessExhausted.Substring(0, 30)).Count); // Logs before each process swap
            // Verify calls
            mockTestSubject.
                Protected().
                As<IOutOfProcessNodeJSServiceProtectedMembers>().
                Verify(t => t.TryInvokeAsync<int>(dummyInvocationRequest, dummyCancellationToken), Times.Exactly(dummyNumProcessRetries + 1));
            Assert.Equal(string.Format(Strings.InvocationException_OutOfProcessNodeJSService_InvocationTimedOut,
                    dummyTimeoutMS,
                    nameof(OutOfProcessNodeJSServiceOptions.TimeoutMS),
                    nameof(OutOfProcessNodeJSServiceOptions)),
                result.Message);
            mockTestSubject.Verify(t => t.MoveToNewProcess(false), times: Times.Exactly(dummyNumProcessRetries));
        }

        [Fact]
        public async void TryInvokeCoreAsync_IfInvocationThrowsExceptionsOtherThanInvocationExceptionRetriesInTheSameProcessAndInANewProcessAndThrowsExceptionWhenNoRetriesRemain()
        {
            // Arrange
            var dummyException = new HttpRequestException(); // Process retries are ignored if exception is an InvocationException (cause by JS)
            const int dummyTimeoutMS = 100;
            const int dummyNumRetries = 2;
            const int dummyNumProcessRetries = 2;
            var dummyOptions = new OutOfProcessNodeJSServiceOptions { TimeoutMS = dummyTimeoutMS, NumRetries = dummyNumRetries, NumProcessRetries = dummyNumProcessRetries };
            Mock<IOptions<OutOfProcessNodeJSServiceOptions>> mockOptionsAccessor = _mockRepository.Create<IOptions<OutOfProcessNodeJSServiceOptions>>();
            mockOptionsAccessor.Setup(o => o.Value).Returns(dummyOptions);
            var loggerStringBuilder = new StringBuilder();
            var dummyInvocationRequest = new InvocationRequest(ModuleSourceType.String, "dummyModuleSource");
            var dummyCancellationToken = new CancellationToken();
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(optionsAccessor: mockOptionsAccessor.Object,
                loggerStringBuilder: loggerStringBuilder);
            mockTestSubject.CallBase = true;
            mockTestSubject.Setup(t => t.ConnectIfNotConnected());
            mockTestSubject.
                Protected().
                As<IOutOfProcessNodeJSServiceProtectedMembers>().
                Setup(t => t.TryInvokeAsync<int>(dummyInvocationRequest, dummyCancellationToken)).
                ThrowsAsync(dummyException);
            mockTestSubject.Setup(t => t.CreateCancellationToken(dummyCancellationToken)).Returns((dummyCancellationToken, null));
            mockTestSubject.Setup(t => t.MoveToNewProcess(false));

            // Act and assert
            HttpRequestException result = await Assert.
                ThrowsAsync<HttpRequestException>(async () => await mockTestSubject.Object.TryInvokeCoreAsync<int>(dummyInvocationRequest, dummyCancellationToken).ConfigureAwait(false)).
                ConfigureAwait(false);
            _mockRepository.VerifyAll();
            // Verify log
            string resultLog = loggerStringBuilder.ToString();
            int expectedNumTries = 1 /* initial */ + dummyNumRetries /* original process retries */ + dummyNumRetries * dummyNumProcessRetries /* retries in new processes */;
            Assert.Equal(expectedNumTries, Regex.Matches(resultLog, Strings.LogWarning_InvocationAttemptFailed.Substring(0, 30)).Count); // Logs after each retry
            Assert.Equal(dummyNumProcessRetries, Regex.Matches(resultLog, Strings.LogWarning_RetriesInExistingProcessExhausted.Substring(0, 30)).Count); // Logs before each process swap
            // Verify calls
            mockTestSubject.
                Protected().
                As<IOutOfProcessNodeJSServiceProtectedMembers>().
                Verify(t => t.TryInvokeAsync<int>(dummyInvocationRequest, dummyCancellationToken), Times.Exactly(expectedNumTries));
            mockTestSubject.Verify(t => t.MoveToNewProcess(false), times: Times.Exactly(2));
            Assert.Same(dummyException, result);
        }

        [Fact]
        public async void TryInvokeCoreAsync_IfProcessRetriesAreDisabledForJSErrorsAndInvocationThrowsInvocationExceptionsRetriesInTheSameProcessAndThrowsExceptionWhenNoRetriesRemain()
        {
            // Arrange
            var dummyException = new InvocationException(); // Process retries are ignored if exception is an InvocationException (cause by JS)
            const int dummyTimeoutMS = 100;
            const int dummyNumRetries = 2;
            const int dummyNumProcessRetries = 2; // Ignored
            var dummyOptions = new OutOfProcessNodeJSServiceOptions
            {
                TimeoutMS = dummyTimeoutMS,
                NumRetries = dummyNumRetries,
                NumProcessRetries = dummyNumProcessRetries,
                EnableProcessRetriesForJavascriptErrors = false
            };
            Mock<IOptions<OutOfProcessNodeJSServiceOptions>> mockOptionsAccessor = _mockRepository.Create<IOptions<OutOfProcessNodeJSServiceOptions>>();
            mockOptionsAccessor.Setup(o => o.Value).Returns(dummyOptions);
            var loggerStringBuilder = new StringBuilder();
            var dummyInvocationRequest = new InvocationRequest(ModuleSourceType.String, "dummyModuleSource");
            var dummyCancellationToken = new CancellationToken();
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(optionsAccessor: mockOptionsAccessor.Object,
                loggerStringBuilder: loggerStringBuilder);
            mockTestSubject.CallBase = true;
            mockTestSubject.Setup(t => t.ConnectIfNotConnected());
            mockTestSubject.
                Protected().
                As<IOutOfProcessNodeJSServiceProtectedMembers>().
                Setup(t => t.TryInvokeAsync<int>(dummyInvocationRequest, dummyCancellationToken)).
                ThrowsAsync(dummyException);
            mockTestSubject.Setup(t => t.CreateCancellationToken(dummyCancellationToken)).Returns((dummyCancellationToken, null));

            // Act and assert
            InvocationException result = await Assert.
                ThrowsAsync<InvocationException>(async () => await mockTestSubject.Object.TryInvokeCoreAsync<int>(dummyInvocationRequest, dummyCancellationToken).ConfigureAwait(false)).
                ConfigureAwait(false);
            _mockRepository.VerifyAll();
            // Verify log
            string resultLog = loggerStringBuilder.ToString();
            int expectedNumTries = 1 /* initial */ + dummyNumRetries /* original process retries */;
            Assert.Equal(expectedNumTries, Regex.Matches(resultLog, Strings.LogWarning_InvocationAttemptFailed.Substring(0, 30)).Count); // Logs after each retry
            // Verify calls
            mockTestSubject.
                Protected().
                As<IOutOfProcessNodeJSServiceProtectedMembers>().
                Verify(t => t.TryInvokeAsync<int>(dummyInvocationRequest, dummyCancellationToken), Times.Exactly(expectedNumTries));
            Assert.Same(dummyException, result);
        }

        [Fact]
        public async void TryInvokeCoreAsync_IfProcessRetriesAreEnabledForJSErrorsAndInvocationThrowsInvocationExceptionsRetriesInTheSameProcessAndInANewProcessAndThrowsExceptionWhenNoRetriesRemain()
        {
            // Arrange
            var dummyException = new InvocationException(); // Process retries are ignored if exception is an InvocationException (cause by JS)
            const int dummyTimeoutMS = 100;
            const int dummyNumRetries = 2;
            const int dummyNumProcessRetries = 2;
            var dummyOptions = new OutOfProcessNodeJSServiceOptions
            {
                TimeoutMS = dummyTimeoutMS,
                NumRetries = dummyNumRetries,
                NumProcessRetries = dummyNumProcessRetries,
                EnableProcessRetriesForJavascriptErrors = true
            };
            Mock<IOptions<OutOfProcessNodeJSServiceOptions>> mockOptionsAccessor = _mockRepository.Create<IOptions<OutOfProcessNodeJSServiceOptions>>();
            mockOptionsAccessor.Setup(o => o.Value).Returns(dummyOptions);
            var loggerStringBuilder = new StringBuilder();
            var dummyInvocationRequest = new InvocationRequest(ModuleSourceType.String, "dummyModuleSource");
            var dummyCancellationToken = new CancellationToken();
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(optionsAccessor: mockOptionsAccessor.Object,
                loggerStringBuilder: loggerStringBuilder);
            mockTestSubject.CallBase = true;
            mockTestSubject.Setup(t => t.ConnectIfNotConnected());
            mockTestSubject.
                Protected().
                As<IOutOfProcessNodeJSServiceProtectedMembers>().
                Setup(t => t.TryInvokeAsync<int>(dummyInvocationRequest, dummyCancellationToken)).
                ThrowsAsync(dummyException);
            mockTestSubject.Setup(t => t.CreateCancellationToken(dummyCancellationToken)).Returns((dummyCancellationToken, null));
            mockTestSubject.Setup(t => t.MoveToNewProcess(false));

            // Act and assert
            InvocationException result = await Assert.
                ThrowsAsync<InvocationException>(async () => await mockTestSubject.Object.TryInvokeCoreAsync<int>(dummyInvocationRequest, dummyCancellationToken).ConfigureAwait(false)).
                ConfigureAwait(false);
            _mockRepository.VerifyAll();
            // Verify log
            string resultLog = loggerStringBuilder.ToString();
            int expectedNumTries = 1 /* initial */ + dummyNumRetries /* original process retries */ + dummyNumRetries * dummyNumProcessRetries /* retries in new processes */;
            Assert.Equal(expectedNumTries, Regex.Matches(resultLog, Strings.LogWarning_InvocationAttemptFailed.Substring(0, 30)).Count); // Logs after each retry
            Assert.Equal(dummyNumProcessRetries, Regex.Matches(resultLog, Strings.LogWarning_RetriesInExistingProcessExhausted.Substring(0, 30)).Count); // Logs before each process swap
            // Verify calls
            mockTestSubject.
                Protected().
                As<IOutOfProcessNodeJSServiceProtectedMembers>().
                Verify(t => t.TryInvokeAsync<int>(dummyInvocationRequest, dummyCancellationToken), Times.Exactly(expectedNumTries));
            mockTestSubject.Verify(t => t.MoveToNewProcess(false), times: Times.Exactly(2));
            Assert.Same(dummyException, result);
        }

        [Fact]
        public async void TryInvokeCoreAsync_DoesNotRetryInvocationIfModuleSourceIsAnUnseekableStream()
        {
            // Arrange
            var dummyCancellationToken = new CancellationToken();
            var dummyException = new InvocationException();
            Mock<Stream> mockStream = _mockRepository.Create<Stream>();
            mockStream.Setup(s => s.CanSeek).Returns(false);
            var dummyInvocationRequest = new InvocationRequest(ModuleSourceType.Stream, moduleStreamSource: mockStream.Object);
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService();
            mockTestSubject.CallBase = true;
            mockTestSubject.Setup(t => t.ConnectIfNotConnected());
            mockTestSubject.
                Protected().
                As<IOutOfProcessNodeJSServiceProtectedMembers>().
                Setup(t => t.TryInvokeAsync<int>(dummyInvocationRequest, dummyCancellationToken)).
                ThrowsAsync(dummyException);
            mockTestSubject.Setup(t => t.CreateCancellationToken(dummyCancellationToken)).Returns((dummyCancellationToken, null));

            // Act and assert
            InvocationException result = await Assert.
                ThrowsAsync<InvocationException>(async () => await mockTestSubject.Object.TryInvokeCoreAsync<int>(dummyInvocationRequest, dummyCancellationToken).ConfigureAwait(false)).
                ConfigureAwait(false);
            _mockRepository.VerifyAll();
            Assert.Same(dummyException, result);
            mockTestSubject.
                Protected().
                As<IOutOfProcessNodeJSServiceProtectedMembers>().
                Verify(t => t.TryInvokeAsync<int>(dummyInvocationRequest, dummyCancellationToken), Times.Once()); // No retries
        }

        [Fact]
        public async void TryInvokeCoreAsync_ResetsStreamPositionAndRetriesInvocationIfModuleSourceIsASeekableStreamThatIsNotAtItsInitialPosition()
        {
            // Arrange
            var dummyCancellationToken = new CancellationToken();
            const int dummyNumRetries = 2;
            const int dummyNumProcessRetries = 2;
            var dummyOptions = new OutOfProcessNodeJSServiceOptions { NumRetries = dummyNumRetries, NumProcessRetries = dummyNumProcessRetries };
            Mock<IOptions<OutOfProcessNodeJSServiceOptions>> mockOptionsAccessor = _mockRepository.Create<IOptions<OutOfProcessNodeJSServiceOptions>>();
            mockOptionsAccessor.Setup(o => o.Value).Returns(dummyOptions);
            var dummyException = new HttpRequestException(); // Process retries are ignored if exception is an InvocationException (cause by JS)
            const int dummyStreamInitialPosition = 1;
            Mock<Stream> mockStream = _mockRepository.Create<Stream>();
            mockStream.Setup(s => s.CanSeek).Returns(true);
            mockStream.SetupSequence(s => s.Position).Returns(dummyStreamInitialPosition).Returns(2).Returns(2); // Position changes after caching of initial position
            var dummyInvocationRequest = new InvocationRequest(ModuleSourceType.Stream, moduleStreamSource: mockStream.Object);
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(optionsAccessor: mockOptionsAccessor.Object);
            mockTestSubject.CallBase = true;
            mockTestSubject.Setup(t => t.ConnectIfNotConnected());
            mockTestSubject.
                Protected().
                As<IOutOfProcessNodeJSServiceProtectedMembers>().
                Setup(t => t.TryInvokeAsync<int>(dummyInvocationRequest, dummyCancellationToken)).
                ThrowsAsync(dummyException);
            mockTestSubject.Setup(t => t.CreateCancellationToken(dummyCancellationToken)).Returns((dummyCancellationToken, null));
            mockTestSubject.Setup(t => t.MoveToNewProcess(false));

            // Act and assert
            HttpRequestException result = await Assert.
                ThrowsAsync<HttpRequestException>(async () => await mockTestSubject.Object.TryInvokeCoreAsync<int>(dummyInvocationRequest, dummyCancellationToken).ConfigureAwait(false)).
                ConfigureAwait(false);
            Assert.Same(dummyException, result);
            _mockRepository.VerifyAll();
            mockTestSubject.
                Protected().
                As<IOutOfProcessNodeJSServiceProtectedMembers>().
                Verify(t => t.TryInvokeAsync<int>(dummyInvocationRequest, It.IsAny<CancellationToken>()), Times.Exactly(dummyNumRetries * (dummyNumProcessRetries + 1) + 1));
            mockStream.VerifySet(s => s.Position = dummyStreamInitialPosition, Times.Exactly(dummyNumRetries * (dummyNumProcessRetries + 1)));
        }

        [Fact]
        public async void TryInvokeCoreAsync_RetriesInvocationIfModuleSourceIsASeekableStreamAtItsInitialPosition()
        {
            // Arrange
            var dummyCancellationToken = new CancellationToken();
            const int dummyNumRetries = 2;
            const int dummyNumProcessRetries = 2;
            var dummyOptions = new OutOfProcessNodeJSServiceOptions { NumRetries = dummyNumRetries, NumProcessRetries = dummyNumProcessRetries };
            Mock<IOptions<OutOfProcessNodeJSServiceOptions>> mockOptionsAccessor = _mockRepository.Create<IOptions<OutOfProcessNodeJSServiceOptions>>();
            mockOptionsAccessor.Setup(o => o.Value).Returns(dummyOptions);
            var dummyException = new HttpRequestException(); // Process retries are ignored if exception is an InvocationException (cause by JS)
            const int dummyStreamInitialPosition = 1;
            Mock<Stream> mockStream = _mockRepository.Create<Stream>();
            mockStream.Setup(s => s.CanSeek).Returns(true);
            mockStream.Setup(s => s.Position).Returns(dummyStreamInitialPosition);
            var dummyInvocationRequest = new InvocationRequest(ModuleSourceType.Stream, moduleStreamSource: mockStream.Object);
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(optionsAccessor: mockOptionsAccessor.Object);
            mockTestSubject.CallBase = true;
            mockTestSubject.Setup(t => t.ConnectIfNotConnected());
            mockTestSubject.
                Protected().
                As<IOutOfProcessNodeJSServiceProtectedMembers>().
                Setup(t => t.TryInvokeAsync<int>(dummyInvocationRequest, dummyCancellationToken)).
                ThrowsAsync(dummyException);
            mockTestSubject.Setup(t => t.CreateCancellationToken(dummyCancellationToken)).Returns((dummyCancellationToken, null));
            mockTestSubject.Setup(t => t.MoveToNewProcess(false));

            // Act and assert
            HttpRequestException result = await Assert.
                ThrowsAsync<HttpRequestException>(async () => await mockTestSubject.Object.TryInvokeCoreAsync<int>(dummyInvocationRequest, dummyCancellationToken).ConfigureAwait(false)).
                ConfigureAwait(false);
            Assert.Same(dummyException, result);
            _mockRepository.VerifyAll();
            mockTestSubject.
                Protected().
                As<IOutOfProcessNodeJSServiceProtectedMembers>().
                Verify(t => t.TryInvokeAsync<int>(dummyInvocationRequest, It.IsAny<CancellationToken>()), Times.Exactly(dummyNumRetries * (dummyNumProcessRetries + 1) + 1));
            mockStream.VerifySet(s => s.Position = dummyStreamInitialPosition, Times.Never);
        }

        [Fact]
        public async void TryInvokeCoreAsync_TracksInvokeTaskIfInvokeTaskTrackingIsEnabled()
        {
            // Arrange
            (bool, int) expectedResult = (true, 4);
            var dummyTrackedInvokeTasks = new ConcurrentDictionary<Task, object?>();
            var dummyInvocationRequest = new InvocationRequest(ModuleSourceType.String, "dummyModuleSource");
            var dummyCancellationToken = new CancellationToken();
            using var dummyInvokeTaskCreationCountdown = new CountdownEvent(1);
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(monitorService: _mockRepository.Create<IMonitorService>().Object,
                taskService: _mockRepository.Create<ITaskService>().Object);
            mockTestSubject.CallBase = true;
            mockTestSubject.Setup(t => t.InitializeFileWatching()).Returns((true, dummyTrackedInvokeTasks, dummyInvokeTaskCreationCountdown)); // track invoke tasks
            mockTestSubject.Setup(t => t.ConnectIfNotConnected());
            mockTestSubject.Setup(t => t.CreateCancellationToken(dummyCancellationToken)).Returns((dummyCancellationToken, null));
            mockTestSubject.
                Setup(t => t.TryTrackedInvokeAsync<int>(dummyInvocationRequest, dummyTrackedInvokeTasks, dummyInvokeTaskCreationCountdown, dummyCancellationToken)).
                ReturnsAsync(expectedResult);

            // Act
            (bool, int) result = await mockTestSubject.Object.TryInvokeCoreAsync<int>(dummyInvocationRequest, dummyCancellationToken).ConfigureAwait(false);

            // Assert
            _mockRepository.VerifyAll();
            Assert.Equal(expectedResult, result);
        }

        // Note for the following (CreateCancellationToken) tests: CancellationToken1.Equal(CancellationToken2) returns CancellationToken1._source == CancellationToken2._source where source is 
        // the cancellation token's parent CancellationTokenSource. CancellationToken is a struct while CancellationTokenSource is a class.
        [Theory]
        [MemberData(nameof(CreateCancellationToken_ReturnsCancellationTokenAndCancellationTokenSourceIfTimeoutIsNotInfinite_Data))]
        public void CreateCancellationToken_ReturnsCancellationTokenAndCancellationTokenSourceIfTimeoutIsNotInfinite(CancellationToken dummyCancellationToken)
        {
            // Arrange
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService();
            mockTestSubject.CallBase = true;

            // Act
            (CancellationToken resultCancellationToken, CancellationTokenSource? resultCancellationTokenSource) = mockTestSubject.Object.CreateCancellationToken(dummyCancellationToken);

            // Assert
            Assert.NotNull(resultCancellationTokenSource);
            Assert.Equal(resultCancellationToken, resultCancellationTokenSource?.Token);
        }

        public static IEnumerable<object[]> CreateCancellationToken_ReturnsCancellationTokenAndCancellationTokenSourceIfTimeoutIsNotInfinite_Data()
        {
            var dummyCancellationTokenSource = new CancellationTokenSource();

            return new object[][]{
                new object[]{CancellationToken.None},
                new object[]{dummyCancellationTokenSource.Token}
            };
        }

        [Fact]
        public void CreateCancellationToken_ReturnsOriginalCancellationTokenIfTimeoutIsInfinite()
        {
            // Arrange
            var dummyOptions = new OutOfProcessNodeJSServiceOptions { TimeoutMS = -1 };
            Mock<IOptions<OutOfProcessNodeJSServiceOptions>> mockOptionsAccessor = _mockRepository.Create<IOptions<OutOfProcessNodeJSServiceOptions>>();
            mockOptionsAccessor.Setup(o => o.Value).Returns(dummyOptions);
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(optionsAccessor: mockOptionsAccessor.Object);
            mockTestSubject.CallBase = true;
            using var dummyCancellationTokenSource = new CancellationTokenSource();
            // Act
            (CancellationToken resultCancellationToken, CancellationTokenSource? resultCancellationTokenSource) = mockTestSubject.Object.CreateCancellationToken(dummyCancellationTokenSource.Token);

            // Assert
            Assert.Null(resultCancellationTokenSource);
            Assert.Equal(dummyCancellationTokenSource.Token, resultCancellationToken);
        }

        [Fact(Timeout = TIMEOUT_MS)]
        public void ConnectIfNotConnected_IfNodeJSProcessIsNullFirstThreadCreatesAndConnectsToNodeJSProcess()
        {
            // Arrange
            Mock<INodeJSProcess> mockNodeJSProcess = _mockRepository.Create<INodeJSProcess>();
            mockNodeJSProcess.Setup(n => n.SetConnected());
            mockNodeJSProcess.Setup(n => n.Connected).Returns(true);
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService();
            mockTestSubject.CallBase = true;
            mockTestSubject.
                Setup(t => t.CreateAndSetUpProcess(It.IsAny<EventWaitHandle>())).
                Returns(mockNodeJSProcess.Object).
                Callback<EventWaitHandle>(eventWaitHandle => eventWaitHandle.Set());
            OutOfProcessNodeJSService testSubject = mockTestSubject.Object;

            // Act
            const int numThreads = 5;
            var threads = new List<Thread>();
            for (int i = 0; i < numThreads; i++)
            {
                var thread = new Thread(() => testSubject.ConnectIfNotConnected());
                threads.Add(thread);
                thread.Start();
            }
            foreach (Thread thread in threads)
            {
                thread.Join();
            }

            // Assert
            _mockRepository.VerifyAll();
            mockTestSubject.Verify(t => t.CreateAndSetUpProcess(It.IsAny<EventWaitHandle>()), Times.Once); // Only creates and connects once
        }

        [Fact(Timeout = TIMEOUT_MS)]
        public void ConnectIfNotConnected_StopsAndStartsFileWatcher()
        {
            // Arrange
            Mock<INodeJSProcess> mockNodeJSProcess = _mockRepository.Create<INodeJSProcess>();
            mockNodeJSProcess.Setup(n => n.SetConnected());
            mockNodeJSProcess.Setup(n => n.Connected).Returns(true);
            var dummyOptions = new OutOfProcessNodeJSServiceOptions() { EnableFileWatching = true };
            Mock<IOptions<OutOfProcessNodeJSServiceOptions>> mockOptionsAccessor = _mockRepository.Create<IOptions<OutOfProcessNodeJSServiceOptions>>();
            mockOptionsAccessor.Setup(o => o.Value).Returns(dummyOptions);
            // So _fileWatcher is set
            Mock<IFileWatcher> mockFileWatcher = _mockRepository.Create<IFileWatcher>();
            Mock<IFileWatcherFactory> mockFileWatcherFactory = _mockRepository.Create<IFileWatcherFactory>();
            mockFileWatcherFactory.
                Setup(f => f.Create(dummyOptions.WatchPath, dummyOptions.WatchSubdirectories, dummyOptions.WatchFileNamePatterns, It.IsAny<FileChangedEventHandler>())).
                Returns(mockFileWatcher.Object);

            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(optionsAccessor: mockOptionsAccessor.Object,
                fileWatcherFactory: mockFileWatcherFactory.Object,
                monitorService: _mockRepository.Create<IMonitorService>().Object,
                taskService: _mockRepository.Create<ITaskService>().Object);
            mockTestSubject.CallBase = true;
            mockTestSubject.
                Setup(t => t.CreateAndSetUpProcess(It.IsAny<EventWaitHandle>())).
                Returns(mockNodeJSProcess.Object).
                Callback<EventWaitHandle>(eventWaitHandle => eventWaitHandle.Set());
            OutOfProcessNodeJSService testSubject = mockTestSubject.Object;

            // Act
            const int numThreads = 5;
            var threads = new List<Thread>();
            for (int i = 0; i < numThreads; i++)
            {
                var thread = new Thread(() => testSubject.ConnectIfNotConnected());
                threads.Add(thread);
                thread.Start();
            }
            foreach (Thread thread in threads)
            {
                thread.Join();
            }

            // Assert
            _mockRepository.VerifyAll();
            mockTestSubject.Verify(t => t.CreateAndSetUpProcess(It.IsAny<EventWaitHandle>()), Times.Once); // Only creates and connects once
            // File watcher stopped while connecting
            mockFileWatcher.Verify(f => f.Stop(), Times.Once);
            mockFileWatcher.Verify(f => f.Start(), Times.Once);
        }

        [Fact(Timeout = TIMEOUT_MS)]
        public void ConnectIfNotConnected_IfNodeJSProcessIsNotConnectedFirstThreadCreatesNodeJSProcessBeforeInvoking()
        {
            // Arrange
            bool dummyIsConnected = false;
            Mock<INodeJSProcess> mockNodeJSProcess = _mockRepository.Create<INodeJSProcess>();
            mockNodeJSProcess.Setup(n => n.SetConnected()).Callback(() => dummyIsConnected = true);
            mockNodeJSProcess.Setup(n => n.Connected).Returns(() => dummyIsConnected); // Use an anonymous method so we have excess to parent scope's variables
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService();
            mockTestSubject.CallBase = true;
            mockTestSubject.
                Setup(t => t.CreateAndSetUpProcess(It.IsAny<EventWaitHandle>())).
                Returns(mockNodeJSProcess.Object).
                Callback<EventWaitHandle>(eventWaitHandle => eventWaitHandle.Set());
            OutOfProcessNodeJSService testSubject = mockTestSubject.Object;
            testSubject.ConnectIfNotConnected(); // Creates _nodeJSProcess
            dummyIsConnected = false; // Disconnect _nodeJSProcess

            // Act
            const int numThreads = 5;
            var threads = new List<Thread>();
            for (int i = 0; i < numThreads; i++)
            {
                var thread = new Thread(() => testSubject.ConnectIfNotConnected());
                threads.Add(thread);
                thread.Start();
            }
            foreach (Thread thread in threads)
            {
                thread.Join();
            }

            // Assert
            _mockRepository.VerifyAll();
            mockTestSubject.Verify(t => t.CreateAndSetUpProcess(It.IsAny<EventWaitHandle>()), Times.Exactly(2)); // Once when we're arranging, once when we're acting
            mockNodeJSProcess.Verify(n => n.Dispose(), Times.Once); // Initial process gets disposed
        }

        [Fact(Timeout = TIMEOUT_MS)]
        public void ConnectIfNotConnected_RetriesConnectionIfConnectionAttemptTimesoutAndThrowsConnectionExceptionIfNoRetriesRemain()
        {
            // Arrange
            const string dummyExitCode = "dummyExitCode";
            const bool dummyHasExited = false;
            const int dummyTimeoutMS = 100; // Arbitrary, we never signal the wait handle, so timeout is always triggered
            const int dummyNumConnectionRetries = 2;
            Mock<INodeJSProcess> mockNodeJSProcess = _mockRepository.Create<INodeJSProcess>();
            mockNodeJSProcess.Setup(n => n.Connected).Returns(false);
            mockNodeJSProcess.Setup(n => n.HasExited).Returns(dummyHasExited);
            mockNodeJSProcess.Setup(n => n.ExitStatus).Returns(dummyExitCode);
            var dummyOptions = new OutOfProcessNodeJSServiceOptions { TimeoutMS = dummyTimeoutMS, NumConnectionRetries = dummyNumConnectionRetries };
            Mock<IOptions<OutOfProcessNodeJSServiceOptions>> mockOptionsAccessor = _mockRepository.Create<IOptions<OutOfProcessNodeJSServiceOptions>>();
            mockOptionsAccessor.Setup(o => o.Value).Returns(dummyOptions);
            var loggerStringBuilder = new StringBuilder();
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(optionsAccessor: mockOptionsAccessor.Object,
                loggerStringBuilder: loggerStringBuilder);
            mockTestSubject.CallBase = true;
            mockTestSubject.Setup(t => t.CreateAndSetUpProcess(It.IsAny<EventWaitHandle>())).Returns(mockNodeJSProcess.Object);
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
                        testSubject.ConnectIfNotConnected();
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
            Assert.Equal(numThreads * dummyNumConnectionRetries, Regex.Matches(logResult, $"{nameof(LogLevel.Warning)}: ").Count); // We don't log on the last failed attempt since we throw then
            mockTestSubject.Verify(t => t.CreateAndSetUpProcess(It.IsAny<EventWaitHandle>()), Times.Exactly(numThreads * (1 + dummyNumConnectionRetries))); // Each thread tries to create and connect to NodeJS process 1 + dummyNumConnectionRetries times
            Assert.Equal(numThreads, results.Count);
            foreach (Exception exception in results)
            {
                // Each thread must throw a connection exception
                Assert.IsType<ConnectionException>(exception);
                Assert.Equal(string.Format(Strings.ConnectionException_OutOfProcessNodeJSService_ConnectionAttemptTimedOut, dummyTimeoutMS, dummyHasExited, dummyExitCode),
                    exception.Message);
            }
            mockNodeJSProcess.Verify(n => n.Dispose(), Times.Exactly(numThreads * (1 + dummyNumConnectionRetries) * 2 - 1)); // We call dispose twice for each attempt other than the first, for which we call dispose only once
        }

        [Fact]
        public void InitializeFileWatching_DoesNothingIfFileWatchingIsDisabled()
        {
            // Arrange
            var dummyOptions = new OutOfProcessNodeJSServiceOptions() { EnableFileWatching = false };
            Mock<IOptions<OutOfProcessNodeJSServiceOptions>> mockOptionsAccessor = _mockRepository.Create<IOptions<OutOfProcessNodeJSServiceOptions>>();
            mockOptionsAccessor.Setup(o => o.Value).Returns(dummyOptions);
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(optionsAccessor: mockOptionsAccessor.Object);
            mockTestSubject.CallBase = true;

            // Act
            (bool resultTrackInvokeTasks, ConcurrentDictionary<Task, object?> resultTrackedInvokeTasks, CountdownEvent resultInvokeTaskCreationCountdown) = mockTestSubject.Object.InitializeFileWatching();

            // Assert
            Assert.False(resultTrackInvokeTasks);
            Assert.Null(resultTrackedInvokeTasks);
            Assert.Null(resultInvokeTaskCreationCountdown);
        }

        [Fact]
        public void InitializeFileWatching_CreatesFileWatcherAndReturnsNothingIfFileWatchingIsEnabledButGracefulShutdownIsNot()
        {
            // Arrange
            const string dummyWatchPath = "dummyWatchPath";
            const bool dummyWatchSubdirectories = false;
            string[] dummyWatchFileNames = new[] { "*.dummy" };
            var dummyOptions = new OutOfProcessNodeJSServiceOptions()
            {
                EnableFileWatching = true,
                GracefulProcessShutdown = false,
                WatchPath = dummyWatchPath,
                WatchSubdirectories = dummyWatchSubdirectories
            };
            Mock<IOptions<OutOfProcessNodeJSServiceOptions>> mockOptionsAccessor = _mockRepository.Create<IOptions<OutOfProcessNodeJSServiceOptions>>();
            mockOptionsAccessor.Setup(o => o.Value).Returns(dummyOptions);
            Mock<IFileWatcherFactory> mockFileWatcherFactory = _mockRepository.Create<IFileWatcherFactory>();
            mockFileWatcherFactory.Setup(f => f.Create(dummyWatchPath, dummyWatchSubdirectories, dummyWatchFileNames, It.IsAny<FileChangedEventHandler>())); // Assigned directly to instance variable
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(optionsAccessor: mockOptionsAccessor.Object,
                fileWatcherFactory: mockFileWatcherFactory.Object,
                monitorService: _mockRepository.Create<IMonitorService>().Object,
                taskService: _mockRepository.Create<ITaskService>().Object);
            mockTestSubject.CallBase = true;

            // Act
            (bool resultTrackInvokeTasks, ConcurrentDictionary<Task, object?> resultTrackedInvokeTasks, CountdownEvent resultInvokeTaskCreationCountdown) = mockTestSubject.Object.InitializeFileWatching();

            // Assert
            Assert.False(resultTrackInvokeTasks);
            Assert.Null(resultTrackedInvokeTasks);
            Assert.Null(resultInvokeTaskCreationCountdown);
        }

        [Fact]
        public void InitializeFileWatching_CreatesFileWatcherAndReturnsInvokeTaskTrackingVariablesIfBothFileWatchingAndGracefulShutdownAreEnabled()
        {
            // Arrange
            const string dummyWatchPath = "dummyWatchPath";
            const bool dummyWatchSubdirectories = false;
            string[] dummyWatchFileNames = new[] { "*.dummy" };
            var dummyOptions = new OutOfProcessNodeJSServiceOptions()
            {
                EnableFileWatching = true,
                GracefulProcessShutdown = true,
                WatchPath = dummyWatchPath,
                WatchSubdirectories = dummyWatchSubdirectories
            };
            Mock<IOptions<OutOfProcessNodeJSServiceOptions>> mockOptionsAccessor = _mockRepository.Create<IOptions<OutOfProcessNodeJSServiceOptions>>();
            mockOptionsAccessor.Setup(o => o.Value).Returns(dummyOptions);
            Mock<IFileWatcherFactory> mockFileWatcherFactory = _mockRepository.Create<IFileWatcherFactory>();
            mockFileWatcherFactory.Setup(f => f.Create(dummyWatchPath, dummyWatchSubdirectories, dummyWatchFileNames, It.IsAny<FileChangedEventHandler>())); // Assigned directly to instance variable
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(optionsAccessor: mockOptionsAccessor.Object,
                fileWatcherFactory: mockFileWatcherFactory.Object,
                monitorService: _mockRepository.Create<IMonitorService>().Object,
                taskService: _mockRepository.Create<ITaskService>().Object);
            mockTestSubject.CallBase = true;

            // Act
            (bool resultTrackInvokeTasks, ConcurrentDictionary<Task, object?> resultTrackedInvokeTasks, CountdownEvent resultInvokeTaskCreationCountdown) = mockTestSubject.Object.InitializeFileWatching();
            using (resultInvokeTaskCreationCountdown)
            {
                // Assert
                Assert.True(resultTrackInvokeTasks);
                Assert.NotNull(resultTrackedInvokeTasks);
                Assert.NotNull(resultInvokeTaskCreationCountdown);
                Assert.Equal(1, resultInvokeTaskCreationCountdown.CurrentCount);
            }
        }

        // Doesn't verify that countdown isn't signaled till after tracked task is added to trackedInvokeTasks.
        // This is verified in SwapProcesses_TryTrackedInvokeAsync_AreThreadSafe.
        [Fact]
        public async void TryTrackedInvokeAsync_TriesTrackedInvoke()
        {
            // Arrange
            const int initialCountdownCount = 1;
            var dummyInvocationRequest = new InvocationRequest(ModuleSourceType.String, "dummyModuleSource");
            var dummyTrackedInvokeTasks = new ConcurrentDictionary<Task, object?>();
            (bool, int) expectedResult = (true, 4);
            int midwayCountdownCount = 0;
            using var dummyWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
            using var dummyInvokeTaskCreationCountdown = new CountdownEvent(initialCountdownCount);
            var dummyCancellationToken = new CancellationToken();
            var dummyTask = Task.Run(() =>
            {
                dummyWaitHandle.WaitOne();
                return expectedResult;
            });
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService();
            mockTestSubject.CallBase = true;
            mockTestSubject.
                Protected().
                As<IOutOfProcessNodeJSServiceProtectedMembers>().
                Setup(t => t.TryInvokeAsync<int>(dummyInvocationRequest, dummyCancellationToken)).
                Callback(() => midwayCountdownCount = dummyInvokeTaskCreationCountdown.CurrentCount).
                Returns(dummyTask);

            // Act and assert
            Task<(bool, int)> resultTask = mockTestSubject.Object.
                TryTrackedInvokeAsync<int>(dummyInvocationRequest, dummyTrackedInvokeTasks, dummyInvokeTaskCreationCountdown, dummyCancellationToken);
            Assert.Equal(initialCountdownCount + 1, midwayCountdownCount);
            Assert.Equal(initialCountdownCount, dummyInvokeTaskCreationCountdown.CurrentCount); // Once task is created, countdown should be signaled
            Assert.Single(dummyTrackedInvokeTasks); // While task hasn't completed, it should be tracked
            Assert.True(dummyTrackedInvokeTasks.ContainsKey(dummyTask)); // While task hasn't completed, it should be tracked
            dummyWaitHandle.Set(); // Allow task to complete
            (bool, int) result = await resultTask.ConfigureAwait(false);
            Assert.Empty(dummyTrackedInvokeTasks); // No longer tracked after completion
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void CreateAndSetUpProcess_CreatesAndSetsUpNodeJSProcess()
        {
            // Arrange
            const string dummyServerScript = "dummyServerScript";
            const string dummyServerScriptName = "dummyServerScriptName";
            var dummyServerScriptAssembly = new DummyAssembly();
            Mock<IEmbeddedResourcesService> mockEmbeddedResourcesService = _mockRepository.Create<IEmbeddedResourcesService>();
            mockEmbeddedResourcesService.Setup(e => e.ReadAsString(dummyServerScriptAssembly, dummyServerScriptName)).Returns(dummyServerScript);
            Mock<INodeJSProcess> mockNodeJSProcess = _mockRepository.Create<INodeJSProcess>();
            mockNodeJSProcess.Setup(n => n.AddOutputReceivedHandler(It.IsAny<MessageReceivedEventHandler>()));
            mockNodeJSProcess.Setup(n => n.AddErrorReceivedHandler(It.IsAny<MessageReceivedEventHandler>()));
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
            INodeJSProcess result = mockTestSubject.Object.CreateAndSetUpProcess(It.IsAny<EventWaitHandle>());

            // Assert
            Assert.Same(mockNodeJSProcess.Object, result);
        }

        [Fact(Timeout = TIMEOUT_MS)] // Calls ConnectIfNotConnected and EventWaitHandle.WaitOne so threading involved
        public void OutputReceivedHandler_IfNodeJSProcessIsNotConnectedAndMessageIsConnectionEstablishedMessageEstablishesConnection()
        {
            // Arrange
            const string dummyMessage = OutOfProcessNodeJSService.CONNECTION_ESTABLISHED_MESSAGE_START;
            Mock<INodeJSProcess> mockNodeJSProcess = _mockRepository.Create<INodeJSProcess>();
            mockNodeJSProcess.Setup(n => n.SetConnected());
            mockNodeJSProcess.Setup(n => n.Connected).Returns(false);
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService();
            mockTestSubject.CallBase = true;
            mockTestSubject.
                Setup(t => t.CreateAndSetUpProcess(It.IsAny<EventWaitHandle>())).
                Callback<EventWaitHandle>(eventWaitHandle => eventWaitHandle.Set()).
                Returns(mockNodeJSProcess.Object);
            mockTestSubject.Protected().As<IOutOfProcessNodeJSServiceProtectedMembers>().Setup(t => t.OnConnectionEstablishedMessageReceived(dummyMessage));
            mockTestSubject.Object.ConnectIfNotConnected(); // Set _nodeJSProcess
            using var dummyWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
            // Act
            mockTestSubject.Object.OutputReceivedHandler(dummyMessage, dummyWaitHandle);

            // Assert
            _mockRepository.VerifyAll();
            Assert.True(dummyWaitHandle.WaitOne(0)); // Ensure that it gets signaled
            mockTestSubject.Protected().As<IOutOfProcessNodeJSServiceProtectedMembers>().Verify(t => t.OnConnectionEstablishedMessageReceived(dummyMessage), Times.Once());
        }

        [Fact]
        public void OutputReceivedHandler_IfNodeJSProcessIsNullLogsMessages()
        {
            // Arrange
            const string dummyMessage = "dummyMessage";
            var loggerStringBuilder = new StringBuilder();
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(loggerStringBuilder: loggerStringBuilder,
                logLevel: LogLevel.Information);
            mockTestSubject.CallBase = true;
            using EventWaitHandle dummyWaitHandle = new(true, EventResetMode.AutoReset);

            // Act
            mockTestSubject.Object.OutputReceivedHandler(dummyMessage, dummyWaitHandle);

            // Assert
            _mockRepository.VerifyAll();
            string logResult = loggerStringBuilder.ToString();
            Assert.Contains(dummyMessage, logResult);
        }

        [Theory(Timeout = TIMEOUT_MS)] // Calls ConnectIfNotConnected so threading involved
        [MemberData(nameof(OutputReceivedHandler_IfNodeJSProcessIsConnectedOrMessageIsNotConnectionEstablishedMessageLogsMessages_Data))]
        public void OutputReceivedHandler_IfNodeJSProcessIsConnectedOrMessageIsNotConnectionEstablishedMessageLogsMessages(bool dummyConnected, string dummyMessage)
        {
            // Arrange
            Mock<INodeJSProcess> mockNodeJSProcess = _mockRepository.Create<INodeJSProcess>();
            mockNodeJSProcess.Setup(n => n.SetConnected());
            mockNodeJSProcess.Setup(n => n.Connected).Returns(dummyConnected);
            var loggerStringBuilder = new StringBuilder();
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(loggerStringBuilder: loggerStringBuilder,
                logLevel: LogLevel.Information);
            mockTestSubject.CallBase = true;
            mockTestSubject.
                Setup(t => t.CreateAndSetUpProcess(It.IsAny<EventWaitHandle>())).
                Callback<EventWaitHandle>(eventWaitHandle => eventWaitHandle.Set()).
                Returns(mockNodeJSProcess.Object);
            mockTestSubject.Object.ConnectIfNotConnected(); // Set _nodeJSProcess
            using EventWaitHandle dummyWaitHandle = new(true, EventResetMode.AutoReset);

            // Act
            mockTestSubject.Object.OutputReceivedHandler(dummyMessage, dummyWaitHandle);

            // Assert
            _mockRepository.VerifyAll();
            string logResult = loggerStringBuilder.ToString();
            Assert.Contains(dummyMessage, logResult);
        }

        public static IEnumerable<object[]> OutputReceivedHandler_IfNodeJSProcessIsConnectedOrMessageIsNotConnectionEstablishedMessageLogsMessages_Data()
        {
            return new object[][]
            {
                new object[]{true, OutOfProcessNodeJSService.CONNECTION_ESTABLISHED_MESSAGE_START},
                new object[]{false, "dummyMessage"},
            };
        }

        [Fact]
        public void ErrorReceivedHandler_LogsMessages()
        {
            // Arrange
            const string dummyMessage = "dummyMessage";
            var loggerStringBuilder = new StringBuilder();
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(loggerStringBuilder: loggerStringBuilder, logLevel: LogLevel.Information);
            mockTestSubject.CallBase = true;

            // Act
            mockTestSubject.Object.ErrorReceivedHandler(dummyMessage);

            // Assert
            _mockRepository.VerifyAll();
            string logResult = loggerStringBuilder.ToString();
            Assert.Equal(logResult, $"{nameof(LogLevel.Error)}: {dummyMessage}\n", ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void FileChangedHandler_LogsChangedFileAndMovesToNewProcess()
        {
            // Arrange
            const string dummyPath = "dummyPath";
            var loggerStringBuilder = new StringBuilder();
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(loggerStringBuilder: loggerStringBuilder, logLevel: LogLevel.Information);
            mockTestSubject.CallBase = true;
            mockTestSubject.Setup(t => t.MoveToNewProcess(true));

            // Act
            mockTestSubject.Object.FileChangedHandler(dummyPath);

            // Assert
            mockTestSubject.Verify(t => t.MoveToNewProcess(true), times: Times.Once);
            Assert.Contains(string.Format(Strings.LogInformation_FileChangedMovingtoNewNodeJSProcess, dummyPath), loggerStringBuilder.ToString());
        }

        [Fact]
        public void MoveToNewProcess_DoesNothingIfConnectingLockNotAquiredAndReswapIfJustConnectedIsFalse()
        {
            // Arrange
            Mock<IMonitorService> mockMonitorService = _mockRepository.Create<IMonitorService>();
            bool dummyAquiredConnectingLock = false;
            mockMonitorService.Setup(m => m.TryEnter(It.IsAny<object>(), ref dummyAquiredConnectingLock));
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(monitorService: mockMonitorService.Object);
            mockTestSubject.CallBase = true;

            // Act
            mockTestSubject.Object.MoveToNewProcess(false);

            // Assert
            _mockRepository.VerifyAll();
            mockMonitorService.Verify(m => m.Enter(It.IsAny<object>(), ref dummyAquiredConnectingLock), Times.Never); // Verifies that we return immediately, nothing else called
        }

        [Fact(Timeout = TIMEOUT_MS)] // Calls ConnectIfNotConnected so threading involved
        public void MoveToNewProcess_DoesNothingIfConnectingLockNotAquiredAndConnecting()
        {
            // Arrange
            Mock<IMonitorService> mockMonitorService = _mockRepository.Create<IMonitorService>();
            bool dummyAquiredConnectingLock = false;
            mockMonitorService.Setup(m => m.TryEnter(It.IsAny<object>(), ref dummyAquiredConnectingLock));
            Mock<INodeJSProcess> mockNodeJSProcess = _mockRepository.Create<INodeJSProcess>();
            mockNodeJSProcess.Setup(n => n.Connected).Returns(false);
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(monitorService: mockMonitorService.Object);
            mockTestSubject.CallBase = true;
            mockTestSubject.
                Setup(t => t.CreateAndSetUpProcess(It.IsAny<EventWaitHandle>())).
                Returns(mockNodeJSProcess.Object).
                Callback<EventWaitHandle>(eventWaitHandle => eventWaitHandle.Set());
            mockTestSubject.Object.ConnectIfNotConnected(); // Creates _nodeJSProcess

            // Act
            mockTestSubject.Object.MoveToNewProcess(true);

            // Assert
            _mockRepository.VerifyAll();
            mockMonitorService.Verify(m => m.Enter(It.IsAny<object>(), ref dummyAquiredConnectingLock), Times.Never); // Verifies that we return immediately, nothing else called
        }

        [Fact]
        public void MoveToNewProcess_MovesToNewProcessIfConnectingLockAcquired()
        {
            // Arrange
            bool dummyInitialAquiredConnectingLock = false;
            const bool dummyFinalAquiredConnectingLock = true;
            object? dummyConnectingLock = null;
            Mock<IMonitorService> mockMonitorService = _mockRepository.Create<IMonitorService>();
            // Mocking function with ref parameter - https://github.com/moq/moq4/issues/105
            mockMonitorService.
                Setup(m => m.TryEnter(It.IsAny<object>(), ref dummyInitialAquiredConnectingLock)).
                Callback(new TryEnterCallback((object connectingLock, ref bool aquiredConnectingLock) =>
                {
                    aquiredConnectingLock = dummyFinalAquiredConnectingLock;
                    dummyConnectingLock = connectingLock;
                }));
            mockMonitorService.Setup(m => m.Exit(It.Is<object>(connectingLock => connectingLock == dummyConnectingLock)));
            // So _fileWatcher is set
            var dummyOptions = new OutOfProcessNodeJSServiceOptions() { EnableFileWatching = true };
            Mock<IOptions<OutOfProcessNodeJSServiceOptions>> mockOptionsAccessor = _mockRepository.Create<IOptions<OutOfProcessNodeJSServiceOptions>>();
            mockOptionsAccessor.Setup(o => o.Value).Returns(dummyOptions);
            Mock<IFileWatcher> mockFileWatcher = _mockRepository.Create<IFileWatcher>();
            mockFileWatcher.Setup(f => f.Stop());
            Mock<IFileWatcherFactory> mockFileWatcherFactory = _mockRepository.Create<IFileWatcherFactory>();
            mockFileWatcherFactory.
                Setup(f => f.Create(dummyOptions.WatchPath, dummyOptions.WatchSubdirectories, dummyOptions.WatchFileNamePatterns, It.IsAny<FileChangedEventHandler>())).
                Returns(mockFileWatcher.Object);

            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(optionsAccessor: mockOptionsAccessor.Object,
                fileWatcherFactory: mockFileWatcherFactory.Object,
                monitorService: mockMonitorService.Object,
                taskService: _mockRepository.Create<ITaskService>().Object);
            mockTestSubject.CallBase = true;
            mockTestSubject.Setup(t => t.SwapProcesses());

            // Act
            mockTestSubject.Object.MoveToNewProcess(default); // reswapIfJustConnected unused

            // Assert
            _mockRepository.VerifyAll();
        }

        [Fact(Timeout = TIMEOUT_MS)] // Calls ConnectIfNotConnected so threading involved
        public void MoveToNewProcess_MovesToNewProcessIfConnectingLockNotAquiredButAlreadyConnectedAndReswapIfJustConnectedIsTrue()
        {
            // Arrange
            bool dummyInitialAquiredConnectingLock = false;
            const bool dummyFinalAquiredConnectingLock = true;
            object? dummyConnectingLock = null;
            Mock<IMonitorService> mockMonitorService = _mockRepository.Create<IMonitorService>();
            // Mocking function with ref parameter - https://github.com/moq/moq4/issues/105
            mockMonitorService.
                Setup(m => m.TryEnter(It.IsAny<object>(), ref dummyInitialAquiredConnectingLock)).
                Callback(new TryEnterCallback((object connectingLock, ref bool _) => dummyConnectingLock = connectingLock));
            mockMonitorService.
                Setup(m => m.Enter(It.Is<object>(connectingLock => connectingLock == dummyConnectingLock), ref dummyInitialAquiredConnectingLock)).
                Callback(new TryEnterCallback((object _, ref bool aquiredConnectingLock) => aquiredConnectingLock = dummyFinalAquiredConnectingLock));
            mockMonitorService.Setup(m => m.Exit(It.Is<object>(connectingLock => connectingLock == dummyConnectingLock)));
            Mock<INodeJSProcess> mockNodeJSProcess = _mockRepository.Create<INodeJSProcess>();
            mockNodeJSProcess.Setup(n => n.Connected).Returns(true);
            // So _fileWatcher is set
            var dummyOptions = new OutOfProcessNodeJSServiceOptions() { EnableFileWatching = true };
            Mock<IOptions<OutOfProcessNodeJSServiceOptions>> mockOptionsAccessor = _mockRepository.Create<IOptions<OutOfProcessNodeJSServiceOptions>>();
            mockOptionsAccessor.Setup(o => o.Value).Returns(dummyOptions);
            Mock<IFileWatcher> mockFileWatcher = _mockRepository.Create<IFileWatcher>();
            mockFileWatcher.Setup(f => f.Stop());
            Mock<IFileWatcherFactory> mockFileWatcherFactory = _mockRepository.Create<IFileWatcherFactory>();
            mockFileWatcherFactory.
                Setup(f => f.Create(dummyOptions.WatchPath, dummyOptions.WatchSubdirectories, dummyOptions.WatchFileNamePatterns, It.IsAny<FileChangedEventHandler>())).
                Returns(mockFileWatcher.Object);

            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(optionsAccessor: mockOptionsAccessor.Object,
                fileWatcherFactory: mockFileWatcherFactory.Object,
                monitorService: mockMonitorService.Object,
                taskService: _mockRepository.Create<ITaskService>().Object);
            mockTestSubject.CallBase = true;
            mockTestSubject.Setup(t => t.SwapProcesses());
            mockTestSubject.
                Setup(t => t.CreateAndSetUpProcess(It.IsAny<EventWaitHandle>())).
                Returns(mockNodeJSProcess.Object).
                Callback<EventWaitHandle>(eventWaitHandle => eventWaitHandle.Set());
            mockTestSubject.Object.ConnectIfNotConnected(); // Creates _nodeJSProcess

            // Act
            mockTestSubject.Object.MoveToNewProcess(true);

            // Assert
            _mockRepository.VerifyAll();
        }

        [Fact(Timeout = TIMEOUT_MS)] // Calls ConnectIfNotConnected so threading involved
        public void SwapProcesses_SwapsToNewProcess()
        {
            // Arrange
            const int dummySafeID = 12345;
            Action? dummyKillProcessAction = null;
            Mock<ITaskService> mockTaskService = _mockRepository.Create<ITaskService>();
            mockTaskService.
                Setup(t => t.Run(It.IsAny<Action>())).
                Callback<Action>((action) => dummyKillProcessAction = action);
            Mock<INodeJSProcess> mockNodeJSProcess = _mockRepository.Create<INodeJSProcess>();
            mockNodeJSProcess.Setup(n => n.SafeID).Returns(dummySafeID);
            mockNodeJSProcess.Setup(n => n.Dispose());
            var loggerStringBuilder = new StringBuilder();
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(taskService: mockTaskService.Object,
                loggerStringBuilder: loggerStringBuilder,
                logLevel: LogLevel.Information);
            mockTestSubject.CallBase = true;
            mockTestSubject.
                Setup(t => t.CreateAndSetUpProcess(It.IsAny<EventWaitHandle>())).
                Returns(mockNodeJSProcess.Object).
                Callback<EventWaitHandle>(eventWaitHandle => eventWaitHandle.Set());
            mockTestSubject.Object.ConnectIfNotConnected(); // Creates _nodeJSProcess

            // Act and assert
            mockTestSubject.Object.SwapProcesses();
            Assert.NotNull(dummyKillProcessAction);
            dummyKillProcessAction!(); // Assert.NotNull throws if dummyKillProcessAction is null
            _mockRepository.VerifyAll();
            mockTestSubject.Verify(t => t.ConnectIfNotConnected(), Times.Exactly(2)); // Once when arranging
            Assert.Contains(string.Format(Strings.LogInformation_KillingNodeJSProcess, dummySafeID), loggerStringBuilder.ToString());
        }

        [Fact(Timeout = TIMEOUT_MS)] // Calls ConnectIfNotConnected so threading involved
        public void SwapProcesses_IfInvokeTaskTrackingIsEnabledAndThereArePendingInvokeTasksStoresTasksAndWaitsForThemToCompleteBeforeDisposingOfLastProcess()
        {
            // Arrange
            const int dummySafeID = 12345;
            object? dummyInvokeTaskTrackingLock = null;
            var dummyInvokeTask = new Task(() => { });
            Action? dummyKillProcessAction = null;
            Mock<IMonitorService> mockMonitorService = _mockRepository.Create<IMonitorService>();
            mockMonitorService.
                Setup(m => m.Enter(It.IsAny<object>())).
                Callback<object>((invokeTaskTrackingLock) => dummyInvokeTaskTrackingLock = invokeTaskTrackingLock);
            mockMonitorService.Setup(m => m.Exit(It.Is<object>(connectingLock => connectingLock == dummyInvokeTaskTrackingLock)));
            Mock<ITaskService> mockTaskService = _mockRepository.Create<ITaskService>();
            mockTaskService.
                Setup(t => t.Run(It.IsAny<Action>())).
                Callback<Action>((action) => dummyKillProcessAction = action);
            mockTaskService.Setup(t => t.WaitAll(new[] { dummyInvokeTask }));
            Mock<INodeJSProcess> mockNodeJSProcess = _mockRepository.Create<INodeJSProcess>();
            mockNodeJSProcess.Setup(n => n.SafeID).Returns(dummySafeID);
            mockNodeJSProcess.Setup(n => n.Dispose());
            var loggerStringBuilder = new StringBuilder();
            var dummyTrackedInvokeTasks = new ConcurrentDictionary<Task, object?>();
            dummyTrackedInvokeTasks.TryAdd(dummyInvokeTask, null);
            using var dummyInvokeTaskCreationCountdown = new CountdownEvent(1);
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(monitorService: mockMonitorService.Object,
                taskService: mockTaskService.Object,
                loggerStringBuilder: loggerStringBuilder,
                logLevel: LogLevel.Information);
            mockTestSubject.CallBase = true;
            mockTestSubject.Setup(t => t.InitializeFileWatching()).Returns((true, dummyTrackedInvokeTasks, dummyInvokeTaskCreationCountdown));
            mockTestSubject.
                Setup(t => t.CreateAndSetUpProcess(It.IsAny<EventWaitHandle>())).
                Returns(mockNodeJSProcess.Object).
                Callback<EventWaitHandle>(eventWaitHandle => eventWaitHandle.Set());
            mockTestSubject.Object.ConnectIfNotConnected(); // Creates _nodeJSProcess

            // Act and assert
            mockTestSubject.Object.SwapProcesses();
            Assert.NotNull(dummyKillProcessAction);
            dummyKillProcessAction!();
            _mockRepository.VerifyAll();
            mockTestSubject.Verify(t => t.ConnectIfNotConnected(), Times.Exactly(2)); // Once when arranging
            Assert.Contains(string.Format(Strings.LogInformation_KillingNodeJSProcess, dummySafeID), loggerStringBuilder.ToString());
            Assert.Empty(dummyTrackedInvokeTasks); // Emptied
            Assert.Equal(1, dummyInvokeTaskCreationCountdown.CurrentCount); // Gets reset
        }

        [Fact(Timeout = TIMEOUT_MS)] // Calls ConnectIfNotConnected so threading involved
        public void SwapProcesses_IfInvokeTaskTrackingIsEnabledAndThereAreNoPendingInvokeTasksDoesNotWaitBeforeDisposingOfLastProcess()
        {
            // Arrange
            const int dummySafeID = 12345;
            object? dummyInvokeTaskTrackingLock = null;
            Action? dummyKillProcessAction = null;
            Mock<IMonitorService> mockMonitorService = _mockRepository.Create<IMonitorService>();
            mockMonitorService.
                Setup(m => m.Enter(It.IsAny<object>())).
                Callback<object>((invokeTaskTrackingLock) => dummyInvokeTaskTrackingLock = invokeTaskTrackingLock);
            mockMonitorService.Setup(m => m.Exit(It.Is<object>(connectingLock => connectingLock == dummyInvokeTaskTrackingLock)));
            Mock<ITaskService> mockTaskService = _mockRepository.Create<ITaskService>();
            mockTaskService.
                Setup(t => t.Run(It.IsAny<Action>())).
                Callback<Action>((action) => dummyKillProcessAction = action);
            Mock<INodeJSProcess> mockNodeJSProcess = _mockRepository.Create<INodeJSProcess>();
            mockNodeJSProcess.Setup(n => n.SafeID).Returns(dummySafeID);
            mockNodeJSProcess.Setup(n => n.Dispose());
            var loggerStringBuilder = new StringBuilder();
            var dummyTrackedInvokeTasks = new ConcurrentDictionary<Task, object?>();
            using var dummyInvokeTaskCreationCountdown = new CountdownEvent(1);
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(monitorService: mockMonitorService.Object,
                taskService: mockTaskService.Object,
                loggerStringBuilder: loggerStringBuilder,
                logLevel: LogLevel.Information);
            mockTestSubject.CallBase = true;
            mockTestSubject.Setup(t => t.InitializeFileWatching()).Returns((true, dummyTrackedInvokeTasks, dummyInvokeTaskCreationCountdown));
            mockTestSubject.
                Setup(t => t.CreateAndSetUpProcess(It.IsAny<EventWaitHandle>())).
                Returns(mockNodeJSProcess.Object).
                Callback<EventWaitHandle>(eventWaitHandle => eventWaitHandle.Set());
            mockTestSubject.Object.ConnectIfNotConnected(); // Creates _nodeJSProcess

            // Act and assert
            mockTestSubject.Object.SwapProcesses();
            Assert.NotNull(dummyKillProcessAction);
            dummyKillProcessAction!();
            _mockRepository.VerifyAll();
            mockTestSubject.Verify(t => t.ConnectIfNotConnected(), Times.Exactly(2)); // Once when arranging
            mockTaskService.VerifyNoOtherCalls();
            Assert.Contains(string.Format(Strings.LogInformation_KillingNodeJSProcess, dummySafeID), loggerStringBuilder.ToString());
            Assert.Equal(1, dummyInvokeTaskCreationCountdown.CurrentCount); // Gets reset
        }

        // Verifies that the "lock and countdown" system drains threads creating tasks while blocking subsequent threads from creating tasks.
        //
        // To step through the lock and countdown system, set breakpoints in TryTrackedInvokeAsync and SwapProcesses and run this test in debug mode.
        // Use VS's parallel windows to keep track of threads.
        [Fact]
        public async void SwapProcesses_TryTrackedInvokeAsync_AreThreadSafe()
        {
            // Arrange
            (bool, int) expectedResult1 = (true, 5);
            (bool, int) expectedResult2 = (false, 10);
            var dummyInvocationRequest = new InvocationRequest(ModuleSourceType.String, "dummyModuleSource");
            var dummyCancellationToken = new CancellationToken();
            var dummyTrackedInvokeTasks = new ConcurrentDictionary<Task, object?>();
            Mock<ITaskService> mockTaskService = _mockRepository.Create<ITaskService>();
            mockTaskService.Setup(t => t.Run(It.IsAny<Action>())); // TODO could run the action to ensure we capture the right task to wait on
            using var dummyInvokeTaskCreationCountdown = new CountdownEvent(1);
            using var dummyEventWaitHandle1 = new EventWaitHandle(false, EventResetMode.ManualReset);
            using var dummyEventWaitHandle2 = new EventWaitHandle(false, EventResetMode.ManualReset);
            using var dummyEventWaitHandle3 = new EventWaitHandle(false, EventResetMode.ManualReset);
            using var dummyEventWaitHandle4 = new EventWaitHandle(false, EventResetMode.ManualReset);
            Mock<IMonitorService> mockMonitorService = _mockRepository.Create<IMonitorService>();
            mockMonitorService.Setup(m => m.Enter(It.IsAny<object>())).Callback<object>((invokeTaskTrackingLock) =>
            {
                Monitor.Enter(invokeTaskTrackingLock);
                dummyEventWaitHandle3.Set();
            });
            mockMonitorService.Setup(m => m.Exit(It.IsAny<object>())).Callback<object>(Monitor.Exit);
            bool firstTryInvokeAsync = true;
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(monitorService: mockMonitorService.Object,
                taskService: mockTaskService.Object);
            mockTestSubject.CallBase = true;
            mockTestSubject.Setup(t => t.InitializeFileWatching()).Returns((true, dummyTrackedInvokeTasks, dummyInvokeTaskCreationCountdown));
            mockTestSubject.Setup(t => t.ConnectIfNotConnected());
            mockTestSubject.
                Protected().
                As<IOutOfProcessNodeJSServiceProtectedMembers>().
                Setup(t => t.TryInvokeAsync<int>(dummyInvocationRequest, dummyCancellationToken)).
                Callback(() =>
                {
                    dummyEventWaitHandle2.Set();
                    dummyEventWaitHandle1.WaitOne();
                }).
                Returns(() =>
                {
                    (bool, int) result = firstTryInvokeAsync ? expectedResult1 : expectedResult2;
                    firstTryInvokeAsync = false;
                    return Task.Run(() =>
                    {
                        dummyEventWaitHandle4.WaitOne();
                        return result;
                    });
                });
            OutOfProcessNodeJSService testSubject = mockTestSubject.Object;

            // Act and assert
            // Simulates a thread creating a task when a file event occurs. Blocked at TryInvokeAsync by dummyEventWaitHandle1.
            var task1 = Task.Run(() => testSubject.TryTrackedInvokeAsync<int>(dummyInvocationRequest, dummyTrackedInvokeTasks, dummyInvokeTaskCreationCountdown, dummyCancellationToken));
            // Simulates a file event thread. Blocked at _invokeTaskCreationCountdown.Wait() in SwapProcesses.
            var task2 = Task.Run(() =>
             {
                 dummyEventWaitHandle2.WaitOne();
                 testSubject.SwapProcesses();
             });
            // Simulates a thread for an invocation that occurs while a file event is processing. Block at _invokeTaskTrackingLock in TryTrackedInvokeAsync.
            var task3 = Task.Run(() =>
            {
                dummyEventWaitHandle3.WaitOne();
                return testSubject.TryTrackedInvokeAsync<int>(dummyInvocationRequest, dummyTrackedInvokeTasks, dummyInvokeTaskCreationCountdown, dummyCancellationToken);
            });
            // Wait for thread 1 to increment countdown count
            dummyEventWaitHandle2.WaitOne();
            // Wait for thread 2 to signal countdown
            while (dummyInvokeTaskCreationCountdown.CurrentCount != 1)
            {
                await Task.Delay(100).ConfigureAwait(false);
            }
            // At this point, all three threads are blocked.
            //
            // Release thread 1, allowing it to "drain" out of task creation block. This should set _invokeTaskCreationCountdown, releasing thread 2, which in turn, should exit
            // _invokeTaskTrackingLock, releasing thread 3.
            dummyEventWaitHandle1.Set();
            // Thread 2 should end first
            await task2.ConfigureAwait(false);
            // Allow threads 1 and 3 to end
            dummyEventWaitHandle4.Set();
            (bool, int) result1 = await task1.ConfigureAwait(false);
            (bool, int) result2 = await task3.ConfigureAwait(false);
            Assert.Equal(expectedResult1, result1);
            Assert.Equal(expectedResult2, result2);
            Assert.Empty(dummyTrackedInvokeTasks);
            Assert.Equal(1, dummyInvokeTaskCreationCountdown.CurrentCount);
            _mockRepository.VerifyAll();
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

        private Mock<OutOfProcessNodeJSService> CreateMockOutOfProcessNodeJSService(INodeJSProcessFactory? nodeProcessFactory = null,
            IOptions<OutOfProcessNodeJSServiceOptions>? optionsAccessor = null,
            IEmbeddedResourcesService? embeddedResourcesService = null,
            IFileWatcherFactory? fileWatcherFactory = null,
            IMonitorService? monitorService = null,
            ITaskService? taskService = null,
            Assembly? serverScriptAssembly = null,
            string? serverScriptName = null,
            bool enableFileWatching = false,
            StringBuilder? loggerStringBuilder = null,

            LogLevel logLevel = LogLevel.Information)
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

            // optionsAccessor.Value is called in the constructor, so we must supply a non null options accessor
            if (optionsAccessor == null)
            {
                Mock<IOptions<OutOfProcessNodeJSServiceOptions>> mockOptionsAccessor = _mockRepository.Create<IOptions<OutOfProcessNodeJSServiceOptions>>();
                mockOptionsAccessor.Setup(o => o.Value).Returns(new OutOfProcessNodeJSServiceOptions() { EnableFileWatching = enableFileWatching });
                optionsAccessor = mockOptionsAccessor.Object;
            }

            return _mockRepository.Create<OutOfProcessNodeJSService>(nodeProcessFactory,
                logger,
                optionsAccessor,
                embeddedResourcesService,
                fileWatcherFactory,
                monitorService,
                taskService,
                serverScriptAssembly,
                serverScriptName);
        }

        private class DummyAssembly : Assembly { }

        public delegate void TryEnterCallback(object obj, ref bool lockTaken);

        public void Dispose()
        {
            if (_serviceProvider != null)
            {
                ((IDisposable)_serviceProvider).Dispose();
            }
        }
    }
}
