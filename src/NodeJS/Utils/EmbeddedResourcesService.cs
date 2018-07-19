using System;
using System.IO;
using System.Reflection;

namespace Jering.JavascriptUtils.NodeJS
{
    /// <summary>
    /// Contains methods for reading embedded resources.
    /// </summary>
    public class EmbeddedResourcesService : IEmbeddedResourcesService
    {
        public string ReadAsString(Assembly embeddedResourceAssembly, string name)
        {
            using (Stream stream = embeddedResourceAssembly.GetManifestResourceStream(name))
            using (var streamReader = new StreamReader(stream))
            {
                return streamReader.ReadToEnd();
            }
        }

        public string ReadAsString(Type typeFromEmbeddedResourceAssembly, string name)
        {
            Assembly asm = typeFromEmbeddedResourceAssembly.GetTypeInfo().Assembly;

            return ReadAsString(asm, name);
        }

        public Stream ReadAsStream(Assembly embeddedResourceAssembly, string name)
        {
            return embeddedResourceAssembly.GetManifestResourceStream(name);
        }

        public Stream ReadAsStream(Type typeFromEmbeddedResourceAssembly, string name)
        {
            Assembly asm = typeFromEmbeddedResourceAssembly.GetTypeInfo().Assembly;

            return ReadAsStream(asm, name);
        }
    }
}