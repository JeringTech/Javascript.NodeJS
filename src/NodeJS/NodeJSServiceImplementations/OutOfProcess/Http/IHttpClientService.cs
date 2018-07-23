using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Jering.JavascriptUtils.NodeJS
{
    /// <summary>
    /// <para>An abstraction for <see cref="HttpClient"/>.</para>
    /// <para>To use a custom <see cref="HttpClient"/> provisioning strategy, implement this interface and overwrite the default DI service for <see cref="IHttpClientService"/>. </para>
    /// <para>For example, if you are using ASP.NET Core's IHttpClientFactory, you can inject it into a custom implementation of <see cref="IHttpClientService"/> and retrieve your
    /// <see cref="HttpClient"/> from it.</para>
    /// </summary>
    public interface IHttpClientService
    {
        /// <summary>
        /// Send a POST request with a cancellation token as an asynchronous operation.
        /// </summary>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="content">The HTTP request content sent to the server.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content, CancellationToken cancellationToken);
    }
}
