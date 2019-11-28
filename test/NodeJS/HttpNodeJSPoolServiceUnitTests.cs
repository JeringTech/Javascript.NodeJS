using Jering.IocServices.System.Net.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using Xunit;

namespace Jering.Javascript.NodeJS.Tests
{
    public class HttpNodeJSPoolServiceUnitTests
    {
        private readonly MockRepository _mockRepository = new MockRepository(MockBehavior.Default);

        [Fact]
        public void GetHttpNodeJSService_ReturnsEachHttpNodeJSServiceAnEqualNumberOfTimes()
        {
            // Arrange
            const int dummyNumHttpNodeJSServices = 5;
            var dummyHttpNodeJSServices = new List<HttpNodeJSService>();
            for (int i = 0; i < dummyNumHttpNodeJSServices; i++)
            {
                dummyHttpNodeJSServices.Add(CreateHttpNodeJSService());
            }
            using (var testSubject = new HttpNodeJSPoolService(new ReadOnlyCollection<HttpNodeJSService>(dummyHttpNodeJSServices)))
            {
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
        }

        private HttpNodeJSService CreateHttpNodeJSService(IOptions<OutOfProcessNodeJSServiceOptions> outOfProcessNodeHostOptionsAccessor = null,
            IHttpContentFactory httpContentFactory = null,
            IEmbeddedResourcesService embeddedResourcesService = null,
            IHttpClientService httpClientService = null,
            IJsonService jsonService = null,
            INodeJSProcessFactory nodeProcessFactory = null,
            ILoggerFactory loggerFactory = null)
        {
            if (loggerFactory == null)
            {
                Mock<ILogger> mockLogger = _mockRepository.Create<ILogger>();
                Mock<ILoggerFactory> mockLoggerFactory = _mockRepository.Create<ILoggerFactory>();
                mockLoggerFactory.Setup(l => l.CreateLogger(typeof(HttpNodeJSService).FullName)).Returns(mockLogger.Object);
                loggerFactory = mockLoggerFactory.Object;
            }

            return new HttpNodeJSService(outOfProcessNodeHostOptionsAccessor,
                httpContentFactory,
                embeddedResourcesService,
                httpClientService,
                jsonService,
                nodeProcessFactory,
                loggerFactory);
        }
    }
}
