using System.Net.Http;

namespace Jering.JavascriptUtils.NodeJS
{
    /// <summary>
    /// To use a custom <see cref="HttpContent"/> implementation, implement this interface and overwrite the default DI service for <see cref="IHttpContentFactory"/>.
    /// </summary>
    public interface IHttpContentFactory
    {
        HttpContent Create(InvocationRequest invocationRequest);
    }
}
