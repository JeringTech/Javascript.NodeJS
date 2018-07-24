namespace Jering.JavascriptUtils.NodeJS
{
    /// <summary>
    /// Options for an <see cref="OutOfProcessNodeJSService"/>.
    /// </summary>
    public class OutOfProcessNodeJSServiceOptions
    {
        /// <summary>
        /// Specifies the maximum duration, to wait for the NodeJS process to connect and to wait for responses to invocations.
        /// </summary>
        public int TimeoutMS { get; set; } = 60000;
    }
}