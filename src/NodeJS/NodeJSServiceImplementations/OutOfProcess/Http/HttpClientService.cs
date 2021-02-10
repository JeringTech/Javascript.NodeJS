using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Jering.Javascript.NodeJS
{
    /// <summary>
    /// The default implementation of <see cref="IHttpClientService"/>.
    /// </summary>
    public class HttpClientService : IHttpClientService
    {
        private readonly HttpClient _httpClient;

        /// <inheritdoc />
        public TimeSpan Timeout { get => _httpClient.Timeout; set => _httpClient.Timeout = value; }

        /// <summary>
        /// Creates a <see cref="HttpClientService"/>.
        /// </summary>
        /// <param name="httpClient">The <see cref="HttpClient"/> to send HTTP requests with.</param>
        public HttpClientService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <inheritdoc />
        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, HttpCompletionOption completionOption, CancellationToken cancellationToken)
        {
            return _httpClient.SendAsync(request, completionOption, cancellationToken);
        }
    }
}
