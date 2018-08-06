
namespace Jering.Javascript.NodeJS
{
    /// <summary>
    /// Represents an error caught in NodeJS.
    /// </summary>
    public class InvocationError
    {
        /// <summary>
        /// Creates an <see cref="InvocationError"/> instance.
        /// </summary>
        /// <param name="errorMessage"></param>
        /// <param name="errorStack"></param>
        public InvocationError(string errorMessage, string errorStack = null)
        {
            ErrorMessage = errorMessage;
            ErrorStack = errorStack;
        }

        /// <summary>
        /// A description of the error.
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// The error's NodeJS stack trace.
        /// </summary>
        public string ErrorStack { get; }
    }
}
