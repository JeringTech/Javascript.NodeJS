using Microsoft.Extensions.Logging;
using System;
using System.Text;

namespace Jering.Javascript.NodeJS.Tests
{
    public class StringBuilderLogger : ILogger
    {
        private readonly object _lock = new object();
        private readonly StringBuilder _stringBuilder;

        public StringBuilderLogger(StringBuilder stringBuilder)
        {
            _stringBuilder = stringBuilder;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            lock (_lock)
            {
                _stringBuilder.Append(logLevel.ToString()).Append(": ").AppendLine(formatter(state, exception));
            }
        }
    }
}
