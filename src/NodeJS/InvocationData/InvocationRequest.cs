using Newtonsoft.Json;
using System;
using System.IO;

namespace Jering.JavascriptUtils.NodeJS
{
    /// <summary>
    /// Represents an invocation request for Node.js.
    /// </summary>
    public class InvocationRequest
    {
        /// <summary>
        /// Creates an <see cref="InvocationRequest"/> instance.
        /// </summary>
        /// <param name="moduleSourceType"><see cref="ModuleSourceType"/></param>
        /// <param name="moduleSource"><see cref="ModuleSource"/></param>
        /// <param name="newCacheIdentifier"><see cref="NewCacheIdentifier"/></param>
        /// <param name="exportName"><see cref="ExportName"/></param>
        /// <param name="args"><see cref="Args"/></param>
        /// <param name="moduleStreamSource"><see cref="ModuleStreamSource"/></param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="moduleSourceType"/> is <see cref="ModuleSourceType.Stream"/> but 
        /// <paramref name="moduleStreamSource"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="moduleSourceType"/> is <see cref="ModuleSourceType.File"/> or 
        /// <see cref="ModuleSourceType.String"/> but  <paramref name="moduleSource"/> is null, whitespace or an empty string.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="moduleSourceType"/> is <see cref="ModuleSourceType.Cache"/> but
        /// <paramref name="moduleSource"/> is null.</exception>
        public InvocationRequest(ModuleSourceType moduleSourceType,
            string moduleSource = null,
            string newCacheIdentifier = null,
            string exportName = null,
            object[] args = null,
            Stream moduleStreamSource = null)
        {
            if (moduleSourceType == ModuleSourceType.Stream)
            {
                // moduleSourceType is stream but moduleStreamSource is null
                if (moduleStreamSource == null)
                {
                    throw new ArgumentException($"{nameof(moduleSourceType)} is {nameof(ModuleSourceType.Stream)} but {nameof(moduleStreamSource)} is null.");
                }
            }
            else if (moduleSourceType == ModuleSourceType.Cache)
            {
                if (moduleSource == null)
                {
                    // moduleSourceType is cache but moduleSource is null
                    throw new ArgumentException($"{nameof(moduleSourceType)} is {moduleSourceType.ToString()} but {nameof(moduleSource)} is null.");
                }
            }
            else if (string.IsNullOrWhiteSpace(moduleSource))
            {
                // moduleSourceType is file or string but moduleSource is null, whitespace or an empty string
                throw new ArgumentException($"{nameof(moduleSourceType)} is {moduleSourceType.ToString()} but {nameof(moduleSource)} is null, whitespace or an empty string.");
            }

            ModuleSourceType = moduleSourceType;
            ModuleSource = moduleSource;
            NewCacheIdentifier = newCacheIdentifier;
            ExportName = exportName;
            Args = args;
            ModuleStreamSource = moduleStreamSource;
        }

        /// <summary>
        /// The source type of the module from which an export will be invoked. This value defaults to <see cref="ModuleSourceType.Cache"/>.
        /// </summary>
        public ModuleSourceType ModuleSourceType { get; }

        /// <summary>
        /// Source of the module from which an export will be invoked. The source can be the path of the module (relative to the project directory),
        /// the module as a string, or the cache identifier of the module.
        /// If <see cref="ModuleSourceType"/> is <see cref="ModuleSourceType.Stream"/>, this value must be null.
        /// If <see cref="ModuleSource"/> is <see cref="ModuleSourceType.File"/> or <see cref="ModuleSourceType.String"/>, this value must not be null, an empty string,
        /// or whitespace.
        /// If <see cref="ModuleSource"/> is <see cref="ModuleSourceType.Cache"/>, this value must not be null.
        /// </summary>
        public string ModuleSource { get; }

        /// <summary>
        /// Node.js's caching key for the module's exports.
        /// If this identifier is not specified, the exports will not be cached. If it is specified, this value must not be null, an empty string, or whitespace.
        /// </summary>
        public string NewCacheIdentifier { get; }

        /// <summary>
        /// The name of the function to invoke. If this value is not specified, the module's exports object is assumed to be a function, and that function is invoked.
        /// If it is specified, it must not be null, an empty string, or whitespace.
        /// </summary>
        public string ExportName { get; }

        /// <summary>
        /// The arguments for the invoked function.
        /// </summary>
        public object[] Args { get; }

        /// <summary>
        /// Stream containing the source of the module.
        /// </summary>
        [JsonIgnore]
        public Stream ModuleStreamSource { get; }
    }
}
