namespace Jering.Javascript.NodeJS
{
    /// <summary>
    /// Options for an <see cref="OutOfProcessNodeJSService"/>.
    /// </summary>
    public class OutOfProcessNodeJSServiceOptions
    {
        /// <summary>
        /// The maximum duration to wait for the NodeJS process to connect and to wait for responses to invocations.
        /// If set to a negative value, the maximum duration will be infinite.
        /// </summary>
        public int TimeoutMS { get; set; } = 60000;

        /// <summary>
        /// The number of times an invocation will be retried. 
        /// If set to a negative value, the invocation will be retried indefinitely.
        /// </summary>
        public int NumRetries { get; set; } = 1;
    }
}