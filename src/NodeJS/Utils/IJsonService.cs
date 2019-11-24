using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Jering.Javascript.NodeJS
{
    /// <summary>
    /// <para>An abstraction for JSON serialization/deserialization.</para>
    /// <para>To perform custom JSON serialization/deserialization, implement this interface and overwrite the default DI service for <see cref="IJsonService"/>.</para>
    /// </summary>
    public interface IJsonService
    {
        /// <summary>
        /// Deserializes the JSON structure contained by the specified <see cref="JsonReader"/> into an instance of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the object to deserialize.</typeparam>
        /// <param name="stream">The <see cref="Stream"/> containing the object.</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns>The instance of T being deserialized.</returns>
        ValueTask<T> DeserializeAsync<T>(Stream stream, CancellationToken cancellationToken = default);

        /// <summary>
        /// Serializes the specified System.Object and writes the JSON structure using the specified <see cref="JsonWriter"/>.
        /// </summary>
        /// <param name="jsonWriter">The <see cref="JsonWriter"/> used to write the JSON structure.</param>
        /// <param name="value">The object to serialize.</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        Task SerializeAsync<T>(Stream stream, T value, CancellationToken cancellationToken = default);
    }
}
