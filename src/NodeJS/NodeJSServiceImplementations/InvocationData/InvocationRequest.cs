using System;
using System.IO;
using System.Text.Json.Serialization;

namespace Jering.Javascript.NodeJS
{
    /// <summary>
    /// Represents an invocation request for NodeJS.
    /// </summary>
    public class InvocationRequest
    {
        private readonly long _streamInitialPosition;

        /// <summary>
        /// Creates an <see cref="InvocationRequest"/>.
        /// </summary>
        /// <param name="moduleSourceType">The module's source type.</param>
        /// <param name="moduleSource">
        /// <para>The module's source.</para>
        /// <para>This value may be the module's path relative to <see cref="NodeJSProcessOptions.ProjectPath"/>, the module as a string, or the module's cache identifier.</para>
        /// <para>If <paramref name="moduleSourceType"/> is not <see cref="ModuleSourceType.Stream"/>, this value must not be <c>null</c>. Additionally, if <paramref name="moduleSourceType"/> 
        /// is <see cref="ModuleSourceType.File"/> or <see cref="ModuleSourceType.String"/>, this value must not be an empty string or whitespace.</para>
        /// </param>
        /// <param name="cacheIdentifier">The module's cache identifier. If this value is <c>null</c>, no attempt is made to retrieve or cache the module's exports.</param>
        /// <param name="exportName">The name of the function in the module's exports to invoke. If this value is <c>null</c>, the module's exports is assumed to be a function and is invoked.</param>
        /// <param name="args">The sequence of JSON-serializable arguments to pass to the function to invoke. If this value is <c>null</c>, no arguments are passed.</param>
        /// <param name="moduleStreamSource">The module as a <see cref="Stream"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="moduleSourceType"/> is <see cref="ModuleSourceType.Stream"/> but <paramref name="moduleStreamSource"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="moduleSourceType"/> is <see cref="ModuleSourceType.Cache"/> but <paramref name="moduleSource"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="moduleSourceType"/> is <see cref="ModuleSourceType.File"/> or <see cref="ModuleSourceType.String"/> but <paramref name="moduleSource"/> 
        /// is <c>null</c>, whitespace or an empty string.</exception>
        public InvocationRequest(ModuleSourceType moduleSourceType,
            string? moduleSource = null,
            string? cacheIdentifier = null,
            string? exportName = null,
            object[]? args = null,
            Stream? moduleStreamSource = null)
        {
            if (moduleSourceType == ModuleSourceType.Stream)
            {
                // moduleSourceType is stream but moduleStreamSource is null
                if (moduleStreamSource == null)
                {
                    throw new ArgumentNullException(nameof(moduleStreamSource), Strings.ArgumentException_InvocationRequest_ModuleStreamSourceCannotBeNull);
                }

                if (moduleStreamSource.CanSeek)
                {
                    _streamInitialPosition = moduleStreamSource.Position; // Can only get or set Position if CanSeek is true - https://docs.microsoft.com/en-us/dotnet/api/system.io.stream.position?view=netstandard-2.0
                }
                ModuleStreamSource = moduleStreamSource;
            }
            else if (moduleSourceType == ModuleSourceType.Cache)
            {
                if (moduleSource == null)
                {
                    // moduleSourceType is cache but moduleSource is null
                    throw new ArgumentNullException(nameof(moduleSource), Strings.ArgumentException_InvocationRequest_ModuleSourceCannotBeNull);
                }
            }
            else if (string.IsNullOrWhiteSpace(moduleSource)) // moduleSourceType is file or string but moduleSource is null, whitespace or an empty string
            {
                throw new ArgumentException(Strings.ArgumentException_InvocationRequest_ModuleSourceCannotBeNullWhitespaceOrAnEmptyString, nameof(moduleSource));
            }

            ModuleSourceType = moduleSourceType;
            ModuleSource = moduleSource;
            CacheIdentifier = cacheIdentifier;
            ExportName = exportName;
            Args = args;
        }

        /// <summary>
        /// Resets <see cref="ModuleStreamSource"/> to its initial position.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if <see cref="ModuleStreamSource"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if <see cref="ModuleStreamSource"/> is an unseekable <see cref="Stream"/>.</exception>
        public void ResetStreamPosition()
        {
            if (ModuleStreamSource == null)
            {
                throw new InvalidOperationException(Strings.InvalidOperationException_InvocationRequest_StreamIsNull);
            }

            if (!ModuleStreamSource.CanSeek)
            {
                throw new InvalidOperationException(Strings.InvalidOperationException_InvocationRequest_StreamIsUnseekable);
            }

            ModuleStreamSource.Position = _streamInitialPosition;
        }

        /// <summary>
        /// Returns a boolean value indicating whether or not <see cref="ModuleStreamSource"/> is at its initial position.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if <see cref="ModuleStreamSource"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if <see cref="ModuleStreamSource"/> is an unseekable <see cref="Stream"/>.</exception>
        public bool CheckStreamAtInitialPosition()
        {
            if (ModuleStreamSource == null)
            {
                throw new InvalidOperationException(Strings.InvalidOperationException_InvocationRequest_StreamIsNull);
            }

            if (!ModuleStreamSource.CanSeek)
            {
                throw new InvalidOperationException(Strings.InvalidOperationException_InvocationRequest_StreamIsUnseekable);
            }

            return ModuleStreamSource.Position == _streamInitialPosition;
        }

        /// <summary>
        /// Gets the source type of the module.
        /// </summary>
        public ModuleSourceType ModuleSourceType { get; }

        /// <summary>
        /// Gets the module's source
        /// </summary>
        public string? ModuleSource { get; }

        /// <summary>
        /// Gets the module's cache identifier.
        /// </summary>
        public string? CacheIdentifier { get; }

        /// <summary>
        /// Gets the name of the function in the module's exports to invoke.
        /// </summary>
        public string? ExportName { get; }

        /// <summary>
        /// Gets the sequence of JSON-serializable arguments to pass to the function to invoke.
        /// </summary>
        public object[]? Args { get; }

        /// <summary>
        /// Gets the module as a <see cref="Stream"/>.
        /// </summary>
        [JsonIgnore]
        public Stream? ModuleStreamSource { get; }
    }
}
