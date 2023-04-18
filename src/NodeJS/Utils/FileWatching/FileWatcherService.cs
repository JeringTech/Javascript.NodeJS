using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Jering.Javascript.NodeJS
{
    /// <summary>
    /// Default implementation of <see cref="IFileWatcherService"/>.
    /// </summary>
    public class FileWatcherService : IFileWatcherService, IDisposable
    {
        // Logging
        private readonly bool _debugLoggingEnabled;
        private readonly bool _infoLoggingEnabled;
        private readonly ILogger<FileWatcherService> _logger;

        // Listeners
        private event Action? _fileChanged;

        // Options
        private readonly NodeJSProcessOptions _nodeJSProcessOptions;
        private readonly OutOfProcessNodeJSServiceOptions _outOfProcessNodeJSServiceOptions;

        // Watcher
        private volatile FileSystemWatcher? _fileSystemWatcher = null; // Volatile since used in double checked lock

        // Concurrency
        private readonly SemaphoreSlim _createFileSystemWatcherLock = new(1, 1);
        private CancellationTokenSource? _cancellationTokenSource;
        private readonly object _cancellationTokenSourceLock = new();

        // Filters
        private ReadOnlyCollection<Regex>? _filters;

        // Disposal
        private bool _disposed;

        /// <summary>
        /// Creates a <see cref="FileWatcherService"/>.
        /// </summary>
        /// <param name="nodeJSProcessOptionsAccessor"></param>
        /// <param name="outOfProcessNodeJSServiceOptions"></param>
        /// <param name="logger"></param>
        public FileWatcherService(IOptions<NodeJSProcessOptions> nodeJSProcessOptionsAccessor,
            IOptions<OutOfProcessNodeJSServiceOptions> outOfProcessNodeJSServiceOptions,
            ILogger<FileWatcherService> logger)
        {
            _nodeJSProcessOptions = nodeJSProcessOptionsAccessor.Value;
            _outOfProcessNodeJSServiceOptions = outOfProcessNodeJSServiceOptions.Value;

            _debugLoggingEnabled = logger.IsEnabled(LogLevel.Debug);
            _infoLoggingEnabled = logger.IsEnabled(LogLevel.Information);
            _logger = logger;
        }

        /// <summary>
        /// Add a listener for file changes.
        /// </summary>
        /// <param name="fileChanged">The listener.</param>
        public async Task AddFileChangedListenerAsync(Action fileChanged)
        {
            // Double checked lock so only one thread creates the file watcher
            if (_fileSystemWatcher == null)
            {
                await _createFileSystemWatcherLock.WaitAsync().ConfigureAwait(false);
                _fileSystemWatcher ??= CreateFileSystemWatcher();
                _createFileSystemWatcherLock.Release();
            }

            // Add listener
            _fileChanged += fileChanged;
        }

        internal virtual FileSystemWatcher CreateFileSystemWatcher()
        {
            // Filters for checking whether files are watched
            _filters = ResolveFilters(_outOfProcessNodeJSServiceOptions.WatchFileNamePatterns);

            // Create FileSystemWatcher instance
            string directoryPath = ResolveDirectoryPath(_outOfProcessNodeJSServiceOptions.WatchPath, _nodeJSProcessOptions.ProjectPath);
            var fileSystemWatcher = new FileSystemWatcher(directoryPath)
            {
                IncludeSubdirectories = _outOfProcessNodeJSServiceOptions.WatchSubdirectories,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName
            };

            // Register handlers for FileSystemWatcher events
            fileSystemWatcher.Changed += InternalFileChangedHandler;
            fileSystemWatcher.Created += InternalFileChangedHandler;
            fileSystemWatcher.Deleted += InternalFileChangedHandler;
            fileSystemWatcher.Renamed += InternalFileRenamedHandler;

            fileSystemWatcher.EnableRaisingEvents = true;

            return fileSystemWatcher;
        }

        internal virtual void InternalFileChangedHandler(object _, FileSystemEventArgs fileSystemEventArgs)
        {
            if (IsPathWatched(fileSystemEventArgs.FullPath))
            {
#pragma warning disable CS4014 // No need to await
                InternalFileHandlerCoreAsync(fileSystemEventArgs.FullPath);
#pragma warning restore CS4014
            }
        }

        internal virtual void InternalFileRenamedHandler(object _, RenamedEventArgs renamedEventArgs)
        {
            // If both new and old paths watched, pass new path
            string path;
            if (IsPathWatched(renamedEventArgs.FullPath))
            {
                path = renamedEventArgs.FullPath;
            }
            else if (IsPathWatched(renamedEventArgs.OldFullPath))
            {
                path = renamedEventArgs.OldFullPath;
            }
            else
            {
                return; // Not watched
            }

#pragma warning disable CS4014 // No need to await
            InternalFileHandlerCoreAsync(path);
#pragma warning restore CS4014
        }

        internal virtual async Task InternalFileHandlerCoreAsync(string path)
        {
            if (_debugLoggingEnabled)
            {
                _logger.LogDebug(string.Format(Strings.LogDebug_InternalFileChangedHandlerCalled, path));
            }

            if (_fileChanged == null)
            {
                return; // No listeners
            }

            // Debounce by cancelling until an invocation occurs with no subsequent invocations for 1ms
            CancellationTokenSource cancellationTokenSource = CancelExistingAndGetNewCancellationTokenSource();
            try
            {
                CancellationToken cancellationToken = cancellationTokenSource.Token;
                await Task.Delay(1).ConfigureAwait(false);
                if (cancellationToken.IsCancellationRequested)
                {
                    if (_debugLoggingEnabled)
                    {
                        _logger.LogDebug(string.Format(Strings.LogDebug_InternalFileChangedHandlerCallDebounced, path));
                    }

                    return;
                }

                // Invoke handlers
                if (_infoLoggingEnabled)
                {
                    _logger.LogInformation(string.Format(Strings.LogInformation_InvokingRegisteredFileChangedHandlers, path));
                }

                _fileChanged();
            }
            finally
            {
                DisposeAndRemoveCancellationTokenSource(cancellationTokenSource);
            }
        }

        internal virtual CancellationTokenSource CancelExistingAndGetNewCancellationTokenSource()
        {
            // Lock to avoid synchronization issues with DisposeCancellationTokenSourceAndClear
            lock (_cancellationTokenSourceLock)
            {
                _cancellationTokenSource?.Cancel();
                return _cancellationTokenSource = new CancellationTokenSource();
            }
        }

        internal virtual void DisposeAndRemoveCancellationTokenSource(CancellationTokenSource cancellationTokenSource)
        {
            // Lock to avoid synchronization issues with CancelExistingAndGetNewCancellationTokenSource
            lock (_cancellationTokenSourceLock)
            {
                cancellationTokenSource.Dispose();
                if (cancellationTokenSource == _cancellationTokenSource)
                {
                    _cancellationTokenSource = null;
                }
            }
        }

        internal virtual bool IsPathWatched(string path)
        {
            if (_filters == null || string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            // TODO netstandard2.1+ use span returning method - https://docs.microsoft.com/en-us/dotnet/api/system.io.path.getfilename?view=netcore-3.1#System_IO_Path_GetFileName_System_ReadOnlySpan_System_Char__
            string fileName = Path.GetFileName(path);

            // TODO netstandard2.1+ use FileSystemName.MatchesSimpleExpression - https://docs.microsoft.com/en-us/dotnet/api/system.io.enumeration.filesystemname.matchessimpleexpression?view=netstandard-2.1
            return _filters.Any(regex => regex.IsMatch(fileName));
        }

        internal virtual string ResolveDirectoryPath(string? directoryPath, string projectPath)
        {
            return string.IsNullOrWhiteSpace(directoryPath) ? projectPath : directoryPath!;
        }

        internal virtual ReadOnlyCollection<Regex> ResolveFilters(IEnumerable<string> fileNamePatterns)
        {
            int count = fileNamePatterns.Count();
            var regexes = new Regex[count];

            for (int i = 0; i < count; i++)
            {
                string fileNamePattern = fileNamePatterns.ElementAt(i);
                // Note that CreateRegex may get called multiple times for the same fileNamePattern - https://github.com/dotnet/runtime/issues/24293. 
                // This is fine for now since it doesn't do much.
                regexes[i] = CreateRegex(fileNamePattern);
            }

            return new ReadOnlyCollection<Regex>(regexes);
        }

        internal virtual Regex CreateRegex(string fileNamePattern)
        {
            string regexPattern = "^" + Regex.Escape(fileNamePattern).Replace("\\*", ".*").Replace("\\?", ".?") + "$";

            return new Regex(regexPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Disposes the <see cref="FileSystemWatcher"/> instance.
        /// </summary>
        /// <param name="disposing">True if the object is disposing or false if it is finalizing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _fileSystemWatcher?.Dispose();
                _createFileSystemWatcherLock?.Dispose();
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            _disposed = true;
        }

        /// <summary>
        /// Disposes the <see cref="FileSystemWatcher"/> instance.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
