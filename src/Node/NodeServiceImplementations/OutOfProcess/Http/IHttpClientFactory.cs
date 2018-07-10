using System.Net.Http;

namespace Jering.JavascriptUtils.Node
{
    public interface IHttpClientFactory
    {
        HttpClient Create();
    }
}
