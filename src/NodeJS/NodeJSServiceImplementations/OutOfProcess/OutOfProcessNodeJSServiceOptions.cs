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
        /// <para>If this value is negative, the maximum duration is infinite.</para>
        /// <para>Defaults to 60000.</para>
        /// </summary>
        public int TimeoutMS { get; set; } = 60000;

        /// <summary>
        /// <para>The number of times an invocation is retried.</para>
        /// <para>If set to a negative value, invocations are retried indefinitely.</para>
        /// <para>If the module source of an invocation is an unseekable stream, the invocation is not retried.
        /// If you require retries for such streams, copy their contents to a <see cref="MemoryStream"/>.</para>
        /// <para>Defaults to 1.</para>
        /// </summary>
        public int NumRetries { get; set; } = 1;

        /// <summary>
        /// <para>The concurrency mode for invocations.</para>
        /// <para>By default, this value is <see cref="Concurrency.None"/> and invocations are executed synchronously by a single NodeJS process; 
        /// mode pros: lower memory overhead and supports all modules, cons: less performant.</para>
        /// <para>If this value is <see cref="Concurrency.MultiProcess"/>, <see cref="ConcurrencyDegree"/> NodeJS processes are created and invocations are
        /// distributed among them using round robin load balancing; mode pros: more performant, cons: higher memory overhead and doesn't work with modules that 
        /// have persistent state.</para>
        /// <para>Defaults to <see cref="Concurrency.None"/>.</para>
        /// </summary>
        public Concurrency Concurrency { get; set; }

        /// <summary>
        /// <para>The concurrency degree.</para>
        /// <para>If <see cref="Concurrency"/> is <see cref="Concurrency.MultiProcess"/>, this value is the number of NodeJS processes.</para>
        /// <para>If this value is less than or equal to 0, concurrency degree is the number of logical processors the current machine has.</para>
        /// <para>This value does nothing if <see cref="Concurrency"/> is <see cref="Concurrency.None"/>.</para>
        /// <para>Defaults to 0.</para>
        /// </summary>
        public int ConcurrencyDegree { get; set; }
    }
}
