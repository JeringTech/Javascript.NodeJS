using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Jering.Javascript.NodeJS
{
    /// <summary>
    /// <para>An abstract <see cref="INodeJSService"/> implementation that facilitates working with an out of process NodeJS instance.</para>
    /// <para>The primary responsibilities of this class are launching and maintaining a NodeJS process.
    /// This class uses the stdout stream of the child process to perform a simple handshake with the NodeJS process. This is agnostic to the mechanism that
    /// derived classes use to actually perform the invocations (e.g., they could use HTTP-RPC, or a binary TCP
    /// protocol, or any other RPC-type mechanism).</para>
    /// </summary>
    /// <seealso cref="INodeJSService" />
    public abstract class OutOfProcessNodeJSService : INodeJSService
    {
        /// <summary>
        /// Start of the message used to perform a handshake with the NodeJS process.
        /// </summary>
        protected internal const string CONNECTION_ESTABLISHED_MESSAGE_START = "[Jering.Javascript.NodeJS: Listening on ";

        /// <summary>
        /// The logger for the NodeJS process's stdout and stderr streams as well as messages from <see cref="OutOfProcessNodeJSService"/> and its implementations.
        /// </summary>
        protected readonly ILogger Logger;

        private readonly bool _debugLoggingEnabled;
        private readonly bool _warningLoggingEnabled;
        private readonly bool _infoLoggingEnabled;
        private readonly IEmbeddedResourcesService _embeddedResourcesService;
        private readonly IMonitorService _monitorService;
        private readonly ITaskService _taskService;
        private readonly IFileWatcherFactory _fileWatcherFactory;
        private readonly INodeJSProcessFactory _nodeProcessFactory;
        private readonly string _serverScriptName;
        private readonly Assembly _serverScriptAssembly;
        private readonly OutOfProcessNodeJSServiceOptions _options;
        private readonly object _connectingLock = new object();
        private readonly object _invokeTaskTrackingLock = new object();
        private readonly int _numRetries;
        private readonly int _numProcessRetries;
        private readonly int _numConnectionRetries;
        private readonly int _timeoutMS;
        private readonly ConcurrentDictionary<Task, object> _trackedInvokeTasks; // TODO use ConcurrentSet when it's available - https://github.com/dotnet/runtime/issues/16443
        private readonly CountdownEvent _invokeTaskCreationCountdown;
        private readonly bool _trackInvokeTasks;

        private bool _disposed;
        private volatile INodeJSProcess _nodeJSProcess; // Volatile since it's used in a double checked lock (we check whether it's null)
        private IFileWatcher _fileWatcher;

        /// <summary>
        /// Creates an <see cref="OutOfProcessNodeJSService"/> instance.
        /// </summary>
        /// <param name="nodeProcessFactory"></param>
        /// <param name="logger"></param>
        /// <param name="optionsAccessor"></param>
        /// <param name="embeddedResourcesService"></param>
        /// <param name="fileWatcherFactory"></param>
        /// <param name="monitorService"></param>
        /// <param name="taskService"></param>
        /// <param name="serverScriptAssembly"></param>
        /// <param name="serverScriptName"></param>
        protected OutOfProcessNodeJSService(INodeJSProcessFactory nodeProcessFactory,
            ILogger logger,
            IOptions<OutOfProcessNodeJSServiceOptions> optionsAccessor,
            IEmbeddedResourcesService embeddedResourcesService,
            IFileWatcherFactory fileWatcherFactory,
            IMonitorService monitorService,
            ITaskService taskService,
            Assembly serverScriptAssembly,
            string serverScriptName) :
            this(nodeProcessFactory, logger, optionsAccessor, embeddedResourcesService, serverScriptAssembly, serverScriptName)
        {
            _fileWatcherFactory = fileWatcherFactory;
            _monitorService = monitorService;
            _taskService = taskService;

            (_trackInvokeTasks, _trackedInvokeTasks, _invokeTaskCreationCountdown) = InitializeFileWatching();
        }

        // DO NOT DELETE - keep for backward compatibility.
        /// <summary>
        /// <para>Creates an <see cref="OutOfProcessNodeJSService"/> instance.</para>
        /// <para>If this constructor is used, file watching is disabled.</para>
        /// </summary>
        /// <param name="nodeProcessFactory"></param>
        /// <param name="logger"></param>
        /// <param name="optionsAccessor"></param>
        /// <param name="embeddedResourcesService"></param>
        /// <param name="serverScriptAssembly"></param>
        /// <param name="serverScriptName"></param>
        protected OutOfProcessNodeJSService(INodeJSProcessFactory nodeProcessFactory,
            ILogger logger,
            IOptions<OutOfProcessNodeJSServiceOptions> optionsAccessor,
            IEmbeddedResourcesService embeddedResourcesService,
            Assembly serverScriptAssembly,
            string serverScriptName)
        {
            _nodeProcessFactory = nodeProcessFactory;
            _options = optionsAccessor?.Value ?? new OutOfProcessNodeJSServiceOptions();
            _embeddedResourcesService = embeddedResourcesService;
            _serverScriptName = serverScriptName;
            _serverScriptAssembly = serverScriptAssembly;
            Logger = logger;

            _debugLoggingEnabled = Logger.IsEnabled(LogLevel.Debug);
            _warningLoggingEnabled = Logger.IsEnabled(LogLevel.Warning);
            _infoLoggingEnabled = Logger.IsEnabled(LogLevel.Information);

            _numRetries = _options.NumRetries;
            _numProcessRetries = _options.NumProcessRetries;
            _numConnectionRetries = _options.NumConnectionRetries;
            _timeoutMS = _options.TimeoutMS;
        }

        /// <summary>
        /// Asynchronously invokes code in the NodeJS instance.
        /// </summary>
        /// <typeparam name="T">The type of the object this method will return. It can be a JSON-serializable type, <see cref="string"/>, or <see cref="Stream"/>.</typeparam>
        /// <param name="invocationRequest">The invocation request to send to the NodeJS process.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the invocation.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        protected abstract Task<(bool, T)> TryInvokeAsync<T>(InvocationRequest invocationRequest, CancellationToken cancellationToken);

        /// <summary>
        /// <para>This method is called when the connection established message from the NodeJS process is received.</para>
        /// <para>The message can be used to complete the handshake with the
        /// NodeJS process, for example by delivering a port and an IP address to use in further communications.</para>
        /// </summary>
        /// <param name="connectionEstablishedMessage">The connection established message.</param>
        protected abstract void OnConnectionEstablishedMessageReceived(string connectionEstablishedMessage);

        /// <inheritdoc />
        public virtual async Task<T> InvokeFromFileAsync<T>(string modulePath, string exportName = null, object[] args = null, CancellationToken cancellationToken = default)
        {
            var invocationRequest = new InvocationRequest(ModuleSourceType.File, modulePath, exportName: exportName, args: args);

            return (await TryInvokeCoreAsync<T>(invocationRequest, cancellationToken).ConfigureAwait(false)).Item2;
        }

        /// <inheritdoc />
        public virtual Task InvokeFromFileAsync(string modulePath, string exportName = null, object[] args = null, CancellationToken cancellationToken = default)
        {
            // Task<T> extends Task
            return InvokeFromFileAsync<Void>(modulePath, exportName, args, cancellationToken);
        }

        /// <inheritdoc />
        public virtual async Task<T> InvokeFromStringAsync<T>(string moduleString, string newCacheIdentifier = null, string exportName = null, object[] args = null, CancellationToken cancellationToken = default)
        {
            var invocationRequest = new InvocationRequest(ModuleSourceType.String, moduleString, newCacheIdentifier, exportName, args);

            return (await TryInvokeCoreAsync<T>(invocationRequest, cancellationToken).ConfigureAwait(false)).Item2;
        }

        /// <inheritdoc />
        public virtual Task InvokeFromStringAsync(string moduleString, string newCacheIdentifier = null, string exportName = null, object[] args = null, CancellationToken cancellationToken = default)
        {
            return InvokeFromStringAsync<Void>(moduleString, newCacheIdentifier, exportName, args, cancellationToken);
        }

        /// <inheritdoc />
        public virtual async Task<T> InvokeFromStringAsync<T>(Func<string> moduleFactory, string cacheIdentifier, string exportName = null, object[] args = null, CancellationToken cancellationToken = default)
        {
            (bool success, T result) = await TryInvokeFromCacheAsync<T>(cacheIdentifier, exportName, args, cancellationToken).ConfigureAwait(false);

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
        public virtual Task InvokeFromStringAsync(Func<string> moduleFactory, string cacheIdentifier, string exportName = null, object[] args = null, CancellationToken cancellationToken = default)
        {
            return InvokeFromStringAsync<Void>(moduleFactory, cacheIdentifier, exportName, args, cancellationToken);
        }

        /// <inheritdoc />
        public virtual async Task<T> InvokeFromStreamAsync<T>(Stream moduleStream, string newCacheIdentifier = null, string exportName = null, object[] args = null, CancellationToken cancellationToken = default)
        {
            var invocationRequest = new InvocationRequest(ModuleSourceType.Stream, null, newCacheIdentifier, exportName, args, moduleStream);

            return (await TryInvokeCoreAsync<T>(invocationRequest, cancellationToken).ConfigureAwait(false)).Item2;
        }

        /// <inheritdoc />
        public virtual Task InvokeFromStreamAsync(Stream moduleStream, string newCacheIdentifier = null, string exportName = null, object[] args = null, CancellationToken cancellationToken = default)
        {
            return InvokeFromStreamAsync<Void>(moduleStream, newCacheIdentifier, exportName, args, cancellationToken);
        }

        /// <inheritdoc />
        public virtual async Task<T> InvokeFromStreamAsync<T>(Func<Stream> moduleFactory, string cacheIdentifier, string exportName = null, object[] args = null, CancellationToken cancellationToken = default)
        {
            (bool success, T result) = await TryInvokeFromCacheAsync<T>(cacheIdentifier, exportName, args, cancellationToken).ConfigureAwait(false);

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
        public virtual Task InvokeFromStreamAsync(Func<Stream> moduleFactory, string cacheIdentifier, string exportName = null, object[] args = null, CancellationToken cancellationToken = default)
        {
            return InvokeFromStreamAsync<Void>(moduleFactory, cacheIdentifier, exportName, args, cancellationToken);
        }

        /// <inheritdoc />
        public virtual Task<(bool, T)> TryInvokeFromCacheAsync<T>(string moduleCacheIdentifier, string exportName = null, object[] args = null, CancellationToken cancellationToken = default)
        {
            var invocationRequest = new InvocationRequest(ModuleSourceType.Cache, moduleCacheIdentifier, exportName: exportName, args: args);

            return TryInvokeCoreAsync<T>(invocationRequest, cancellationToken);
        }

        /// <inheritdoc />
        public virtual async Task<bool> TryInvokeFromCacheAsync(string moduleCacheIdentifier, string exportName = null, object[] args = null, CancellationToken cancellationToken = default)
        {
            return (await TryInvokeFromCacheAsync<Void>(moduleCacheIdentifier, exportName, args, cancellationToken).ConfigureAwait(false)).Item1;
        }

        internal virtual async Task<(bool, T)> TryInvokeCoreAsync<T>(InvocationRequest invocationRequest, CancellationToken cancellationToken)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(OutOfProcessNodeJSService));
            }

            int numRetries = _numRetries;
            int numProcessRetries = _numProcessRetries;
            while (true)
            {
                CancellationTokenSource cancellationTokenSource = null;
                try
                {
                    // If we haven't connected to a NodeJS process or we've been disconnected, connect to a new process.
                    // We want this within the while loop so if we disconnect between tries, we connect before retrying.
                    ConnectIfNotConnected();

                    // Create cancellation token
                    CancellationToken invokeCancellationToken;
                    (invokeCancellationToken, cancellationTokenSource) = CreateCancellationToken(cancellationToken); // We need CTS so we can dispose of it

                    return await (_trackInvokeTasks ?
                        TryTrackedInvokeAsync<T>(invocationRequest, invokeCancellationToken, _trackedInvokeTasks, _invokeTaskCreationCountdown) :
                        TryInvokeAsync<T>(invocationRequest, invokeCancellationToken)).ConfigureAwait(false);
                }
                catch (ConnectionException)
                {
                    // ConnectIfNotConnected has its own retry logic
                    throw;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // Invocation canceled, don't retry
                    throw;
                }
                catch (OperationCanceledException) when (numRetries == 0 && numProcessRetries == 0)
                {
                    // Invocation timed out and no more retries
                    throw new InvocationException(string.Format(Strings.InvocationException_OutOfProcessNodeJSService_InvocationTimedOut,
                        _timeoutMS,
                        nameof(OutOfProcessNodeJSServiceOptions.TimeoutMS),
                        nameof(OutOfProcessNodeJSServiceOptions)));
                }
                catch (Exception exception) when (numRetries != 0 || numProcessRetries != 0)
                {
                    if (invocationRequest.ModuleSourceType == ModuleSourceType.Stream)
                    {
                        if (!invocationRequest.ModuleStreamSource.CanSeek)
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
                }
                finally
                {
                    cancellationTokenSource?.Dispose();
                }

                if (numRetries == 0)
                {
                    // If retries in the existing process have been exhausted but process retries remain,
                    // move to new process and reset numRetries.
                    if (numProcessRetries > 0)
                    {
                        if (_warningLoggingEnabled)
                        {
                            Logger.LogWarning(string.Format(Strings.LogWarning_RetriesInExistingProcessExhausted, numProcessRetries < 0 ? "infinity" : numProcessRetries.ToString()));
                        }

                        numProcessRetries = numProcessRetries > 0 ? numProcessRetries - 1 : numProcessRetries;
                        numRetries = _numRetries - 1;

                        MoveToNewProcess(false);
                    }
                }
                else
                {
                    numRetries = numRetries > 0 ? numRetries - 1 : numRetries;
                }
            }
        }

        internal virtual (CancellationToken, CancellationTokenSource) CreateCancellationToken(CancellationToken cancellationToken)
        {
            if (_timeoutMS >= 0)
            {
                var cancellationTokenSource = new CancellationTokenSource(_timeoutMS);

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

        internal virtual void ConnectIfNotConnected()
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

            // Apart from the thread creating the process, all other threads will be blocked.
            lock (_connectingLock)
            {
                if (_nodeJSProcess?.Connected == true)
                {
                    return;
                }

                // No need to listen for file events while connecting - the new NodeJS process will reload all files.
                // Stopping _fileWatcher prevents its underlying FileSystemWatcher's buffer from overflowing. 
                // 
                // Note that we don't need to worry about file events filling up the buffer between entering this _connectingLock block 
                // and the following line because MoveToNewProcess returns immediately if we're connecting.
                _fileWatcher?.Stop();

                int numConnectionRetries = _numConnectionRetries;
                EventWaitHandle waitHandle = null;

                while (true)
                {
                    try
                    {
                        // If an exception is thrown below, between CreateAndSetUpProcess and returning, we might retry despite having
                        // started a process. Dispose to avoid orphan processes.
                        _nodeJSProcess?.Dispose();

                        // If the new process is created successfully, the WaitHandle is set by OutputDataReceivedHandler.
                        waitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

                        _nodeJSProcess = CreateAndSetUpProcess(waitHandle); // Throws InvalidOperationException if it can't create the process

                        if (_debugLoggingEnabled)
                        {
                            Logger.LogDebug(string.Format(Strings.LogDebug_OutOfProcessNodeJSService_BeforeWait, Thread.CurrentThread.ManagedThreadId.ToString()));
                        }

                        if (waitHandle.WaitOne(_timeoutMS < 0 ? -1 : _timeoutMS))
                        {
                            // Start listening for file events before unblocking all threads.
                            //
                            // If we don't connect successfully, there's no point starting the file watcher since there's nothing to restart if a file changes.
                            // For that reason, this doesn't need to be in a finally block.
                            _fileWatcher?.Start();

                            _nodeJSProcess.SetConnected();

                            return;
                        }
                        else
                        {
                            // Kills and disposes
                            _nodeJSProcess.Dispose();

                            // We're unlikely to get to this point. If we do we want the issue to be logged.
                            throw new ConnectionException(string.Format(Strings.ConnectionException_OutOfProcessNodeJSService_ConnectionAttemptTimedOut,
                                _timeoutMS,
                                _nodeJSProcess.HasExited,
                                _nodeJSProcess.ExitStatus));
                        }
                    }
                    catch (Exception exception) when (numConnectionRetries != 0)
                    {
                        if (_warningLoggingEnabled)
                        {
                            Logger.LogWarning(string.Format(Strings.LogWarning_ConnectionAttemptFailed, numConnectionRetries < 0 ? "infinity" : numConnectionRetries.ToString(), exception.ToString()));
                        }
                    }
                    catch (Exception exception) when (!(exception is ConnectionException))
                    {
                        // Wrap so users can easily identify connection issues
                        throw new ConnectionException(Strings.ConnectionException_OutOfProcessNodeJSService_FailedToConnect, exception);
                    }
                    finally
                    {
                        waitHandle?.Dispose();
                    }

                    numConnectionRetries = numConnectionRetries > 0 ? numConnectionRetries - 1 : numConnectionRetries;
                }
            }
        }

        // File watching graceful shutdown overview:
        //
        // If graceful shutdown is enabled, we wait till all of the current process's invocations complete before we kill it.
        // To do this, we store invoke tasks and call Task.WaitAll on them before killing processes.
        //
        // We store invoke tasks in an air-tight manner.
        // When a file event occurs, we enter _connectionLock and ditch the current process (see MoveToNewProcess). 
        // Subsequent invocations get blocked in ConnectIfNotConnected. However, there may be in-flight invocations that have gotten past 
        // ConnectIfNotConnected. These invocations could be sent to the ditched process.
        // Therefore we allow their threads to "drain" past the task creation block (see TryTrackedInvokeAsync) before we store
        // invoke tasks (see SwapProcesses). 
        //
        // With this sytem in place, we're guaranteed to get every invoke task sent to the process we're ditching.

        // Don't directly assign newed objects to instance variables. This way:
        // - they can be readonly,
        // - we can test if they were initialized correctly,
        // - and we can mock this method to return custom objects.
        //
        // Perfect dependency inversion would entail creating factories for these types. This internal virtual method does the job for now.
        internal virtual (bool trackInvokeTasks, ConcurrentDictionary<Task, object> trackedInvokeTasks, CountdownEvent invokeTaskCreationCountdown) InitializeFileWatching()
        {
            if (!_options.EnableFileWatching ||
                _fileWatcherFactory == null ||
                _monitorService == null ||
                _taskService == null)
            {
                return default;
            }

            _fileWatcher = _fileWatcherFactory.Create(_options.WatchPath, _options.WatchSubdirectories, _options.WatchFileNamePatterns, FileChangedHandler);

            if (!_options.GracefulProcessShutdown)
            {
                return default;
            }

            // Note that we don't start file watching in this method. It's started when we actually have a process to restart.

            return (true, new ConcurrentDictionary<Task, object>(), new CountdownEvent(1));
        }

        internal virtual async Task<(bool, T)> TryTrackedInvokeAsync<T>(InvocationRequest invocationRequest,
            CancellationToken cancellationToken,
            // Instance variables newed in this class must be passed as arguments so we can mock them for testability.
            ConcurrentDictionary<Task, object> trackedInvokeTasks,
            CountdownEvent invokeTaskCreationCountdown)
        {
            // Create tracked task
            lock (_invokeTaskTrackingLock)
            {
                invokeTaskCreationCountdown.AddCount();
            }
            Task<(bool, T)> trackedInvokeTask = null;
            try
            {
                trackedInvokeTask = TryInvokeAsync<T>(invocationRequest, cancellationToken);
                trackedInvokeTasks.TryAdd(trackedInvokeTask, null);
            }
            finally
            {
                // Signal if we fail to create task or once task has been created and added to tracked tasks.
                invokeTaskCreationCountdown.Signal();
            }

            // Await tracked task
            try
            {
                return await trackedInvokeTask.ConfigureAwait(false);
            }
            finally
            {
                // Remove completed task, note that it might already have been removed in SwapProcesses
                trackedInvokeTasks.TryRemove(trackedInvokeTask, out object _);
            }
        }

        // FileSystemWatcher handles file events synchronously, storing pending events in a buffer - https://github.com/dotnet/runtime/blob/master/src/libraries/System.IO.FileSystem.Watcher/src/System/IO/FileSystemWatcher.Win32.cs.
        // We don't need to worry about this method being called simultaneously by multiple threads.
        internal virtual void FileChangedHandler(string path)
        {
            if (_infoLoggingEnabled)
            {
                Logger.LogInformation(string.Format(Strings.LogInformation_FileChangedMovingtoNewNodeJSProcess, path));
            }

            MoveToNewProcess(true);
        }

        internal virtual void MoveToNewProcess(bool reswapIfJustConnected)
        {
            bool acquiredConnectingLock = false;
            try
            {
                _monitorService.TryEnter(_connectingLock, ref acquiredConnectingLock);

                if (!acquiredConnectingLock)
                {
                    if (!reswapIfJustConnected || // Don't need to reswap again if we've just connected. We only need to reswap on just connected if a file changed and we need to reload it.
                        _nodeJSProcess?.Connected != true) // If we're connecting, do nothing.
                    {
                        return;
                    }

                    // If we get here, _nodeJSProcess.SetConnected() has been called in ConnectIfNotConnected, but ConnectIfNotConnected hasn't exited _connectingLock.
                    // Once _nodeJSProcess.SetConnected() is called, invocations aren't blocked in ConnectIfNotConnected. This means the NodeJS process may have already
                    // received invocations and loaded user modules.
                    //
                    // If we're moving to a new process because a file changed, we must create a new process again.
                    _monitorService.Enter(_connectingLock, ref acquiredConnectingLock);
                }

                // Immediately stop file watching to avoid FileSystemWatcher buffer overflows. Restarted in ConnectIfNotConnected.
                _fileWatcher?.Stop();

                SwapProcesses();
            }
            finally
            {
                if (acquiredConnectingLock)
                {
                    _monitorService.Exit(_connectingLock);
                }
            }
        }

        internal virtual void SwapProcesses()
        {
            INodeJSProcess lastNodeJSProcess = null;
            ICollection<Task> lastProcessInvokeTasks = null;

            try
            {
                // Ditch current process, subsequent invocations get blocked in ConnectIfNotConnected
                lastNodeJSProcess = _nodeJSProcess;
                _nodeJSProcess = null;

                // Get last-process invoke tasks if we're tracking invoke tasks
                if (_trackInvokeTasks)
                {
                    // Wait for all threads invoking in last process to start tasks (see TryTrackedInvokeAsync<T>)
                    _monitorService.Enter(_invokeTaskTrackingLock);
                    _invokeTaskCreationCountdown.Signal(); // So countdown can reach 0
                    _invokeTaskCreationCountdown.Wait(); // Wait for countdown to hit 0

                    // Store last-process-tasks. Note that ConcurrentDictionary.Keys returns a static ReadOnlyCollection 
                    // - https://github.com/dotnet/runtime/blob/master/src/libraries/System.Collections.Concurrent/src/System/Collections/Concurrent/ConcurrentDictionary.cs#L1977.
                    // Also note that trackedInvokeTasks may empty between calling Count and Keys, but it doesn't matter since _taskService.WaitAll doesn't throw if 
                    // its argument is empty.
                    lastProcessInvokeTasks = _trackedInvokeTasks.Count == 0 ? null : _trackedInvokeTasks.Keys;

                    // TODO if a user invokes, changes a file, invokes more and changes a file again, and so on,
                    // we could end up with multiple NodeJS processes shutting down simultaneously. This is not a pressing issue:
                    // most machines run hundreds of processes at any one time, invocations tend not to be long running, and file changes (made by humans)
                    // are likely to be spaced apart. Nonetheless, consider tracking process killing tasks (created in the 
                    // finally block below) and calling Task.WaitAny here if such tasks accumulate.

                    // Reset
                    _trackedInvokeTasks.Clear();
                    _invokeTaskCreationCountdown.Reset(1);
                }

                // Connect to new process
                ConnectIfNotConnected();
            }
            finally
            {
                if (_trackInvokeTasks)
                {
                    _monitorService.Exit(_invokeTaskTrackingLock); // Exit will never throw since we're guaranteed to have entered the lock if we get here
                }

                // Kill last process. At this point, we've started _fileWatcher, so we want to offload this to another thread to avoid
                // blocking file events.
                _taskService.Run(() =>
                {
                    if (lastProcessInvokeTasks != null)
                    {
                        try
                        {
                            // Wait for last process invocations to complete
                            _taskService.WaitAll(lastProcessInvokeTasks.ToArray());
                        }
                        catch { /* Do nothing, invocation exceptions are handled by TryInvokeAsyncCore<T> */ }
                    }

                    // Kill process
                    if (_infoLoggingEnabled)
                    {
                        Logger.LogInformation(string.Format(Strings.LogInformation_KillingNodeJSProcess, lastNodeJSProcess.SafeID));
                    }
                    lastNodeJSProcess.Dispose();
                });
            }
        }

        internal virtual INodeJSProcess CreateAndSetUpProcess(EventWaitHandle waitHandle)
        {
            // Create new process
            string serverScript = _embeddedResourcesService.ReadAsString(_serverScriptAssembly, _serverScriptName);
            INodeJSProcess result = _nodeProcessFactory.Create(serverScript);

            // stdout and stderr
            result.AddOutputReceivedHandler((object sender, string message) => OutputReceivedHandler(sender, message, waitHandle));
            result.AddErrorReceivedHandler(ErrorReceivedHandler);
            result.BeginOutputReadLine();
            result.BeginErrorReadLine();

            return result;
        }

        internal void OutputReceivedHandler(object _, string message, EventWaitHandle waitHandle)
        {
            // _nodeJSProcess could be null if we receive a message from a ditched process.
            //
            // Note that we should not get a connection message for any process other than the current _nodeJSProcess
            // because ConnectIfNotConnected is synchronous.
            if (_nodeJSProcess?.Connected == false && message.StartsWith(CONNECTION_ESTABLISHED_MESSAGE_START))
            {
                OnConnectionEstablishedMessageReceived(message);

                if (_infoLoggingEnabled)
                {
                    Logger.LogInformation(string.Format(Strings.LogInformation_ConnectedToNodeJSProcess, _nodeJSProcess.SafeID));
                }

                if (_debugLoggingEnabled)
                {
                    Logger.LogDebug(string.Format(Strings.LogDebug_OutOfProcessNodeJSService_BeforeSet, Thread.CurrentThread.ManagedThreadId.ToString()));
                }

                waitHandle.Set();
            }
            else if (_infoLoggingEnabled)
            {
                Logger.LogInformation(message);
            }
        }

        internal void ErrorReceivedHandler(object _, string message)
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
                _fileWatcher?.Dispose();
                _invokeTaskCreationCountdown?.Dispose();
            }

            _disposed = true;
        }
    }
}
