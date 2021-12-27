using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Jering.Javascript.NodeJS.Tests
{
    public class HttpNodeJSServiceUnitTests
    {
        private readonly MockRepository _mockRepository = new(MockBehavior.Default);

        [Fact]
        public async Task TryInvokeAsync_ReturnsTupleContainingFalseAndDefaultIfHttpResponseHas404StatusCode()
        {
            // Arrange
            var dummyInvocationRequest = new InvocationRequest(ModuleSourceType.Cache, "dummyModuleSource");
            Mock<HttpContent> mockRequestHttpContent = _mockRepository.Create<HttpContent>(); // HttpContent is an abstract class
            Mock<IHttpContentFactory> mockHttpContentFactory = _mockRepository.Create<IHttpContentFactory>();
            mockHttpContentFactory.Setup(h => h.Create(dummyInvocationRequest)).Returns(mockRequestHttpContent.Object);
            var dummyHttpResponseMessage = new HttpResponseMessage(HttpStatusCode.NotFound);
            Mock<IHttpClientService> mockHttpClientService = _mockRepository.Create<IHttpClientService>();
            mockHttpClientService.Setup(h => h.SendAsync(It.Is<HttpRequestMessage>(hr => ReferenceEquals(hr.Content, mockRequestHttpContent.Object)),
                    HttpCompletionOption.ResponseHeadersRead,
                    CancellationToken.None)).
                ReturnsAsync(dummyHttpResponseMessage);
            ExposedHttpNodeJSService testSubject = CreateHttpNodeJSService(httpContentFactory: mockHttpContentFactory.Object, httpClientService: mockHttpClientService.Object);

            // Act
            (bool success, string? value) = await testSubject.ExposedTryInvokeAsync<string>(dummyInvocationRequest, CancellationToken.None).ConfigureAwait(false);

            // Assert
            _mockRepository.VerifyAll();
            Assert.False(success);
            Assert.Null(value);
        }

        [Fact]
        public async Task TryInvokeAsync_ThrowsInvocationExceptionIfHttpResponseHas500StatusCode()
        {
            // Arrange
            var dummyInvocationRequest = new InvocationRequest(ModuleSourceType.Cache, "dummyModuleSource");
            Mock<HttpContent> mockRequestHttpContent = _mockRepository.Create<HttpContent>(); // HttpContent is an abstract class
            Mock<IHttpContentFactory> mockHttpContentFactory = _mockRepository.Create<IHttpContentFactory>();
            mockHttpContentFactory.Setup(h => h.Create(dummyInvocationRequest)).Returns(mockRequestHttpContent.Object);
            var dummyHttpResponseMessage = new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StreamContent(new MemoryStream()) };
            Mock<IHttpClientService> mockHttpClientService = _mockRepository.Create<IHttpClientService>();
            mockHttpClientService.Setup(h => h.SendAsync(It.Is<HttpRequestMessage>(hr => ReferenceEquals(hr.Content, mockRequestHttpContent.Object)),
                    HttpCompletionOption.ResponseHeadersRead,
                    CancellationToken.None)).
                ReturnsAsync(dummyHttpResponseMessage);
            var dummyInvocationError = new InvocationError("dummyErrorMessage", "dummyErrorStack");
            Mock<IJsonService> mockJsonService = _mockRepository.Create<IJsonService>();
            mockJsonService.Setup(j => j.DeserializeAsync<InvocationError>(It.IsAny<Stream>(), CancellationToken.None)).ReturnsAsync(dummyInvocationError);
            ExposedHttpNodeJSService testSubject = CreateHttpNodeJSService(httpContentFactory: mockHttpContentFactory.Object,
                httpClientService: mockHttpClientService.Object,
                jsonService: mockJsonService.Object);

            // Act and assert
            InvocationException result = await Assert.ThrowsAsync<InvocationException>(() => testSubject.ExposedTryInvokeAsync<string>(dummyInvocationRequest, CancellationToken.None)).ConfigureAwait(false);
            _mockRepository.VerifyAll();
            Assert.Equal(dummyInvocationError.ErrorMessage + Environment.NewLine + dummyInvocationError.ErrorStack, result.Message, ignoreLineEndingDifferences: true);
        }

        [Fact]
        public async Task TryInvokeAsync_ReturnsTupleContainingTrueAndNullIfHttpResponseHas200StatusCodeAndTypeParameterIsVoid()
        {
            // Arrange
            var dummyInvocationRequest = new InvocationRequest(ModuleSourceType.Cache, "dummyModuleSource");
            Mock<HttpContent> mockRequestHttpContent = _mockRepository.Create<HttpContent>(); // HttpContent is an abstract class
            Mock<IHttpContentFactory> mockHttpContentFactory = _mockRepository.Create<IHttpContentFactory>();
            mockHttpContentFactory.Setup(h => h.Create(dummyInvocationRequest)).Returns(mockRequestHttpContent.Object);
            var dummyHttpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            Mock<IHttpClientService> mockHttpClientService = _mockRepository.Create<IHttpClientService>();
            mockHttpClientService.Setup(h => h.SendAsync(It.Is<HttpRequestMessage>(hr => ReferenceEquals(hr.Content, mockRequestHttpContent.Object)),
                    HttpCompletionOption.ResponseHeadersRead,
                    CancellationToken.None)).
                ReturnsAsync(dummyHttpResponseMessage);
            ExposedHttpNodeJSService testSubject = CreateHttpNodeJSService(httpContentFactory: mockHttpContentFactory.Object,
                httpClientService: mockHttpClientService.Object);

            // Act
            (bool success, Void? value) = await testSubject.ExposedTryInvokeAsync<Void>(dummyInvocationRequest, CancellationToken.None).ConfigureAwait(false);

            // Assert
            _mockRepository.VerifyAll();
            Assert.True(success);
            Assert.Null(value);
        }

        [Fact]
        public async Task TryInvokeAsync_ReturnsTupleContainingTrueAndAStreamIfHttpResponseHas200StatusCodeAndTypeParameterIsStream()
        {
            // Arrange
            var dummyInvocationRequest = new InvocationRequest(ModuleSourceType.Cache, "dummyModuleSource");
            Mock<HttpContent> mockRequestHttpContent = _mockRepository.Create<HttpContent>(); // HttpContent is an abstract class
            Mock<IHttpContentFactory> mockHttpContentFactory = _mockRepository.Create<IHttpContentFactory>();
            mockHttpContentFactory.Setup(h => h.Create(dummyInvocationRequest)).Returns(mockRequestHttpContent.Object);
            var dummyHttpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new MemoryStream()) };
            Mock<IHttpClientService> mockHttpClientService = _mockRepository.Create<IHttpClientService>();
            mockHttpClientService.Setup(h => h.SendAsync(It.Is<HttpRequestMessage>(hr => ReferenceEquals(hr.Content, mockRequestHttpContent.Object)),
                    HttpCompletionOption.ResponseHeadersRead,
                    CancellationToken.None)).
                ReturnsAsync(dummyHttpResponseMessage);
            ExposedHttpNodeJSService testSubject = CreateHttpNodeJSService(httpContentFactory: mockHttpContentFactory.Object,
                httpClientService: mockHttpClientService.Object);

            // Act
            (bool success, Stream? value) = await testSubject.ExposedTryInvokeAsync<Stream>(dummyInvocationRequest, CancellationToken.None).ConfigureAwait(false);

            // Assert
            _mockRepository.VerifyAll();
            Assert.True(success);
            Assert.NotNull(value);
        }

        [Fact]
        public async Task TryInvokeAsync_ReturnsTupleContainingTrueAndAStringIfHttpResponseHas200StatusCodeAndTypeParameterIsString()
        {
            // Arrange
            var dummyInvocationRequest = new InvocationRequest(ModuleSourceType.Cache, "dummyModuleSource");
            Mock<HttpContent> mockRequestHttpContent = _mockRepository.Create<HttpContent>(); // HttpContent is an abstract class
            Mock<IHttpContentFactory> mockHttpContentFactory = _mockRepository.Create<IHttpContentFactory>();
            mockHttpContentFactory.Setup(h => h.Create(dummyInvocationRequest)).Returns(mockRequestHttpContent.Object);
            const string dummyValue = "dummyValue";
            var dummyMemoryStream = new MemoryStream(Encoding.UTF8.GetBytes(dummyValue));
            var dummyHttpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(dummyMemoryStream) };
            Mock<IHttpClientService> mockHttpClientService = _mockRepository.Create<IHttpClientService>();
            mockHttpClientService.Setup(h => h.SendAsync(It.Is<HttpRequestMessage>(hr => ReferenceEquals(hr.Content, mockRequestHttpContent.Object)),
                    HttpCompletionOption.ResponseHeadersRead,
                    CancellationToken.None)).
                ReturnsAsync(dummyHttpResponseMessage);
            ExposedHttpNodeJSService testSubject = CreateHttpNodeJSService(httpContentFactory: mockHttpContentFactory.Object,
                httpClientService: mockHttpClientService.Object);

            // Act
            (bool success, string? value) = await testSubject.ExposedTryInvokeAsync<string>(dummyInvocationRequest, CancellationToken.None).ConfigureAwait(false);

            // Assert
            _mockRepository.VerifyAll();
            Assert.True(success);
            Assert.Equal(dummyValue, value);
        }

        [Fact]
        public async Task TryInvokeAsync_ReturnsTupleContainingTrueAndAnObjectIfHttpResponseHas200StatusCodeAndTypeParameterIsAnObject()
        {
            // Arrange
            var dummyInvocationRequest = new InvocationRequest(ModuleSourceType.Cache, "dummyModuleSource");
            Mock<HttpContent> mockRequestHttpContent = _mockRepository.Create<HttpContent>(); // HttpContent is an abstract class
            Mock<IHttpContentFactory> mockHttpContentFactory = _mockRepository.Create<IHttpContentFactory>();
            mockHttpContentFactory.Setup(h => h.Create(dummyInvocationRequest)).Returns(mockRequestHttpContent.Object);
            var dummyHttpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new MemoryStream()) };
            Mock<IHttpClientService> mockHttpClientService = _mockRepository.Create<IHttpClientService>();
            mockHttpClientService.Setup(h => h.SendAsync(It.Is<HttpRequestMessage>(hr => ReferenceEquals(hr.Content, mockRequestHttpContent.Object)),
                    HttpCompletionOption.ResponseHeadersRead,
                    CancellationToken.None)).
                ReturnsAsync(dummyHttpResponseMessage);
            var dummyObject = new DummyClass();
            Mock<IJsonService> mockJsonService = _mockRepository.Create<IJsonService>();
            mockJsonService.Setup(j => j.DeserializeAsync<DummyClass>(It.IsAny<Stream>(), CancellationToken.None)).ReturnsAsync(dummyObject);
            ExposedHttpNodeJSService testSubject = CreateHttpNodeJSService(httpContentFactory: mockHttpContentFactory.Object,
                httpClientService: mockHttpClientService.Object,
                jsonService: mockJsonService.Object);

            // Act
            (bool success, DummyClass? value) = await testSubject.ExposedTryInvokeAsync<DummyClass>(dummyInvocationRequest, CancellationToken.None).ConfigureAwait(false);

            // Assert
            _mockRepository.VerifyAll();
            Assert.True(success);
            Assert.Same(dummyObject, value);
        }

        [Fact]
        public async Task TryInvokeAsync_ThrowsInvocationExceptionIfHttpResponseHasAnUnexpectedStatusCode()
        {
            // Arrange
            var dummyInvocationRequest = new InvocationRequest(ModuleSourceType.Cache, "dummyModuleSource");
            Mock<HttpContent> mockRequestHttpContent = _mockRepository.Create<HttpContent>(); // HttpContent is an abstract class
            Mock<IHttpContentFactory> mockHttpContentFactory = _mockRepository.Create<IHttpContentFactory>();
            mockHttpContentFactory.Setup(h => h.Create(dummyInvocationRequest)).Returns(mockRequestHttpContent.Object);
            const HttpStatusCode dummyHttpStatusCode = HttpStatusCode.NoContent;
            var dummyHttpResponseMessage = new HttpResponseMessage(dummyHttpStatusCode);
            Mock<IHttpClientService> mockHttpClientService = _mockRepository.Create<IHttpClientService>();
            mockHttpClientService.Setup(h => h.SendAsync(It.Is<HttpRequestMessage>(hr => ReferenceEquals(hr.Content, mockRequestHttpContent.Object)),
                    HttpCompletionOption.ResponseHeadersRead,
                    CancellationToken.None)).
                ReturnsAsync(dummyHttpResponseMessage);
            ExposedHttpNodeJSService testSubject = CreateHttpNodeJSService(httpContentFactory: mockHttpContentFactory.Object, httpClientService: mockHttpClientService.Object);

            // Act and assert
            InvocationException result = await Assert.ThrowsAsync<InvocationException>(() => testSubject.ExposedTryInvokeAsync<string>(dummyInvocationRequest, CancellationToken.None)).ConfigureAwait(false);

            // Assert
            _mockRepository.VerifyAll();
            Assert.Equal(string.Format(Strings.InvocationException_HttpNodeJSService_UnexpectedStatusCode, dummyHttpStatusCode), result.Message);
        }

        [Theory]
        [MemberData(nameof(OnConnectionEstablishedMessageReceived_ExtractsEndPoint_Data))]
        public void OnConnectionEstablishedMessageReceived_ExtractsEndPoint(string dummyIP, string dummyPort, string expectedResult)
        {
            // Arrange
            var loggerStringBuilder = new StringBuilder();
            string dummyConnectionEstablishedMessage = $"[Jering.Javascript.NodeJS: HttpVersion - HTTP/1.1 Listening on IP - {dummyIP} Port - {dummyPort}]";
            ExposedHttpNodeJSService testSubject = CreateHttpNodeJSService(loggerStringBuilder: loggerStringBuilder);

            // Act
            testSubject.ExposedOnConnectionEstablishedMessageReceived(dummyConnectionEstablishedMessage);

            // Assert
            Assert.Equal(expectedResult, testSubject._endpoint?.AbsoluteUri);
            Assert.Contains(string.Format(Strings.LogInformation_HttpEndpoint, "HTTP/1.1", expectedResult), loggerStringBuilder.ToString());
        }

        public static IEnumerable<object[]> OnConnectionEstablishedMessageReceived_ExtractsEndPoint_Data()
        {
            return new object[][]
            {
                new object[]{"127.0.0.1", "12345", "http://127.0.0.1:12345/"}, // IPv4, arbitrary port
                new object[]{"::1", "543", "http://[::1]:543/"} // IPv6, arbitrary port
            };
        }

        private class DummyClass
        {
        }

        private ExposedHttpNodeJSService CreateHttpNodeJSService(IOptions<OutOfProcessNodeJSServiceOptions>? outOfProcessNodeHostOptionsAccessor = null,
            IOptions<HttpNodeJSServiceOptions>? httpNodeJSServiceOptionsAccessor = null,
            IHttpContentFactory? httpContentFactory = null,
            IEmbeddedResourcesService? embeddedResourcesService = null,
            IFileWatcherFactory? fileWatcherFactory = null,
            IMonitorService? monitorService = null,
            ITaskService? taskService = null,
            IHttpClientService? httpClientService = null,
            IJsonService? jsonService = null,
            INodeJSProcessFactory? nodeProcessFactory = null,
            ILogger<HttpNodeJSService>? logger = null,
            StringBuilder? loggerStringBuilder = null)
        {
            if (logger == null)
            {
                // Log output
                if (loggerStringBuilder != null)
                {
                    var services = new ServiceCollection();
                    services.AddLogging(lb =>
                    {
                        lb.
                            AddProvider(new StringBuilderProvider(loggerStringBuilder)).
                            AddFilter<StringBuilderProvider>((LogLevel LogLevel) => LogLevel >= LogLevel.Trace);
                    });
                    logger = services.BuildServiceProvider().GetRequiredService<ILogger<HttpNodeJSService>>();
                }
                else
                {
                    Mock<ILogger<HttpNodeJSService>> mockLogger = _mockRepository.Create<ILogger<HttpNodeJSService>>();
                    logger = mockLogger.Object;
                }
            }

            // We call optionsAccessor.Value in constructors, so options accessors must be mocked with Value property setup
            if(httpNodeJSServiceOptionsAccessor == null)
            {
                Mock<IOptions<HttpNodeJSServiceOptions>> mockHttpNodeJSServiceOptionsAccessor = _mockRepository.Create<IOptions<HttpNodeJSServiceOptions>>();
#if NETCOREAPP3_1 || NET5_0_OR_GREATER
                mockHttpNodeJSServiceOptionsAccessor.Setup(m => m.Value).Returns(new HttpNodeJSServiceOptions());
#endif
                httpNodeJSServiceOptionsAccessor = mockHttpNodeJSServiceOptionsAccessor.Object;
            }

            if(outOfProcessNodeHostOptionsAccessor == null)
            {
                Mock<IOptions<OutOfProcessNodeJSServiceOptions>> mockOutOfProcessNodeJSServiceOptions = _mockRepository.Create<IOptions<OutOfProcessNodeJSServiceOptions>>();
                mockOutOfProcessNodeJSServiceOptions.Setup(m => m.Value).Returns(new OutOfProcessNodeJSServiceOptions());
                outOfProcessNodeHostOptionsAccessor = mockOutOfProcessNodeJSServiceOptions.Object;
            }

            return new ExposedHttpNodeJSService(outOfProcessNodeHostOptionsAccessor,
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

        private class ExposedHttpNodeJSService : HttpNodeJSService
        {
            public ExposedHttpNodeJSService(IOptions<OutOfProcessNodeJSServiceOptions> outOfProcessNodeJSServiceOptionsAccessor,
            IOptions<HttpNodeJSServiceOptions> httpNodeJSServiceOptionsAccessor,
            IHttpContentFactory httpContentFactory,
            IEmbeddedResourcesService embeddedResourcesService,
            IFileWatcherFactory fileWatcherFactory,
            IMonitorService monitorService,
            ITaskService taskService,
            IHttpClientService httpClientService,
            IJsonService jsonService,
            INodeJSProcessFactory nodeJSProcessFactory,
            ILogger<HttpNodeJSService> logger) :
                base(outOfProcessNodeJSServiceOptionsAccessor,
                    httpNodeJSServiceOptionsAccessor,
                    httpContentFactory,
                    embeddedResourcesService,
                    fileWatcherFactory,
                    monitorService,
                    taskService,
                    httpClientService,
                    jsonService,
                    nodeJSProcessFactory,
                    logger)
            {
            }

            public Task<(bool, T?)> ExposedTryInvokeAsync<T>(InvocationRequest invocationRequest, CancellationToken cancellationToken)
            {
                return TryInvokeAsync<T>(invocationRequest, cancellationToken);
            }

            public void ExposedOnConnectionEstablishedMessageReceived(string connectionEstablishedMessage)
            {
                OnConnectionEstablishedMessageReceived(connectionEstablishedMessage);
            }
        }
    }
}
