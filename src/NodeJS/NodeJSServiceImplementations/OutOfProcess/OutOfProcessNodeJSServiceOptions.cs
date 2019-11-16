using System.IO;

namespace Jering.Javascript.NodeJS
{
    /// <summary>
    /// Options for an <see cref="OutOfProcessNodeJSService"/>.
    /// </summary>
    public class OutOfProcessNodeJSServiceOptions
    {
        /// <summary>
        /// <para>The maximum duration to wait for the NodeJS process to connect and to wait for responses to invocations.</para>
        /// <para>If set to a negative value, the maximum duration will be infinite.</para>
        /// </summary>
        public int TimeoutMS { get; set; } = 60000;

        /// <summary>
        /// <para>The number of times an invocation will be retried.</para>
        /// <para>If set to a negative value, invocations will be retried indefinitely.</para>
        /// <para>If the module source of an invocation is an unseekable stream, the invocation will not be retried.
        /// If you require retries for such streams, copy their contents to a <see cref="MemoryStream"/>.</para>
        /// </summary>
        public int NumRetries { get; set; } = 1;
    }
}
