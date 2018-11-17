using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;
using Xunit.Abstractions;

namespace Jering.Javascript.NodeJS.Tests
{
    public class OutOfProcessNodeJSServiceIntegrationTests : IDisposable
    {
        private IServiceProvider _serviceProvider;
        private const int _numThreads = 5; // Arbitrary
        private static readonly CountdownEvent _countdownEvent = new CountdownEvent(_numThreads); // Only used by 1 test
        private readonly ITestOutputHelper _testOutputHelper;

        public OutOfProcessNodeJSServiceIntegrationTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async void TryInvokeCoreAsync_ThrowsObjectDisposedExceptionIfObjectHasBeenDisposedOf()
        {
            // Arrange
            DummyExceptionThrowingNodeJSService testSubject = CreateDummyNodeService<DummyExceptionThrowingNodeJSService>();
            testSubject.Dispose();

            // Act and assert
            ObjectDisposedException result = await Assert.ThrowsAsync<ObjectDisposedException>(async () => await testSubject.TryInvokeCoreAsync<string>(null, CancellationToken.None).ConfigureAwait(false)).ConfigureAwait(false);
            Assert.Equal($"Cannot access a disposed object.\nObject name: '{nameof(OutOfProcessNodeJSService)}'.", result.Message, ignoreLineEndingDifferences: true);
        }

        // TODO are there better techniques for testing multi-threaded code? 
        [Fact]
        public void TryInvokeCoreAsync_HandlesSimultaneousRequestsFromDifferentThreads()
        {
            // Arrange
            DummyThreadCounterNodeJSService testSubject = CreateDummyNodeService<DummyThreadCounterNodeJSService>();
            var threads = new List<Thread>();

            // Act
            for (int i = 0; i < _numThreads; i++)
            {
                var thread = new Thread(async () => await testSubject.TryInvokeCoreAsync<string>(null, CancellationToken.None).ConfigureAwait(false));

                thread.Start();
                threads.Add(thread);
            }

            _countdownEvent.Wait();

            // Assert
            // If DummyNodeJSService.TryInvokeAsync gets called by each thread, it means the NodeJS process was successfully started by a thread and that the other 
            // threads were successfully resumed once the process was live.
            Assert.Equal(0, _countdownEvent.CurrentCount);
        }

        [Fact]
        public async void TryInvokeCoreAsync_RetriesThenThrowsInvocationExceptionIfAnInvocationTimesOutRepeatedly()
        {
            // Arrange
            var resultStringBuilder = new StringBuilder();
            IServiceCollection dummyServices = new ServiceCollection();
            dummyServices.Configure<OutOfProcessNodeJSServiceOptions>(options => options.NumRetries = 1);
            DummyExceptionThrowingNodeJSService testSubject = CreateDummyNodeService<DummyExceptionThrowingNodeJSService>(dummyServices, resultStringBuilder);

            // Act and assert
            InvocationException result = await Assert.
                ThrowsAsync<InvocationException>(async () => await testSubject.TryInvokeCoreAsync<string>(null, CancellationToken.None).ConfigureAwait(false)).ConfigureAwait(false);
            Assert.Equal(string.Format(Strings.InvocationException_OutOfProcessNodeJSService_InvocationTimedOut,
                    new OutOfProcessNodeJSServiceOptions().TimeoutMS, // Get the default
                    nameof(OutOfProcessNodeJSServiceOptions.TimeoutMS),
                    nameof(OutOfProcessNodeJSServiceOptions)),
                    result.Message);
            string logResult = resultStringBuilder.ToString();
            Assert.StartsWith(string.Format(Strings.LogError_InvocationAttemptFailed, 1, new OperationCanceledException().ToString()), logResult);
        }

        // On the first javascript invocation, an attempt is made to connect to node. If this attempt timesout, an InvocationException should be thrown.
        // Note: OutOfProcessNodeJSService combines the cancellation token that we pass with a cancellation token that has the timeout specified in OutOfProcessNodeJSServiceOptions.
        // The combined cancellation token cancels when any of its underlying tokens cancel, so we can get the connection to cancel immediately.
        [Fact]
        public async void TryInvokeCoreAsync_ThrowsInvocationExceptionIfANodeConnectionAttemptTimesout()
        {
            // Arrange
            IServiceCollection dummyServices = new ServiceCollection();
            dummyServices.Configure<OutOfProcessNodeJSServiceOptions>(options => options.NumRetries = 0);
            DummyNeverConnectingNodeJSService testSubject = CreateDummyNodeService<DummyNeverConnectingNodeJSService>(dummyServices);
            var dummyTimeoutCTS = new CancellationTokenSource(200); // Arbitrary delay

            // Act and assert
            InvocationException result = await Assert.
                ThrowsAsync<InvocationException>(async () => await testSubject.TryInvokeCoreAsync<string>(null, dummyTimeoutCTS.Token).ConfigureAwait(false)).ConfigureAwait(false);
            Assert.Equal(string.Format(Strings.InvocationException_OutOfProcessNodeJSService_ConnectionTimedOut,
                new OutOfProcessNodeJSServiceOptions().TimeoutMS, // Get the default
                "False",
                "n/a"),
                result.Message);
        }

        private class DummyNeverConnectingNodeJSService : OutOfProcessNodeJSService
        {
            public DummyNeverConnectingNodeJSService(INodeJSProcessFactory nodeProcessFactory,
                ILogger<DummyExceptionThrowingNodeJSService> nodeServiceLogger,
                IOptions<OutOfProcessNodeJSServiceOptions> optionsAccessor,
                IEmbeddedResourcesService embeddedResourcesService) :
                base(nodeProcessFactory, nodeServiceLogger, optionsAccessor, embeddedResourcesService, typeof(HttpNodeJSService).GetTypeInfo().Assembly, "HttpServer.js")
            {
            }

            protected override void OnConnectionEstablishedMessageReceived(string connectionEstablishedMessage)
            {
                // Do nothing
            }

            protected override Task<(bool, T)> TryInvokeAsync<T>(InvocationRequest invocationRequest, CancellationToken cancellationToken)
            {
                // Do nothing
                return Task.FromResult(default((bool, T)));
            }

            internal override void ConnectToInputOutputStreams(Process nodeProcess)
            {
                // Do nothing so that we never connect
            }
        }

        private class DummyExceptionThrowingNodeJSService : OutOfProcessNodeJSService
        {
            public DummyExceptionThrowingNodeJSService(INodeJSProcessFactory nodeProcessFactory,
                ILogger<DummyExceptionThrowingNodeJSService> nodeServiceLogger,
                IOptions<OutOfProcessNodeJSServiceOptions> optionsAccessor,
                IEmbeddedResourcesService embeddedResourcesService) :
                base(nodeProcessFactory, nodeServiceLogger, optionsAccessor, embeddedResourcesService, typeof(HttpNodeJSService).GetTypeInfo().Assembly, "HttpServer.js")
            {
            }

            protected override void OnConnectionEstablishedMessageReceived(string connectionEstablishedMessage)
            {
                // Do nothing
            }

            protected override Task<(bool, T)> TryInvokeAsync<T>(InvocationRequest invocationRequest, CancellationToken cancellationToken)
            {
                throw new OperationCanceledException();
            }
        }

        private class DummyThreadCounterNodeJSService : OutOfProcessNodeJSService
        {
            public DummyThreadCounterNodeJSService(INodeJSProcessFactory nodeProcessFactory,
                ILogger<DummyThreadCounterNodeJSService> nodeServiceLogger,
                IOptions<OutOfProcessNodeJSServiceOptions> optionsAccessor,
                IEmbeddedResourcesService embeddedResourcesService) :
                base(nodeProcessFactory, nodeServiceLogger, optionsAccessor, embeddedResourcesService, typeof(HttpNodeJSService).GetTypeInfo().Assembly, "HttpServer.js")
            {
            }

            protected override void OnConnectionEstablishedMessageReceived(string connectionEstablishedMessage)
            {
                // Do nothing
            }

            protected override Task<(bool, T)> TryInvokeAsync<T>(InvocationRequest invocationRequest, CancellationToken cancellationToken)
            {
                _countdownEvent.Signal();
                return Task.FromResult((true, default(T)));
            }
        }

        private T CreateDummyNodeService<T>(IServiceCollection services = null, StringBuilder loggerStringBuilder = null) where T : class, INodeJSService
        {
            (services ?? (services = new ServiceCollection())).AddNodeJS();
            services.AddLogging(lb =>
            {
                lb.
                    AddProvider(new TestOutputProvider(_testOutputHelper)).
                    AddFilter<TestOutputProvider>((LogLevel loglevel) => loglevel >= LogLevel.Debug);

                if (loggerStringBuilder != null)
                {
                    lb.
                        AddProvider(new StringBuilderProvider(loggerStringBuilder)).
                        AddFilter<StringBuilderProvider>((LogLevel LogLevel) => LogLevel >= LogLevel.Warning);
                }
            });
            services.AddSingleton<INodeJSService, T>(); // Override default service

            _serviceProvider = services.BuildServiceProvider();

            return _serviceProvider.GetRequiredService<INodeJSService>() as T;
        }

        public void Dispose()
        {
            ((IDisposable)_serviceProvider).Dispose();
        }
    }
}
