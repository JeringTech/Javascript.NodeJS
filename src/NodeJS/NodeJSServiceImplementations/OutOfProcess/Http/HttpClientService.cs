using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Jering.JavascriptUtils.NodeJS
{
    public class HttpClientService : IHttpClientService, IDisposable
    {
        private readonly HttpClient _httpClient;
        private bool _disposed;

        public HttpClientService()
        {
            _httpClient = new HttpClient()
            {
                Timeout = Timeout.InfiniteTimeSpan // Allow cancellation token to control timeout
            };
        }

        public Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content, CancellationToken cancellationToken)
        {
            return _httpClient.PostAsync(requestUri, content, cancellationToken);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _httpClient.Dispose();
                _disposed = true;
            }
        }
    }
}
