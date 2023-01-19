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
            using var dummyCancellationTokenSource = new CancellationTokenSource();
            CancellationToken dummyCancellationToken = dummyCancellationTokenSource.Token;
            var dummyException = new ConnectionException();
            var dummyInvocationRequest = new InvocationRequest(ModuleSourceType.String, "dummyModuleSource");
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService();
            mockTestSubject.CallBase = true;
            mockTestSubject.Setup(t => t.ConnectIfNotConnectedAsync(dummyCancellationToken)).Throws(dummyException);

            // Act and assert
            Assert.ThrowsAsync<ConnectionException>(async () => await mockTestSubject.Object.TryInvokeCoreAsync<int>(dummyInvocationRequest, dummyCancellationToken).ConfigureAwait(false));
            _mockRepository.VerifyAll();
            mockTestSubject.Verify(t => t.ConnectIfNotConnectedAsync(dummyCancellationToken), Times.Exactly(1)); // No retries
        }

        [Fact]
        public void TryInvokeCoreAsync_DoesNotRetryInvocationsThatAreCanceledAndThrowsOperationCanceledException()
        {
            // Arrange
            using var dummyCancellationTokenSource = new CancellationTokenSource();
            CancellationToken dummyCancellationToken = dummyCancellationTokenSource.Token;
            var dummyException = new OperationCanceledException();
            var dummyInvocationRequest = new InvocationRequest(ModuleSourceType.String, "dummyModuleSource");
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService();
            mockTestSubject.CallBase = true;
            mockTestSubject.Setup(t => t.ConnectIfNotConnectedAsync(dummyCancellationToken)).Returns(new ValueTask());
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
            const int dummyInvocationTimeoutMS = 100;
            const int dummyNumRetries = 2;
            const int dummyNumProcessRetries = 2;
            var dummyOptions = new OutOfProcessNodeJSServiceOptions { InvocationTimeoutMS = dummyInvocationTimeoutMS, NumRetries = dummyNumRetries, NumProcessRetries = dummyNumProcessRetries };
            Mock<IOptions<OutOfProcessNodeJSServiceOptions>> mockOptionsAccessor = _mockRepository.Create<IOptions<OutOfProcessNodeJSServiceOptions>>();
            mockOptionsAccessor.Setup(o => o.Value).Returns(dummyOptions);
            var loggerStringBuilder = new StringBuilder();
            var dummyInvocationRequest = new InvocationRequest(ModuleSourceType.String, "dummyModuleSource");
            var dummyCancellationToken = new CancellationToken();
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(optionsAccessor: mockOptionsAccessor.Object,
                loggerStringBuilder: loggerStringBuilder);
            mockTestSubject.CallBase = true;
            mockTestSubject.Setup(t => t.ConnectIfNotConnectedAsync(dummyCancellationToken)).Returns(new ValueTask());
            mockTestSubject.
                Protected().
                As<IOutOfProcessNodeJSServiceProtectedMembers>().
                Setup(t => t.TryInvokeAsync<int>(dummyInvocationRequest, dummyCancellationToken)).
                ThrowsAsync(dummyException);
            mockTestSubject.Setup(t => t.CreateCancellationToken(dummyCancellationToken)).Returns((dummyCancellationToken, null));
            mockTestSubject.Setup(t => t.MoveToNewProcessAsync(false)).Returns(new ValueTask());

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
                    dummyInvocationTimeoutMS,
                    nameof(OutOfProcessNodeJSServiceOptions),
                    nameof(OutOfProcessNodeJSServiceOptions.InvocationTimeoutMS)),
                result.Message);
            mockTestSubject.Verify(t => t.MoveToNewProcessAsync(false), times: Times.Exactly(2));
        }

        [Fact]
        public async void TryInvokeCoreAsync_WithRetriesAndNoProcessRetries_RetriesOnlyOnCurrentProcess()
        {
            // Arrange
            var dummyException = new OperationCanceledException();
            const int dummyInvocationTimeoutMS = 100;
            const int dummyNumRetries = 2;
            const int dummyNumProcessRetries = 0;
            var dummyOptions = new OutOfProcessNodeJSServiceOptions
            {
                InvocationTimeoutMS = dummyInvocationTimeoutMS,
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
            mockTestSubject.Setup(t => t.ConnectIfNotConnectedAsync(dummyCancellationToken)).Returns(new ValueTask());
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
            Assert.Equal(dummyNumRetries, Regex.Matches(resultLog, Strings.LogWarning_InvocationAttemptFailed.Substring(0, 30)).Count); // Logs after each retry
            Assert.Empty(Regex.Matches(resultLog, Strings.LogWarning_RetriesInExistingProcessExhausted.Substring(0, 30))); // Logs before each process swap
            // Verify calls
            mockTestSubject.
                Protected().
                As<IOutOfProcessNodeJSServiceProtectedMembers>().
                Verify(t => t.TryInvokeAsync<int>(dummyInvocationRequest, dummyCancellationToken), Times.Exactly(dummyNumRetries + 1));
            Assert.Equal(string.Format(Strings.InvocationException_OutOfProcessNodeJSService_InvocationTimedOut,
                    dummyInvocationTimeoutMS,
                    nameof(OutOfProcessNodeJSServiceOptions),
                    nameof(OutOfProcessNodeJSServiceOptions.InvocationTimeoutMS)),
                result.Message);
            mockTestSubject.Verify(t => t.MoveToNewProcessAsync(false), times: Times.Exactly(0));
        }

        [Fact]
        public async void TryInvokeCoreAsync_WithProcessRetriesAndNoRetries_RetriesOnlyOnNewProcess()
        {
            // Arrange
            var dummyException = new OperationCanceledException();
            const int dummyInvocationTimeoutMS = 100;
            const int dummyNumRetries = 0;
            const int dummyNumProcessRetries = 2;
            var dummyOptions = new OutOfProcessNodeJSServiceOptions
            {
                InvocationTimeoutMS = dummyInvocationTimeoutMS,
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
            mockTestSubject.Setup(t => t.ConnectIfNotConnectedAsync(dummyCancellationToken)).Returns(new ValueTask());
            mockTestSubject.
                Protected().
                As<IOutOfProcessNodeJSServiceProtectedMembers>().
                Setup(t => t.TryInvokeAsync<int>(dummyInvocationRequest, dummyCancellationToken)).
                ThrowsAsync(dummyException);
            mockTestSubject.Setup(t => t.CreateCancellationToken(dummyCancellationToken)).Returns((dummyCancellationToken, null));
            mockTestSubject.Setup(t => t.MoveToNewProcessAsync(false)).Returns(new ValueTask());

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
                    dummyInvocationTimeoutMS,
                    nameof(OutOfProcessNodeJSServiceOptions),
                    nameof(OutOfProcessNodeJSServiceOptions.InvocationTimeoutMS)),
                result.Message);
            mockTestSubject.Verify(t => t.MoveToNewProcessAsync(false), times: Times.Exactly(dummyNumProcessRetries));
        }

        [Fact]
        public async void TryInvokeCoreAsync_IfInvocationThrowsExceptionsOtherThanInvocationExceptionRetriesInTheSameProcessAndInANewProcessAndThrowsExceptionWhenNoRetriesRemain()
        {
            // Arrange
            var dummyException = new HttpRequestException(); // Process retries are ignored if exception is an InvocationException (cause by JS)
            const int dummyInvocationTimeoutMS = 100;
            const int dummyNumRetries = 2;
            const int dummyNumProcessRetries = 2;
            var dummyOptions = new OutOfProcessNodeJSServiceOptions { InvocationTimeoutMS = dummyInvocationTimeoutMS, NumRetries = dummyNumRetries, NumProcessRetries = dummyNumProcessRetries };
            Mock<IOptions<OutOfProcessNodeJSServiceOptions>> mockOptionsAccessor = _mockRepository.Create<IOptions<OutOfProcessNodeJSServiceOptions>>();
            mockOptionsAccessor.Setup(o => o.Value).Returns(dummyOptions);
            var loggerStringBuilder = new StringBuilder();
            var dummyInvocationRequest = new InvocationRequest(ModuleSourceType.String, "dummyModuleSource");
            var dummyCancellationToken = new CancellationToken();
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(optionsAccessor: mockOptionsAccessor.Object,
                loggerStringBuilder: loggerStringBuilder);
            mockTestSubject.CallBase = true;
            mockTestSubject.Setup(t => t.ConnectIfNotConnectedAsync(dummyCancellationToken)).Returns(new ValueTask());
            mockTestSubject.
                Protected().
                As<IOutOfProcessNodeJSServiceProtectedMembers>().
                Setup(t => t.TryInvokeAsync<int>(dummyInvocationRequest, dummyCancellationToken)).
                ThrowsAsync(dummyException);
            mockTestSubject.Setup(t => t.CreateCancellationToken(dummyCancellationToken)).Returns((dummyCancellationToken, null));
            mockTestSubject.Setup(t => t.MoveToNewProcessAsync(false)).Returns(new ValueTask());

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
            mockTestSubject.Verify(t => t.MoveToNewProcessAsync(false), times: Times.Exactly(2));
            Assert.Same(dummyException, result);
        }

        [Fact]
        public async void TryInvokeCoreAsync_IfProcessRetriesAreDisabledForJSErrorsAndInvocationThrowsInvocationExceptionsRetriesInTheSameProcessAndThrowsExceptionWhenNoRetriesRemain()
        {
            // Arrange
            var dummyException = new InvocationException(); // Process retries are ignored if exception is an InvocationException (cause by JS)
            const int dummyInvocationTimeoutMS = 100;
            const int dummyNumRetries = 2;
            const int dummyNumProcessRetries = 2; // Ignored
            var dummyOptions = new OutOfProcessNodeJSServiceOptions
            {
                InvocationTimeoutMS = dummyInvocationTimeoutMS,
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
            mockTestSubject.Setup(t => t.ConnectIfNotConnectedAsync(dummyCancellationToken)).Returns(new ValueTask());
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
            const int dummyInvocationTimeoutMS = 100;
            const int dummyNumRetries = 2;
            const int dummyNumProcessRetries = 2;
            var dummyOptions = new OutOfProcessNodeJSServiceOptions
            {
                InvocationTimeoutMS = dummyInvocationTimeoutMS,
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
            mockTestSubject.Setup(t => t.ConnectIfNotConnectedAsync(dummyCancellationToken)).Returns(new ValueTask());
            mockTestSubject.
                Protected().
                As<IOutOfProcessNodeJSServiceProtectedMembers>().
                Setup(t => t.TryInvokeAsync<int>(dummyInvocationRequest, dummyCancellationToken)).
                ThrowsAsync(dummyException);
            mockTestSubject.Setup(t => t.CreateCancellationToken(dummyCancellationToken)).Returns((dummyCancellationToken, null));
            mockTestSubject.Setup(t => t.MoveToNewProcessAsync(false)).Returns(new ValueTask());

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
            mockTestSubject.Verify(t => t.MoveToNewProcessAsync(false), times: Times.Exactly(2));
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
            mockTestSubject.Setup(t => t.ConnectIfNotConnectedAsync(dummyCancellationToken)).Returns(new ValueTask());
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
            mockTestSubject.Setup(t => t.ConnectIfNotConnectedAsync(dummyCancellationToken)).Returns(new ValueTask());
            mockTestSubject.
                Protected().
                As<IOutOfProcessNodeJSServiceProtectedMembers>().
                Setup(t => t.TryInvokeAsync<int>(dummyInvocationRequest, dummyCancellationToken)).
                ThrowsAsync(dummyException);
            mockTestSubject.Setup(t => t.CreateCancellationToken(dummyCancellationToken)).Returns((dummyCancellationToken, null));
            mockTestSubject.Setup(t => t.MoveToNewProcessAsync(false)).Returns(new ValueTask());

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
            mockTestSubject.Setup(t => t.ConnectIfNotConnectedAsync(dummyCancellationToken)).Returns(new ValueTask());
            mockTestSubject.
                Protected().
                As<IOutOfProcessNodeJSServiceProtectedMembers>().
                Setup(t => t.TryInvokeAsync<int>(dummyInvocationRequest, dummyCancellationToken)).
                ThrowsAsync(dummyException);
            mockTestSubject.Setup(t => t.CreateCancellationToken(dummyCancellationToken)).Returns((dummyCancellationToken, null));
            mockTestSubject.Setup(t => t.MoveToNewProcessAsync(false)).Returns(new ValueTask());

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
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(taskService: _mockRepository.Create<ITaskService>().Object,
                blockDrainerService: _mockRepository.Create<IBlockDrainerService>().Object);
            mockTestSubject.CallBase = true;
            mockTestSubject.Setup(t => t.InitializeFileWatching()).Returns((true, dummyTrackedInvokeTasks)); // track invoke tasks
            mockTestSubject.Setup(t => t.ConnectIfNotConnectedAsync(dummyCancellationToken)).Returns(new ValueTask());
            mockTestSubject.Setup(t => t.CreateCancellationToken(dummyCancellationToken)).Returns((dummyCancellationToken, null));
            mockTestSubject.
                Setup(t => t.TryTrackedInvokeAsync<int>(dummyInvocationRequest, dummyTrackedInvokeTasks, dummyCancellationToken)).
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
            var dummyOptions = new OutOfProcessNodeJSServiceOptions { InvocationTimeoutMS = -1 };
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
        public void ConnectIfNotConnectedAsync_IfNodeJSProcessIsNullFirstThreadStopsFileWatcherAndConnectsToANewNodeJSProcess()
        {
            // Arrange
            var dummyOptions = new OutOfProcessNodeJSServiceOptions() { EnableFileWatching = true };
            Mock<IOptions<OutOfProcessNodeJSServiceOptions>> mockOptionsAccessor = _mockRepository.Create<IOptions<OutOfProcessNodeJSServiceOptions>>();
            mockOptionsAccessor.Setup(o => o.Value).Returns(dummyOptions);
            // So _fileWatcher is set
            Mock<IFileWatcher> mockFileWatcher = _mockRepository.Create<IFileWatcher>();
            Mock<IFileWatcherFactory> mockFileWatcherFactory = _mockRepository.Create<IFileWatcherFactory>();
            mockFileWatcherFactory.
                Setup(f => f.Create(dummyOptions.WatchPath, dummyOptions.WatchSubdirectories, dummyOptions.WatchFileNamePatterns, It.IsAny<FileChangedEventHandler>())).
                Returns(mockFileWatcher.Object);
            Mock<INodeJSProcess> mockNodeJSProcess = _mockRepository.Create<INodeJSProcess>();
            mockNodeJSProcess.Setup(n => n.Connected).Returns(true);
            Mock<INodeJSProcessFactory> mockNodeJSProcessFactory = _mockRepository.Create<INodeJSProcessFactory>();
            mockNodeJSProcessFactory.Setup(n => n.Create(It.IsAny<string>(), It.IsAny<EventHandler>())).Returns(mockNodeJSProcess.Object);
            Mock<IEmbeddedResourcesService> mockEmbeddedResourcesService = _mockRepository.Create<IEmbeddedResourcesService>();
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(nodeProcessFactory: mockNodeJSProcessFactory.Object,
                embeddedResourcesService: mockEmbeddedResourcesService.Object,
                optionsAccessor: mockOptionsAccessor.Object,
                fileWatcherFactory: mockFileWatcherFactory.Object);
            mockTestSubject.CallBase = true;
            mockTestSubject.
                Setup(t => t.CreateNewProcessAndConnectAsync()).
                Callback(() => mockTestSubject.Object.CreateAndSetUpProcess(null!)).
                Returns(Task.CompletedTask);
            OutOfProcessNodeJSService testSubject = mockTestSubject.Object;

            // Act
            const int numThreads = 5;
            var threads = new List<Thread>();
            for (int i = 0; i < numThreads; i++)
            {
                var thread = new Thread(async () => await testSubject.ConnectIfNotConnectedAsync(CancellationToken.None).ConfigureAwait(false));
                threads.Add(thread);
                thread.Start();
            }
            foreach (Thread thread in threads)
            {
                thread.Join();
            }

            // Assert
            _mockRepository.VerifyAll();
            mockTestSubject.Verify(t => t.CreateNewProcessAndConnectAsync(), Times.Once); // Only creates and connects once
            mockFileWatcher.Verify(f => f.Stop(), Times.Once);
        }

        [Fact(Timeout = TIMEOUT_MS)]
        public void ConnectIfNotConnectedAsync_IfNodeJSProcessIsNotConnectedFirstThreadStopsFileWatcherAndConnectsToANewNodeJSProcess()
        {
            // Arrange
            var dummyOptions = new OutOfProcessNodeJSServiceOptions() { EnableFileWatching = true };
            Mock<IOptions<OutOfProcessNodeJSServiceOptions>> mockOptionsAccessor = _mockRepository.Create<IOptions<OutOfProcessNodeJSServiceOptions>>();
            mockOptionsAccessor.Setup(o => o.Value).Returns(dummyOptions);
            // So _fileWatcher is set
            Mock<IFileWatcher> mockFileWatcher = _mockRepository.Create<IFileWatcher>();
            Mock<IFileWatcherFactory> mockFileWatcherFactory = _mockRepository.Create<IFileWatcherFactory>();
            mockFileWatcherFactory.
                Setup(f => f.Create(dummyOptions.WatchPath, dummyOptions.WatchSubdirectories, dummyOptions.WatchFileNamePatterns, It.IsAny<FileChangedEventHandler>())).
                Returns(mockFileWatcher.Object);
            bool dummyIsConnected = false;
            Mock<INodeJSProcess> mockNodeJSProcess = _mockRepository.Create<INodeJSProcess>();
            mockNodeJSProcess.Setup(n => n.Connected).Returns(() => dummyIsConnected);
            Mock<INodeJSProcessFactory> mockNodeJSProcessFactory = _mockRepository.Create<INodeJSProcessFactory>();
            mockNodeJSProcessFactory.Setup(n => n.Create(It.IsAny<string>(), It.IsAny<EventHandler>())).Returns(mockNodeJSProcess.Object);
            Mock<IEmbeddedResourcesService> mockEmbeddedResourcesService = _mockRepository.Create<IEmbeddedResourcesService>();
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(nodeProcessFactory: mockNodeJSProcessFactory.Object,
                embeddedResourcesService: mockEmbeddedResourcesService.Object,
                optionsAccessor: mockOptionsAccessor.Object,
                fileWatcherFactory: mockFileWatcherFactory.Object);
            mockTestSubject.CallBase = true;
            mockTestSubject.Setup(t => t.CreateNewProcessAndConnectAsync()).Callback(() => dummyIsConnected = true).Returns(Task.CompletedTask);
            mockTestSubject.Object.CreateAndSetUpProcess(null!);
            OutOfProcessNodeJSService testSubject = mockTestSubject.Object;

            // Act
            const int numThreads = 5;
            var threads = new List<Thread>();
            for (int i = 0; i < numThreads; i++)
            {
                var thread = new Thread(async () => await testSubject.ConnectIfNotConnectedAsync(CancellationToken.None).ConfigureAwait(false));
                threads.Add(thread);
                thread.Start();
            }
            foreach (Thread thread in threads)
            {
                thread.Join();
            }

            // Assert
            _mockRepository.VerifyAll();
            mockTestSubject.Verify(t => t.CreateNewProcessAndConnectAsync(), Times.Once); // Only creates and connects once
            mockFileWatcher.Verify(f => f.Stop(), Times.Once);
        }

        [Fact]
        public async void CreateNewProcessAndConnectAsync_CreatesNewProcessAndConnectsToIt()
        {
            // Arrange
            const int dummyProcessID = 12345;
            const int dummyConnectionTimeoutMS = 100; // Arbitrary, we release the semaphoreSlim before we wait on it
            Mock<IEmbeddedResourcesService> mockEmbeddedResourcesService = _mockRepository.Create<IEmbeddedResourcesService>();
            Mock<INodeJSProcess> mockInitialNodeJSProcess = _mockRepository.Create<INodeJSProcess>();
#if NET5_0 || NET6_0 || NET7_0
            mockInitialNodeJSProcess.Setup(n => n.DisposeAsync());
#else
            mockInitialNodeJSProcess.Setup(n => n.Dispose());
#endif
            Mock<INodeJSProcessFactory> mockNodeJSProcessFactory = _mockRepository.Create<INodeJSProcessFactory>();
            mockNodeJSProcessFactory.Setup(n => n.Create(It.IsAny<string>(), It.IsAny<EventHandler>())).Returns(mockInitialNodeJSProcess.Object);
            Mock<INodeJSProcess> mockNewNodeJSProcess = _mockRepository.Create<INodeJSProcess>();
            mockNewNodeJSProcess.Setup(n => n.SafeID).Returns(dummyProcessID);
            mockNewNodeJSProcess.Setup(n => n.HasExited).Returns(false);
            mockNewNodeJSProcess.Setup(n => n.SetConnected());
            var dummyOptions = new OutOfProcessNodeJSServiceOptions { ConnectionTimeoutMS = dummyConnectionTimeoutMS, EnableFileWatching = true };
            Mock<IOptions<OutOfProcessNodeJSServiceOptions>> mockOptionsAccessor = _mockRepository.Create<IOptions<OutOfProcessNodeJSServiceOptions>>();
            mockOptionsAccessor.Setup(o => o.Value).Returns(dummyOptions);
            // So _fileWatcher is set
            Mock<IFileWatcher> mockFileWatcher = _mockRepository.Create<IFileWatcher>();
            mockFileWatcher.Setup(f => f.Start());
            Mock<IFileWatcherFactory> mockFileWatcherFactory = _mockRepository.Create<IFileWatcherFactory>();
            mockFileWatcherFactory.
                Setup(f => f.Create(dummyOptions.WatchPath, dummyOptions.WatchSubdirectories, dummyOptions.WatchFileNamePatterns, It.IsAny<FileChangedEventHandler>())).
                Returns(mockFileWatcher.Object);
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(optionsAccessor: mockOptionsAccessor.Object,
                fileWatcherFactory: mockFileWatcherFactory.Object,
                nodeProcessFactory: mockNodeJSProcessFactory.Object,
                embeddedResourcesService: mockEmbeddedResourcesService.Object);
            mockTestSubject.CallBase = true;
            mockTestSubject.
                Setup(t => t.CreateAndSetUpProcess(It.IsNotNull<DisposeTrackingSemaphoreSlim>())).
                Callback((DisposeTrackingSemaphoreSlim semaphoreSlim) => semaphoreSlim.Release()).
                Returns(mockNewNodeJSProcess.Object);
            OutOfProcessNodeJSService testSubject = mockTestSubject.Object;
            testSubject.CreateAndSetUpProcess(null!); // Create initial process

            // Act
            await testSubject.CreateNewProcessAndConnectAsync().ConfigureAwait(false);

            // Assert
            _mockRepository.VerifyAll();
        }

        [Fact]
        public async void CreateNewProcessAndConnectAsync_RetriesIfNodeJSProcessExitsBeforeConnectingAndThrowsConnectionExceptionIfNoRetriesRemain()
        {
            // Arrange
            const int dummyProcessID = 12345;
            const int dummyConnectionTimeoutMS = 100; // Arbitrary, we release the semaphoreSlim before we wait on it
            const int dummyNumConnectionRetries = 2;
            Mock<INodeJSProcess> mockNewNodeJSProcess = _mockRepository.Create<INodeJSProcess>();
            mockNewNodeJSProcess.Setup(n => n.SafeID).Returns(dummyProcessID);
            mockNewNodeJSProcess.Setup(n => n.HasExited).Returns(true);
#if NET5_0 || NET6_0 || NET7_0
            mockNewNodeJSProcess.Setup(n => n.DisposeAsync());
#else
            mockNewNodeJSProcess.Setup(n => n.Dispose());
#endif
            var dummyOptions = new OutOfProcessNodeJSServiceOptions { ConnectionTimeoutMS = dummyConnectionTimeoutMS, NumConnectionRetries = dummyNumConnectionRetries };
            Mock<IOptions<OutOfProcessNodeJSServiceOptions>> mockOptionsAccessor = _mockRepository.Create<IOptions<OutOfProcessNodeJSServiceOptions>>();
            mockOptionsAccessor.Setup(o => o.Value).Returns(dummyOptions);
            var loggerStringBuilder = new StringBuilder();
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(optionsAccessor: mockOptionsAccessor.Object,
                loggerStringBuilder: loggerStringBuilder);
            mockTestSubject.CallBase = true;
            mockTestSubject.
                Setup(t => t.CreateAndSetUpProcess(It.IsAny<DisposeTrackingSemaphoreSlim>())).
                Callback((DisposeTrackingSemaphoreSlim semaphoreSlim) => semaphoreSlim.Release()).
                Returns(mockNewNodeJSProcess.Object);
            OutOfProcessNodeJSService testSubject = mockTestSubject.Object;

            // Act and assert
            ConnectionException resultException = await Assert.ThrowsAsync<ConnectionException>(() => testSubject.CreateNewProcessAndConnectAsync()).ConfigureAwait(false);
            string expectedResultExceptionMessage = string.Format(Strings.ConnectionException_OutOfProcessNodeJSService_ProcessExitedBeforeConnecting, dummyProcessID);
            Assert.Equal(expectedResultExceptionMessage, resultException.Message);
            string logResult = loggerStringBuilder.ToString();
            for (int i = dummyNumConnectionRetries; i > 0; i--)
            {
                // Verify that attempts are failing because process exited before connecting
                Assert.Contains(string.Format(Strings.LogWarning_ConnectionAttemptFailed, dummyNumConnectionRetries, new ConnectionException(expectedResultExceptionMessage)), logResult);
            }
            _mockRepository.VerifyAll();
#if NET5_0 || NET6_0 || NET7_0
            mockNewNodeJSProcess.Verify(n => n.DisposeAsync(), Times.Exactly(dummyNumConnectionRetries * 2 + 1)); // Dispose called after failure, also called when retrying (we don't retry after the last failure)
#else
            mockNewNodeJSProcess.Verify(n => n.Dispose(), Times.Exactly(dummyNumConnectionRetries * 2 + 1)); // Dispose called after failure, also called when retrying (we don't retry after the last failure)
#endif
            mockTestSubject.Verify(t => t.CreateAndSetUpProcess(It.IsNotNull<DisposeTrackingSemaphoreSlim>()), Times.Exactly(dummyNumConnectionRetries + 1)); // Once for first try, once for each retry
        }

        [Fact(Timeout = TIMEOUT_MS)]
        public async void CreateNewProcessAndConnectAsync_RetriesIfConnectionAttemptTimesoutAndThrowsConnectionExceptionIfNoRetriesRemain()
        {
            // Arrange
            const int dummyProcessID = 12345;
            const int dummyConnectionTimeoutMS = 10; // Short duration so timeout occurs quickly
            const int dummyNumConnectionRetries = 2;
            const string dummyExitStatus = "dummyExitStatus";
            Mock<INodeJSProcess> mockNewNodeJSProcess = _mockRepository.Create<INodeJSProcess>();
            mockNewNodeJSProcess.Setup(n => n.SafeID).Returns(dummyProcessID);
            mockNewNodeJSProcess.Setup(n => n.HasExited).Returns(false);
            mockNewNodeJSProcess.Setup(n => n.ExitStatus).Returns(dummyExitStatus);
#if NET5_0 || NET6_0 || NET7_0
            mockNewNodeJSProcess.Setup(n => n.DisposeAsync());
#else
            mockNewNodeJSProcess.Setup(n => n.Dispose());
#endif
            var dummyOptions = new OutOfProcessNodeJSServiceOptions { ConnectionTimeoutMS = dummyConnectionTimeoutMS, NumConnectionRetries = dummyNumConnectionRetries };
            Mock<IOptions<OutOfProcessNodeJSServiceOptions>> mockOptionsAccessor = _mockRepository.Create<IOptions<OutOfProcessNodeJSServiceOptions>>();
            mockOptionsAccessor.Setup(o => o.Value).Returns(dummyOptions);
            var loggerStringBuilder = new StringBuilder();
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(optionsAccessor: mockOptionsAccessor.Object,
                loggerStringBuilder: loggerStringBuilder);
            mockTestSubject.CallBase = true;
            mockTestSubject.Setup(t => t.CreateAndSetUpProcess(It.IsAny<DisposeTrackingSemaphoreSlim>())).Returns(mockNewNodeJSProcess.Object);
            OutOfProcessNodeJSService testSubject = mockTestSubject.Object;

            // Act and assert
            ConnectionException resultException = await Assert.ThrowsAsync<ConnectionException>(() => testSubject.CreateNewProcessAndConnectAsync()).ConfigureAwait(false);
            string expectedResultExceptionMessage = string.Format(Strings.ConnectionException_OutOfProcessNodeJSService_ConnectionAttemptTimedOut, dummyConnectionTimeoutMS, dummyProcessID, false,
                dummyExitStatus);
            Assert.Equal(expectedResultExceptionMessage, resultException.Message);
            string logResult = loggerStringBuilder.ToString();
            for (int i = dummyNumConnectionRetries; i > 0; i--)
            {
                // Verify that attempts are failing because attempts timed out
                Assert.Contains(string.Format(Strings.LogWarning_ConnectionAttemptFailed, dummyNumConnectionRetries, new ConnectionException(expectedResultExceptionMessage)), logResult);
            }
            _mockRepository.VerifyAll();
#if NET5_0 || NET6_0 || NET7_0
            mockNewNodeJSProcess.Verify(n => n.DisposeAsync(), Times.Exactly(dummyNumConnectionRetries * 2 + 1)); // Dispose called after failure, also called when retrying (we don't retry after the last failure)
#else
            mockNewNodeJSProcess.Verify(n => n.Dispose(), Times.Exactly(dummyNumConnectionRetries * 2 + 1)); // Dispose called after failure, also called when retrying (we don't retry after the last failure)
#endif
            mockTestSubject.Verify(t => t.CreateAndSetUpProcess(It.IsNotNull<DisposeTrackingSemaphoreSlim>()), Times.Exactly(dummyNumConnectionRetries + 1)); // Once for first try, once for each retry
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
            (bool resultTrackInvokeTasks, ConcurrentDictionary<Task, object?> resultTrackedInvokeTasks) = mockTestSubject.Object.InitializeFileWatching();

            // Assert
            Assert.False(resultTrackInvokeTasks);
            Assert.Null(resultTrackedInvokeTasks);
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
                taskService: _mockRepository.Create<ITaskService>().Object);
            mockTestSubject.CallBase = true;

            // Act
            (bool resultTrackInvokeTasks, ConcurrentDictionary<Task, object?> resultTrackedInvokeTasks) = mockTestSubject.Object.InitializeFileWatching();

            // Assert
            Assert.False(resultTrackInvokeTasks);
            Assert.Null(resultTrackedInvokeTasks);
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
                taskService: _mockRepository.Create<ITaskService>().Object);
            mockTestSubject.CallBase = true;

            // Act
            (bool resultTrackInvokeTasks, ConcurrentDictionary<Task, object?> resultTrackedInvokeTasks) = mockTestSubject.Object.InitializeFileWatching();

            // Assert
            Assert.True(resultTrackInvokeTasks);
            Assert.NotNull(resultTrackedInvokeTasks);
        }

        [Fact]
        public async void TryTrackedInvokeAsync_TriesTrackedInvoke()
        {
            // Arrange
            Mock<IBlockDrainerService> mockBlockDrainerService = _mockRepository.Create<IBlockDrainerService>();
            mockBlockDrainerService.Setup(b => b.EnterBlockAsync()).Returns(Task.CompletedTask);
            mockBlockDrainerService.Setup(b => b.ExitBlock());
            var dummyInvocationRequest = new InvocationRequest(ModuleSourceType.String, "dummyModuleSource");
            var dummyTrackedInvokeTasks = new ConcurrentDictionary<Task, object?>();
            (bool, int) expectedResult = (true, 4);
            using var dummySemaphoreSlim = new SemaphoreSlim(0, 1);
            var dummyCancellationToken = new CancellationToken();
            var dummyInvokeTask = Task.Run(async () =>
            {
                await dummySemaphoreSlim.WaitAsync().ConfigureAwait(false);
                return expectedResult;
            });
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(blockDrainerService: mockBlockDrainerService.Object);
            mockTestSubject.CallBase = true;
            mockTestSubject.
                Protected().
                As<IOutOfProcessNodeJSServiceProtectedMembers>().
                Setup(t => t.TryInvokeAsync<int>(dummyInvocationRequest, dummyCancellationToken)).
                Returns(dummyInvokeTask);

            // Act and assert
            Task<(bool, int)> resultTask = mockTestSubject.Object.TryTrackedInvokeAsync<int>(dummyInvocationRequest, dummyTrackedInvokeTasks, dummyCancellationToken);
            Assert.Single(dummyTrackedInvokeTasks); // While task hasn't completed, it should be tracked
            Assert.True(dummyTrackedInvokeTasks.ContainsKey(dummyInvokeTask)); // While task hasn't completed, it should be tracked
            dummySemaphoreSlim.Release(); // Allow task to complete
            (bool, int) result = await resultTask.ConfigureAwait(false);
            Assert.Empty(dummyTrackedInvokeTasks); // No longer tracked after completion
            Assert.Equal(expectedResult, result);
            _mockRepository.VerifyAll();
        }

        [Fact]
        public void FileChangedHandler_LogsChangedFileStopsFileWatchingAndMovesToNewProcess()
        {
            // Arrange
            const string dummyPath = "dummyPath";
            var loggerStringBuilder = new StringBuilder();
            var dummyOptions = new OutOfProcessNodeJSServiceOptions { EnableFileWatching = true };
            Mock<IOptions<OutOfProcessNodeJSServiceOptions>> mockOptionsAccessor = _mockRepository.Create<IOptions<OutOfProcessNodeJSServiceOptions>>();
            mockOptionsAccessor.Setup(o => o.Value).Returns(dummyOptions);
            // So _fileWatcher is set
            Mock<IFileWatcher> mockFileWatcher = _mockRepository.Create<IFileWatcher>();
            mockFileWatcher.Setup(f => f.Stop());
            Mock<IFileWatcherFactory> mockFileWatcherFactory = _mockRepository.Create<IFileWatcherFactory>();
            mockFileWatcherFactory.
                Setup(f => f.Create(dummyOptions.WatchPath, dummyOptions.WatchSubdirectories, dummyOptions.WatchFileNamePatterns, It.IsAny<FileChangedEventHandler>())).
                Returns(mockFileWatcher.Object);
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(optionsAccessor: mockOptionsAccessor.Object,
                fileWatcherFactory: mockFileWatcherFactory.Object,
                loggerStringBuilder: loggerStringBuilder,
                logLevel: LogLevel.Information);
            mockTestSubject.CallBase = true;
            mockTestSubject.Setup(t => t.MoveToNewProcessAsync(true));

            // Act
            mockTestSubject.Object.FileChangedHandler(dummyPath);

            // Assert
            Assert.Contains(string.Format(Strings.LogInformation_FileChangedMovingtoNewNodeJSProcess, dummyPath), loggerStringBuilder.ToString());
            _mockRepository.VerifyAll();
        }

        [Fact]
        public async void MoveToNewProcessAsync_DoesNothingIfAlreadyConnectingAndNewNodeJSProcessAlreadyCreated()
        {
            // Arrange
            Mock<IEmbeddedResourcesService> mockEmbeddedResourcesService = _mockRepository.Create<IEmbeddedResourcesService>();
            Mock<INodeJSProcess> mockNewNodeJSProcess = _mockRepository.Create<INodeJSProcess>();
            mockNewNodeJSProcess.Setup(n => n.Connected).Returns(false);
            Mock<INodeJSProcessFactory> mockNodeJSProcessFactory = _mockRepository.Create<INodeJSProcessFactory>();
            mockNodeJSProcessFactory.Setup(n => n.Create(It.IsAny<string>(), It.IsAny<EventHandler>())).Returns(mockNewNodeJSProcess.Object);
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(nodeProcessFactory: mockNodeJSProcessFactory.Object, 
                embeddedResourcesService: mockEmbeddedResourcesService.Object);
            mockTestSubject.CallBase = true;
            mockTestSubject.Setup(t => t.CreateNewProcessAndConnectAsync()).Returns(new Task(() => { })); // Unstarted task (never completes)
            OutOfProcessNodeJSService testSubject = mockTestSubject.Object;
#pragma warning disable CS4014
            testSubject.MoveToNewProcessAsync(true); // Take connecting lock - SemaphoreSlim.WaitAsync completes synchronously when count > 0, so we do not need to await this
#pragma warning restore CS4014
            testSubject.CreateAndSetUpProcess(null!); // Create new process

            // Act
            await testSubject.MoveToNewProcessAsync(true).ConfigureAwait(false); // Should complete synchronously despite lock being taken

            // Assert
            _mockRepository.VerifyAll();
        }

        [Fact]
        public async void MoveToNewProcessAsync_DoesNothingIfAlreadyConnectingButNewNodeJSProcessNotCreatedYet()
        {
            // Arrange
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService();
            mockTestSubject.CallBase = true;
            mockTestSubject.Setup(t => t.CreateNewProcessAndConnectAsync()).Returns(new Task(() => { })); // Unstarted task (never completes)
            OutOfProcessNodeJSService testSubject = mockTestSubject.Object;
#pragma warning disable CS4014
            testSubject.MoveToNewProcessAsync(true); // Take connecting lock - SemaphoreSlim.WaitAsync completes synchronously when count > 0, so we do not need to await this
#pragma warning restore CS4014

            // Act
            await testSubject.MoveToNewProcessAsync(true).ConfigureAwait(false); // Should complete synchronously despite lock being taken

            // Assert
            _mockRepository.VerifyAll();
        }

        [Fact]
        public async void MoveToNewProcessAsync_DoesNothingIfJustConnectedAndReconnectIfJustConnectedIsFalse()
        {
            // Arrange
            Mock<IEmbeddedResourcesService> mockEmbeddedResourcesService = _mockRepository.Create<IEmbeddedResourcesService>();
            Mock<INodeJSProcess> mockNewNodeJSProcess = _mockRepository.Create<INodeJSProcess>();
            mockNewNodeJSProcess.Setup(n => n.Connected).Returns(true); // Just connected
            Mock<INodeJSProcessFactory> mockNodeJSProcessFactory = _mockRepository.Create<INodeJSProcessFactory>();
            mockNodeJSProcessFactory.Setup(n => n.Create(It.IsAny<string>(), It.IsAny<EventHandler>())).Returns(mockNewNodeJSProcess.Object);
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(nodeProcessFactory: mockNodeJSProcessFactory.Object,
                embeddedResourcesService: mockEmbeddedResourcesService.Object);
            mockTestSubject.CallBase = true;
            mockTestSubject.Setup(t => t.CreateNewProcessAndConnectAsync()).Returns(new Task(() => { })); // Unstarted task (never completes)
            OutOfProcessNodeJSService testSubject = mockTestSubject.Object;
#pragma warning disable CS4014
            testSubject.MoveToNewProcessAsync(true); // Take connecting lock - SemaphoreSlim.WaitAsync completes synchronously when count > 0, so we do not need to await this
#pragma warning restore CS4014
            testSubject.CreateAndSetUpProcess(null!); // Create new process

            // Act
            await testSubject.MoveToNewProcessAsync(false).ConfigureAwait(false); // Should complete synchronously despite lock being taken

            // Assert
            _mockRepository.VerifyAll();
        }

        [Fact]
        public async void MoveToNewProcessAsync_MovesToNewProcess()
        {
            // Arrange
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService();
            mockTestSubject.CallBase = true;
            mockTestSubject.Setup(t => t.CreateNewProcessAndConnectAsync()).Returns(Task.CompletedTask);
            OutOfProcessNodeJSService testSubject = mockTestSubject.Object;

            // Act
            await testSubject.MoveToNewProcessAsync(false).ConfigureAwait(false);

            // Assert
            _mockRepository.VerifyAll();
        }

        [Fact]
        public async void MoveToNewProcessAsync_HandlesTrackedTasksIfTaskTrackingIsEnabled()
        {
            // Arrange
            // So task tracking is enabled
            var dummyOptions = new OutOfProcessNodeJSServiceOptions() { EnableFileWatching = true, GracefulProcessShutdown = true };
            Mock<IOptions<OutOfProcessNodeJSServiceOptions>> mockOptionsAccessor = _mockRepository.Create<IOptions<OutOfProcessNodeJSServiceOptions>>();
            mockOptionsAccessor.Setup(o => o.Value).Returns(dummyOptions);
            Mock<IBlockDrainerService> mockBlockDrainerService = _mockRepository.Create<IBlockDrainerService>();
            mockBlockDrainerService.Setup(b => b.DrainBlockAndPreventEntryAsync()).Returns(Task.CompletedTask);
            mockBlockDrainerService.Setup(b => b.ResetAfterDraining());
            Mock<ITaskService> mockTaskService = _mockRepository.Create<ITaskService>();
            mockTaskService.Setup(t => t.WhenAll(It.IsAny<Task[]>())).Returns(Task.CompletedTask);
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(optionsAccessor: mockOptionsAccessor.Object, 
                blockDrainerService: mockBlockDrainerService.Object,
                taskService: mockTaskService.Object);
            mockTestSubject.CallBase = true;
            mockTestSubject.Setup(t => t.CreateNewProcessAndConnectAsync()).Returns(Task.CompletedTask);
            var dummyTrackedTasks = new ConcurrentDictionary<Task, object?>();
            dummyTrackedTasks.TryAdd(Task.CompletedTask, null); // Add a single entry so we can check whether MoveToNewProcessAsync awaits tracked tasks
            mockTestSubject.Setup(t => t.InitializeFileWatching()).Returns((true, dummyTrackedTasks));
            OutOfProcessNodeJSService testSubject = mockTestSubject.Object;

            // Act
            await testSubject.MoveToNewProcessAsync(false).ConfigureAwait(false);

            // Assert
            _mockRepository.VerifyAll();
            Assert.Empty(dummyTrackedTasks); // Gets cleared
        }

        [Fact]
        public async void MoveToNewProcessAsync_HandlesAnyExistingNodeJSProcess()
        {
            // Arrange
            Mock<IEmbeddedResourcesService> mockEmbeddedResourcesService = _mockRepository.Create<IEmbeddedResourcesService>();
            Mock<INodeJSProcess> mockNewNodeJSProcess = _mockRepository.Create<INodeJSProcess>();
#if NET5_0 || NET6_0 || NET7_0
            mockNewNodeJSProcess.Setup(n => n.DisposeAsync());
#else
            mockNewNodeJSProcess.Setup(n => n.Dispose());
#endif
            Mock<INodeJSProcessFactory> mockNodeJSProcessFactory = _mockRepository.Create<INodeJSProcessFactory>();
            mockNodeJSProcessFactory.Setup(n => n.Create(It.IsAny<string>(), It.IsAny<EventHandler>())).Returns(mockNewNodeJSProcess.Object);
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(embeddedResourcesService: mockEmbeddedResourcesService.Object,
                nodeProcessFactory: mockNodeJSProcessFactory.Object);
            mockTestSubject.CallBase = true;
            mockTestSubject.Setup(t => t.CreateNewProcessAndConnectAsync()).Returns(Task.CompletedTask);
            OutOfProcessNodeJSService testSubject = mockTestSubject.Object;
            testSubject.CreateAndSetUpProcess(null!); // Create process

            // Act
            await testSubject.MoveToNewProcessAsync(false).ConfigureAwait(false);

            // Assert
            _mockRepository.VerifyAll();
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
            mockNodeJSProcess.Setup(n => n.BeginOutputAndErrorReading());
            Mock<INodeJSProcessFactory> mockNodeJSProcessFactory = _mockRepository.Create<INodeJSProcessFactory>();
            mockNodeJSProcessFactory.Setup(n => n.Create(dummyServerScript, It.IsAny<EventHandler>())).Returns(mockNodeJSProcess.Object);
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(mockNodeJSProcessFactory.Object,
                embeddedResourcesService: mockEmbeddedResourcesService.Object,
                serverScriptAssembly: dummyServerScriptAssembly,
                serverScriptName: dummyServerScriptName);
            mockTestSubject.CallBase = true;

            // Act
            INodeJSProcess result = mockTestSubject.Object.CreateAndSetUpProcess(new DisposeTrackingSemaphoreSlim(1, 1));

            // Assert
            Assert.Same(mockNodeJSProcess.Object, result);
            _mockRepository.VerifyAll();
        }

        [Fact]
        public void OutputReceivedHandler_IfNodeJSProcessIsNotConnectedAndMessageIsConnectionEstablishedMessageEstablishesConnection()
        {
            // Arrange
            const string dummyMessage = "dummyMessage";
            Mock<IEmbeddedResourcesService> mockEmbeddedResourcesService = _mockRepository.Create<IEmbeddedResourcesService>();
            Mock<INodeJSProcess> mockNodeJSProcess = _mockRepository.Create<INodeJSProcess>();
            Mock<INodeJSProcessFactory> mockNodeJSProcessFactory = _mockRepository.Create<INodeJSProcessFactory>();
            mockNodeJSProcessFactory.Setup(n => n.Create(It.IsAny<string>(), It.IsAny<EventHandler>())).Returns(mockNodeJSProcess.Object);
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(embeddedResourcesService: mockEmbeddedResourcesService.Object,
                nodeProcessFactory: mockNodeJSProcessFactory.Object);
            mockTestSubject.CallBase = true;
            // Override ConnectionEstablishedMessageRegex to match "dummyMessage"
            mockTestSubject.Protected().As<IOutOfProcessNodeJSServiceProtectedMembers>().Setup(t => t.ConnectionEstablishedMessageRegex).Returns(new Regex(".*"));
            mockTestSubject.Protected().As<IOutOfProcessNodeJSServiceProtectedMembers>().Setup(t => t.OnConnectionEstablishedMessageReceived(It.IsAny<System.Text.RegularExpressions.Match>()));
            mockTestSubject.Object.CreateAndSetUpProcess(null!); // Set _nodeJSProcess
            var dummySempahoreSlim = new DisposeTrackingSemaphoreSlim(0, 1);

            // Act
            mockTestSubject.Object.OutputReceivedHandler(dummyMessage, dummySempahoreSlim);

            // Assert
            _mockRepository.VerifyAll();
            Assert.Equal(1, dummySempahoreSlim.CurrentCount); // Ensure that it gets signaled
        }

        [Fact]
        public void OutputReceivedHandler_IfNodeJSProcessIsNullLogsMessages()
        {
            // Arrange
            const string dummyMessage = "dummyMessage";
            var loggerStringBuilder = new StringBuilder();
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(loggerStringBuilder: loggerStringBuilder, logLevel: LogLevel.Information);
            mockTestSubject.CallBase = true;

            // Act
            mockTestSubject.Object.OutputReceivedHandler(dummyMessage, new DisposeTrackingSemaphoreSlim(1, 1));

            // Assert
            _mockRepository.VerifyAll();
            string logResult = loggerStringBuilder.ToString();
            Assert.Contains(dummyMessage, logResult);
        }

        [Fact]
        public void OutputReceivedHandler_IfNodeJSProcessIsConnectedLogsMessage()
        {
            // Arrange
            const string dummyMessage = "dummyMessage";
            Mock<IEmbeddedResourcesService> mockEmbeddedResourcesService = _mockRepository.Create<IEmbeddedResourcesService>();
            Mock<INodeJSProcess> mockNodeJSProcess = _mockRepository.Create<INodeJSProcess>();
            mockNodeJSProcess.Setup(n => n.Connected).Returns(true); // Connected
            Mock<INodeJSProcessFactory> mockNodeJSProcessFactory = _mockRepository.Create<INodeJSProcessFactory>();
            mockNodeJSProcessFactory.Setup(n => n.Create(It.IsAny<string>(), It.IsAny<EventHandler>())).Returns(mockNodeJSProcess.Object);
            var loggerStringBuilder = new StringBuilder();
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(loggerStringBuilder: loggerStringBuilder,
                embeddedResourcesService: mockEmbeddedResourcesService.Object,
                nodeProcessFactory: mockNodeJSProcessFactory.Object,
                logLevel: LogLevel.Information);
            mockTestSubject.CallBase = true;
            OutOfProcessNodeJSService testSubject = mockTestSubject.Object;
            testSubject.CreateAndSetUpProcess(null!); // Set _nodeJSProcess

            // Act
            testSubject.OutputReceivedHandler(dummyMessage, new DisposeTrackingSemaphoreSlim(0, 1));

            // Assert
            _mockRepository.VerifyAll();
            string logResult = loggerStringBuilder.ToString();
            Assert.Contains(dummyMessage, logResult);
        }

        [Fact]
        public void OutputReceivedHandler_IfMessageIsNotConnectionEstablishedMessageLogsMessage()
        {
            // Arrange
            const string dummyMessage = "dummyMessage";
            Mock<IEmbeddedResourcesService> mockEmbeddedResourcesService = _mockRepository.Create<IEmbeddedResourcesService>();
            Mock<INodeJSProcess> mockNodeJSProcess = _mockRepository.Create<INodeJSProcess>();
            mockNodeJSProcess.Setup(n => n.Connected).Returns(false); // Not connected
            Mock<INodeJSProcessFactory> mockNodeJSProcessFactory = _mockRepository.Create<INodeJSProcessFactory>();
            mockNodeJSProcessFactory.Setup(n => n.Create(It.IsAny<string>(), It.IsAny<EventHandler>())).Returns(mockNodeJSProcess.Object);
            var loggerStringBuilder = new StringBuilder();
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(loggerStringBuilder: loggerStringBuilder,
                embeddedResourcesService: mockEmbeddedResourcesService.Object,
                nodeProcessFactory: mockNodeJSProcessFactory.Object,
                logLevel: LogLevel.Information);
            mockTestSubject.CallBase = true;
            // Not connected but message does not match handshake-message regex so it should be treated as a normal message
            mockTestSubject.Protected().As<IOutOfProcessNodeJSServiceProtectedMembers>().Setup(t => t.ConnectionEstablishedMessageRegex).Returns(new Regex("arbitraryPattern"));
            OutOfProcessNodeJSService testSubject = mockTestSubject.Object;
            testSubject.CreateAndSetUpProcess(null!); // Set _nodeJSProcess

            // Act
            testSubject.OutputReceivedHandler(dummyMessage, new DisposeTrackingSemaphoreSlim(0, 1));

            // Assert
            _mockRepository.VerifyAll();
            string logResult = loggerStringBuilder.ToString();
            Assert.Contains(dummyMessage, logResult);
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

        // Mocking protected members: https://github.com/Moq/moq4/wiki/Quickstart#miscellaneous
        private interface IOutOfProcessNodeJSServiceProtectedMembers
        {
            Regex ConnectionEstablishedMessageRegex { get; }
            void OnConnectionEstablishedMessageReceived(System.Text.RegularExpressions.Match connectionEstablishedMessage);
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
            ITaskService? taskService = null,
            IBlockDrainerService? blockDrainerService = null,
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
                taskService,
                blockDrainerService,
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
