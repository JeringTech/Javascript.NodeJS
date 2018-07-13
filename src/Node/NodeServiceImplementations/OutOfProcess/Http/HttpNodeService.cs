using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Jering.JavascriptUtils.Node
{
    /// <summary>
    /// A specialisation of the OutOfProcessNodeInstance base class that uses HTTP to perform RPC invocations.
    ///
    /// The Node child process starts an HTTP listener on an arbitrary available port (except where a nonzero
    /// port number is specified as a constructor parameter), and signals which port was selected using the same
    /// input/output-based mechanism that the base class uses to determine when the child process is ready to
    /// accept RPC invocations.
    /// </summary>
    /// <seealso cref="HostingModels.BaseNodeInstance" />
    public class HttpNodeService : OutOfProcessNodeService
    {
        private static readonly JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore,

        };

        private readonly HttpClient _httpClient;
        private readonly JsonSerializer _jsonSerializer;
        private bool _disposed;
        private string _endpoint;

        public HttpNodeService(IOptions<OutOfProcessNodeServiceOptions> outOfProcessNodeHostOptionsAccessor,
            IEmbeddedResourcesService embeddedResourcesService,
            INodeInvocationRequestFactory invocationRequestDataFactory,
            INodeProcessFactory nodeProcessFactory,
            IHttpClientFactory httpClientFactory,
            ILogger<HttpNodeService> nodeServiceLogger) :
            base(nodeProcessFactory,
                embeddedResourcesService.ReadAsString(typeof(HttpNodeService), "HttpServer.js"),
                invocationRequestDataFactory,
                nodeServiceLogger,
                outOfProcessNodeHostOptionsAccessor.Value)
        {
            _httpClient = httpClientFactory.Create();
            _jsonSerializer = JsonSerializer.Create(jsonSerializerSettings);
        }

        public override async Task<T> InvokeAsync<T>(NodeInvocationRequest nodeInvocationRequest, CancellationToken cancellationToken)
        {
            using (var invocationContent = new NodeInvocationContent(_jsonSerializer, nodeInvocationRequest))
            using (HttpResponseMessage httpResponseMessage = await _httpClient.PostAsync(_endpoint, invocationContent, cancellationToken).ConfigureAwait(false))
            {
                // 400 = cache miss
                // 500 = throw nodeinvocationexception
                // 200 = success
            }

            return default(T);
        }

        // TODO clean up disposal
        protected override void DisposeCore()
        {
            base.DisposeCore();

            if (!_disposed)
            {
                _httpClient.Dispose();
                _disposed = true;
            }
        }

        public override void OnConnectionEstablishedMessageReceived(string connectionEstablishedMessage)
        {
            // Start after message start and "IP - "
            int startIndex = CONNECTION_ESTABLISHED_MESSAGE_START.Length + 5;
            var stringBuilder = new StringBuilder("http://");

            for (int i = startIndex; i < connectionEstablishedMessage.Length; i++)
            {
                char currentChar = connectionEstablishedMessage[i];

                if (currentChar == ':')
                {
                    // ::1
                    stringBuilder.Append("[::1]");
                    i += 2;
                }
                else if (currentChar == ' ')
                {
                    stringBuilder.Append(':');

                    // Skip over "Port - "
                    i += 7;
                    continue;
                }
                else if (currentChar == ']')
                {
                    _endpoint = stringBuilder.ToString();
                    return;
                }
                else
                {
                    stringBuilder.Append(currentChar);
                }
            }
        }

#pragma warning disable 649 // These properties are populated via JSON deserialization
        private class RpcJsonResponse
        {
            public string ErrorMessage { get; set; }
            public string ErrorDetails { get; set; }
        }
#pragma warning restore 649
    }
}
