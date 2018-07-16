using System.Diagnostics;

namespace Jering.JavascriptUtils.Node
{
    public interface INodeJSProcessFactory
    {
        /// <summary>
        /// Creates a Node.js process running <paramref name="nodeServerScript"/>.
        /// </summary>
        /// <param name="nodeServerScript">The server script to run on Node.js.</param>
        Process Create(string nodeServerScript);
    }
}
