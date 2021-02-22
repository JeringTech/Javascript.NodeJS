using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

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

        // File watching
        private static readonly string TEMP_WATCH_DIRECTORY = Path.Combine(Path.GetTempPath(), nameof(HttpNodeJSPoolServiceIntegrationTests) + "/"); // Dummy directory to watch for file changes
        private Uri _tempWatchDirectoryUri;

        private readonly ITestOutputHelper _testOutputHelper;

        public HttpNodeJSPoolServiceIntegrationTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

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
            foreach (IGrouping<int, int> group in groupedResults)
            {
                Assert.Equal(expectedNumInvocationsPerProcess, group.Count());
            }
        }

        // FileWatching integration tests aren't for specific HttpNodeJSPoolService methods, rather they test how HttpNodeJSPoolService reacts to 
        // file events.

        // When graceful shutdown is true, we kill initial processes only after their invocations complete.
        [Fact(Timeout = TIMEOUT_MS)]
        public async void FileWatching_RespectsWatchGracefulShutdownOptionWhenItsTrue()
        {
            // Arrange
            const int dummyNumProcesses = 5;
            RecreateWatchDirectory();
            // Create initial module
            string dummylongRunningTriggerPath = new Uri(_tempWatchDirectoryUri, "dummyTriggerFile").AbsolutePath; // fs.watch can't deal with backslashes in paths
            File.WriteAllText(dummylongRunningTriggerPath, string.Empty); // fs.watch returns immediately if path to watch doesn't exist
            string dummyInitialModule = $@"module.exports = {{
    getPid: (callback) => callback(null, process.pid),
    longRunning: (callback) => {{
        fs.watch('{dummylongRunningTriggerPath}', 
            null, 
            () => {{
                callback(null, process.pid);
            }}
        );
    }}
}}";
            string dummyModuleFilePath = new Uri(_tempWatchDirectoryUri, "dummyModule.js").AbsolutePath;
            File.WriteAllText(dummyModuleFilePath, dummyInitialModule);
            var dummyServices = new ServiceCollection();
            dummyServices.Configure<OutOfProcessNodeJSServiceOptions>(options =>
            {
                options.EnableFileWatching = true;
                options.WatchPath = TEMP_WATCH_DIRECTORY;
                // Graceful shutdown is true by default
            });
            HttpNodeJSPoolService testSubject = CreateHttpNodeJSPoolService(dummyNumProcesses, dummyServices);

            // Act
            var getPidTasks = new Task<int>[dummyNumProcesses];
            for (int i = 0; i < dummyNumProcesses; i++)
            {
                getPidTasks[i] = testSubject.InvokeFromFileAsync<int>(dummyModuleFilePath, "getPid");
            }
            Task.WaitAll(getPidTasks);
            int[] initialProcessID1s = getPidTasks.Select(task => task.Result).ToArray();
            var initialProcesses = new Process[dummyNumProcesses];
            for (int i = 0; i < dummyNumProcesses; i++)
            {
                // Create Process instances for initial processes so we can verify that they get killed
                initialProcesses[i] = Process.GetProcessById(initialProcessID1s[i]);
            }
            var longRunningTasks = new Task<int>[dummyNumProcesses];
            for (int i = 0; i < dummyNumProcesses; i++)
            {
                longRunningTasks[i] = testSubject.InvokeFromFileAsync<int>(dummyModuleFilePath, "longRunning");
            }
            File.WriteAllText(dummyModuleFilePath, "module.exports = { getPid: (callback) => callback(null, process.pid) }");// Trigger shifts to new processes
            // Wait for all processes to shift
            var newProcessIDs = new List<int>(dummyNumProcesses);
            do
            {
                int newProcessID = await testSubject.InvokeFromFileAsync<int>(dummyModuleFilePath, "getPid").ConfigureAwait(false);
                if (!initialProcessID1s.Contains(newProcessID) && !newProcessIDs.Contains(newProcessID))
                {
                    newProcessIDs.Add(newProcessID);
                }
            }
            while (newProcessIDs.Count != dummyNumProcesses); // Poll until we've shifted to new processes. If we don't successfully shift to new processes, test will timeout.
            // End long running invocations
            File.AppendAllText(dummylongRunningTriggerPath, "dummyContent");
            Task.WaitAll(longRunningTasks);
            int[] initialProcessID2s = longRunningTasks.Select(task => task.Result).ToArray();
            foreach (Process initialProcess in initialProcesses)
            {
                initialProcess.WaitForExit(); // Should exit after the long running invocations complete
            }

            // Assert
            foreach (int initialProcessID1 in initialProcessID1s)
            {
                Assert.Contains(initialProcessID1, initialProcessID2s); // Long running invocations complete in initial processes
            }
        }

        // When graceful shutdown is false, we kill the initial process immediately. Invocation retry in the new process.
        [Fact(Timeout = TIMEOUT_MS)]
        public async void FileWatching_RespectsWatchGracefulShutdownOptionWhenItsFalse()
        {
            // Arrange
            const int dummyNumProcesses = 5;
            RecreateWatchDirectory();
            // Create initial module
            const string dummyInitialModule = @"module.exports = {
    getPid: (callback) => callback(null, process.pid),
    longRunning: (callback) => setInterval(() => { /* Do nothing */ }, 1000)
}";
            string dummyModuleFilePath = new Uri(_tempWatchDirectoryUri, "dummyModule.js").AbsolutePath;
            File.WriteAllText(dummyModuleFilePath, dummyInitialModule);
            var resultStringBuilder = new StringBuilder();
            var dummyServices = new ServiceCollection();
            dummyServices.Configure<OutOfProcessNodeJSServiceOptions>(options =>
            {
                options.EnableFileWatching = true;
                options.WatchPath = TEMP_WATCH_DIRECTORY;
                options.WatchGracefulShutdown = false;
            });
            HttpNodeJSPoolService testSubject = CreateHttpNodeJSPoolService(dummyNumProcesses, dummyServices, resultStringBuilder);

            // Act
            var getPidTasks = new Task<int>[dummyNumProcesses];
            for (int i = 0; i < dummyNumProcesses; i++)
            {
                getPidTasks[i] = testSubject.InvokeFromFileAsync<int>(dummyModuleFilePath, "getPid");
            }
            Task.WaitAll(getPidTasks);
            int[] intialProcessIDs = getPidTasks.Select(task => task.Result).ToArray();
            var initialProcesses = new Process[dummyNumProcesses];
            for (int i = 0; i < dummyNumProcesses; i++)
            {
                // Create Process instances for initial processes so we can verify that they get killed
                initialProcesses[i] = Process.GetProcessById(intialProcessIDs[i]);
            }
            var longRunningTasks = new Task<int>[dummyNumProcesses];
            for (int i = 0; i < dummyNumProcesses; i++)
            {
                longRunningTasks[i] = testSubject.InvokeFromFileAsync<int>(dummyModuleFilePath, "longRunning");
            }
            // Trigger shifts to new processes
            const string dummyNewModule = @"module.exports = {
    getPid: (callback) => callback(null, process.pid),
    longRunning: (callback) => callback(null, process.pid)
}";
            File.WriteAllText(dummyModuleFilePath, dummyNewModule);
            // Wait for initial processes to exit
            foreach (Process initialProcess in initialProcesses)
            {
                initialProcess.WaitForExit();
            }
            // Wait for all processes to shift
            var newProcessID1s = new List<int>(dummyNumProcesses);
            do
            {
                int newProcessID = await testSubject.InvokeFromFileAsync<int>(dummyModuleFilePath, "getPid").ConfigureAwait(false);
                if (!intialProcessIDs.Contains(newProcessID) && !newProcessID1s.Contains(newProcessID))
                {
                    newProcessID1s.Add(newProcessID);
                }
            }
            while (newProcessID1s.Count != dummyNumProcesses); // Poll until we've shifted to new processes. If we don't successfully shift to new processes, test will timeout.
            // Because graceful shutdown is disabled, long running invocations should fail in initial processes and retry successfully in new processes
            Task.WaitAll(longRunningTasks);
            int[] newProcessID2s = longRunningTasks.Select(task => task.Result).ToArray();

            // Assert
            string resultLog = resultStringBuilder.ToString();
            Assert.Equal(dummyNumProcesses, Regex.Matches(resultLog, nameof(HttpRequestException)).Count); // HttpRequestException thrown when initial processes are killed
            foreach (int newProcessID1 in newProcessID1s)
            {
                Assert.Contains(newProcessID1, newProcessID2s); // Long running invocations should complete in new processes
            }
        }

        /// <summary>
        /// Specify <paramref name="loggerStringBuilder"/> for access to all logging output.
        /// </summary>
        private HttpNodeJSPoolService CreateHttpNodeJSPoolService(int numProcesses, ServiceCollection services = default, StringBuilder loggerStringBuilder = default)
        {
            services ??= new ServiceCollection();
            services.AddNodeJS();
            services.Configure<OutOfProcessNodeJSServiceOptions>(options =>
            {
                options.Concurrency = Concurrency.MultiProcess;
                options.ConcurrencyDegree = numProcesses;
            });

            services.AddLogging(lb =>
            {
                lb.
                    AddProvider(new TestOutputProvider(_testOutputHelper)).
                    AddFilter<TestOutputProvider>((LogLevel loglevel) => loglevel >= LogLevel.Debug);

                if (loggerStringBuilder != null)
                {
                    lb.
                        AddProvider(new StringBuilderProvider(loggerStringBuilder)).
                        AddFilter<StringBuilderProvider>((LogLevel LogLevel) => LogLevel >= LogLevel.Information);
                }
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

            if (_tempWatchDirectoryUri != null)
            {
                TryDeleteWatchDirectory();
            }
        }

        private void RecreateWatchDirectory()
        {
            TryDeleteWatchDirectory();
            Directory.CreateDirectory(TEMP_WATCH_DIRECTORY);
            _tempWatchDirectoryUri = new Uri(TEMP_WATCH_DIRECTORY);
        }

        private void TryDeleteWatchDirectory()
        {
            try
            {
                Directory.Delete(TEMP_WATCH_DIRECTORY, true);
            }
            catch
            {
                // Do nothing
            }
        }
    }
}
