using System;

namespace Jering.JavascriptUtils.NodeJS
{
    /// <summary>
    /// Represents an exception caused by an error caught in NodeJS.
    /// </summary>
    public class InvocationException : Exception
    {
        /// <summary>
        /// Creates an <see cref="InvocationException"/> instance.
        /// </summary>
        /// <param name="message">A description of the NodeJS error.</param>
        public InvocationException(string message) : base(message)
        {
        }

        /// <summary>
        /// Creates an <see cref="InvocationException"/> instance.
        /// </summary>
        /// <param name="message">A description of the NodeJS error.</param>
        /// <param name="stack">The NodeJS error's stack trace.</param>
        public InvocationException(string message, string stack)
            : base(message + Environment.NewLine + stack)
        {
        }
    }
}