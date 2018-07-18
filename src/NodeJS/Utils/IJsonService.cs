using Newtonsoft.Json;

namespace Jering.JavascriptUtils.NodeJS
{
    /// <summary>
    /// Implement this interface and overwrite the default dependency injection service to do custom JSON serialization/deserialization. 
    /// </summary>
    public interface IJsonService
    {
        T Deserialize<T>(JsonReader jsonReader);
        void Serialize(JsonWriter jsonWriter, object value);
    }
}
