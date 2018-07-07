using Jering.JavascriptUtils.Node.HostingModels;
using System;
using System.IO;

namespace Jering.JavascriptUtils.Node.NodeHosts.OutOfProcessHosts
{
    public class InvocationDataFactory : IInvocationDataFactory
    {
        public InvocationData Create(ModuleSourceType moduleSourceType,
            string moduleSource = null,
            string newCacheIdentifier = null,
            string exportName = null,
            object[] args = null,
            Stream moduleStreamSource = null)
        {
            // Throw if data is invalid
            if(moduleSourceType == ModuleSourceType.Stream && moduleStreamSource == null)
            {
                if (moduleStreamSource == null) {
                    throw new ArgumentException($"{nameof(moduleSourceType)} is {nameof(ModuleSourceType.Stream)} but {nameof(moduleStreamSource)} is null.");
                }
                else if(moduleSource != null)
                {
                    throw new ArgumentException($"{nameof(moduleSourceType)} is {nameof(ModuleSourceType.Stream)} but {nameof(moduleSource)} is defined.");
                }
            }

            var serializableInvocationData = new SerializableInvocationData(moduleSource,
                moduleSourceType,
                newCacheIdentifier,
                exportName,
                args);

            return new InvocationData(serializableInvocationData, moduleStreamSource);
        }
    }
}
