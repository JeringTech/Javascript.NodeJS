using Newtonsoft.Json;
using System;
using System.IO;

namespace Jering.Javascript.NodeJS
{
    /// <summary>
    /// Represents an invocation request for NodeJS.
    /// </summary>
    public class InvocationRequest
    {
        /// <summary>
        /// Creates an <see cref="InvocationRequest"/> instance.
        /// </summary>
        /// <param name="moduleSourceType">The source type of the module to be invoked.</param>
        /// <param name="moduleSource"><para>The source of the module to be invoked.</para>
        /// <para>The source can be the path of the module relative to <see cref="NodeJSProcessOptions.ProjectPath"/>,
        /// the module as a string, or the cache identifier of the module.</para>
        /// <para> If <paramref name="moduleSourceType"/> is not <see cref="ModuleSourceType.Stream"/>, this parameter must be specified.
        /// Additionally, if <paramref name="moduleSourceType"/> is <see cref="ModuleSourceType.File"/> or <see cref="ModuleSourceType.String"/>, this parameter must not be an empty string
        /// or whitespace.</para>
        /// </param>
        /// <param name="newCacheIdentifier"><para>The new cache identifier for the module to be invoked.</para>
        /// <para>If this parameter is not specified, the module will not be cached. If it is specified, this parameter must not be an empty string or whitespace.</para></param>
        /// <param name="exportName"><para>The name of the function in the module's exports to invoke.</para> 
        /// <para>If this value is not specified, the module's exports object is assumed to be a function, and that function is invoked.
        /// If it is specified, it must not be an empty string or whitespace.</para></param>
        /// <param name="args">The arguments for the function to invoke.</param>
        /// <param name="moduleStreamSource">The module as a <see cref="Stream"/>.</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="moduleSourceType"/> is <see cref="ModuleSourceType.Stream"/> but 
        /// <paramref name="moduleStreamSource"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="moduleSourceType"/> is <see cref="ModuleSourceType.File"/> or 
        /// <see cref="ModuleSourceType.String"/> but <paramref name="moduleSource"/> is null, whitespace or an empty string.</exception>
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
        /// The source type of the module to be invoked.
        /// </summary>
        public ModuleSourceType ModuleSourceType { get; }

        /// <summary>
        /// The source of the module to be invoked.
        /// </summary>
        public string ModuleSource { get; }

        /// <summary>
        /// The new cache identifier for the module to be invoked.
        /// </summary>
        public string NewCacheIdentifier { get; }

        /// <summary>
        /// The name of the function in the module's exports to invoke.
        /// </summary>
        public string ExportName { get; }

        /// <summary>
        /// The arguments for the function to invoke.
        /// </summary>
        public object[] Args { get; }

        /// <summary>
        /// The module as a <see cref="Stream"/>.
        /// </summary>
        [JsonIgnore]
        public Stream ModuleStreamSource { get; }
    }
}
