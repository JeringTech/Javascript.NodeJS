using Jering.IocServices.System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace Jering.Javascript.NodeJS.Tests
{
    public class NodeJSServiceCollectionExtensionsUnitTests
    {
        [Fact]
        public void AddNodeJS_AddsServices()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddNodeJS();

            // Assert
            IServiceProvider serviceProvider = services.BuildServiceProvider();
            INodeJSService _ = serviceProvider.GetRequiredService<INodeJSService>(); // As long as this doesn't throw, the dependency graph is valid
        }

        [Theory]
        [MemberData(nameof(IHttpClientServiceFactory_SetsTimeout_Data))]
        public void IHttpClientServiceFactory_SetsTimeout(int dummyTimeoutMS, TimeSpan expectedTimeoutMS)
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddNodeJS();
            services.Configure<OutOfProcessNodeJSServiceOptions>(options => options.TimeoutMS = dummyTimeoutMS);
            IServiceProvider serviceProvider = services.BuildServiceProvider();

            // Act
            var result = NodeJSServiceCollectionExtensions.IHttpClientServiceFactory(serviceProvider) as HttpClientService;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedTimeoutMS, result.Timeout);
        }

        public static IEnumerable<object[]> IHttpClientServiceFactory_SetsTimeout_Data()
        {
            return new object[][]
            {
                // -1 == infinite
                new object[]{ -1, Timeout.InfiniteTimeSpan},
                // All other values == value + 1000
                new object[]{ 0, TimeSpan.FromMilliseconds(1000)},
                new object[]{ 1000, TimeSpan.FromMilliseconds(2000)}
            };
        }

        [Theory]
        [MemberData(nameof(INodeJSServiceFactory_CreatesAHttpNodeJSPoolServiceIfConcurrencyIsMultiProcessAndMoreThan1ProcessesIsRequested_Data))]
        public void INodeJSServiceFactory_CreatesAHttpNodeJSPoolServiceIfConcurrencyIsMultiProcessAndMoreThan1ProcessesIsRequested(int dummyConcurrencyDegree,
            int dummyNumLogicalProcessors,
            int expectedSize)
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddNodeJS();
            services.AddSingleton(typeof(IEnvironmentService), new DummyEnvironmentService(dummyNumLogicalProcessors));
            services.Configure<OutOfProcessNodeJSServiceOptions>(options =>
            {
                options.Concurrency = Concurrency.MultiProcess;
                options.ConcurrencyDegree = dummyConcurrencyDegree;
            });
            IServiceProvider serviceProvider = services.BuildServiceProvider();

            // Act
            var result = NodeJSServiceCollectionExtensions.INodeJSServiceFactory(serviceProvider) as HttpNodeJSPoolService;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedSize, result.Size);
        }

        public static IEnumerable<object[]> INodeJSServiceFactory_CreatesAHttpNodeJSPoolServiceIfConcurrencyIsMultiProcessAndMoreThan1ProcessesIsRequested_Data()
        {
            return new object[][]
            {
                // If concurrency degree is <= 0, number of processes == number of logical processors
                new object[]{ -1, 5, 5 },
                new object[]{ 0, 8, 8 },
                // If concurrency degree is > 1, number of processes == specified number
                new object[]{ 5, 1, 5 }
            };
        }

        [Theory]
        [MemberData(nameof(INodeJSServiceFactory_CreatesAHttpNodeJSServiceIfConcurrencyIsNoneOrOnly1ProcessIsRequested_Data))]
        public void INodeJSServiceFactory_CreatesAHttpNodeJSServiceIfConcurrencyIsNoneOrOnly1ProcessIsRequested(Concurrency dummyConcurrency,
            int dummyNumLogicalProcessors,
            int dummyConcurrencyDegree)
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddNodeJS();
            services.AddSingleton(typeof(IEnvironmentService), new DummyEnvironmentService(dummyNumLogicalProcessors));
            services.Configure<OutOfProcessNodeJSServiceOptions>(options =>
            {
                options.Concurrency = dummyConcurrency;
                options.ConcurrencyDegree = dummyConcurrencyDegree;
            });
            IServiceProvider serviceProvider = services.BuildServiceProvider();

            // Act
            var result = NodeJSServiceCollectionExtensions.INodeJSServiceFactory(serviceProvider) as HttpNodeJSService;

            // Assert
            Assert.NotNull(result);
        }

        public static IEnumerable<object[]> INodeJSServiceFactory_CreatesAHttpNodeJSServiceIfConcurrencyIsNoneOrOnly1ProcessIsRequested_Data()
        {
            return new object[][]
            {
                // So long as Concurrency is None, creates a HttpNodeJSService
                new object[]{Concurrency.None, 8, 1},
                new object[]{Concurrency.None, 1, 0},
                // If Concurrency is MultiProcess but ConcurrencyDegree is 1 creates a HttpNodeJSService
                new object[]{Concurrency.MultiProcess, 5, 1},
                // If Concurrency is MultiProcess and ConcurrencyDegree is less than 1 but machine only has 1 logical processor, creates a HttpNodeJSService
                new object[]{Concurrency.MultiProcess, 1, 0}
            };
        }

        public class DummyEnvironmentService : IEnvironmentService
        {
            public DummyEnvironmentService(int dummyProcessorCount)
            {
                ProcessorCount = dummyProcessorCount;
            }

            public int ProcessorCount { get; }
        }
    }
}
