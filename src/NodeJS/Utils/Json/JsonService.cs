using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
#if NET6_0_OR_GREATER
using System.Text.Json.Serialization;
#endif

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

#if NET6_0_OR_GREATER
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, // https://docs.microsoft.com/en-sg/dotnet/fundamentals/syslib-diagnostics/syslib0020
#else
            IgnoreNullValues = true,
#endif

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
