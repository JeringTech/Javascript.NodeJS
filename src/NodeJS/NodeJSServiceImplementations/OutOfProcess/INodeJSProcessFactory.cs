using System.Diagnostics;

namespace Jering.Javascript.NodeJS
{
    /// <summary>
    /// <para>An abstraction for NodeJS process creation.</para>
    /// <para>To customize process creation, implement this interface and overwrite the default DI service for <see cref="INodeJSProcessFactory"/>.</para>
    /// </summary>
    public interface INodeJSProcessFactory
    {
        /// <summary>
        /// Creates a NodeJS process running <paramref name="serverScript"/>.
        /// </summary>
        /// <param name="serverScript">The server script to run on NodeJS.</param>
        Process Create(string serverScript);
    }
}
