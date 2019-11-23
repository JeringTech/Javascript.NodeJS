using System.IO;
using System.Runtime.CompilerServices;
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
        public static readonly JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultBufferSize = 64536,

            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            IgnoreNullValues = true,

            //PropertyNameCaseInsensitive = true
        };

        public ValueTask<T> DeserializeAsync<T>(Stream stream, CancellationToken cancellationToken = default)
        {
            return JsonSerializer.DeserializeAsync<T>(stream, jsonSerializerOptions, cancellationToken);
        }

        public async Task SerializeAsync(Stream stream, object value, CancellationToken cancellationToken = default)
        {
            await JsonSerializer.SerializeAsync(stream, value, value.GetType(), jsonSerializerOptions, cancellationToken);
        }
    }
}
