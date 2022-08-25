using System;

namespace Jering.Javascript.NodeJS
{
    /// <summary>
    /// <para>An abstraction for NodeJS process creation.</para>
    /// <para>To customize process creation, implement this interface and overwrite the default DI service for <see cref="INodeJSProcessFactory"/>.</para>
    /// </summary>
    public interface INodeJSProcessFactory
    {
        /// <summary>
        /// Creates an <see cref="INodeJSProcess"/> running <paramref name="serverScript"/>.
        /// </summary>
        /// <param name="serverScript">The server script to run on NodeJS.</param>
        INodeJSProcess Create(string serverScript);

        /// <summary>
        /// Creates an <see cref="INodeJSProcess"/> running <paramref name="serverScript"/> with exited event handle <paramref name="exitedEventHandler"/>.
        /// </summary>
        /// <param name="serverScript">The server script to run on NodeJS.</param>
        /// <param name="exitedEventHandler">The NodeJS process's exited event handler.</param>
        INodeJSProcess Create(string serverScript, EventHandler exitedEventHandler);
    }
}
