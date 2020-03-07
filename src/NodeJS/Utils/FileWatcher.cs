using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Jering.Javascript.NodeJS
{
    /// <summary>
    /// <para>Represents the method that will handle file changed events.</para>
    /// </summary>
    /// <param name="path">The path of the changed file.</param>
    public delegate void FileChangedEventHandler(string path);

    /// <summary>
    /// The default implementation of <see cref="IFileWatcher"/>.
    /// </summary>
    public class FileWatcher : IFileWatcher
    {
        private readonly FileSystemWatcher _fileSystemWatcher;
        private readonly IEnumerable<Regex> _filters;
        private readonly FileChangedEventHandler _fileChangedEventHandler;

        private bool _disposed;

        /// <summary>
        /// Creates a <see cref="FileWatcher"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="fileSystemWatcher"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="filters"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="fileChangedEventHandler"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="filters"/> is empty.</exception>
        public FileWatcher(FileSystemWatcher fileSystemWatcher, IEnumerable<Regex> filters, FileChangedEventHandler fileChangedEventHandler)
        {
            _fileSystemWatcher = fileSystemWatcher ?? throw new ArgumentNullException(nameof(fileSystemWatcher));
            _filters = filters ?? throw new ArgumentNullException(nameof(filters));

            if (!_filters.Any())
            {
                throw new ArgumentException(Strings.ArgumentException_Shared_ValueCannotBeEmpty, nameof(filters));
            }

            _fileChangedEventHandler += fileChangedEventHandler ?? throw new ArgumentNullException(nameof(fileChangedEventHandler));

            // Register handlers for FileSystemWatcher events
            _fileSystemWatcher.Changed += InternalFileChangedHandler;
            _fileSystemWatcher.Created += InternalFileChangedHandler;
            _fileSystemWatcher.Deleted += InternalFileChangedHandler;
            _fileSystemWatcher.Renamed += InternalFileRenamedHandler;
        }

        /// <inheritdoc />
        public void Stop()
        {
            _fileSystemWatcher.EnableRaisingEvents = false;
        }

        /// <inheritdoc />
        public void Start()
        {
            _fileSystemWatcher.EnableRaisingEvents = true;
        }

        internal virtual void InternalFileChangedHandler(object _, FileSystemEventArgs fileSystemEventArgs)
        {
            string fullPath = fileSystemEventArgs.FullPath;
            if (IsPathWatched(fullPath))
            {
                _fileChangedEventHandler(fullPath);
            }
        }

        internal virtual void InternalFileRenamedHandler(object _, RenamedEventArgs renamedEventArgs)
        {
            string fullPath = renamedEventArgs.FullPath;
            string oldFullPath;

            if (IsPathWatched(fullPath))
            {
                _fileChangedEventHandler(fullPath); // If both new and old paths watched, pass new path
            }
            else if (IsPathWatched(oldFullPath = renamedEventArgs.OldFullPath))
            {
                _fileChangedEventHandler(oldFullPath);
            }
        }

        internal virtual bool IsPathWatched(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            // TODO netstandard2.1+ use span returning method - https://docs.microsoft.com/en-us/dotnet/api/system.io.path.getfilename?view=netcore-3.1#System_IO_Path_GetFileName_System_ReadOnlySpan_System_Char__
            string fileName = Path.GetFileName(path);

            // TODO netstandard2.1+ use FileSystemName.MatchesSimpleExpression - https://docs.microsoft.com/en-us/dotnet/api/system.io.enumeration.filesystemname.matchessimpleexpression?view=netstandard-2.1
            return _filters.Any(regex => regex.IsMatch(fileName));
        }

        /// <summary>
        /// Disposes of the <see cref="FileWatcher"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this); // In case a sub class overrides Object.Finalize - https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-dispose#the-dispose-overload
        }

        /// <summary>
        /// Disposes of the NodeJS <see cref="FileWatcher"/>.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _fileSystemWatcher.Dispose();
            }

            _disposed = true;
        }
    }
}
