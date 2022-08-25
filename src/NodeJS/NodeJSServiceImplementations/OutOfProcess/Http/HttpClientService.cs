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
        public HttpClientService(HttpClient httpClient, IOptions<OutOfProcessNodeJSServiceOptions> outOfProcessNodeJSServiceOptionsAccessor)
        {
            _httpClient = httpClient;

            // Set timeout to invocation timeout + 1000 so that HttpClient does not timeout requests if user specifies a timeout > HttpClient's (100,000)
            OutOfProcessNodeJSServiceOptions options = outOfProcessNodeJSServiceOptionsAccessor.Value;
            httpClient.Timeout = options.InvocationTimeoutMS < 0 ? System.Threading.Timeout.InfiniteTimeSpan : TimeSpan.FromMilliseconds(options.InvocationTimeoutMS + 1000);
        }

        /// <inheritdoc />
        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, HttpCompletionOption completionOption, CancellationToken cancellationToken)
        {
            return _httpClient.SendAsync(request, completionOption, cancellationToken);
        }
    }
}
