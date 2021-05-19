using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Xunit;

namespace Jering.Javascript.NodeJS.Tests
{
    [Collection(nameof(StaticNodeJSService))]
    public class StaticNodeJSServiceIntegrationTests
    {
        private const int TIMEOUT_MS = 60000;

        [Fact(Timeout = TIMEOUT_MS)]
        public async void DisposeServiceProvider_RestartsNodeJSProcess()
        {
            // Arrange
            const string dummyTestVariableName = "TEST_VARIABLE";
            const string dummyTestVariableValue = "testVariableValue";
            StaticNodeJSService.Configure<NodeJSProcessOptions>(options => options.EnvironmentVariables.Add(dummyTestVariableName, dummyTestVariableValue));
            string? result1 = await StaticNodeJSService.
                InvokeFromStringAsync<string>($"module.exports = (callback) => callback(null, process.env.{dummyTestVariableName});").ConfigureAwait(false);

            // Act
            StaticNodeJSService.DisposeServiceProvider(); // Dispose, environment variable should not be set in the next call

            // Assert
            string? result2 = await StaticNodeJSService.
                InvokeFromStringAsync<string>($"module.exports = (callback) => callback(null, process.env.{dummyTestVariableName});").ConfigureAwait(false);
            Assert.Equal(dummyTestVariableValue, result1);
            Assert.Equal(string.Empty, result2);
        }

        [Fact(Timeout = TIMEOUT_MS)]
        public async void Configure_RestartsNodeJSProcessWithNewOptions()
        {
            // Arrange
            const string dummyTestVariableName = "TEST_VARIABLE";
            const string dummyTestVariableValue1 = "testVariableValue1";
            const string dummyTestVariableValue2 = "testVariableValue2";
            StaticNodeJSService.Configure<NodeJSProcessOptions>(options => options.EnvironmentVariables.Add(dummyTestVariableName, dummyTestVariableValue1));
            string? result1 = await StaticNodeJSService.
                InvokeFromStringAsync<string>($"module.exports = (callback) => callback(null, process.env.{dummyTestVariableName});").ConfigureAwait(false);

            // Act
            StaticNodeJSService.Configure<NodeJSProcessOptions>(options => options.EnvironmentVariables.Add(dummyTestVariableName, dummyTestVariableValue2));

            // Assert
            string? result2 = await StaticNodeJSService.
                InvokeFromStringAsync<string>($"module.exports = (callback) => callback(null, process.env.{dummyTestVariableName});").ConfigureAwait(false);
            Assert.Equal(dummyTestVariableValue1, result1);
            Assert.Equal(dummyTestVariableValue2, result2);
        }

        [Fact(Timeout = TIMEOUT_MS)]
        public async void SetServices_RestartsNodeJSProcessWithNewServices()
        {
            // Arrange
            const string dummyTestVariableName = "TEST_VARIABLE_1";
            const string dummyTestVariableValue1 = "testVariableValue1";
            const string dummyTestVariableValue2 = "testVariableValue2";
            StaticNodeJSService.Configure<NodeJSProcessOptions>(options => options.EnvironmentVariables.Add(dummyTestVariableName, dummyTestVariableValue1));
            string? result1 = await StaticNodeJSService.
                InvokeFromStringAsync<string>($"module.exports = (callback) => callback(null, process.env.{dummyTestVariableName});").ConfigureAwait(false);
            var dummyServices = new ServiceCollection();
            dummyServices.
                AddNodeJS().
                Configure<NodeJSProcessOptions>(options => options.EnvironmentVariables.Add(dummyTestVariableName, dummyTestVariableValue2));

            // Act
            StaticNodeJSService.SetServices(dummyServices);

            // Assert
            string? result2 = await StaticNodeJSService.
                InvokeFromStringAsync<string>($"module.exports = (callback) => callback(null, process.env.{dummyTestVariableName});").ConfigureAwait(false);
            Assert.Equal(dummyTestVariableValue1, result1);
            Assert.Equal(dummyTestVariableValue2, result2);
        }

        // This test ensures that private method GetOrCreateNodeJSService properly handles multiple concurrent requests
        [Fact(Timeout = TIMEOUT_MS)]
        public void AllInvokeMethods_AreThreadSafe()
        {
            // Arrange
            StaticNodeJSService.DisposeServiceProvider(); // In case previous test registered a custom service

            // Act
            var results = new ConcurrentQueue<string?>();
            const int numThreads = 5;
            var threads = new List<Thread>();
            for (int i = 0; i < numThreads; i++)
            {
                var thread = new Thread(() => results.Enqueue(StaticNodeJSService.InvokeFromStringAsync<string>("module.exports = (callback) => callback(null, process.pid);").GetAwaiter().GetResult()));
                threads.Add(thread);
                thread.Start();
            }
            foreach (Thread thread in threads)
            {
                thread.Join();
            }

            // Assert
            Assert.Equal(numThreads, results.Count);
            Assert.Single(results.Distinct()); // All invocations should run in process started by first invocation
        }
    }
}
