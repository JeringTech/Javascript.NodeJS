using System;
using System.IO;
using System.Reflection;

namespace Jering.JavascriptUtils.Node
{
    /// <summary>
    /// Contains methods for reading embedded resources.
    /// </summary>
    public class EmbeddedResourcesService : IEmbeddedResourcesService
    {
        public string ReadAsString(Type assemblyContainingType, string name)
        {
            Assembly asm = assemblyContainingType.GetTypeInfo().Assembly;

            using (Stream stream = asm.GetManifestResourceStream(name))
            using (var streamReader = new StreamReader(stream))
            {
                return streamReader.ReadToEnd();
            }
        }

        public Stream ReadAsStream(Type assemblyContainingType, string name)
        {
            Assembly asm = assemblyContainingType.GetTypeInfo().Assembly;

            return asm.GetManifestResourceStream(name);
        }
    }
}