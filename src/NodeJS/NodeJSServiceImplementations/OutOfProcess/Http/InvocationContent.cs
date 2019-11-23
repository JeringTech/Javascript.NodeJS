using System.Buffers;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;

namespace Jering.Javascript.NodeJS
{

    /// <summary>
    /// <para>An implementation of <see cref="HttpContent"/> that serializes an <see cref="InvocationRequest"/> to a <see cref="Stream"/>.</para>
    /// <para>Serializion is handled differently if <see cref="InvocationRequest.ModuleStreamSource"/> is specified since a stream cannot be efficiently serialized to JSON. 
    /// <see cref="InvocationContent"/> creates a request with Content-Type multipart/mixed and sends the stream as a separate part.
    /// </para>
    /// </summary>
    public class InvocationContent : HttpContent
    {
        private static readonly JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultBufferSize = 64536,

            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            IgnoreNullValues = true,
        };

        //private static readonly Encoding UTF8NoBOM = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

        // Arbitrary boundary
        internal static readonly byte[] BOUNDARY_BYTES = Encoding.UTF8.GetBytes("--Uiw6+hXl3k+5ia0cUYGhjA==");

        private static readonly MediaTypeHeaderValue _multipartContentType = new MediaTypeHeaderValue("multipart/mixed");
        private readonly IJsonService _jsonService;
        private readonly InvocationRequest _invocationRequest;

        /// <summary>
        /// Creates an <see cref="InvocationContent"/> instance.
        /// </summary>
        /// <param name="jsonService">The service for serializing data to JSON.</param>
        /// <param name="invocationRequest">The invocation request to transmit over Http.</param>
        public InvocationContent(IJsonService jsonService, InvocationRequest invocationRequest)
        {
            _invocationRequest = invocationRequest;
            _jsonService = jsonService;

            if (invocationRequest.ModuleSourceType == ModuleSourceType.Stream)
            {
                Headers.ContentType = _multipartContentType;
            }
        }

        /// <summary>
        /// Serialize the HTTP content to a stream as an asynchronous operation.
        /// </summary>
        /// <param name="stream">The target stream.</param>
        /// <param name="context">Information about the transport (channel binding token, for example). This parameter may be null.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            await _jsonService.SerializeAsync(stream, _invocationRequest).ConfigureAwait(false);

            // TODO Stream writer allocates both a char[] and a byte[] for buffering, it is slower than just serializing to string and writing the string to the stream
            // (at least for small-average size payloads). Support for ArrayPool buffers is coming - https://github.com/dotnet/corefx/issues/23874, might need to target
            // netcoreapp2.1
            // using (var streamWriter = new StreamWriter(stream, UTF8NoBOM, 256, true))
            // using (var jsonWriter = new JsonTextWriter(streamWriter))
            // {
            //     _jsonService.Serialize(jsonWriter, _invocationRequest);
            // };

            if (_invocationRequest.ModuleSourceType == ModuleSourceType.Stream)
            {
                await stream.WriteAsync(BOUNDARY_BYTES, 0, BOUNDARY_BYTES.Length).ConfigureAwait(false);
                await _invocationRequest.ModuleStreamSource.CopyToAsync(stream).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Determines whether the HTTP content has a valid length in bytes.
        /// </summary>
        /// <param name="length">The length in bytes of the HTTP content.</param>
        /// <returns>true if length is a valid length; otherwise, false.</returns>
        protected override bool TryComputeLength(out long length)
        {
            length = -1;

            // Can't determine length
            return false;
        }
    }
}
