using Jering.JavascriptUtils.Node.HostingModels;
using System.IO;

namespace Jering.JavascriptUtils.Node.NodeHosts.OutOfProcessHosts
{
    /// <summary>
    /// Invocation data to be sent to the Node.js process. 
    /// </summary>
    public class InvocationData
    {
        public InvocationData(SerializableInvocationData serializableInvocationData,
            Stream moduleStreamSource)
        {
            SerializableInvocationData = serializableInvocationData;
            ModuleStreamSource = moduleStreamSource;
        }

        /// <summary>
        /// Serializable portion of the invocation data to be sent to the Node.js process. 
        /// </summary>
        public SerializableInvocationData SerializableInvocationData { get; }

        /// <summary>
        /// Stream containing the source of the module from which an export will be invoked.
        /// </summary>
        public Stream ModuleStreamSource { get; }
    }
}
