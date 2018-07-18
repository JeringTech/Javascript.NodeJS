using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Jering.JavascriptUtils.NodeJS
{
    /// <summary>
    /// Implement this interface and overwrite the default dependency injection service to use custom <see cref="HttpClient"/> provisioning strategies. For example, if you are using ASP.NET 
    /// Core's IHttpClientFactory, you can inject it into a custom implementation of IHttpClientService.
    /// </summary>
    public interface IHttpClientService
    {
        Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content, CancellationToken cancellationToken);
    }
}
