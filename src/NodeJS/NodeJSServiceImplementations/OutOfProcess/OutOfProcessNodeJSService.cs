using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Jering.Javascript.NodeJS
{
    /// <summary>
    /// <para>An abstract <see cref="INodeJSService"/> implementation that facilitates working with an out of process NodeJS instance.</para>
    /// <para>The primary responsibilities of this class are launching and maintaining a NodeJS process.
    /// This class uses the stdout stream of the child process to perform a simple handshake with the NodeJS process. This is agnostic to the mechanism that
    /// derived classes use to actually perform the invocations (e.g., they could use HTTP-RPC, or a binary TCP protocol, or any other RPC-type mechanism).</para>
    /// </summary>
    public abstract class OutOfProcessNodeJSService : INodeJSService
    {
        /// <summary>
        /// The logger for the NodeJS process's stdout and stderr streams as well as messages from <see cref="OutOfProcessNodeJSService"/> and its implementations.
        /// </summary>
        protected readonly ILogger Logger;

        private readonly bool _debugLoggingEnabled;
        private readonly bool _warningLoggingEnabled;
        private readonly bool _infoLoggingEnabled;
        private readonly IEmbeddedResourcesService _embeddedResourcesService;
        private readonly ITaskService _taskService;
        private readonly IBlockDrainerService _blockDrainerService;
        private readonly IFileWatcherService _fileWatcherService;
        private readonly INodeJSProcessFactory _nodeProcessFactory;
        private readonly string _serverScriptName;
        private readonly Assembly _serverScriptAssembly;
        private readonly OutOfProcessNodeJSServiceOptions _options;
        private readonly SemaphoreSlim _connectingLock = new(1, 1);
        private readonly int _numRetries;
        private readonly int _numProcessRetries;
        private readonly int _numConnectionRetries;
        private readonly bool _enableProcessRetriesForJavascriptErrors;
        private readonly int _connectionTimeoutMS;
        private readonly int _invocationTimeoutMS;
        private readonly ConcurrentDictionary<Task, object?> _trackedInvokeTasks; // TODO use ConcurrentSet when it's available - https://github.com/dotnet/runtime/issues/16443
        private readonly bool _trackInvokeTasks;
        private readonly object _fileChangeAggregateLock = new();
        private readonly int _fileChangeAggregateDelayInMilliseconds;

        private bool _disposed;
        private volatile INodeJSProcess? _nodeJSProcess; // Volatile since it's used in a double checked lock
        private Timer? _fileChangeAggregateTimer;

        /// <summary>
        /// <para>This regex is used to determine successful initialization of the process.</para>
        /// <para>All match groups contained in the regex are passed as arguments to the <see cref="OnConnectionEstablishedMessageReceived"/> method.</para>
        /// </summary>
        protected abstract Regex ConnectionEstablishedMessageRegex { get; }

        /// <summary>
        /// Creates an <see cref="OutOfProcessNodeJSService"/> instance.
        /// </summary>
        /// <param name="nodeProcessFactory"></param>
        /// <param name="logger"></param>
        /// <param name="optionsAccessor"></param>
        /// <param name="embeddedResourcesService"></param>
        /// <param name="fileWatcherService"></param>
        /// <param name="taskService"></param>
        /// <param name="blockDrainerService"></param>
        /// <param name="serverScriptAssembly"></param>
        /// <param name="serverScriptName"></param>
        protected OutOfProcessNodeJSService(INodeJSProcessFactory nodeProcessFactory,
            ILogger logger,
            IOptions<OutOfProcessNodeJSServiceOptions> optionsAccessor,
            IEmbeddedResourcesService embeddedResourcesService,
            IFileWatcherService fileWatcherService,
            ITaskService taskService,
            IBlockDrainerService blockDrainerService,
            Assembly serverScriptAssembly,
            string serverScriptName)
        {
            _fileWatcherService = fileWatcherService;
            _taskService = taskService;
            _blockDrainerService = blockDrainerService;

            _nodeProcessFactory = nodeProcessFactory;
            _options = optionsAccessor.Value;
            _embeddedResourcesService = embeddedResourcesService;
            _serverScriptName = serverScriptName;
            _serverScriptAssembly = serverScriptAssembly;
            Logger = logger;

            _fileChangeAggregateDelayInMilliseconds = _options.AggregateTimeout;

            _debugLoggingEnabled = Logger.IsEnabled(LogLevel.Debug);
            _warningLoggingEnabled = Logger.IsEnabled(LogLevel.Warning);
            _infoLoggingEnabled = Logger.IsEnabled(LogLevel.Information);

            _numRetries = _options.NumRetries;
            _numProcessRetries = _options.NumProcessRetries;
            _numConnectionRetries = _options.NumConnectionRetries;
            _connectionTimeoutMS = _options.ConnectionTimeoutMS;
            _invocationTimeoutMS = _options.InvocationTimeoutMS;
            _enableProcessRetriesForJavascriptErrors = _options.EnableProcessRetriesForJavascriptErrors;

            (_trackInvokeTasks, _trackedInvokeTasks) = InitializeFileWatching();
        }

        /// <summary>
        /// Asynchronously invokes code in the NodeJS instance.
        /// </summary>
        /// <typeparam name="T">The type of the object this method will return. It can be a JSON-serializable type, <see cref="string"/>, or <see cref="Stream"/>.</typeparam>
        /// <param name="invocationRequest">The invocation request to send to the NodeJS process.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the invocation.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        protected abstract Task<(bool, T?)> TryInvokeAsync<T>(InvocationRequest invocationRequest, CancellationToken cancellationToken);

        /// <summary>
        /// <para>This method is called when the connection established message from the NodeJS process is received.</para>
        /// <para>The message can be used to complete the handshake with the
        /// NodeJS process, for example by delivering a port and an IP address to use in further communications.</para>
        /// </summary>
        /// <param name="connectionMessageMatch">The regex match that can be used to extract additional arguments to complete the handshake.</param>
        protected abstract void OnConnectionEstablishedMessageReceived(Match connectionMessageMatch);

        /// <inheritdoc />
        public virtual async Task<T?> InvokeFromFileAsync<T>(string modulePath, string? exportName = null, object?[]? args = null, CancellationToken cancellationToken = default)
        {
            var invocationRequest = new InvocationRequest(ModuleSourceType.File, modulePath, exportName: exportName, args: args);

            return (await TryInvokeCoreAsync<T>(invocationRequest, cancellationToken).ConfigureAwait(false)).Item2;
        }

        /// <inheritdoc />
        public virtual Task InvokeFromFileAsync(string modulePath, string? exportName = null, object?[]? args = null, CancellationToken cancellationToken = default)
        {
            // Task<T> extends Task
            return InvokeFromFileAsync<Void>(modulePath, exportName, args, cancellationToken);
        }

        /// <inheritdoc />
        public virtual async Task<T?> InvokeFromStringAsync<T>(string moduleString, string? cacheIdentifier = null, string? exportName = null, object?[]? args = null, CancellationToken cancellationToken = default)
        {
            var invocationRequest = new InvocationRequest(ModuleSourceType.String, moduleString, cacheIdentifier, exportName, args);

            return (await TryInvokeCoreAsync<T>(invocationRequest, cancellationToken).ConfigureAwait(false)).Item2;
        }

        /// <inheritdoc />
        public virtual Task InvokeFromStringAsync(string moduleString, string? cacheIdentifier = null, string? exportName = null, object?[]? args = null, CancellationToken cancellationToken = default)
        {
            return InvokeFromStringAsync<Void>(moduleString, cacheIdentifier, exportName, args, cancellationToken);
        }

        /// <inheritdoc />
        public virtual async Task<T?> InvokeFromStringAsync<T>(Func<string> moduleFactory, string cacheIdentifier, string? exportName = null, object?[]? args = null, CancellationToken cancellationToken = default)
        {
            (bool success, T? result) = await TryInvokeFromCacheAsync<T>(cacheIdentifier, exportName, args, cancellationToken).ConfigureAwait(false);

            if (success)
            {
                return result;
            }

            if (moduleFactory == null)
            {
                throw new ArgumentNullException(nameof(moduleFactory));
            }

            // If module doesn't exist in cache, create module string and send it to the NodeJS process
            var invocationRequest = new InvocationRequest(ModuleSourceType.String, moduleFactory(), cacheIdentifier, exportName, args);

            return (await TryInvokeCoreAsync<T>(invocationRequest, cancellationToken).ConfigureAwait(false)).Item2;
        }

        /// <inheritdoc />
        public virtual Task InvokeFromStringAsync(Func<string> moduleFactory, string cacheIdentifier, string? exportName = null, object?[]? args = null, CancellationToken cancellationToken = default)
        {
            return InvokeFromStringAsync<Void>(moduleFactory, cacheIdentifier, exportName, args, cancellationToken);
        }

        /// <inheritdoc />
        public virtual async Task<T?> InvokeFromStreamAsync<T>(Stream moduleStream, string? cacheIdentifier = null, string? exportName = null, object?[]? args = null, CancellationToken cancellationToken = default)
        {
            var invocationRequest = new InvocationRequest(ModuleSourceType.Stream, null, cacheIdentifier, exportName, args, moduleStream);

            return (await TryInvokeCoreAsync<T>(invocationRequest, cancellationToken).ConfigureAwait(false)).Item2;
        }

        /// <inheritdoc />
        public virtual Task InvokeFromStreamAsync(Stream moduleStream, string? cacheIdentifier = null, string? exportName = null, object?[]? args = null, CancellationToken cancellationToken = default)
        {
            return InvokeFromStreamAsync<Void>(moduleStream, cacheIdentifier, exportName, args, cancellationToken);
        }

        /// <inheritdoc />
        public virtual async Task<T?> InvokeFromStreamAsync<T>(Func<Stream> moduleFactory, string cacheIdentifier, string? exportName = null, object?[]? args = null, CancellationToken cancellationToken = default)
        {
            (bool success, T? result) = await TryInvokeFromCacheAsync<T>(cacheIdentifier, exportName, args, cancellationToken).ConfigureAwait(false);

            if (success)
            {
                return result;
            }

            if (moduleFactory == null)
            {
                throw new ArgumentNullException(nameof(moduleFactory));
            }

            using Stream moduleStream = moduleFactory();
            // If module doesn't exist in cache, create module stream and send it to the NodeJS process
            var invocationRequest = new InvocationRequest(ModuleSourceType.Stream, null, cacheIdentifier, exportName, args, moduleStream);

            return (await TryInvokeCoreAsync<T>(invocationRequest, cancellationToken).ConfigureAwait(false)).Item2;
        }

        /// <inheritdoc />
        public virtual Task InvokeFromStreamAsync(Func<Stream> moduleFactory, string cacheIdentifier, string? exportName = null, object?[]? args = null, CancellationToken cancellationToken = default)
        {
            return InvokeFromStreamAsync<Void>(moduleFactory, cacheIdentifier, exportName, args, cancellationToken);
        }

        /// <inheritdoc />
        public virtual Task<(bool, T?)> TryInvokeFromCacheAsync<T>(string moduleCacheIdentifier, string? exportName = null, object?[]? args = null, CancellationToken cancellationToken = default)
        {
            var invocationRequest = new InvocationRequest(ModuleSourceType.Cache, moduleCacheIdentifier, exportName: exportName, args: args);

            return TryInvokeCoreAsync<T>(invocationRequest, cancellationToken);
        }

        /// <inheritdoc />
        public virtual async Task<bool> TryInvokeFromCacheAsync(string moduleCacheIdentifier, string? exportName = null, object?[]? args = null, CancellationToken cancellationToken = default)
        {
            return (await TryInvokeFromCacheAsync<Void>(moduleCacheIdentifier, exportName, args, cancellationToken).ConfigureAwait(false)).Item1;
        }

        /// <inheritdoc />
        public virtual ValueTask MoveToNewProcessAsync()
        {
            return MoveToNewProcessAsync(true);
        }

        internal virtual async Task<(bool, T?)> TryInvokeCoreAsync<T>(InvocationRequest invocationRequest, CancellationToken cancellationToken)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(OutOfProcessNodeJSService));
            }

            int numRetries = _numRetries;
            int numProcessRetries = _numProcessRetries;
            while (true)
            {
                CancellationTokenSource? cancellationTokenSource = null;
                try
                {
                    // If we aren't connected to a NodeJS process, connect to a new process.
                    // We want this within the while loop so if we disconnect between tries, we connect before retrying.
                    await ConnectIfNotConnectedAsync(cancellationToken).ConfigureAwait(false);

                    // Create cancellation token so we can add a timeout 
                    (CancellationToken invokeCancellationToken, cancellationTokenSource) = CreateCancellationToken(cancellationToken); // We need the CTS for disposal

                    return await (_trackInvokeTasks ?
                        TryTrackedInvokeAsync<T>(invocationRequest, _trackedInvokeTasks, invokeCancellationToken) :
                        TryInvokeAsync<T>(invocationRequest, invokeCancellationToken)).ConfigureAwait(false);
                }
                catch (ConnectionException)
                {
                    // ConnectIfNotConnected has its own retry logic
                    throw;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // TODO what happens in NodeJS when we cancel an invocation from the .Net process?
                    // Investigate, make sure NodeJS handles such situations properly. 

                    // Invocation canceled, don't retry
                    throw;
                }
                catch (OperationCanceledException) when (numRetries == 0 && numProcessRetries == 0)
                {
                    // Invocation timed out and no more retries
                    throw new InvocationException(string.Format(Strings.InvocationException_OutOfProcessNodeJSService_InvocationTimedOut,
                        _invocationTimeoutMS,
                        nameof(OutOfProcessNodeJSServiceOptions),
                        nameof(OutOfProcessNodeJSServiceOptions.InvocationTimeoutMS)));
                }
                catch (Exception exception) when (numRetries != 0 || numProcessRetries != 0)
                {
                    if (invocationRequest.ModuleSourceType == ModuleSourceType.Stream)
                    {
                        if (!invocationRequest.ModuleStreamSource!.CanSeek) // ModuleStreamSource is not null if ModuleSourceType is Stream
                        {
                            // Don't retry if stream source is unseekable. Callers can "cache" stream contents in a memory stream if they want retries.
                            throw;
                        }
                        else if (!invocationRequest.CheckStreamAtInitialPosition())
                        {
                            invocationRequest.ResetStreamPosition();
                        }
                    }

                    if (_warningLoggingEnabled)
                    {
                        Logger.LogWarning(string.Format(Strings.LogWarning_InvocationAttemptFailed, numRetries < 0 ? "infinity" : numRetries.ToString(), exception.ToString()));
                    }

                    if (numRetries == 0 && exception is InvocationException && !_enableProcessRetriesForJavascriptErrors)
                    {
                        // Don't retry in new process if exception is caused by JS error and process retries for JS errors is not enabled
                        throw;
                    }
                }
                catch (Exception exception) when (_warningLoggingEnabled) // numRetries == 0 && numProcessRetries == 0
                {
                    Logger.LogWarning(string.Format(Strings.LogWarning_InvocationAttemptFailed, numRetries < 0 ? "infinity" : numRetries.ToString(), exception.ToString()));
                    throw;
                }
                finally
                {
                    cancellationTokenSource?.Dispose();
                }

                if (numRetries == 0) // If we get here, numProcessRetries != 0
                {
                    // If retries in the existing process have been exhausted but process retries remain, move to new process and reset numRetries.
                    if (_warningLoggingEnabled)
                    {
                        Logger.LogWarning(string.Format(Strings.LogWarning_RetriesInExistingProcessExhausted, numProcessRetries < 0 ? "infinity" : numProcessRetries.ToString()));
                    }

                    numProcessRetries = numProcessRetries > 0 ? numProcessRetries - 1 : numProcessRetries; // numProcessRetries can be negative (retry indefinitely)
                    numRetries = _numRetries > 0 ? _numRetries - 1 : _numRetries;

                    await MoveToNewProcessAsync(false).ConfigureAwait(false);
                }
                else
                {
                    numRetries = numRetries > 0 ? numRetries - 1 : numRetries; // numRetries can be negative (retry indefinitely)
                }
            }
        }

        internal virtual (CancellationToken, CancellationTokenSource?) CreateCancellationToken(CancellationToken cancellationToken)
        {
            if (_invocationTimeoutMS >= 0)
            {
                var cancellationTokenSource = new CancellationTokenSource(_invocationTimeoutMS);

                if (cancellationToken != CancellationToken.None)
                {
                    cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cancellationTokenSource.Token);
                }

                return (cancellationTokenSource.Token, cancellationTokenSource);
            }
            else
            {
                return (cancellationToken, null);
            }
        }

        internal virtual async ValueTask ConnectIfNotConnectedAsync(CancellationToken cancellationToken)
        {
            // Connected calls Process.HasExited, which throws if the Process instance is not attached to a process. This won't occur since
            // _nodeJSProcess is only ever assigned already-started Process instances (see CreateProcess). Process.HasExited also throws if
            // it can't access the process's exit code. People encounter this when they create a Process instance for an existing process,
            // and the existing process requires elevated privileges. This isn't an issue for us since we're creating the NodeJS
            // process and always have necessary privileges.
            //
            //  This is safe threading-wise since _nodeJSProcess is volatile and its property getters enclose logic in lock blocks
            if (_nodeJSProcess?.Connected == true)
            {
                return;
            }

            // Apart from the operation creating the process, block all other threads
            await _connectingLock.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (_nodeJSProcess?.Connected == true)
                {
                    return;
                }

                await CreateNewProcessAndConnectAsync().ConfigureAwait(false);
            }
            finally
            {
                _connectingLock.Release();
            }
        }

        /// <summary>
        /// Caller must hold <see cref="_connectingLock"/>.
        /// </summary>
        internal virtual async Task CreateNewProcessAndConnectAsync()
        {
            int numConnectionRetries = _numConnectionRetries;
            DisposeTrackingSemaphoreSlim? semaphoreSlim = null;

            while (true)
            {
                try
                {
                    // If an exception is thrown below, between CreateAndSetUpProcess and returning from this method, we might retry despite having
                    // started a process. Call dispose to avoid such processes becoming orphans.
                    if (_nodeJSProcess != null)
                    {
#if NET5_0 || NET6_0 || NET7_0
                        await _nodeJSProcess.DisposeAsync().ConfigureAwait(false);
#else
                        _nodeJSProcess.Dispose();
#endif
                    }

                    // If the new process is created successfully, the semaphoreSlim is released by OutputReceivedHandler.
                    semaphoreSlim = new DisposeTrackingSemaphoreSlim(0, 1);

                    // Create and start process
                    _nodeJSProcess = CreateAndSetUpProcess(semaphoreSlim); // May throw InvalidOperationException if it fails

                    // Get process ID in case we need to log it
                    int processId = _nodeJSProcess.SafeID;

                    if (_debugLoggingEnabled)
                    {
                        Logger.LogDebug(string.Format(Strings.LogDebug_OutOfProcessNodeJSService_WaitingOnProcessConnectionSemaphore,
                            processId,
                            Thread.CurrentThread.ManagedThreadId.ToString(),
                            Thread.CurrentThread.IsThreadPoolThread));
                    }

                    if (await semaphoreSlim.WaitAsync(_connectionTimeoutMS < 0 ? -1 : _connectionTimeoutMS).ConfigureAwait(false))
                    {
                        // Process exited before timeout
                        if (_nodeJSProcess.HasExited)
                        {
                            // Dispose
#if NET5_0 || NET6_0 || NET7_0
                            await _nodeJSProcess.DisposeAsync().ConfigureAwait(false);
#else
                            _nodeJSProcess.Dispose();
#endif

                            throw new ConnectionException(string.Format(Strings.ConnectionException_OutOfProcessNodeJSService_ProcessExitedBeforeConnecting, processId));
                        }

                        // Start listening for file events before unblocking all operations.
                        if (_debugLoggingEnabled)
                        {
                            Logger.LogDebug(string.Format(Strings.LogDebug_OutOfProcessNodeJSService_StartingFileWatcher, _nodeJSProcess.SafeID));
                        }

                        _nodeJSProcess.SetConnected();

                        return;
                    }

                    // Connection attempt timed out
                    //
                    // We're unlikely to get to this point. If we do we want to log the issue

                    // Generate exception message. This must be done before disposing the process so HasExited and ExitStatus are meaningful.
                    string exceptionMessage = string.Format(Strings.ConnectionException_OutOfProcessNodeJSService_ConnectionAttemptTimedOut,
                        _connectionTimeoutMS,
                        processId,
                        _nodeJSProcess.HasExited,
                        _nodeJSProcess.ExitStatus);

                    // Kills and disposes process
#if NET5_0 || NET6_0 || NET7_0
                    await _nodeJSProcess.DisposeAsync().ConfigureAwait(false);
#else
                    _nodeJSProcess.Dispose();
#endif

                    throw new ConnectionException(exceptionMessage);
                }
                catch (Exception exception) when (numConnectionRetries != 0)
                {
                    if (_warningLoggingEnabled)
                    {
                        Logger.LogWarning(string.Format(Strings.LogWarning_ConnectionAttemptFailed, numConnectionRetries < 0 ? "infinity" : numConnectionRetries.ToString(), exception.ToString()));
                    }
                }
                catch (Exception exception) when (exception is not ConnectionException)
                {
                    // Wrap so users can easily identify connection issues
                    throw new ConnectionException(Strings.ConnectionException_OutOfProcessNodeJSService_FailedToConnect, exception);
                }
                finally
                {
                    semaphoreSlim?.Dispose();
                }

                numConnectionRetries = numConnectionRetries > 0 ? numConnectionRetries - 1 : numConnectionRetries;
            }
        }

        // File watching graceful shutdown overview:
        //
        // If graceful shutdown is enabled, we wait till all of the current process's invocations complete before we kill it.
        // To do this, we store invoke tasks and call Task.WhenAll on them before killing processes.
        //
        // We store invoke tasks in an air-tight manner:
        // When a file event occurs, we enter _connectionLock and ditch the current process (see MoveToNewProcessAsync). 
        // Subsequent invocations get blocked in ConnectIfNotConnectedAsync. However, there may be in-flight invocations that have gotten past 
        // ConnectIfNotConnectedAsync. These invocations could be sent to the ditched process.
        // Therefore we wait for them to "drain" past the task creation block (see TryTrackedInvokeAsync) before we store
        // invoke tasks (see MoveToNewProcessAsync). 
        //
        // With this sytem in place, we're guaranteed to store every invoke task sent to the process we're ditching.
        //
        // Don't directly assign newed objects to instance variables. This way:
        // - they can be readonly,
        // - we can test if they were initialized correctly,
        // - and we can mock this method to return custom objects.
        //
        // Perfect dependency inversion would entail creating factories for these types. This internal virtual method does the job for now.
        internal virtual (bool trackInvokeTasks, ConcurrentDictionary<Task, object?> trackedInvokeTasks) InitializeFileWatching()
        {
            if (!_options.EnableFileWatching)
            {
                return default;
            }

            _fileWatcherService.AddFileChangedListenerAsync(FileChangedHandler);

            if (!_options.GracefulProcessShutdown)
            {
                return default;
            }

            // Note that we don't start file watching in this method. It's started when we actually have a process to restart.

            return (true, new ConcurrentDictionary<Task, object?>());
        }

        internal virtual async Task<(bool, T?)> TryTrackedInvokeAsync<T>(InvocationRequest invocationRequest,
            // Instance variables newed in this class must be passed as arguments so we can mock them in tests.
            ConcurrentDictionary<Task, object?> trackedInvokeTasks,
            CancellationToken cancellationToken)
        {
            // Enter block where invocation is started
            await _blockDrainerService.EnterBlockAsync().ConfigureAwait(false);

            Task<(bool, T?)>? trackedInvokeTask = null;
            try
            {
                trackedInvokeTask = TryInvokeAsync<T>(invocationRequest, cancellationToken);
                trackedInvokeTasks.TryAdd(trackedInvokeTask, null);
            }
            finally
            {
                // Whether or not the invocation started successfully, exit the block
                _blockDrainerService.ExitBlock();
            }

            // Await tracked task
            try
            {
                return await trackedInvokeTask.ConfigureAwait(false);
            }
            finally
            {
                // Remove completed task, note that it might already have been removed in MoveToNewProcessAsync
                trackedInvokeTasks.TryRemove(trackedInvokeTask, out object? _);
            }
        }

        // FileSystemWatcher handles file events synchronously (one after another), storing pending events in a buffer - https://github.com/dotnet/runtime/blob/master/src/libraries/System.IO.FileSystem.Watcher/src/System/IO/FileSystemWatcher.Win32.cs.
        internal virtual void FileChangedHandler()
        {
            if (_debugLoggingEnabled)
            {
                Logger.LogDebug(string.Format(Strings.LogDebug_FileChangedHandlerInvokedForProcess, _nodeJSProcess?.SafeID));
            }

            lock (_fileChangeAggregateLock)
            {
                // If the timer is already running, disable it.
                _fileChangeAggregateTimer?.Change(Timeout.Infinite, Timeout.Infinite);

                // Start or restart the timer.
                if (_fileChangeAggregateTimer == null)
                {
                    _fileChangeAggregateTimer = new Timer(ExecuteFileChangeAggregate, null, _fileChangeAggregateDelayInMilliseconds, Timeout.Infinite);
                }
                else
                {
                    _fileChangeAggregateTimer.Change(_fileChangeAggregateDelayInMilliseconds, Timeout.Infinite);
                }
            }
        }

        // Delegate for timer execution
        private void ExecuteFileChangeAggregate(object? state)
        {
#pragma warning disable CS4014
            // No need to await
            //
            // Note that we need to reconnect even if we've just connected so that the changed file is loaded.
            MoveToNewProcessAsync(true);
#pragma warning restore CS4014

            lock (_fileChangeAggregateLock)
            {
                _fileChangeAggregateTimer?.Dispose();
                _fileChangeAggregateTimer = null;
            }
        }

        // Just connected refers to the situation where _nodeJSProcess.Connected is true but _connectingLock has not been released
        internal virtual async ValueTask MoveToNewProcessAsync(bool reconnectIfJustConnected)
        {
            // Already connecting or just connected
            if (_connectingLock.CurrentCount == 0)
            {
                if (_nodeJSProcess?.Connected != true ||  // If we're connecting, do nothing.
                    !reconnectIfJustConnected) // Don't need to reconnect if we've just connected
                {
                    return;
                }

                // If we get here, _nodeJSProcess.SetConnected() has been called in ConnectIfNotConnectedAsync, but ConnectIfNotConnectedAsync hasn't released _connectingLock.
                // Once _nodeJSProcess.SetConnected() is called, invocations aren't blocked in ConnectIfNotConnectedAsync. This means the NodeJS process may have already
                // received invocations and loaded user modules.
                //
                // If we're moving to a new process because a file changed, we must create a new process again.
            }

            INodeJSProcess? lastNodeJSProcess = null;
            ICollection<Task>? lastProcessInvokeTasks = null;

            await _connectingLock.WaitAsync().ConfigureAwait(false);
            try
            {
                // Ditch current process, subsequent invocations get blocked in ConnectIfNotConnectedAsync
                lastNodeJSProcess = _nodeJSProcess;
                _nodeJSProcess = null;

                // Get last-process's in-flight invoke tasks if we're tracking invoke tasks
                if (_trackInvokeTasks)
                {
                    // Wait for all operations invoking in last process to start tasks (see TryTrackedInvokeAsync)
                    await _blockDrainerService.DrainBlockAndPreventEntryAsync().ConfigureAwait(false);

                    // Store last-process's in-flight invoke tasks. Note that ConcurrentDictionary.Keys returns a static ReadOnlyCollection 
                    // - https://github.com/dotnet/runtime/blob/master/src/libraries/System.Collections.Concurrent/src/System/Collections/Concurrent/ConcurrentDictionary.cs#L1977.
                    // Also note that trackedInvokeTasks may empty between calling Count and Keys, but it doesn't matter since _taskService.WhenAll doesn't throw if 
                    // its argument is empty.
                    lastProcessInvokeTasks = _trackedInvokeTasks.IsEmpty ? null : _trackedInvokeTasks.Keys;
                    _trackedInvokeTasks.Clear();

                    // TODO if a user invokes, changes a file, invokes more and changes a file again, and so on,
                    // we could end up with multiple NodeJS processes shutting down simultaneously. This is not a pressing issue:
                    // most machines run hundreds of processes at any one time, invocations tend not to be long running, and file changes (made by humans)
                    // are likely to be spaced apart. Nonetheless, consider tracking process killing tasks (created in the 
                    // finally block below) and calling Task.WaitAny here if such tasks accumulate.
                }

                // Connect to new process
                await CreateNewProcessAndConnectAsync().ConfigureAwait(false);
            }
            finally
            {
                if (_trackInvokeTasks)
                {
                    _blockDrainerService.ResetAfterDraining();
                }

                _connectingLock.Release();

                // Wait for all in-flight tasks complete
                if (lastProcessInvokeTasks != null)
                {
                    try
                    {
                        await _taskService.WhenAll(lastProcessInvokeTasks.ToArray()).ConfigureAwait(false);
                    }
                    catch { /* Do nothing, invocation exceptions are handled by TryInvokeAsyncCore */ }
                }

                // Kill last process
                if (lastNodeJSProcess != null)
                {
                    if (_infoLoggingEnabled)
                    {
                        Logger.LogInformation(string.Format(Strings.LogInformation_KillingNodeJSProcess, lastNodeJSProcess.SafeID));
                    }
#if NET5_0 || NET6_0 || NET7_0
                    await lastNodeJSProcess.DisposeAsync().ConfigureAwait(false);
#else
                    lastNodeJSProcess.Dispose();
#endif
                }
            }
        }

        internal virtual INodeJSProcess CreateAndSetUpProcess(DisposeTrackingSemaphoreSlim semaphoreSlim)
        {
            // Create new process
            string serverScript = _embeddedResourcesService.ReadAsString(_serverScriptAssembly, _serverScriptName);
            INodeJSProcess result = _nodeProcessFactory.Create(serverScript, (object? sender, EventArgs args) => semaphoreSlim.ReleaseIfNotDisposed());

            // stdout and stderr
            result.AddOutputReceivedHandler((string message) => OutputReceivedHandler(message, semaphoreSlim));
            result.AddErrorReceivedHandler(ErrorReceivedHandler);
            result.BeginOutputAndErrorReading();

            return _nodeJSProcess = result;
        }

        internal void OutputReceivedHandler(string message, DisposeTrackingSemaphoreSlim semaphoreSlim)
        {
            // _nodeJSProcess could be null if we receive a message from a ditched process.
            //
            // Note that we should not get a connection message for any process other than the current _nodeJSProcess
            // because CreateNewProcessAndConnectAsync is never executed in parallel (for the same instance).
            if (_nodeJSProcess?.Connected == false && ConnectionEstablishedMessageRegex.Match(message) is { Success: true } match)
            {
                OnConnectionEstablishedMessageReceived(match);

                if (_infoLoggingEnabled)
                {
                    Logger.LogInformation(string.Format(Strings.LogInformation_ConnectedToNodeJSProcess, _nodeJSProcess.SafeID));
                }

                if (_debugLoggingEnabled)
                {
                    Logger.LogDebug(string.Format(Strings.LogDebug_OutOfProcessNodeJSService_ReleasingProcessConnectionSemaphore,
                        _nodeJSProcess.SafeID,
                        Thread.CurrentThread.ManagedThreadId.ToString(),
                        Thread.CurrentThread.IsThreadPoolThread));
                }

                semaphoreSlim.Release();
            }
            else if (_infoLoggingEnabled)
            {
                Logger.LogInformation(message);
            }
        }

        internal void ErrorReceivedHandler(string message)
        {
            Logger.LogError(message);
        }

        /// <summary>
        /// Disposes this instance. This method is not thread-safe. It should only be called after all other calls to this instance's methods have returned.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this); // In case a sub class overrides Object.Finalize - https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-dispose#the-dispose-overload
        }

        /// <summary>
        /// Disposes the instance. This method is not thread-safe. It should only be called after all other calls to this instance's methods have returned.
        /// </summary>
        /// <param name="disposing">True if the object is disposing or false if it is finalizing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _nodeJSProcess?.Dispose();
                _connectingLock?.Dispose();
                _blockDrainerService?.Dispose();
            }

            _disposed = true;
        }
    }
}
