using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Jering.Javascript.NodeJS.Tests
{
    public sealed class TestOutputProvider : ILoggerProvider
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public TestOutputProvider(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new TestOutputLogger(_testOutputHelper);
        }

        public void Dispose()
        {
            // Do nothing
        }
    }
}
