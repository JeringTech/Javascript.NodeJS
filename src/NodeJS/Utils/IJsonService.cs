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
        /// Deserializes the JSON contained by <paramref name="stream"/> into a <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of the object to create.</typeparam>
        /// <param name="stream">The <see cref="Stream"/> containing the JSON to deserialize.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel deserialization.</param>
        ValueTask<T?> DeserializeAsync<T>(Stream stream, CancellationToken cancellationToken = default);

        /// <summary>
        /// Serializes <paramref name="value"/> and writes the generated JSON to <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to write generated JSON to.</param>
        /// <param name="value">The object to serialize.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel serialization.</param>
        Task SerializeAsync<T>(Stream stream, T value, CancellationToken cancellationToken = default);
    }
}
