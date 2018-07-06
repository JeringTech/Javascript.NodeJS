using Jering.JavascriptUtils.Node.Node.OutOfProcessHosts;
using Jering.JavascriptUtils.Node.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jering.JavascriptUtils.Node.HostingModels
{
    public class HttpNodeHostFactory : INodeHostFactory
    {
        private readonly OutOfProcessNodeHostOptions _outOfProcessNodeHostOptions;
        private readonly IEmbeddedResourcesService _embeddedResourcesService;
        private readonly INodeProcessFactory _nodeProcessFactory;
        private readonly ILogger _nodeOutputLogger;

        public HttpNodeHostFactory(IOptions<OutOfProcessNodeHostOptions> outOfProcessNodeHostOptionsAccessor,
            IEmbeddedResourcesService embeddedResourcesService,
            INodeProcessFactory nodeProcessFactory,
            ILogger nodeOutputLogger)
        {
            _outOfProcessNodeHostOptions = outOfProcessNodeHostOptionsAccessor.Value;
            _embeddedResourcesService = embeddedResourcesService;
            _nodeProcessFactory = nodeProcessFactory;
            _nodeOutputLogger = nodeOutputLogger;
        }

        public INodeHost Create()
        {
            // Retrieve embedded http server script
            string nodeServerScript = _embeddedResourcesService.ReadAsString(typeof(HttpNodeHost), "HttpServer.js");

            return new HttpNodeHost(_nodeProcessFactory, nodeServerScript, _nodeOutputLogger, _outOfProcessNodeHostOptions);
        }
    }
}
