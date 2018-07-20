using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Jering.JavascriptUtils.NodeJS
{
    /// <summary>
    /// <para>An implementation of <see cref="HttpContent"/> that serializes a <see cref="InvocationRequest"/> to a <see cref="Stream"/>.</para>
    /// <para>
    /// <see cref="InvocationRequest.ModuleStreamSource"/> can't be efficiently serialized to JSON as part of its parent. For one thing, the string it 
    /// represents typically isn't escaped. <see cref="InvocationContent"/> creates a request with Content-Type 
    /// multipart/mixed if <see cref="InvocationRequest.ModuleStreamSource"/> is not null.
    /// </para>
    /// </summary>
    public class InvocationContent : HttpContent
    {
        // Arbitrary boundary
        internal const string BOUNDARY = "--Uiw6+hXl3k+5ia0cUYGhjA==";
        internal static byte[] BOUNDARY_BYTES = Encoding.UTF8.GetBytes(BOUNDARY);

        private readonly IJsonService _jsonService;
        private readonly InvocationRequest _invocationRequest;

        public InvocationContent(IJsonService jsonService, InvocationRequest invocationRequest)
        {
            _invocationRequest = invocationRequest;
            _jsonService = jsonService;

            if (invocationRequest.ModuleSourceType == ModuleSourceType.Stream) {
                Headers.ContentType =  new MediaTypeHeaderValue("multipart/mixed");
            }
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            // TODO why is this faster than writing directly to the stream?
            string json = _jsonService.Serialize(_invocationRequest);
            await stream.WriteAsync(Encoding.UTF8.GetBytes(json), 0, json.Length).ConfigureAwait(false);

            if(_invocationRequest.ModuleSourceType == ModuleSourceType.Stream)
            {
                await stream.WriteAsync(BOUNDARY_BYTES, 0, BOUNDARY_BYTES.Length).ConfigureAwait(false);
                await _invocationRequest.ModuleStreamSource.CopyToAsync(stream).ConfigureAwait(false);
            }

            //var streamWriter = new StreamWriter(stream, UTF8NoBOM, 256, true);
            //var jsonWriter = new JsonTextWriter(streamWriter);
            //_jsonService.Serialize(jsonWriter, _invocationRequest);
            //await streamWriter.FlushAsync().ConfigureAwait(false);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = -1;

            // Can't determine length
            return false;
        }
    }
}