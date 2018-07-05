using Microsoft.Extensions.Options;

namespace Jering.JavascriptUtils.Node.HostingModels
{
    public class HttpNodeInstanceFactory : INodeInstanceFactory
    {
        private readonly HttpNodeInstanceOptions _nodeInstanceOptions;

        public HttpNodeInstanceFactory(IOptions<HttpNodeInstanceOptions> optionsAccessor)
        {
            _nodeInstanceOptions = optionsAccessor.Value;
        }

        public INodeInstance Create()
        {
            return new HttpNodeInstance(_nodeInstanceOptions);
        }
    }
}
