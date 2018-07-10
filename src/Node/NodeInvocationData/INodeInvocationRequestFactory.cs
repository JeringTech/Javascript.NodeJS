using System.IO;

namespace Jering.JavascriptUtils.Node
{
    public interface INodeInvocationRequestFactory
    {
        /// <summary>
        /// Creates a valid <see cref="NodeInvocationRequest"/> instance.
        /// </summary>
        /// <param name="moduleSourceType"><see cref="SerializableInvocationRequestData.ModuleSourceType"/></param>
        /// <param name="moduleSource"><see cref="SerializableInvocationRequestData.ModuleSource"/></param>
        /// <param name="newCacheIdentifier"><see cref="SerializableInvocationRequestData.NewCacheIdentifier"/></param>
        /// <param name="exportName"><see cref="SerializableInvocationRequestData.ExportName"/></param>
        /// <param name="args"><see cref="SerializableInvocationRequestData.Args"/></param>
        /// <param name="moduleStreamSource"><see cref="NodeInvocationRequest.ModuleStreamSource"/></param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="moduleSourceType"/> is <see cref="ModuleSourceType.Stream"/> but 
        /// <paramref name="moduleStreamSource"/> is null or <paramref name="moduleSource"/> is not null.</exception>
        NodeInvocationRequest Create(ModuleSourceType moduleSourceType,
            string moduleSource = null,
            string newCacheIdentifier = null,
            string exportName = null,
            object[] args = null,
            Stream moduleStreamSource = null);
    }
}
