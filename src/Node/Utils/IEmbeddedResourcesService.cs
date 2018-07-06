using System;
using System.IO;

namespace Jering.JavascriptUtils.Node.Utils
{
    public interface IEmbeddedResourcesService
    {
        string ReadAsString(Type assemblyContainingType, string path);

        Stream ReadAsStream(Type assemblyContainingType, string path);
    }
}
