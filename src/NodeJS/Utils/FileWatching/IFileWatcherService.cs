using System;
using System.Threading.Tasks;

namespace Jering.Javascript.NodeJS
{
    /// <summary>
    /// A service for watching files for changes.
    /// </summary>
    public interface IFileWatcherService
    {
        /// <summary>
        /// Add a listener for file changes.
        /// </summary>
        /// <param name="fileChanged">The listener.</param>
        Task AddFileChangedListenerAsync(Action fileChanged);
    }
}