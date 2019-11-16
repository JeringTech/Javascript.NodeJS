using System.Net.Http;

namespace Jering.Javascript.NodeJS
{
    /// <summary>
    /// <para>An abstraction for <see cref="HttpContent"/> creation.</para>
    /// <para>To use a custom <see cref="HttpContent"/> implementation, implement this interface and overwrite the default DI service for <see cref="IHttpContentFactory"/>.</para>
    /// </summary>
    public interface IHttpContentFactory
    {
        /// <summary>
        /// Creates an instance of a <see cref="HttpContent"/> implementation.
        /// </summary>
        /// <param name="invocationRequest">The invocation request to transmit over Http.</param>
        HttpContent Create(InvocationRequest invocationRequest);
    }
}
