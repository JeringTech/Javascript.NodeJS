using System.Net.Http;

namespace Jering.JavascriptUtils.Node
{
    public class HttpClientFactory : IHttpClientFactory
    {
        public HttpClient _httpClient;

        public HttpClient Create()
        {
            // TODO create lightweight httpmessagehandler
            return _httpClient ?? (_httpClient = new HttpClient());
        }
    }
}
