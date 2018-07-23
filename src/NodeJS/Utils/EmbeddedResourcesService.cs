using System;
using System.IO;
using System.Reflection;

namespace Jering.JavascriptUtils.NodeJS
{
    /// <summary>
    /// The default implementation of <see cref="IEmbeddedResourcesService"/>.
    /// </summary>
    public class EmbeddedResourcesService : IEmbeddedResourcesService
    {
        /// <inheritdoc />
        public string ReadAsString(Assembly embeddedResourceAssembly, string name)
        {
            using (Stream stream = embeddedResourceAssembly.GetManifestResourceStream(name))
            using (var streamReader = new StreamReader(stream))
            {
                return streamReader.ReadToEnd();
            }
        }

        /// <inheritdoc />
        public string ReadAsString(Type typeFromEmbeddedResourceAssembly, string name)
        {
            Assembly asm = typeFromEmbeddedResourceAssembly.GetTypeInfo().Assembly;

            return ReadAsString(asm, name);
        }

        /// <inheritdoc />
        public Stream ReadAsStream(Assembly embeddedResourceAssembly, string name)
        {
            return embeddedResourceAssembly.GetManifestResourceStream(name);
        }

        /// <inheritdoc />
        public Stream ReadAsStream(Type typeFromEmbeddedResourceAssembly, string name)
        {
            Assembly asm = typeFromEmbeddedResourceAssembly.GetTypeInfo().Assembly;

            return ReadAsStream(asm, name);
        }
    }
}