using System;

namespace Jering.Javascript.NodeJS
{
    /// <summary>
    /// An abstraction for watching files.
    /// </summary>
    public interface IFileWatcher : IDisposable
    {
        /// <summary>
        /// Stops file watching.
        /// </summary>
        void Stop();

        /// <summary>
        /// Starts file watching.
        /// </summary>
        void Start();
    }
}
