namespace Jering.JavascriptUtils.NodeJS
{
    public class OutOfProcessNodeJSServiceOptions
    {
        /// <summary>
        /// Specifies the maximum duration, to wait for the Node.js process to connect and to wait for responses to invocations. 
        /// Generally, these tasks should complete in under a millisecond, so this default should be sufficient.
        /// </summary>
        public int TimeoutMS { get; set; } = 1000;
    }
}