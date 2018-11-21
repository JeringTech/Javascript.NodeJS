using System;
using System.Runtime.Serialization;

namespace Jering.Javascript.NodeJS
{
    /// <summary>
    /// Represents an exception caused by an error caught in NodeJS.
    /// </summary>
    [Serializable]
    public class InvocationException : Exception
    {
        /// <summary>
        /// Creates an <see cref="InvocationException"/> instance.
        /// </summary>
        public InvocationException() : base()
        {
        }

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

        /// <summary>
        /// Creates a <see cref="InvocationException"/> instance.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected InvocationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}