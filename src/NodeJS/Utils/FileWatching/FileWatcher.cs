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
        private readonly string _directoryPath;
        private readonly bool _includeSubdirectories;
        private readonly IEnumerable<Regex> _filters;
        private readonly FileChangedEventHandler _fileChangedEventHandler;
        private readonly NotifyFilters _notifyFilters;

        private volatile FileSystemWatcher? _fileSystemWatcher;
        private bool _disposed;
        private readonly object _stopLock = new();

        /// <summary>
        /// Creates a <see cref="FileWatcher"/>.
        /// </summary>
        /// <param name="directoryPath">The path of the directory to watch for file changes.</param>
        /// <param name="includeSubdirectories">The value specifying whether to watch subdirectories of <paramref name="directoryPath"/>.</param>
        /// <param name="filters">The filters for file names to watch.</param>
        /// <param name="fileChangedEventHandler">The method that will handle file changed events.</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="directoryPath"/> is <c>null</c>, whitespace or an empty string.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="filters"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="fileChangedEventHandler"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="filters"/> is empty.</exception>
        public FileWatcher(string directoryPath, bool includeSubdirectories, IEnumerable<Regex> filters, FileChangedEventHandler fileChangedEventHandler)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                throw new ArgumentException(Strings.ArgumentException_Shared_ValueCannotBeNullWhitespaceOrAnEmptyString, nameof(directoryPath));
            }
            _directoryPath = directoryPath;

            _includeSubdirectories = includeSubdirectories;
            _filters = filters ?? throw new ArgumentNullException(nameof(filters));

            if (!_filters.Any())
            {
                throw new ArgumentException(Strings.ArgumentException_Shared_ValueCannotBeEmpty, nameof(filters));
            }

            _fileChangedEventHandler += fileChangedEventHandler ?? throw new ArgumentNullException(nameof(fileChangedEventHandler));

            _notifyFilters = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
        }

        /// <inheritdoc />
        public void Stop()
        {
            // FileSystemWatcher sometimes raises multiple events when a file changes - https://github.com/dotnet/runtime/issues/24079.
            // This can cause multiple uncesssary NodeJS process restarts (see OutOfProcessNodeJSService).
            // 
            // To avoid such restarts, we need to stop/ignore events once one is raised.
            // Setting FileSystemWatcher.EnableRaisingEvents to false is insufficient - once set to true, previously buffered events may be raised (observed on macos). 
            //
            // Disposing works since it removes all handlers from the instance - https://github.com/dotnet/runtime/blob/a27884006dc5515e072adb4fa7c6f97cca08abd8/src/libraries/System.IO.FileSystem.Watcher/src/System/IO/FileSystemWatcher.cs#L343-L370.
            // Microsoft.AspNetCore.NodeServices took this approach as well.
            //
            // If stop is called before start, _fileSystemWatcher may be null.
            if (_fileSystemWatcher == null)
            {
                return;
            }

            lock (_stopLock)
            {
                if (_fileSystemWatcher == null)
                {
                    return;
                }

                _fileSystemWatcher.Dispose(); // Not thread-safe, so wrap in double checked lock
                _fileSystemWatcher = null;
            }
        }

        // TODO FileSystemWatcher uses ThreadPool threads so this can be inefficient
        /// <inheritdoc />
        public void Start()
        {
            _fileSystemWatcher = CreateFileSystemWatcher();

            // Register handlers for FileSystemWatcher events
            _fileSystemWatcher.Changed += InternalFileChangedHandler;
            _fileSystemWatcher.Created += InternalFileChangedHandler;
            _fileSystemWatcher.Deleted += InternalFileChangedHandler;
            _fileSystemWatcher.Renamed += InternalFileRenamedHandler;

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

        internal virtual FileSystemWatcher CreateFileSystemWatcher()
        {
            return new FileSystemWatcher(_directoryPath)
            {
                IncludeSubdirectories = _includeSubdirectories,
                NotifyFilter = _notifyFilters
            };
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
        /// <remarks>This method is not thread-safe.</remarks>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _fileSystemWatcher?.Dispose();
            }

            _disposed = true;
        }
    }
}
