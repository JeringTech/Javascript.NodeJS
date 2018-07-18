using System.Net.Http;

namespace Jering.JavascriptUtils.NodeJS
{
    public interface IHttpContentFactory
    {
        HttpContent Create(InvocationRequest invocationRequest);
    }
}
