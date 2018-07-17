namespace Jering.JavascriptUtils.Node
{
    /// <summary>
    /// Represents errors caught in Node.js.
    /// </summary>
    public class InvocationError
    {
        public InvocationError(string errorMessage, string errorStack = null)
        {
            ErrorMessage = errorMessage;
            ErrorStack = errorStack;
        }

        public string ErrorMessage { get; }
        public string ErrorStack { get; }
    }
}
