using System.Net.Http;

namespace Jering.JavascriptUtils.Node
{
    public class HttpClientFactory : IHttpClientFactory
    {
        public HttpClient _httpClient;

        public HttpClient Create()
        {
            return _httpClient ?? (_httpClient = new HttpClient());
        }
    }
}
