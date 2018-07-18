using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Jering.JavascriptUtils.NodeJS
{
    /// <summary>
    /// <para>To use custom <see cref="HttpClient"/> provisioning strategies, implement this interface and overwrite the default DI service for <see cref="IHttpClientService"/>. </para>
    /// <para>For example, if you are using ASP.NET Core's IHttpClientFactory, you can inject it into a custom implementation of <see cref="IHttpClientService"/>.</para>
    /// </summary>
    public interface IHttpClientService
    {
        Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content, CancellationToken cancellationToken);
    }
}
