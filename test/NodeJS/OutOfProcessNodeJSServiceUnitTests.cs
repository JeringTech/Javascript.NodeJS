using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
            using (var dummyCancellationTokenSource = new CancellationTokenSource())
            {
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
                mockTestSubject.Setup(t => t.CreateCts(dummyCancellationToken)).Returns((dummyCancellationToken, null));
                dummyCancellationTokenSource.Cancel(); // Cancel token

                // Act and assert
                Assert.ThrowsAsync<OperationCanceledException>(async () => await mockTestSubject.Object.TryInvokeCoreAsync<int>(dummyInvocationRequest, dummyCancellationToken).ConfigureAwait(false));
                _mockRepository.VerifyAll();
                mockTestSubject.
                    Protected().
                    As<IOutOfProcessNodeJSServiceProtectedMembers>().
                    Verify(t => t.TryInvokeAsync<int>(dummyInvocationRequest, It.IsAny<CancellationToken>()), Times.Exactly(1)); // No retries
            }
        }

        [Fact]
        public async void TryInvokeCoreAsync_RetriesInvocationsThatTimeoutAndThrowsInvocationExceptionIfNoRetriesRemain()
        {
            // Arrange
            var dummyException = new OperationCanceledException();
            const int dummyTimeoutMS = 100;
            const int dummyNumRetries = 2;
            var outOfProcessNodeJSServiceOptions = new OutOfProcessNodeJSServiceOptions { TimeoutMS = dummyTimeoutMS, NumRetries = dummyNumRetries };
            Mock<IOptions<OutOfProcessNodeJSServiceOptions>> mockOptionsAccessor = _mockRepository.Create<IOptions<OutOfProcessNodeJSServiceOptions>>();
            mockOptionsAccessor.Setup(o => o.Value).Returns(outOfProcessNodeJSServiceOptions);
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
            mockTestSubject.Setup(t => t.CreateCts(dummyCancellationToken)).Returns((dummyCancellationToken, null));

            // Act and assert
            InvocationException result = await Assert.
                ThrowsAsync<InvocationException>(async () => await mockTestSubject.Object.TryInvokeCoreAsync<int>(dummyInvocationRequest, dummyCancellationToken).ConfigureAwait(false)).
                ConfigureAwait(false);
            _mockRepository.VerifyAll();
            Assert.Equal(dummyNumRetries, Regex.Matches(loggerStringBuilder.ToString(), $"{nameof(LogLevel.Warning)}: ").Count); // Must log before each retry
            mockTestSubject.
                Protected().
                As<IOutOfProcessNodeJSServiceProtectedMembers>().
                Verify(t => t.TryInvokeAsync<int>(dummyInvocationRequest, dummyCancellationToken), Times.Exactly(1 + dummyNumRetries));
            Assert.Equal(string.Format(Strings.InvocationException_OutOfProcessNodeJSService_InvocationTimedOut,
                    dummyTimeoutMS,
                    nameof(OutOfProcessNodeJSServiceOptions.TimeoutMS),
                    nameof(OutOfProcessNodeJSServiceOptions)),
                result.Message);
        }

        [Fact]
        public async void TryInvokeCoreAsync_RetriesInvocationsThatThrowExceptionsAndThrowsExceptionIfNoRetriesRemain()
        {
            // Arrange
            var dummyException = new InvocationException();
            const int dummyTimeoutMS = 100;
            const int dummyNumRetries = 2;
            var outOfProcessNodeJSServiceOptions = new OutOfProcessNodeJSServiceOptions { TimeoutMS = dummyTimeoutMS, NumRetries = dummyNumRetries };
            Mock<IOptions<OutOfProcessNodeJSServiceOptions>> mockOptionsAccessor = _mockRepository.Create<IOptions<OutOfProcessNodeJSServiceOptions>>();
            mockOptionsAccessor.Setup(o => o.Value).Returns(outOfProcessNodeJSServiceOptions);
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
            mockTestSubject.Setup(t => t.CreateCts(dummyCancellationToken)).Returns((dummyCancellationToken, null));

            // Act and assert
            InvocationException result = await Assert.
                ThrowsAsync<InvocationException>(async () => await mockTestSubject.Object.TryInvokeCoreAsync<int>(dummyInvocationRequest, dummyCancellationToken).ConfigureAwait(false)).
                ConfigureAwait(false);
            _mockRepository.VerifyAll();
            Assert.Equal(dummyNumRetries, Regex.Matches(loggerStringBuilder.ToString(), $"{nameof(LogLevel.Warning)}: ").Count); // Must log before each retry
            mockTestSubject.
                Protected().
                As<IOutOfProcessNodeJSServiceProtectedMembers>().
                Verify(t => t.TryInvokeAsync<int>(dummyInvocationRequest, dummyCancellationToken), Times.Exactly(1 + dummyNumRetries));
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
            mockTestSubject.Setup(t => t.CreateCts(dummyCancellationToken)).Returns((dummyCancellationToken, null));

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
            var outOfProcessNodeJSServiceOptions = new OutOfProcessNodeJSServiceOptions { NumRetries = dummyNumRetries };
            Mock<IOptions<OutOfProcessNodeJSServiceOptions>> mockOptionsAccessor = _mockRepository.Create<IOptions<OutOfProcessNodeJSServiceOptions>>();
            mockOptionsAccessor.Setup(o => o.Value).Returns(outOfProcessNodeJSServiceOptions);
            var dummyException = new InvocationException();
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
            mockTestSubject.Setup(t => t.CreateCts(dummyCancellationToken)).Returns((dummyCancellationToken, null));

            // Act and assert
            InvocationException result = await Assert.
                ThrowsAsync<InvocationException>(async () => await mockTestSubject.Object.TryInvokeCoreAsync<int>(dummyInvocationRequest, dummyCancellationToken).ConfigureAwait(false)).
                ConfigureAwait(false);
            Assert.Same(dummyException, result);
            _mockRepository.VerifyAll();
            mockTestSubject.
                Protected().
                As<IOutOfProcessNodeJSServiceProtectedMembers>().
                Verify(t => t.TryInvokeAsync<int>(dummyInvocationRequest, It.IsAny<CancellationToken>()), Times.Exactly(1 + dummyNumRetries));
            mockStream.VerifySet(s => s.Position = dummyStreamInitialPosition, Times.Exactly(dummyNumRetries));
        }

        [Fact]
        public async void TryInvokeCoreAsync_RetriesInvocationIfModuleSourceIsASeekableStreamAtItsInitialPosition()
        {
            // Arrange
            var dummyCancellationToken = new CancellationToken();
            const int dummyNumRetries = 2;
            var outOfProcessNodeJSServiceOptions = new OutOfProcessNodeJSServiceOptions { NumRetries = dummyNumRetries };
            Mock<IOptions<OutOfProcessNodeJSServiceOptions>> mockOptionsAccessor = _mockRepository.Create<IOptions<OutOfProcessNodeJSServiceOptions>>();
            mockOptionsAccessor.Setup(o => o.Value).Returns(outOfProcessNodeJSServiceOptions);
            var dummyException = new InvocationException();
            const int dummyStreamInitialPosition = 1;
            Mock<Stream> mockStream = _mockRepository.Create<Stream>();
            mockStream.Setup(s => s.CanSeek).Returns(true);
            mockStream.SetupSequence(s => s.Position).Returns(dummyStreamInitialPosition).Returns(dummyStreamInitialPosition).Returns(dummyStreamInitialPosition); // Stay at initial position
            var dummyInvocationRequest = new InvocationRequest(ModuleSourceType.Stream, moduleStreamSource: mockStream.Object);
            Mock<OutOfProcessNodeJSService> mockTestSubject = CreateMockOutOfProcessNodeJSService(optionsAccessor: mockOptionsAccessor.Object);
            mockTestSubject.CallBase = true;
            mockTestSubject.Setup(t => t.ConnectIfNotConnected());
            mockTestSubject.
                Protected().
                As<IOutOfProcessNodeJSServiceProtectedMembers>().
                Setup(t => t.TryInvokeAsync<int>(dummyInvocationRequest, dummyCancellationToken)).
                ThrowsAsync(dummyException);
            mockTestSubject.Setup(t => t.CreateCts(dummyCancellationToken)).Returns((dummyCancellationToken, null));

            // Act and assert
            InvocationException result = await Assert.
                ThrowsAsync<InvocationException>(async () => await mockTestSubject.Object.TryInvokeCoreAsync<int>(dummyInvocationRequest, dummyCancellationToken).ConfigureAwait(false)).
                ConfigureAwait(false);
            Assert.Same(dummyException, result);
            _mockRepository.VerifyAll();
            mockTestSubject.
                Protected().
                As<IOutOfProcessNodeJSServiceProtectedMembers>().
                Verify(t => t.TryInvokeAsync<int>(dummyInvocationRequest, It.IsAny<CancellationToken>()), Times.Exactly(1 + dummyNumRetries));
            mockStream.VerifySet(s => s.Position = dummyStreamInitialPosition, Times.Never);
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
            using (var dummyWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset))
            {
                // Act
                mockTestSubject.Object.OutputReceivedHandler(null, dummyMessage, dummyWaitHandle);

                // Assert
                _mockRepository.VerifyAll();
                Assert.True(dummyWaitHandle.WaitOne(0)); // Ensure that it gets signaled
                mockTestSubject.Protected().As<IOutOfProcessNodeJSServiceProtectedMembers>().Verify(t => t.OnConnectionEstablishedMessageReceived(dummyMessage), Times.Once());
            }
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

            // Act
            mockTestSubject.Object.OutputReceivedHandler(null, dummyMessage, null);

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
            mockTestSubject.Object.ErrorReceivedHandler(null, dummyMessage);

            // Assert
            _mockRepository.VerifyAll();
            string logResult = loggerStringBuilder.ToString();
            Assert.Equal(logResult, $"{nameof(LogLevel.Error)}: {dummyMessage}\n", ignoreLineEndingDifferences: true);
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

        public void Dispose()
        {
            ((IDisposable)_serviceProvider).Dispose();
        }
    }
}
