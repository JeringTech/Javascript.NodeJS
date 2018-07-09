using Jering.JavascriptUtils.Node.Node.OutOfProcessHosts;
using Jering.JavascriptUtils.Node.NodeHosts.OutOfProcessHosts;
using Jering.JavascriptUtils.Node.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Jering.JavascriptUtils.Node.HostingModels
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
    internal class HttpNodeHost : OutOfProcessNodeHost
    {
        private static readonly Regex EndpointMessageRegex =
            new Regex(@"^\[Jering.JavascriptUtils.Node.HttpNodeHost:Listening on {(.*?)} port (\d+)\]$");

        private static readonly JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            TypeNameHandling = TypeNameHandling.None
        };

        private readonly HttpClient _client;
        private bool _disposed;
        private string _endpoint;

        public HttpNodeHost(IOptions<OutOfProcessNodeHostOptions> outOfProcessNodeHostOptionsAccessor,
            IEmbeddedResourcesService embeddedResourcesService,
            IInvocationDataFactory invocationDataFactory,
            INodeProcessFactory nodeProcessFactory,
            ILogger nodeOutputLogger) :
            base(nodeProcessFactory, 
                embeddedResourcesService.ReadAsString(typeof(HttpNodeHost), "HttpServer.js"), 
                invocationDataFactory,
                nodeOutputLogger, 
                outOfProcessNodeHostOptionsAccessor.Value)
        {
            // TODO di client accessor
            _client = new HttpClient
            {
                // TODO no timeout scenario
                Timeout = TimeSpan.FromMilliseconds(outOfProcessNodeHostOptionsAccessor.Value.InvocationTimeoutMS + 1000)
            };
        }

        protected override async Task<T> InvokeAsync<T>(InvocationData invocationData, CancellationToken cancellationToken)
        {
            // Create memory stream?
            // Create streamwriter
            // create jsontextwriter
            // serialize to stream
            // create multipartformdatacontent
            // add json stream to multipartformdatacontent
            //  - use content type application/json
            // if module stream exists add it as well
            //  - use content type octet-stream
            // post

            // if status code is any of the expected codes, create an invokeresultdata (rename invocationdata to invokerequestdata)
            //  - otherwise throw
            // invokeresultdata should contain stream and only create string if it is read
            // 

            var serializer = JsonSerializer.Create(GetJsonSerializerSettings());

            using (var sw = new StreamWriter(stream))
            using (var jsonTextWriter = new JsonTextWriter(sw))
            {
                serializer.Serialize(jsonTextWriter, obj);
            }

            using (var content =
    new MultipartFormDataContent("Upload----" + DateTime.Now.ToString(CultureInfo.InvariantCulture)))
            {
                content.Add(new StreamContent(new MemoryStream(image)), "bilddatei", "upload.jpg");

                using (
                   var message =
                       await client.PostAsync("http://www.directupload.net/index.php?mode=upload", content))
                {
                    var input = await message.Content.ReadAsStringAsync();

                    return !string.IsNullOrWhiteSpace(input) ? Regex.Match(input, @"http://\w*\.directupload\.net/images/\d*/\w*\.[a-z]{3}").Value : null;
                }
            }



            string payloadJson = JsonConvert.SerializeObject(invocationData, jsonSerializerSettings);
            var payload = new StringContent(payloadJson, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await _client.PostAsync(_endpoint, payload, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                // Unfortunately there's no true way to cancel ReadAsStringAsync calls, hence AbandonIfCancelled
                string responseJson = await response.Content.ReadAsStringAsync().OrThrowOnCancellation(cancellationToken).ConfigureAwait(false);
                RpcJsonResponse responseError = JsonConvert.DeserializeObject<RpcJsonResponse>(responseJson, jsonSerializerSettings);

                throw new NodeInvocationException(responseError.ErrorMessage, responseError.ErrorDetails);
            }

            System.Net.Http.Headers.MediaTypeHeaderValue responseContentType = response.Content.Headers.ContentType;
            switch (responseContentType.MediaType)
            {
                case "text/plain":
                    // String responses can skip JSON encoding/decoding
                    if (typeof(T) != typeof(string))
                    {
                        throw new ArgumentException(
                            "Node module responded with non-JSON string. This cannot be converted to the requested generic type: " +
                            typeof(T).FullName);
                    }

                    string responseString = await response.Content.ReadAsStringAsync().OrThrowOnCancellation(cancellationToken).ConfigureAwait(false);
                    return (T)(object)responseString;

                case "application/json":
                    string responseJson = await response.Content.ReadAsStringAsync().OrThrowOnCancellation(cancellationToken).ConfigureAwait(false);
                    return JsonConvert.DeserializeObject<T>(responseJson, jsonSerializerSettings);

                case "application/octet-stream":
                    // Streamed responses have to be received as System.IO.Stream instances
                    if (typeof(T) != typeof(Stream) && typeof(T) != typeof(object))
                    {
                        throw new ArgumentException(
                            "Node module responded with binary stream. This cannot be converted to the requested generic type: " +
                            typeof(T).FullName + ". Instead you must use the generic type System.IO.Stream.");
                    }

                    return (T)(object)(await response.Content.ReadAsStreamAsync().OrThrowOnCancellation(cancellationToken).ConfigureAwait(false));

                default:
                    throw new InvalidOperationException("Unexpected response content type: " + responseContentType.MediaType);
            }
        }

        // TODO clean up disposal
        protected override void DisposeCore()
        {
            base.DisposeCore();

            if (!_disposed)
            {
                if (disposing)
                {
                    _client.Dispose();
                }

                _disposed = true;
            }
        }

        // TODO extract ip and endpoint from connection established message
        protected override void OnConnectionEstablishedMessageReceived(string connectionEstablishedMessage)
        {
            Match match = string.IsNullOrEmpty(_endpoint) ? EndpointMessageRegex.Match(outputData) : null;
            if (match != null && match.Success)
            {
                int port = int.Parse(match.Groups[2].Captures[0].Value);
                string resolvedIpAddress = match.Groups[1].Captures[0].Value;

                //IPv6 must be wrapped with [] brackets
                resolvedIpAddress = resolvedIpAddress == "::1" ? $"[{resolvedIpAddress}]" : resolvedIpAddress;
                _endpoint = $"http://{resolvedIpAddress}:{port}";
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
