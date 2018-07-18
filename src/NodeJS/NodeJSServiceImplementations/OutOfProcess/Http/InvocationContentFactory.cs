using System.Net.Http;

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
            return new InvocationContent(_jsonService, invocationRequest);
        }
    }
}
