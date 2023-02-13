using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Jering.Javascript.NodeJS
{
    /// <summary>
    /// <para>An implementation of <see cref="OutOfProcessNodeJSService"/> that uses Http for inter-process communication.</para>
    /// <para>NodeJS child processes start Http servers to receive invocation requests over Http.</para>
    /// </summary>
    public class HttpNodeJSService : OutOfProcessNodeJSService
    {
        /// <summary>
        /// Regex to match message used to perform a handshake with the NodeJS process.
        /// </summary>
        private static readonly Regex _sharedConnectionEstablishedMessageRegex = new(@"\[Jering\.Javascript\.NodeJS: HttpVersion - (?<protocol>HTTP\/\d\.\d) Listening on IP - (?<ip>[^ ]+) Port - (?<port>\d+)\]", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

        internal const string HTTP11_SERVER_SCRIPT_NAME = "Http11Server.js";
        internal const string HTTP20_SERVER_SCRIPT_NAME = "Http20Server.js";

        private readonly IHttpContentFactory _httpContentFactory;
        private readonly IJsonService _jsonService;
        private readonly ILogger<HttpNodeJSService> _logger;
        private readonly IHttpClientService _httpClientService;
#if NETCOREAPP3_1 || NET5_0_OR_GREATER
        private readonly Version _httpVersion;
#endif

        private bool _disposed;
        // Volatile since it may be updated by different threads and we always
        // want to use the most recent instance
        internal volatile Uri? _endpoint;

        /// <inheritdoc />
        protected override Regex ConnectionEstablishedMessageRegex => _sharedConnectionEstablishedMessageRegex;

        /// <summary>
        /// Creates an <see cref="HttpNodeJSService"/>.
        /// </summary>
        /// <param name="outOfProcessNodeJSServiceOptionsAccessor">The <see cref="OutOfProcessNodeJSServiceOptions"/> accessor.</param>
        /// <param name="httpNodeJSServiceOptionsAccessor">The <see cref="HttpNodeJSServiceOptions"/> accessor.</param>
        /// <param name="httpContentFactory">The factory for creating <see cref="HttpContent"/>s.</param>
        /// <param name="embeddedResourcesService">The service for retrieving NodeJS Http server scripts.</param>
        /// <param name="fileWatcherFactory">The service for creating <see cref="IFileWatcher"/>s</param>
        /// <param name="taskService">The service for utilizing tasks.</param>
        /// <param name="blockDrainerService">The service for draining code blocks.</param>
        /// <param name="httpClientService">The service for utilizing <see cref="HttpClient"/>.</param>
        /// <param name="jsonService">The service for JSON serialization and deserialization.</param>
        /// <param name="nodeJSProcessFactory">The factory for creating <see cref="NodeJSProcess"/>s.</param>
        /// <param name="logger">The logger for the instance.</param>
        public HttpNodeJSService(IOptions<OutOfProcessNodeJSServiceOptions> outOfProcessNodeJSServiceOptionsAccessor,
            IOptions<HttpNodeJSServiceOptions> httpNodeJSServiceOptionsAccessor,
            IHttpContentFactory httpContentFactory,
            IEmbeddedResourcesService embeddedResourcesService,
            IFileWatcherFactory fileWatcherFactory,
            ITaskService taskService,
            IBlockDrainerService blockDrainerService,
            IHttpClientService httpClientService,
            IJsonService jsonService,
            INodeJSProcessFactory nodeJSProcessFactory,
            ILogger<HttpNodeJSService> logger) :
            base(nodeJSProcessFactory,
                logger,
                outOfProcessNodeJSServiceOptionsAccessor,
                embeddedResourcesService,
                fileWatcherFactory,
                taskService,
                blockDrainerService,
                typeof(HttpNodeJSService).GetTypeInfo().Assembly,
#if NETCOREAPP3_1 || NET5_0_OR_GREATER
                httpNodeJSServiceOptionsAccessor.Value.Version == HttpVersion.Version20 ? HTTP20_SERVER_SCRIPT_NAME : HTTP11_SERVER_SCRIPT_NAME)
#else
                HTTP11_SERVER_SCRIPT_NAME)
#endif

        {
            _httpClientService = httpClientService;
            _jsonService = jsonService;
            _logger = logger;
            _httpContentFactory = httpContentFactory;
#if NETCOREAPP3_1 || NET5_0_OR_GREATER
            _httpVersion = httpNodeJSServiceOptionsAccessor.Value.Version == HttpVersion.Version20 ? HttpVersion.Version20 : HttpVersion.Version11;
#endif
        }

        /// <inheritdoc />
        protected override async Task<(bool, T?)> TryInvokeAsync<T>(InvocationRequest invocationRequest, CancellationToken cancellationToken) where T : default // https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-9.0/unconstrained-type-parameter-annotations#default-constraint, https://github.com/dotnet/csharplang/issues/3297
        {
            using HttpContent httpContent = _httpContentFactory.Create(invocationRequest);
            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, _endpoint)
            {
#if NETCOREAPP3_1 || NET5_0_OR_GREATER
                Version = _httpVersion,
#endif
#if NET5_0_OR_GREATER
                VersionPolicy = HttpVersionPolicy.RequestVersionExact,
#endif
                Content = httpContent,
            };

            // Some notes on disposal:
            // HttpResponseMessage.Dispose simply calls Dispose on HttpResponseMessage.Content. By default, HttpResponseMessage.Content is a StreamContent instance that has an underlying 
            // NetworkStream instance that should be disposed. When HttpResponseMessage.Content.ReadAsStreamAsync is called, the NetworkStream is wrapped in a read-only delegating stream
            // and returned. In most cases below, StreamReader is used to read the read-only stream, upon disposal of the StreamReader, the underlying stream and thus the NetworkStream
            // are disposed. If HttpStatusCode is NotFound or an exception is thrown, we manually call HttpResponseMessage.Dispose. If we return the stream, we pass on the responsibility 
            // for disposing it to the caller.
            HttpResponseMessage? httpResponseMessage = null;
            try
            {
                httpResponseMessage = await _httpClientService.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

                if (httpResponseMessage.StatusCode == HttpStatusCode.NotFound)
                {
                    httpResponseMessage.Dispose();
                    return (false, default);
                }

                if (httpResponseMessage.StatusCode == HttpStatusCode.InternalServerError)
                {
#if NET5_0_OR_GREATER
                    using Stream stream = await httpResponseMessage.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
#else
                    using Stream stream = await httpResponseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false);
#endif
                    InvocationError? invocationError = await _jsonService.DeserializeAsync<InvocationError>(stream, cancellationToken).ConfigureAwait(false);
                    throw new InvocationException(invocationError?.ErrorMessage ?? "Unexpected error", invocationError?.ErrorStack);
                }

                if (httpResponseMessage.StatusCode == HttpStatusCode.OK)
                {
                    if (typeof(T) == typeof(Void)) // Returned value doesn't matter
                    {
                        return (true, default);
                    }
                    else if (typeof(T) == typeof(string))
                    {
#if NET5_0_OR_GREATER
                        string result = await httpResponseMessage.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
#else
                        string result = await httpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif
                        return (true, (T)(object)result);
                    }
                    else if (typeof(T) == typeof(Stream))
                    {
#if NET5_0_OR_GREATER
                        Stream stream = await httpResponseMessage.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
#else
                        Stream stream = await httpResponseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false);
#endif
                        return (true, (T)(object)stream); // User's reponsibility to handle disposal
                    }
                    else if (typeof(T) == typeof(HttpResponseMessage))
                    {
                        return (true, (T)(object)httpResponseMessage); 
                    }
                    else
                    {
#if NET5_0_OR_GREATER
                        using Stream stream = await httpResponseMessage.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
#else
                        using Stream stream = await httpResponseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false);
#endif
                        T? result = await _jsonService.DeserializeAsync<T>(stream, cancellationToken).ConfigureAwait(false);
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

        /// <inheritdoc />
        protected override void OnConnectionEstablishedMessageReceived(Match connectionMessageMatch)
        {
            _endpoint = new UriBuilder
                {
                    Scheme = "http",
                    Host = connectionMessageMatch.Groups["ip"].Value,
                    Port = int.Parse(connectionMessageMatch.Groups["port"].Value),
                }.Uri;

            _logger.LogInformation(string.Format(Strings.LogInformation_HttpEndpoint,
                connectionMessageMatch.Groups["protocol"].Value, // Pluck out HTTP version
                _endpoint));
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
