using System.IO;

namespace Jering.JavascriptUtils.Node
{
    public interface IInvocationRequestFactory
    {
        /// <summary>
        /// Creates a valid <see cref="InvocationRequest"/> instance.
        /// </summary>
        /// <param name="moduleSourceType"><see cref="SerializableInvocationRequestData.ModuleSourceType"/></param>
        /// <param name="moduleSource"><see cref="SerializableInvocationRequestData.ModuleSource"/></param>
        /// <param name="newCacheIdentifier"><see cref="SerializableInvocationRequestData.NewCacheIdentifier"/></param>
        /// <param name="exportName"><see cref="SerializableInvocationRequestData.ExportName"/></param>
        /// <param name="args"><see cref="SerializableInvocationRequestData.Args"/></param>
        /// <param name="moduleStreamSource"><see cref="InvocationRequest.ModuleStreamSource"/></param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="moduleSourceType"/> is <see cref="ModuleSourceType.Stream"/> but 
        /// <paramref name="moduleStreamSource"/> is null or <paramref name="moduleSource"/> is not null.</exception>
        InvocationRequest Create(ModuleSourceType moduleSourceType,
            string moduleSource = null,
            string newCacheIdentifier = null,
            string exportName = null,
            object[] args = null,
            Stream moduleStreamSource = null);
    }
}
