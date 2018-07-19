using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Jering.JavascriptUtils.NodeJS
{
    /// <summary>
    /// <para>An implementation of <see cref="OutOfProcessNodeJSService"/> that uses Http for inter-process communication.</para>
    /// <para>
    /// The NodeJS child process starts a Http server on an arbitrary port (unless otherwise specified
    /// in <see cref="NodeJSProcessOptions"/>). The server receives invocation requests as Http requests,
    /// performs the invocations and responds with Http responses.
    /// </para>
    /// </summary>
    public class HttpNodeJSService : OutOfProcessNodeJSService
    {
        private const string SERVER_SCRIPT_NAME = "HttpServer.js";

        private readonly IHttpContentFactory _httpContentFactory;
        private readonly IJsonService _jsonService;
        private readonly IHttpClientService _httpClientService;

        private bool _disposed;
        internal string Endpoint;

        public HttpNodeJSService(IOptions<OutOfProcessNodeJSServiceOptions> outOfProcessNodeJSServiceOptionsAccessor,
            IHttpContentFactory httpContentFactory,
            IEmbeddedResourcesService embeddedResourcesService,
            IHttpClientService httpClientService,
            IJsonService jsonService,
            INodeJSProcessFactory nodeJSProcessFactory,
            ILogger<HttpNodeJSService> nodeServiceLogger) :
            base(nodeJSProcessFactory,
                nodeServiceLogger,
                outOfProcessNodeJSServiceOptionsAccessor,
                embeddedResourcesService,
                typeof(HttpNodeJSService).Assembly,
                SERVER_SCRIPT_NAME)
        {
            _httpClientService = httpClientService;
            _jsonService = jsonService;
            _httpContentFactory = httpContentFactory;
        }

        protected override async Task<(bool, T)> TryInvokeAsync<T>(InvocationRequest invocationRequest, CancellationToken cancellationToken)
        {
            using (HttpContent httpContent = _httpContentFactory.Create(invocationRequest))
            {
                // All HttpResponseMessage.Dispose does is call HttpContent.Dispose. Using default options, this is unecessary, for the following reason:
                // HttpClient loads the response content into a MemoryStream
                // FinishSendAsyncBuffered - https://github.com/dotnet/corefx/blob/c42b2cd477976504b1ae0e4b71d48e92f0459d49/src/System.Net.Http/src/System/Net/Http/HttpClient.cs#L468
                // LoadIntoBufferAsync - https://github.com/dotnet/corefx/blob/c42b2cd477976504b1ae0e4b71d48e92f0459d49/src/System.Net.Http/src/System/Net/Http/HttpContent.cs#L374
                // Disposing a MemoryStream instance just toggles some flags
                // Dispose - https://github.com/dotnet/corefx/blob/c42b2cd477976504b1ae0e4b71d48e92f0459d49/src/Common/src/CoreLib/System/IO/MemoryStream.cs#L124
                // Since memoryStreams don't use unmanaged resources, calling Dispose on HttpResponseMessage is unecessary.
                HttpResponseMessage httpResponseMessage = await _httpClientService.PostAsync(Endpoint, httpContent, cancellationToken).ConfigureAwait(false);

                if (httpResponseMessage.StatusCode == HttpStatusCode.NotFound)
                {
                    return (false, default(T));
                }

                if (httpResponseMessage.StatusCode == HttpStatusCode.InternalServerError)
                {
                    using (Stream stream = await httpResponseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    using (var streamReader = new StreamReader(stream))
                    using (var jsonTextReader = new JsonTextReader(streamReader))
                    {
                        InvocationError invocationError = _jsonService.Deserialize<InvocationError>(jsonTextReader);
                        throw new InvocationException(invocationError.ErrorMessage, invocationError.ErrorStack);
                    }
                }

                if (httpResponseMessage.StatusCode == HttpStatusCode.OK)
                {
                    Stream stream = await httpResponseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false);
                    var value = default(T);

                    if (typeof(T) == typeof(Stream))
                    {
                        value = (T)(object)stream;
                    }
                    else if (typeof(T) == typeof(string))
                    {
                        // Stream reader closes the stream when it is diposed
                        using (var streamReader = new StreamReader(stream))
                        {
                            value = (T)(object)await streamReader.ReadToEndAsync().ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        using (var streamReader = new StreamReader(stream))
                        using (var jsonTextReader = new JsonTextReader(streamReader))
                        {
                            value = _jsonService.Deserialize<T>(jsonTextReader);
                        }
                    }

                    return (true, value);
                }

                throw new InvocationException($"Http response received with unexpected status code: {httpResponseMessage.StatusCode}.");
            }
        }

        protected override void OnConnectionEstablishedMessageReceived(string connectionEstablishedMessage)
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
                    Endpoint = stringBuilder.ToString();
                    return;
                }
                else
                {
                    stringBuilder.Append(currentChar);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            base.Dispose(disposing);

            _disposed = true;
        }
    }
}
