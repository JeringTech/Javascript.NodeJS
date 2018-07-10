using System;
using System.IO;

namespace Jering.JavascriptUtils.Node
{
    public class NodeInvocationRequestFactory : INodeInvocationRequestFactory
    {
        public NodeInvocationRequest Create(ModuleSourceType moduleSourceType,
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

            return new NodeInvocationRequest(moduleSource,
                moduleSourceType,
                newCacheIdentifier,
                exportName,
                args,
                moduleStreamSource);
        }
    }
}
