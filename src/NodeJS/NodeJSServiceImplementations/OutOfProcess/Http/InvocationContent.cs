using Newtonsoft.Json;
using System.Buffers;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Jering.JavascriptUtils.NodeJS
{
    /// <summary>
    /// <para>An implementation of <see cref="HttpContent"/> that serializes an <see cref="InvocationRequest"/> to a <see cref="Stream"/>.</para>
    /// <para>Serializion is handled differently if <see cref="InvocationRequest.ModuleStreamSource"/> is specified since a stream cannot be efficiently serialized to JSON. 
    /// <see cref="InvocationContent"/> creates a request with Content-Type multipart/mixed and sends the stream as a separate part.
    /// </para>
    /// </summary>
    public class InvocationContent : HttpContent
    {
        //private static readonly Encoding UTF8NoBOM = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

        // Arbitrary boundary
        internal static byte[] BOUNDARY_BYTES = Encoding.UTF8.GetBytes("--Uiw6+hXl3k+5ia0cUYGhjA==");

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
                Headers.ContentType = new MediaTypeHeaderValue("multipart/mixed");
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
            char[] chars = null;
            byte[] bytes = null;
            try
            {
                // TODO can be better, the following types can make sizeable allocations
                var stringBuilder = new StringBuilder(256);
                var stringWriter = new StringWriter(stringBuilder, CultureInfo.InvariantCulture);
                var jsonTextWriter = new JsonTextWriter(stringWriter);
                _jsonService.Serialize(jsonTextWriter, _invocationRequest);

                int numChars = stringBuilder.Length;
                chars = ArrayPool<char>.Shared.Rent(numChars);
                stringBuilder.CopyTo(0, chars, 0, numChars);

                int numBytes = Encoding.UTF8.GetByteCount(chars, 0, numChars);
                bytes = ArrayPool<byte>.Shared.Rent(numBytes);
                Encoding.UTF8.GetBytes(chars, 0, numChars, bytes, 0);

                await stream.WriteAsync(bytes, 0, numBytes).ConfigureAwait(false);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(bytes);
                ArrayPool<char>.Shared.Return(chars);
            }

            // TODO Stream writer allocates both a char[] and a byte[] for buffering, it is slower than just serializing to string and writing the string to the stream
            // (at least for small-average size payloads). Support for ArrayPool buffers is coming - https://github.com/dotnet/corefx/issues/23874
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