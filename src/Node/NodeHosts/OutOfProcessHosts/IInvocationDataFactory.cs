using System.IO;

namespace Jering.JavascriptUtils.Node.NodeHosts.OutOfProcessHosts
{
    public interface IInvocationDataFactory
    {
        /// <summary>
        /// Creates a valid <see cref="InvocationData"/> instance.
        /// </summary>
        /// <param name="moduleSourceType"><see cref="SerializableInvocationData.ModuleSourceType"/></param>
        /// <param name="moduleSource"><see cref="SerializableInvocationData.ModuleSource"/></param>
        /// <param name="newCacheIdentifier"><see cref="SerializableInvocationData.NewCacheIdentifier"/></param>
        /// <param name="exportName"><see cref="SerializableInvocationData.ExportName"/></param>
        /// <param name="args"><see cref="SerializableInvocationData.Args"/></param>
        /// <param name="moduleStreamSource"><see cref="InvocationData.ModuleStreamSource"/></param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="moduleSourceType"/> is <see cref="ModuleSourceType.Stream"/> but 
        /// <paramref name="moduleStreamSource"/> is null or <paramref name="moduleSource"/> is not null.</exception>
        InvocationData Create(ModuleSourceType moduleSourceType,
            string moduleSource = null,
            string newCacheIdentifier = null,
            string exportName = null,
            object[] args = null,
            Stream moduleStreamSource = null);
    }
}
