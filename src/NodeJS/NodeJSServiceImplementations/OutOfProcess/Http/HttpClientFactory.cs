using System;
using System.Net.Http;
using System.Threading;

namespace Jering.JavascriptUtils.Node
{
    public class HttpClientFactory : IHttpClientFactory
    {
        public HttpClient _httpClient;

        public HttpClient Create()
        {
            // TODO create lightweight httpmessagehandler
            return _httpClient ?? (_httpClient = new HttpClient()
            {
                Timeout = Timeout.InfiniteTimeSpan // Allow cancellation token to control timeout
            });
        }
    }
}
