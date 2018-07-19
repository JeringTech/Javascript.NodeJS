using System.Diagnostics;

namespace Jering.JavascriptUtils.NodeJS
{
    /// <summary>
    /// To customize process creation, implement this interface and overwrite the default DI service for <see cref="INodeJSProcessFactory"/>.
    /// </summary>
    public interface INodeJSProcessFactory
    {
        /// <summary>
        /// Creates a Node.js process running <paramref name="serverScript"/>.
        /// </summary>
        /// <param name="serverScript">The server script to run on Node.js.</param>
        Process Create(string serverScript);
    }
}
