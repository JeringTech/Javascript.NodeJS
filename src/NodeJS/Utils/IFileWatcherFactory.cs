using System.Collections.Generic;

namespace Jering.Javascript.NodeJS
{
    /// <summary>
    /// An abstraction for creating <see cref="IFileWatcher"/>s.
    /// </summary>
    public interface IFileWatcherFactory
    {
        /// <summary>
        /// Creates an <see cref="IFileWatcher"/>.
        /// </summary>
        /// <param name="watchPath">The path of the directory to watch for file changes.</param>
        /// <param name="watchSubdirectories">The value specifying whether to watch subdirectories of <paramref name="watchPath"/>.</param>
        /// <param name="watchFileNamesPatterns">The file name patterns to watch.</param>
        /// <param name="fileChangedEventHandler">The method that will handle file changed events.</param>
        /// <returns></returns>
        IFileWatcher Create(string watchPath, bool watchSubdirectories, IEnumerable<string> watchFileNamesPatterns, FileChangedEventHandler fileChangedEventHandler);
    }
}
