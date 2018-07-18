using System.Net.Http;
using System.Net.Http.Headers;

namespace Jering.JavascriptUtils.NodeJS
{
    public class InvocationContentFactory : IHttpContentFactory
    {
        private readonly IJsonService _jsonService;

        public InvocationContentFactory(IJsonService jsonService)
        {
            _jsonService = jsonService;
        }

        public HttpContent Create(InvocationRequest invocationRequest)
        {
            MediaTypeHeaderValue mediaTypeHeaderValue = invocationRequest.ModuleSourceType == ModuleSourceType.Stream ?
                new MediaTypeHeaderValue("multipart/mixed") :
                new MediaTypeHeaderValue("application/json");

            return new InvocationContent(_jsonService, invocationRequest, mediaTypeHeaderValue);
        }
    }
}
