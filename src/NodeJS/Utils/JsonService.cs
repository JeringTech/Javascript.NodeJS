using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Jering.Javascript.NodeJS
{
    /// <summary>
    /// The default implementation of <see cref="IJsonService"/>.
    /// </summary>
    public class JsonService : IJsonService
    {
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,

            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            IgnoreNullValues = true,

            PropertyNameCaseInsensitive = true
        };

        /// <inheritdoc />
        public ValueTask<T?> DeserializeAsync<T>(Stream stream, CancellationToken cancellationToken = default)
        {
            return JsonSerializer.DeserializeAsync<T>(stream, _jsonSerializerOptions, cancellationToken);
        }

        /// <inheritdoc />
        public Task SerializeAsync<T>(Stream stream, T value, CancellationToken cancellationToken = default)
        {
            return JsonSerializer.SerializeAsync(stream, value, _jsonSerializerOptions, cancellationToken);
        }
    }
}
