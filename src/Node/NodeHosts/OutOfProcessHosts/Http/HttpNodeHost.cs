using Jering.JavascriptUtils.Node.Node.OutOfProcessHosts;
using Jering.JavascriptUtils.Node.NodeHosts.OutOfProcessHosts;
using Microsoft.Extensions.Logging;
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

        public HttpNodeHost(INodeProcessFactory nodeProcessFactory,
            string nodeServerScript,
            ILogger nodeOutputLogger,
            OutOfProcessNodeHostOptions outOfProcessNodeHostOptions) :
            base(nodeProcessFactory, nodeServerScript, nodeOutputLogger, outOfProcessNodeHostOptions)
        {
            // TODO di client accessor
            _client = new HttpClient
            {
                // TODO no timeout scenario
                Timeout = TimeSpan.FromMilliseconds(outOfProcessNodeHostOptions.InvocationTimeoutMS + 1000)
            };
        }

        protected override async Task<T> InvokeExportAsync<T>(SerializableInvocationData invocationInfo, CancellationToken cancellationToken)
        {
            string payloadJson = JsonConvert.SerializeObject(invocationInfo, jsonSerializerSettings);
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
