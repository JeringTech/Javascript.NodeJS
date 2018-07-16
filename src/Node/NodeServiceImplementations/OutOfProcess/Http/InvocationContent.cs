using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Jering.JavascriptUtils.Node
{
    public class NodeInvocationContent : HttpContent
    {
        // Default encoding for StreamWriters - https://github.com/dotnet/corefx/blob/master/src/Common/src/CoreLib/System/IO/EncodingCache.cs
        private static readonly Encoding UTF8NoBOM = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
        private const string BOUNDARY = "--Jering.JavascriptUtils.Node";

        private readonly JsonSerializer _jsonSerializer;
        private readonly InvocationRequest _nodeInvocationRequest;

        public NodeInvocationContent(JsonSerializer jsonSerializer, InvocationRequest nodeInvocationRequest)
        {
            _jsonSerializer = jsonSerializer ?? throw new ArgumentNullException(nameof(jsonSerializer));
            _nodeInvocationRequest = nodeInvocationRequest ?? throw new ArgumentNullException(nameof(nodeInvocationRequest));

            Headers.ContentType = _nodeInvocationRequest.ModuleSourceType == ModuleSourceType.Stream ?
                new MediaTypeHeaderValue("multipart/mixed") :
                new MediaTypeHeaderValue("application/json");
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            // By default, when StreamWriter is disposed, it closes the stream it writes to. In this instance, the stream is a HttpContentStream 
            // that will be further utilized by the framework. If it StreamWrite is allowed to close it, an exception will be thrown by the framework
            // when it attempts to write to it. The StreamWriter constructor that allows leaveOpen to be specified requires encoding and bufferSize to be specified,
            // here they are simply the defaults used by StreamWriter's other constructors - 
            // https://github.com/dotnet/corefx/blob/master/src/Common/src/CoreLib/System/IO/StreamWriter.cs.
            using (var streamWriter = new StreamWriter(stream, UTF8NoBOM, 1024, true))
            using (var jsonTextWriter = new JsonTextWriter(streamWriter))
            {
                _jsonSerializer.Serialize(jsonTextWriter, _nodeInvocationRequest);

                if (_nodeInvocationRequest.ModuleStreamSource != null)
                {
                    streamWriter.Write(BOUNDARY);
                    streamWriter.Flush();
                    await _nodeInvocationRequest.ModuleStreamSource.CopyToAsync(stream).ConfigureAwait(false);
                }
            }
        }

        protected override bool TryComputeLength(out long length)
        {
            length = -1;

            // Can't determine length
            return false;
        }
    }
}