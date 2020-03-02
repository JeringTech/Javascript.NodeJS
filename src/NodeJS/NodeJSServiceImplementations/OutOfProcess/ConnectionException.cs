using System;
using System.Runtime.Serialization;

namespace Jering.Javascript.NodeJS
{
    /// <summary>
    /// Represents an unrecoverable issue encountered when connecting to NodeJS.
    /// </summary>
    [Serializable]
    public class ConnectionException : Exception
    {
        /// <summary>
        /// Creates a <see cref="ConnectionException"/>.
        /// </summary>
        public ConnectionException()
        {
        }

        /// <summary>
        /// Creates a <see cref="ConnectionException"/>.
        /// </summary>
        /// <param name="message">The connection issue's description.</param>
        public ConnectionException(string message) : base(message)
        {
        }

        /// <summary>
        /// Creates a <see cref="ConnectionException"/>.
        /// </summary>
        /// <param name="info">The data store for serialization/deserialization.</param>
        /// <param name="context">The struct representing the source and destination of a serialized stream.</param>
        protected ConnectionException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        /// <summary>
        /// Creates a <see cref="ConnectionException"/>.
        /// </summary>
        /// <param name="message">The connection issue's description.</param>
        /// <param name="innerException">The inner exception.</param>
        public ConnectionException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
