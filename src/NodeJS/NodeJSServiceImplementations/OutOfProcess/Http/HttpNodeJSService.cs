using Jering.IocServices.System.Net.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Jering.Javascript.NodeJS
{
    /// <summary>
    /// <para>An implementation of <see cref="OutOfProcessNodeJSService"/> that uses Http for inter-process communication.</para>
    /// <para>The NodeJS child process starts a Http server on an arbitrary port (unless otherwise specified
    /// using <see cref="NodeJSProcessOptions.Port"/>) and receives invocation requests as Http requests.</para>
    /// </summary>
    public class HttpNodeJSService : OutOfProcessNodeJSService
    {
        internal const string SERVER_SCRIPT_NAME = "HttpServer.js";

        private readonly IHttpContentFactory _httpContentFactory;
        private readonly IJsonService _jsonService;
        private readonly IHttpClientService _httpClientService;

        private bool _disposed;
        internal Uri Endpoint;

        /// <summary>
        /// Creates a <see cref="HttpNodeJSService"/> instance.
        /// </summary>
        /// <param name="outOfProcessNodeJSServiceOptionsAccessor"></param>
        /// <param name="httpContentFactory"></param>
        /// <param name="embeddedResourcesService"></param>
        /// <param name="httpClientService"></param>
        /// <param name="jsonService"></param>
        /// <param name="nodeJSProcessFactory"></param>
        /// <param name="loggerFactory"></param>
        public HttpNodeJSService(IOptions<OutOfProcessNodeJSServiceOptions> outOfProcessNodeJSServiceOptionsAccessor,
            IHttpContentFactory httpContentFactory,
            IEmbeddedResourcesService embeddedResourcesService,
            IHttpClientService httpClientService,
            IJsonService jsonService,
            INodeJSProcessFactory nodeJSProcessFactory,
            ILoggerFactory loggerFactory) :
            base(nodeJSProcessFactory,
                loggerFactory.CreateLogger(typeof(HttpNodeJSService)),
                outOfProcessNodeJSServiceOptionsAccessor,
                embeddedResourcesService,
                typeof(HttpNodeJSService).GetTypeInfo().Assembly,
                SERVER_SCRIPT_NAME)
        {
            _httpClientService = httpClientService;
            _jsonService = jsonService;
            _httpContentFactory = httpContentFactory;
        }

        /// <inheritdoc />
        protected override async Task<(bool, T)> TryInvokeAsync<T>(InvocationRequest invocationRequest, CancellationToken cancellationToken)
        {
            using (HttpContent httpContent = _httpContentFactory.Create(invocationRequest))
            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, Endpoint))
            {
                httpRequestMessage.Content = httpContent;

                // Some notes on disposal:
                // HttpResponseMessage.Dispose simply calls Dispose on HttpResponseMessage.Content. By default, HttpResponseMessage.Content is a StreamContent instance that has an underlying 
                // NetworkStream instance that should be disposed. When HttpResponseMessage.Content.ReadAsStreamAsync is called, the NetworkStream is wrapped in a read-only delegating stream
                // and returned. In most cases below, StreamReader is used to read the read-only stream, upon disposal of the StreamReader, the underlying stream and thus the NetworkStream
                // are disposed. If HttpStatusCode is NotFound or an exception is thrown, we manually call HttpResponseMessage.Dispose. If we return the stream, we pass on the responsibility 
                // for disposing it to the caller.
                HttpResponseMessage httpResponseMessage = null;
                try
                {
                    httpResponseMessage = await _httpClientService.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

                    if (httpResponseMessage.StatusCode == HttpStatusCode.NotFound)
                    {
                        httpResponseMessage.Dispose();
                        return (false, default(T));
                    }

                    if (httpResponseMessage.StatusCode == HttpStatusCode.InternalServerError)
                    {
                        using (Stream stream = await httpResponseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false))
                        {
                            InvocationError invocationError = await _jsonService.DeserializeAsync<InvocationError>(stream, cancellationToken).ConfigureAwait(false);
                            throw new InvocationException(invocationError.ErrorMessage, invocationError.ErrorStack);
                        }
                    }

                    if (httpResponseMessage.StatusCode == HttpStatusCode.OK)
                    {
                        if(typeof(T) == typeof(Void)) // Returned value doesn't matter
                        {
                            return (true, default);
                        }

                        if (typeof(T) == typeof(string))
                        {
                            string result = await httpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
                            return (true, (T)(object)result);
                        }

                        using (Stream stream = await httpResponseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false))
                        {
                            if (typeof(T) == typeof(Stream))
                            {
                                return (true, (T)(object)stream);
                            }

                            T result = await _jsonService.DeserializeAsync<T>(stream, cancellationToken).ConfigureAwait(false);

                            return (true, result);
                        }
                    }
                }
                catch
                {
                    httpResponseMessage?.Dispose();

                    throw;
                }

                throw new InvocationException(string.Format(Strings.InvocationException_HttpNodeJSService_UnexpectedStatusCode, httpResponseMessage.StatusCode));
            }
        }

        /// <inheritdoc />
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
                }
                else if (currentChar == ']')
                {
                    Endpoint = new Uri(stringBuilder.ToString());
                    return;
                }
                else
                {
                    stringBuilder.Append(currentChar);
                }
            }
        }

        /// <inheritdoc />
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
