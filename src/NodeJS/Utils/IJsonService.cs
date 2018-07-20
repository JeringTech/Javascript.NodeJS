using Newtonsoft.Json;

namespace Jering.JavascriptUtils.NodeJS
{
    /// <summary>
    /// To perform custom JSON serialization/deserialization, implement this interface and overwrite the default DI service for IJsonService. 
    /// </summary>
    public interface IJsonService
    {
        T Deserialize<T>(JsonReader jsonReader);
        void Serialize(JsonWriter jsonWriter, object value);
        string Serialize(object value);
    }
}
