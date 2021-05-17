
namespace Jering.Javascript.NodeJS
{
    /// <summary>
    /// Represents an error caught in NodeJS.
    /// </summary>
    public class InvocationError
    {
        /// <summary>
        /// Creates an <see cref="InvocationError"/>.
        /// </summary>
        /// <param name="errorMessage">The error's description.</param>
        /// <param name="errorStack">The error's NodeJS stack trace.</param>
        public InvocationError(string errorMessage, string? errorStack = null)
        {
            ErrorMessage = errorMessage;
            ErrorStack = errorStack;
        }

        // TODO this class is supposed to be immutable, but System.Text.Json doesn't
        // work with private setters the way newtonsoft.json does. Seems like this feature
        // will be added eventually - https://github.com/dotnet/corefx/issues/38163#issuecomment-553152589.
        /// <summary>
        /// The error's description.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// The error's NodeJS stack trace.
        /// </summary>
        public string? ErrorStack { get; set; }
    }
}
