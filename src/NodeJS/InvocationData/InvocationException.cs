using System;

namespace Jering.JavascriptUtils.Node
{
    /// <summary>
    /// Represents an exception caused by invoking Node.js code.
    /// </summary>
    public class InvocationException : Exception
    {
        /// <summary>
        /// If true, indicates that the invocation failed because the Node.js instance could not be reached. For example,
        /// it might have already shut down or previously crashed.
        /// </summary>
        public bool NodeInstanceUnavailable { get; }

        /// <summary>
        /// Creates a new instance of <see cref="InvocationException"/>.
        /// </summary>
        /// <param name="message">A description of the exception.</param>
        public InvocationException(string message) : base(message)
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="InvocationException"/>.
        /// </summary>
        /// <param name="message">A description of the exception.</param>
        /// <param name="stack">Node.js stack trace, representing the exception.</param>
        public InvocationException(string message, string stack)
            : base(message + Environment.NewLine + stack)
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="InvocationException"/>.
        /// </summary>
        /// <param name="message">A description of the exception.</param>
        /// <param name="stack">Node.js stack trace, representing the exception.</param>
        /// <param name="nodeInstanceUnavailable">Specifies a value for the <see cref="NodeInstanceUnavailable"/> flag.</param>
        public InvocationException(string message, string stack, bool nodeInstanceUnavailable)
            : this(message, stack)
        {
            NodeInstanceUnavailable = nodeInstanceUnavailable;
        }
    }
}