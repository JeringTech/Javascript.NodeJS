using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Xunit;

namespace Jering.Javascript.NodeJS.Tests
{
    /// <summary>
    /// <see cref="HttpNodeJSPoolService"/> is a pool of <see cref="HttpNodeJSService"/> instances. It's purpose is to delegate invocations to
    /// NodeJS processes evenly, so that is what we test here.
    /// </summary>
    public class HttpNodeJSPoolServiceIntegrationTests : IDisposable
    {
        private IServiceProvider _serviceProvider;
        // Set to true to break in NodeJS (see CreateHttpNodeJSPoolService)
        private const bool DEBUG_NODEJS = false;
        // Set to -1 when debugging in NodeJS
        private const int TIMEOUT_MS = 60000;

        [Fact(Timeout = TIMEOUT_MS)]
        public void AllInvokeMethods_InvokeJavascriptInMultipleProcesses()
        {
            // Arrange
            const int dummyNumProcesses = 5;
            const string dummyModule = @"module.exports = (callback) => {
    callback(null, process.pid);
}";
            HttpNodeJSPoolService testSubject = CreateHttpNodeJSPoolService(dummyNumProcesses);

            // Act
            var results = new ConcurrentBag<int>();
            const int numThreads = 5;
            const int numInvocationsPerThread = 10;
            var threads = new List<Thread>();
            for (int i = 0; i < numThreads; i++)
            {
                var thread = new Thread(() =>
                {
                    for (int j = 0; j < numInvocationsPerThread; j++)
                    {
                        results.Add(testSubject.InvokeFromStringAsync<int>(dummyModule, "dummyCacheIdentifier").GetAwaiter().GetResult());
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
            const int expectedNumInvocationsPerProcess = numThreads * numInvocationsPerThread / dummyNumProcesses;
            IEnumerable<IGrouping<int, int>> groupedResults = results.GroupBy(pid => pid);
            Assert.Equal(dummyNumProcesses, groupedResults.Count());
            foreach(IGrouping<int, int> group in groupedResults)
            {
                Assert.Equal(expectedNumInvocationsPerProcess, group.Count());
            }
        }

        /// <summary>
        /// Specify <paramref name="loggerStringBuilder"/> for access to all logging output.
        /// </summary>
        private HttpNodeJSPoolService CreateHttpNodeJSPoolService(int numProcesses)
        {
            var services = new ServiceCollection();
            services.AddNodeJS();
            services.Configure<OutOfProcessNodeJSServiceOptions>(options =>
            {
                options.Concurrency = Concurrency.MultiProcess;
                options.ConcurrencyDegree = numProcesses;
            });

            if (Debugger.IsAttached && DEBUG_NODEJS)
            {
                services.Configure<NodeJSProcessOptions>(options => options.NodeAndV8Options = "--inspect-brk"); // An easy way to step through NodeJS code is to use Chrome. Consider option 1 from this list https://nodejs.org/en/docs/guides/debugging-getting-started/#chrome-devtools-55.
                services.Configure<OutOfProcessNodeJSServiceOptions>(options => options.TimeoutMS = -1);
            }

            _serviceProvider = services.BuildServiceProvider();

            return _serviceProvider.GetRequiredService<INodeJSService>() as HttpNodeJSPoolService;
        }

        public void Dispose()
        {
            ((IDisposable)_serviceProvider).Dispose();
        }
    }
}
