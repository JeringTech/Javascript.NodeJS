using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Jering.Javascript.NodeJS
{
    /// <summary>
    /// The default implementation of <see cref="IJsonService"/>.
    /// </summary>
    public class JsonService : IJsonService
    {
        private static readonly JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore
        };

        private readonly JsonSerializer _jsonSerializer;

        /// <summary>
        /// Creates a <see cref="JsonService"/> instance.
        /// </summary>
        public JsonService()
        {
            _jsonSerializer = JsonSerializer.Create(_jsonSerializerSettings);
        }

        /// <inheritdoc />
        public void Serialize(JsonWriter jsonWriter, object value)
        {
            _jsonSerializer.Serialize(jsonWriter, value);
        }

        /// <inheritdoc />
        public T Deserialize<T>(JsonReader jsonReader)
        {
            return _jsonSerializer.Deserialize<T>(jsonReader);
        }
    }
}
