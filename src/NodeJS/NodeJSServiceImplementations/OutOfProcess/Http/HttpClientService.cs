using Microsoft.Extensions.Options;
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
        /// <param name="outOfProcessNodeJSServiceOptionsAccessor">The <see cref="OutOfProcessNodeJSServiceOptions"/> accessor.</param>
        public HttpClientService(HttpClient httpClient,
            IOptions<OutOfProcessNodeJSServiceOptions> outOfProcessNodeJSServiceOptionsAccessor)
        {
            _httpClient = httpClient;

            // Configure
            OutOfProcessNodeJSServiceOptions outOfProcessNodeJSServiceOptions = outOfProcessNodeJSServiceOptionsAccessor.Value;
            httpClient.Timeout = outOfProcessNodeJSServiceOptions.TimeoutMS == -1 ? System.Threading.Timeout.InfiniteTimeSpan :
                TimeSpan.FromMilliseconds(outOfProcessNodeJSServiceOptions.TimeoutMS + 1000);
        }

        /// <inheritdoc />
        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, HttpCompletionOption completionOption, CancellationToken cancellationToken)
        {
            return _httpClient.SendAsync(request, completionOption, cancellationToken);
        }
    }
}
