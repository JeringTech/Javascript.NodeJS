using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Jering.Javascript.NodeJS.Tests
{
    /// <summary>
    /// These tests are de facto tests for HttpServer.ts. They serve the additional role of verifying that IPC works.
    /// </summary>
    public class HttpNodeJSServiceIntegrationTests : IDisposable
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private IServiceProvider _serviceProvider;
        private const int _timeoutMS = 60000;
        // Set to true to break in NodeJS (see CreateHttpNodeJSService)
        private const bool _debugNodeJS = false;

        public HttpNodeJSServiceIntegrationTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact(Timeout = _timeoutMS)]
        public async void TryInvokeFromCacheAsync_InvokesJavascriptIfModuleIsCached()
        {
            // Arrange
            const string dummyResultString = "success";
            const string dummyCacheIdentifier = "dummyCacheIdentifier";
            HttpNodeJSService testSubject = CreateHttpNodeJSService();

            // Cache
            await testSubject.
                InvokeFromStringAsync<DummyResult>("module.exports = (callback, resultString) => callback(null, {result: resultString});",
                    dummyCacheIdentifier,
                    args: new[] { dummyResultString }).
                ConfigureAwait(false);

            // Act
            (bool success, DummyResult value) = await testSubject.TryInvokeFromCacheAsync<DummyResult>(dummyCacheIdentifier, args: new[] { dummyResultString }).ConfigureAwait(false);

            // Assert
            Assert.True(success);
            Assert.Equal(dummyResultString, value.Result);
        }

        [Fact(Timeout = _timeoutMS)]
        public async void TryInvokeFromCacheAsync_ReturnsFalseIfModuleIsNotCached()
        {
            // Arrange
            const string dummyResultString = "success";
            const string dummyCacheIdentifier = "dummyCacheIdentifier";
            HttpNodeJSService testSubject = CreateHttpNodeJSService();

            // Act
            (bool success, DummyResult value) = await testSubject.TryInvokeFromCacheAsync<DummyResult>(dummyCacheIdentifier, args: new[] { dummyResultString }).ConfigureAwait(false);

            // Assert
            Assert.False(success);
            Assert.Null(value);
        }

        [Fact(Timeout = _timeoutMS)]
        public async void TryInvokeFromCacheAsync_IsThreadSafe()
        {
            // Arrange
            const string dummyResultString = "success";
            const string dummyCacheIdentifier = "dummyCacheIdentifier";
            HttpNodeJSService testSubject = CreateHttpNodeJSService();

            // Cache
            await testSubject.
                InvokeFromStringAsync<DummyResult>("module.exports = (callback, resultString) => callback(null, {result: resultString});",
                    dummyCacheIdentifier,
                    args: new[] { dummyResultString }).
                ConfigureAwait(false);

            // Act
            var results = new ConcurrentQueue<(bool, DummyResult)>();
            const int numThreads = 5;
            var threads = new List<Thread>();
            for (int i = 0; i < numThreads; i++)
            {
                var thread = new Thread(() => results.Enqueue(testSubject.TryInvokeFromCacheAsync<DummyResult>(dummyCacheIdentifier, args: new[] { dummyResultString }).GetAwaiter().GetResult()));
                threads.Add(thread);
                thread.Start();
            }
            foreach (Thread thread in threads)
            {
                thread.Join();
            }

            // Assert
            Assert.Equal(numThreads, results.Count);
            foreach ((bool success, DummyResult value) in results)
            {
                Assert.True(success);
                Assert.Equal(dummyResultString, value.Result);
            }
        }

        [Fact(Timeout = _timeoutMS)]
        public async void InvokeFromStreamAsync_InvokesJavascript()
        {
            // Arrange
            const string dummyResultString = "success";
            HttpNodeJSService testSubject = CreateHttpNodeJSService();

            DummyResult result;
            using (var memoryStream = new MemoryStream())
            using (var streamWriter = new StreamWriter(memoryStream))
            {
                streamWriter.Write("module.exports = (callback, resultString) => callback(null, {result: resultString});");
                streamWriter.Flush();
                memoryStream.Position = 0;

                // Act
                result = await testSubject.InvokeFromStreamAsync<DummyResult>(memoryStream, args: new[] { dummyResultString }).ConfigureAwait(false);
            }

            // Assert
            Assert.Equal(dummyResultString, result.Result);
        }

        [Fact(Timeout = _timeoutMS)]
        public void InvokeFromStreamAsync_IsThreadSafe()
        {
            // Arrange
            const string dummyModule = "module.exports = (callback, resultString) => callback(null, {result: resultString});";
            const string dummyResultString = "success";
            HttpNodeJSService testSubject = CreateHttpNodeJSService();

            // Act
            var results = new ConcurrentQueue<DummyResult>();
            const int numThreads = 5;
            var threads = new List<Thread>();
            for (int i = 0; i < numThreads; i++)
            {
                var thread = new Thread(() =>
                {
                    using (var memoryStream = new MemoryStream())
                    using (var streamWriter = new StreamWriter(memoryStream))
                    {
                        streamWriter.Write(dummyModule);
                        streamWriter.Flush();
                        memoryStream.Position = 0;
                        results.Enqueue(testSubject.InvokeFromStreamAsync<DummyResult>(memoryStream, args: new[] { dummyResultString }).GetAwaiter().GetResult());
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
            Assert.Equal(numThreads, results.Count);
            foreach (DummyResult result in results)
            {
                Assert.Equal(dummyResultString, result.Result);
            }
        }

        [Fact(Timeout = _timeoutMS)]
        public async void InvokeFromStringAsync_InvokesJavascript()
        {
            // Arrange
            const string dummyResultString = "success";
            HttpNodeJSService testSubject = CreateHttpNodeJSService();

            // Act
            DummyResult result = await testSubject.
                InvokeFromStringAsync<DummyResult>("module.exports = (callback, resultString) => callback(null, {result: resultString});", args: new[] { dummyResultString }).ConfigureAwait(false);

            // Assert
            Assert.Equal(dummyResultString, result.Result);
        }

        [Fact(Timeout = _timeoutMS)]
        public void InvokeFromStringAsync_IsThreadSafe()
        {
            // Arrange
            const string dummyModule = "module.exports = (callback, resultString) => callback(null, {result: resultString});";
            const string dummyResultString = "success";
            HttpNodeJSService testSubject = CreateHttpNodeJSService();

            // Act
            var results = new ConcurrentQueue<DummyResult>();
            const int numThreads = 5;
            var threads = new List<Thread>();
            for (int i = 0; i < numThreads; i++)
            {
                var thread = new Thread(() => results.Enqueue(testSubject.InvokeFromStringAsync<DummyResult>(dummyModule, args: new[] { dummyResultString }).GetAwaiter().GetResult()));
                threads.Add(thread);
                thread.Start();
            }
            foreach (Thread thread in threads)
            {
                thread.Join();
            }

            // Assert
            Assert.Equal(numThreads, results.Count);
            foreach (DummyResult result in results)
            {
                Assert.Equal(dummyResultString, result.Result);
            }
        }

        [Fact(Timeout = _timeoutMS)]
        public async void InvokeFromFileAsync_InvokesJavascript()
        {
            const string dummyResultString = "success";
            HttpNodeJSService testSubject = CreateHttpNodeJSService();

            // Act
            DummyResult result = await testSubject.
                InvokeFromFileAsync<DummyResult>("dummyModule.js", args: new[] { dummyResultString }).ConfigureAwait(false);

            // Assert
            Assert.Equal(dummyResultString, result.Result);
        }

        [Fact(Timeout = _timeoutMS)]
        public void InvokeFromFileAsync_IsThreadSafe()
        {
            // Arrange
            const string dummyModule = "dummyModule.js";
            const string dummyResultString = "success";
            HttpNodeJSService testSubject = CreateHttpNodeJSService();

            // Act
            var results = new ConcurrentQueue<DummyResult>();
            const int numThreads = 5;
            var threads = new List<Thread>();
            for (int i = 0; i < numThreads; i++)
            {
                var thread = new Thread(() => results.Enqueue(testSubject.InvokeFromFileAsync<DummyResult>(dummyModule, args: new[] { dummyResultString }).GetAwaiter().GetResult()));
                threads.Add(thread);
                thread.Start();
            }
            foreach (Thread thread in threads)
            {
                thread.Join();
            }

            // Assert
            Assert.Equal(numThreads, results.Count);
            foreach (DummyResult result in results)
            {
                Assert.Equal(dummyResultString, result.Result);
            }
        }

        [Fact(Timeout = _timeoutMS)]
        public async void AllInvokeMethods_ThrowInvocationExceptionIfModuleHasNoExports()
        {
            // Arrange
            const string dummyModule = "return null;";
            HttpNodeJSService testSubject = CreateHttpNodeJSService();

            // Act
            InvocationException result = await Assert.ThrowsAsync<InvocationException>(() =>
                testSubject.InvokeFromStringAsync<DummyResult>(dummyModule)).
                ConfigureAwait(false);

            // Assert
            Assert.StartsWith($"The module \"{dummyModule}...\" has no exports. Ensure that the module assigns a function or an object containing functions to module.exports.", result.Message);
        }

        [Fact(Timeout = _timeoutMS)]
        public async void AllInvokeMethods_ThrowInvocationExceptionIfThereIsNoModuleExportWithSpecifiedExportName()
        {
            // Arrange
            const string dummyExportName = "dummyExportName";
            const string dummyCacheIdentifier = "dummyCacheIdentifier";
            HttpNodeJSService testSubject = CreateHttpNodeJSService();

            // Act
            InvocationException result = await Assert.ThrowsAsync<InvocationException>(() =>
                testSubject.InvokeFromStringAsync<DummyResult>("module.exports = (callback) => callback(null, {result: 'success'});", dummyCacheIdentifier, dummyExportName)).
                ConfigureAwait(false);

            // Assert
            Assert.StartsWith($"The module {dummyCacheIdentifier} has no export named {dummyExportName}.", result.Message);
        }

        [Fact(Timeout = _timeoutMS)]
        public async void AllInvokeMethods_ThrowInvocationExceptionIfModuleExportWithSpecifiedExportNameIsNotAFunction()
        {
            // Arrange
            const string dummyExportName = "dummyExportName";
            HttpNodeJSService testSubject = CreateHttpNodeJSService();

            // Act
            InvocationException result = await Assert.ThrowsAsync<InvocationException>(() =>
                testSubject.InvokeFromStringAsync<DummyResult>($"module.exports = {{{dummyExportName}: 'not a function'}};", exportName: dummyExportName)).
                ConfigureAwait(false);

            // Assert
            Assert.StartsWith($"The export named {dummyExportName} from module \"module.exports = {{dummyEx...\" is not a function.", result.Message);
        }

        [Fact(Timeout = _timeoutMS)]
        public async void AllInvokeMethods_ThrowInvocationExceptionIfNoExportNameSpecifiedAndModuleExportsIsNotAFunction()
        {
            // Arrange
            HttpNodeJSService testSubject = CreateHttpNodeJSService();

            // Act
            InvocationException result = await Assert.ThrowsAsync<InvocationException>(() =>
                testSubject.InvokeFromStringAsync<DummyResult>("module.exports = {result: 'not a function'};")).
                ConfigureAwait(false);

            // Assert
            Assert.StartsWith("The module \"module.exports = {result:...\" does not export a function.", result.Message);
        }

        [Fact(Timeout = _timeoutMS)]
        public async void AllInvokeMethods_ThrowInvocationExceptionIfInvokedMethodCallsCallbackWithError()
        {
            // Arrange
            const string dummyErrorString = "error";
            HttpNodeJSService testSubject = CreateHttpNodeJSService();

            // Act
            InvocationException result = await Assert.ThrowsAsync<InvocationException>(() =>
                testSubject.InvokeFromStringAsync<DummyResult>("module.exports = (callback, errorString) => callback(new Error(errorString));", args: new[] { dummyErrorString })).
                ConfigureAwait(false);

            // Assert
            Assert.StartsWith(dummyErrorString, result.Message); // Complete message includes the stack
        }

        [Fact(Timeout = _timeoutMS)]
        public async void AllInvokeMethods_InvokeASpecificExportIfExportNameIsProvided()
        {
            // Arrange
            const string dummyResultString = "success";
            const string dummyExportName = "dummyExportName";
            HttpNodeJSService testSubject = CreateHttpNodeJSService();

            // Act
            DummyResult result = await testSubject.
                InvokeFromStringAsync<DummyResult>($"module.exports = {{ {dummyExportName}: (callback, resultString) => callback(null, {{result: resultString}}) }};", exportName: dummyExportName, args: new[] { dummyResultString }).ConfigureAwait(false);

            // Assert
            Assert.Equal(dummyResultString, result.Result);
        }

        // Tests the interaction between the Http server and OutOfProcessNodeJSService.TryCreateMessage
        [Fact(Timeout = _timeoutMS)]
        public async void AllInvokeMethods_ReceiveAndLogMessages()
        {
            // Arrange
            const string dummySinglelineString = "dummySingleLineString";
            const string dummyMultilineString = "dummy\nMultiline\nString\n";
            var resultStringBuilder = new StringBuilder();
            HttpNodeJSService testSubject = CreateHttpNodeJSService(resultStringBuilder);

            // Act
            await testSubject.
                InvokeFromStringAsync<string>($@"module.exports = (callback) => {{ 
    console.log('{dummySinglelineString}'); 
    console.log(`{dummyMultilineString}`);
    callback();

    // Does not work
    // process.stdout.end();
    // process.on(finish, () => callback());

    // Does not work
    // process.stdout.write('', 'utf8', () => callback());
}}").ConfigureAwait(false);
            // Disposing of HttpNodeServices causes Process.Kill and Process.WaitForExit(500) to be called on the node process, this gives it time for it to flush its output.
            //
            // TODO On Linux and macOS, Node.js does not flush stdout completely when Process.Kill is called, even if Process.WaitForExit is called immediately after.
            // Note that console.log just writes to stdout under the hood -https://nodejs.org/docs/latest-v8.x/api/console.html#console_console_log_data_args.
            // There flakiness causes this test to fail randomly. The whole stdout flushing issue seems like a persistent Node.js problem - https://github.com/nodejs/node/issues/6456. 
            // Several attempts have been made to flush/write to stdout synchronously in the js above, to no avail.
            // The following Thread.Sleep(500) works almost all the time, but isn't a clean solution.
            Thread.Sleep(500);
            ((IDisposable)_serviceProvider).Dispose();
            string result = resultStringBuilder.ToString();

            // Assert
            Assert.Equal($"{nameof(LogLevel.Information)}: {dummySinglelineString}\n{nameof(LogLevel.Information)}: {dummyMultilineString}", result, ignoreLineEndingDifferences: true);
        }

        /// <summary>
        /// Specify <paramref name="loggerStringBuilder"/> for access to all logging output.
        /// </summary>
        private HttpNodeJSService CreateHttpNodeJSService(StringBuilder loggerStringBuilder = null)
        {
            var services = new ServiceCollection();
            services.AddNodeJS(); // Default INodeService is HttpNodeService
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

            if (Debugger.IsAttached && _debugNodeJS)
            {
                services.Configure<NodeJSProcessOptions>(options => options.NodeAndV8Options = "--inspect-brk"); // An easy way to step through NodeJS code is to use Chrome. Consider option 1 from this list https://nodejs.org/en/docs/guides/debugging-getting-started/#chrome-devtools-55.
                services.Configure<OutOfProcessNodeJSServiceOptions>(options => options.TimeoutMS = -1);
            }

            _serviceProvider = services.BuildServiceProvider();

            return _serviceProvider.GetRequiredService<INodeJSService>() as HttpNodeJSService;
        }

        private class DummyResult
        {
            public string Result { get; set; }
        }

        public void Dispose()
        {
            ((IDisposable)_serviceProvider).Dispose();
        }
    }
}
