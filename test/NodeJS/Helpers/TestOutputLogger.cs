using Microsoft.Extensions.Logging;
using System;
using Xunit.Abstractions;

namespace Jering.Javascript.NodeJS.Tests
{
    public class TestOutputLogger : ILogger
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public TestOutputLogger(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null!; // We're not using scopes
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            // Thread safe - https://github.com/xunit/xunit/blob/c54cc52ffb275c81afed022521870193bbca6c39/src/xunit.execution/Sdk/Frameworks/TestOutputHelper.cs
            _testOutputHelper.WriteLine($"{logLevel}: {formatter(state, exception)}");
        }
    }
}
