namespace Jering.Javascript.NodeJS
{
    /// <summary>
    /// Options for an <see cref="OutOfProcessNodeJSService"/>.
    /// </summary>
    public class OutOfProcessNodeJSServiceOptions
    {
        /// <summary>
        /// The maximum duration to wait for the NodeJS process to connect and to wait for responses to invocations.
        /// If set to -1, the maximum duration will be infinite.
        /// </summary>
        public int TimeoutMS { get; set; } = 10000;
    }
}