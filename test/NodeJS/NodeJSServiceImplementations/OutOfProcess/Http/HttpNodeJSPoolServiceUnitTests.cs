using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using Xunit;

namespace Jering.Javascript.NodeJS.Tests
{
    // Strategy
    // - Ensure that GetHttpNodeJSService returns each HttpNodeJSServices in the pool an equal number of times.
    // - Ensure INodeJSService member implementations call the right method on the returned HttpNodeJSService.
    public class HttpNodeJSPoolServiceUnitTests
    {
        private const int TIMEOUT_MS = 60000;
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
            Mock<HttpNodeJSService> mockHttpNodeJSService = CreateMockHttpNodeJSService();
            mockHttpNodeJSService.
                Setup(t => t.InvokeFromFileAsync<int>(dummyModulePath, dummyExportName, dummyArgs, dummyCancellationToken)).
                ReturnsAsync(dummyResult);
            HttpNodeJSPoolService testSubject = CreateHttpNodeJSPoolService(new[] { mockHttpNodeJSService.Object });

            // Act
            int result = await testSubject.InvokeFromFileAsync<int>(dummyModulePath, dummyExportName, dummyArgs, dummyCancellationToken).ConfigureAwait(false);

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
            Mock<HttpNodeJSService> mockHttpNodeJSService = CreateMockHttpNodeJSService();
            mockHttpNodeJSService.
                Setup(t => t.InvokeFromFileAsync(dummyModulePath, dummyExportName, dummyArgs, dummyCancellationToken));
            HttpNodeJSPoolService testSubject = CreateHttpNodeJSPoolService(new[] { mockHttpNodeJSService.Object });

            // Act
            await testSubject.InvokeFromFileAsync(dummyModulePath, dummyExportName, dummyArgs, dummyCancellationToken).ConfigureAwait(false);

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
            Mock<HttpNodeJSService> mockHttpNodeJSService = CreateMockHttpNodeJSService();
            mockHttpNodeJSService.
                Setup(t => t.InvokeFromStringAsync<int>(dummyModuleString, dummyNewCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken)).
                ReturnsAsync(dummyResult);
            HttpNodeJSPoolService testSubject = CreateHttpNodeJSPoolService(new[] { mockHttpNodeJSService.Object });

            // Act
            int result = await testSubject.InvokeFromStringAsync<int>(dummyModuleString, dummyNewCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken).ConfigureAwait(false);

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
            Mock<HttpNodeJSService> mockHttpNodeJSService = CreateMockHttpNodeJSService();
            mockHttpNodeJSService.Setup(t => t.InvokeFromStringAsync(dummyModuleString, dummyNewCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken));
            HttpNodeJSPoolService testSubject = CreateHttpNodeJSPoolService(new[] { mockHttpNodeJSService.Object });

            // Act
            await testSubject.InvokeFromStringAsync(dummyModuleString, dummyNewCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken).ConfigureAwait(false);

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
            Mock<HttpNodeJSService> mockHttpNodeJSService = CreateMockHttpNodeJSService();
            mockHttpNodeJSService.
                Setup(t => t.InvokeFromStringAsync<int>(dummyModuleFactory, dummyCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken)).
                ReturnsAsync(dummyResult);
            HttpNodeJSPoolService testSubject = CreateHttpNodeJSPoolService(new[] { mockHttpNodeJSService.Object });

            // Act
            int result = await testSubject.InvokeFromStringAsync<int>(dummyModuleFactory, dummyCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken).ConfigureAwait(false);

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
            Mock<HttpNodeJSService> mockHttpNodeJSService = CreateMockHttpNodeJSService();
            mockHttpNodeJSService.
                Setup(t => t.InvokeFromStringAsync(dummyModuleFactory, dummyCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken));
            HttpNodeJSPoolService testSubject = CreateHttpNodeJSPoolService(new[] { mockHttpNodeJSService.Object });

            // Act
            await testSubject.InvokeFromStringAsync(dummyModuleFactory, dummyCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken).ConfigureAwait(false);

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
            Mock<HttpNodeJSService> mockHttpNodeJSService = CreateMockHttpNodeJSService();
            mockHttpNodeJSService.
                Setup(t => t.InvokeFromStreamAsync<int>(dummyModuleStream, dummyNewCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken)).
                ReturnsAsync(dummyResult);
            HttpNodeJSPoolService testSubject = CreateHttpNodeJSPoolService(new[] { mockHttpNodeJSService.Object });

            // Act
            int result = await testSubject.InvokeFromStreamAsync<int>(dummyModuleStream, dummyNewCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken).ConfigureAwait(false);

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
            Mock<HttpNodeJSService> mockHttpNodeJSService = CreateMockHttpNodeJSService();
            mockHttpNodeJSService.Setup(t => t.InvokeFromStreamAsync(dummyModuleStream, dummyNewCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken));
            HttpNodeJSPoolService testSubject = CreateHttpNodeJSPoolService(new[] { mockHttpNodeJSService.Object });

            // Act
            await testSubject.InvokeFromStreamAsync(dummyModuleStream, dummyNewCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken).ConfigureAwait(false);

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
            Mock<HttpNodeJSService> mockHttpNodeJSService = CreateMockHttpNodeJSService();
            mockHttpNodeJSService.
                Setup(t => t.InvokeFromStreamAsync<int>(dummyModuleFactory, dummyCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken)).
                ReturnsAsync(dummyResult);
            HttpNodeJSPoolService testSubject = CreateHttpNodeJSPoolService(new[] { mockHttpNodeJSService.Object });

            // Act
            int result = await testSubject.InvokeFromStreamAsync<int>(dummyModuleFactory, dummyCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken).ConfigureAwait(false);

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
            Mock<HttpNodeJSService> mockHttpNodeJSService = CreateMockHttpNodeJSService();
            mockHttpNodeJSService.
                Setup(t => t.InvokeFromStreamAsync(dummyModuleFactory, dummyCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken));
            HttpNodeJSPoolService testSubject = CreateHttpNodeJSPoolService(new[] { mockHttpNodeJSService.Object });

            // Act
            await testSubject.InvokeFromStreamAsync(dummyModuleFactory, dummyCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken).ConfigureAwait(false);

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
            Mock<HttpNodeJSService> mockHttpNodeJSService = CreateMockHttpNodeJSService();
            mockHttpNodeJSService.
                Setup(t => t.TryInvokeFromCacheAsync<int>(dummyModuleCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken)).
                ReturnsAsync((true, dummyResult));
            HttpNodeJSPoolService testSubject = CreateHttpNodeJSPoolService(new[] { mockHttpNodeJSService.Object });

            // Act
            (bool success, int result) = await testSubject.TryInvokeFromCacheAsync<int>(dummyModuleCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken).ConfigureAwait(false);

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
            Mock<HttpNodeJSService> mockHttpNodeJSService = CreateMockHttpNodeJSService();
            mockHttpNodeJSService.
                Setup(t => t.TryInvokeFromCacheAsync(dummyModuleCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken)).
                ReturnsAsync(true);
            HttpNodeJSPoolService testSubject = CreateHttpNodeJSPoolService(new[] { mockHttpNodeJSService.Object });

            // Act
            bool success = await testSubject.TryInvokeFromCacheAsync(dummyModuleCacheIdentifier, dummyExportName, dummyArgs, dummyCancellationToken).ConfigureAwait(false);

            // Assert
            Assert.True(success);
            _mockRepository.VerifyAll();
        }

        [Fact(Timeout = TIMEOUT_MS)]
        public void GetHttpNodeJSService_ReturnsEachHttpNodeJSServiceAnEqualNumberOfTimes()
        {
            // Arrange
            const int dummyNumHttpNodeJSServices = 5;
            var dummyHttpNodeJSServices = new HttpNodeJSService[dummyNumHttpNodeJSServices];
            for (int i = 0; i < dummyNumHttpNodeJSServices; i++)
            {
                dummyHttpNodeJSServices[i] = CreateHttpNodeJSService();
            }

            HttpNodeJSPoolService testSubject = CreateHttpNodeJSPoolService(dummyHttpNodeJSServices);

            // Act
            var results = new ConcurrentBag<HttpNodeJSService>();
            const int numThreads = 5;
            const int numGetsPerThread = 10;
            var threads = new List<Thread>();
            for (int i = 0; i < numThreads; i++)
            {
                var thread = new Thread(() =>
                {
                    for (int j = 0; j < numGetsPerThread; j++)
                    {
                        results.Add(testSubject.GetHttpNodeJSService());
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
            const int expectedNumPerHttpNodeJSService = numThreads * numGetsPerThread / dummyNumHttpNodeJSServices;
            Assert.Equal(expectedNumPerHttpNodeJSService, results.Count(httpNodeJSService => httpNodeJSService == dummyHttpNodeJSServices[0]));
            Assert.Equal(expectedNumPerHttpNodeJSService, results.Count(httpNodeJSService => httpNodeJSService == dummyHttpNodeJSServices[1]));
            Assert.Equal(expectedNumPerHttpNodeJSService, results.Count(httpNodeJSService => httpNodeJSService == dummyHttpNodeJSServices[2]));
            Assert.Equal(expectedNumPerHttpNodeJSService, results.Count(httpNodeJSService => httpNodeJSService == dummyHttpNodeJSServices[3]));
            Assert.Equal(expectedNumPerHttpNodeJSService, results.Count(httpNodeJSService => httpNodeJSService == dummyHttpNodeJSServices[4]));
        }

        private static HttpNodeJSPoolService CreateHttpNodeJSPoolService(HttpNodeJSService[] httpNodeJSServices)
        {
            return new HttpNodeJSPoolService(new ReadOnlyCollection<HttpNodeJSService>(httpNodeJSServices));
        }

        private Mock<HttpNodeJSService> CreateMockHttpNodeJSService(IOptions<OutOfProcessNodeJSServiceOptions>? outOfProcessNodeHostOptionsAccessor = null,
            IOptions<HttpNodeJSServiceOptions>? httpNodeJSServiceOptionsAccessor = null,
            IHttpContentFactory? httpContentFactory = null,
            IEmbeddedResourcesService? embeddedResourcesService = null,
            IFileWatcherFactory? fileWatcherFactory = null,
            IMonitorService? monitorService = null,
            ITaskService? taskService = null,
            IHttpClientService? httpClientService = null,
            IJsonService? jsonService = null,
            INodeJSProcessFactory? nodeProcessFactory = null,
            ILogger<HttpNodeJSService>? logger = null)
        {
            if (logger == null)
            {
                Mock<ILogger<HttpNodeJSService>> mockLogger = _mockRepository.Create<ILogger<HttpNodeJSService>>();
                logger = mockLogger.Object;
            }

            if (httpNodeJSServiceOptionsAccessor == null)
            {
                Mock<IOptions<HttpNodeJSServiceOptions>> mockHttpNodeJSServiceOptionsAccessor = _mockRepository.Create<IOptions<HttpNodeJSServiceOptions>>();
#if NETCOREAPP3_1 || NET5_0_OR_GREATER
                mockHttpNodeJSServiceOptionsAccessor.Setup(m => m.Value).Returns(new HttpNodeJSServiceOptions());
#endif
                httpNodeJSServiceOptionsAccessor = mockHttpNodeJSServiceOptionsAccessor.Object;
            }

            if (outOfProcessNodeHostOptionsAccessor == null)
            {
                Mock<IOptions<OutOfProcessNodeJSServiceOptions>> mockOutOfProcessNodeJSServiceOptions = _mockRepository.Create<IOptions<OutOfProcessNodeJSServiceOptions>>();
                mockOutOfProcessNodeJSServiceOptions.Setup(m => m.Value).Returns(new OutOfProcessNodeJSServiceOptions());
                outOfProcessNodeHostOptionsAccessor = mockOutOfProcessNodeJSServiceOptions.Object;
            }

            return _mockRepository.Create<HttpNodeJSService>(outOfProcessNodeHostOptionsAccessor,
                httpNodeJSServiceOptionsAccessor,
                httpContentFactory,
                embeddedResourcesService,
                fileWatcherFactory,
                monitorService,
                taskService,
                httpClientService,
                jsonService,
                nodeProcessFactory,
                logger);
        }

        private HttpNodeJSService CreateHttpNodeJSService(IOptions<OutOfProcessNodeJSServiceOptions>? outOfProcessNodeHostOptionsAccessor = null,
            IOptions<HttpNodeJSServiceOptions>? httpNodeJSServiceOptionsAccessor = null,
            IHttpContentFactory? httpContentFactory = null,
            IEmbeddedResourcesService? embeddedResourcesService = null,
            IFileWatcherFactory? fileWatcherFactory = null,
            IMonitorService? monitorService = null,
            ITaskService? taskService = null,
            IHttpClientService? httpClientService = null,
            IJsonService? jsonService = null,
            INodeJSProcessFactory? nodeProcessFactory = null,
            ILogger<HttpNodeJSService>? logger = null)
        {
            if (logger == null)
            {
                Mock<ILogger<HttpNodeJSService>> mockLogger = _mockRepository.Create<ILogger<HttpNodeJSService>>();
                logger = mockLogger.Object;
            }

            // We call optionsAccessor.Value in constructors, so options accessors must be mocked with Value property setup
            if (httpNodeJSServiceOptionsAccessor == null)
            {
                Mock<IOptions<HttpNodeJSServiceOptions>> mockHttpNodeJSServiceOptionsAccessor = _mockRepository.Create<IOptions<HttpNodeJSServiceOptions>>();
                mockHttpNodeJSServiceOptionsAccessor.Setup(m => m.Value).Returns(new HttpNodeJSServiceOptions());
                httpNodeJSServiceOptionsAccessor = mockHttpNodeJSServiceOptionsAccessor.Object;
            }

            if (outOfProcessNodeHostOptionsAccessor == null)
            {
                Mock<IOptions<OutOfProcessNodeJSServiceOptions>> mockOutOfProcessNodeJSServiceOptions = _mockRepository.Create<IOptions<OutOfProcessNodeJSServiceOptions>>();
                mockOutOfProcessNodeJSServiceOptions.Setup(m => m.Value).Returns(new OutOfProcessNodeJSServiceOptions());
                outOfProcessNodeHostOptionsAccessor = mockOutOfProcessNodeJSServiceOptions.Object;
            }

            return new HttpNodeJSService(outOfProcessNodeHostOptionsAccessor,
                httpNodeJSServiceOptionsAccessor,
                httpContentFactory ?? _mockRepository.Create<IHttpContentFactory>().Object,
                embeddedResourcesService ?? _mockRepository.Create<IEmbeddedResourcesService>().Object,
                fileWatcherFactory ?? _mockRepository.Create<IFileWatcherFactory>().Object,
                monitorService ?? _mockRepository.Create<IMonitorService>().Object,
                taskService ?? _mockRepository.Create<ITaskService>().Object,
                httpClientService ?? _mockRepository.Create<IHttpClientService>().Object,
                jsonService ?? _mockRepository.Create<IJsonService>().Object,
                nodeProcessFactory ?? _mockRepository.Create<INodeJSProcessFactory>().Object,
                logger);
        }
    }
}
