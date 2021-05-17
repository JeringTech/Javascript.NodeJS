using System;
using System.Runtime.Serialization;

namespace Jering.Javascript.NodeJS
{
    /// <summary>
    /// Represents an unrecoverable issue encountered when trying to invoke javascript in NodeJS.
    /// </summary>
    [Serializable]
    public class InvocationException : Exception
    {
        /// <summary>
        /// Creates an <see cref="InvocationException"/>.
        /// </summary>
        public InvocationException()
        {
        }

        /// <summary>
        /// Creates an <see cref="InvocationException"/>.
        /// </summary>
        /// <param name="message">The NodeJS error's description.</param>
        public InvocationException(string message) : base(message)
        {
        }

        /// <summary>
        /// Creates an <see cref="InvocationException"/>.
        /// </summary>
        /// <param name="message">The NodeJS error's description.</param>
        /// <param name="stack">The NodeJS error's stack trace.</param>
        public InvocationException(string message, string? stack)
            : base(message + Environment.NewLine + (stack ?? string.Empty))
        {
        }

        /// <summary>
        /// Creates an <see cref="InvocationException"/>.
        /// </summary>
        /// <param name="info">The data store for serialization/deserialization.</param>
        /// <param name="context">The struct representing the source and destination of a serialized stream.</param>
        protected InvocationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
