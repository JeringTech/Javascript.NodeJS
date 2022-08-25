using System;
using System.IO;
using System.Reflection;

namespace Jering.Javascript.NodeJS
{
    /// <summary>
    /// The default implementation of <see cref="IEmbeddedResourcesService"/>.
    /// </summary>
    public class EmbeddedResourcesService : IEmbeddedResourcesService
    {
        /// <inheritdoc />
        public string ReadAsString(Assembly embeddedResourceAssembly, string name)
        {
            Stream? stream = null;
            StreamReader? streamReader = null;
            try
            {
                stream = embeddedResourceAssembly.GetManifestResourceStream(name) ?? 
                    throw new InvalidOperationException(string.Format(Strings.InvalidOperations_EmbeddedResourcesService_NoEmbeddedResourceWithSpecifiedName, name));
                streamReader = new StreamReader(stream);

                return streamReader.ReadToEnd();
            }
            finally
            {
                stream?.Dispose();
                streamReader?.Dispose();
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
            return embeddedResourceAssembly.GetManifestResourceStream(name) ?? 
                throw new InvalidOperationException(string.Format(Strings.InvalidOperations_EmbeddedResourcesService_NoEmbeddedResourceWithSpecifiedName, name));
        }

        /// <inheritdoc />
        public Stream ReadAsStream(Type typeFromEmbeddedResourceAssembly, string name)
        {
            Assembly asm = typeFromEmbeddedResourceAssembly.GetTypeInfo().Assembly;

            return ReadAsStream(asm, name);
        }
    }
}
