using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
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
        private readonly IEmbeddedResourcesService _embeddedResourcesService;
        private readonly INodeJSProcessFactory _nodeProcessFactory;
        private readonly string _serverScriptName;
        private readonly Assembly _serverScriptAssembly;
        private readonly OutOfProcessNodeJSServiceOptions _options;
        private readonly object _connectingLock = new object();
        private bool _disposed;
        private volatile INodeJSProcess _nodeJSProcess; // Volatile since it's used in a double checked lock (we check whether it is null)

        /// <summary>
        /// Creates an<see cref="OutOfProcessNodeJSService"/> instance.
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

            using (Stream moduleStream = moduleFactory())
            {
                // If module doesn't exist in cache, create module stream and send it to the NodeJS process
                var invocationRequest = new InvocationRequest(ModuleSourceType.Stream, null, cacheIdentifier, exportName, args, moduleStream);

                return (await TryInvokeCoreAsync<T>(invocationRequest, cancellationToken).ConfigureAwait(false)).Item2;
            }
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
            int numRetries = _options.NumRetries;

            while (true)
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(OutOfProcessNodeJSService));
                }

                CancellationTokenSource cancellationTokenSource = null;
                EventWaitHandle waitHandle = null;
                try
                {
                    // If the the NodeJS process has not been instantiated or has been disconnected for some reason, attempt to create a 
                    // new process. Apart from the thread creating the process, all other threads will be blocked. If the new process 
                    // is created successfully, all threads will be released by OutputDataReceivedHandler.
                    if (_nodeJSProcess?.Connected != true) // Safe since _nodeJSProcess is volatile and its property getters enclose logic in lock blocks
                    {
                        lock (_connectingLock)
                        {
                            if (_nodeJSProcess?.Connected != true)
                            {
                                waitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

                                CreateAndConnectToNodeJSProcess(waitHandle);

                                if (_debugLoggingEnabled)
                                {
                                    Logger.LogDebug(string.Format(Strings.LogDebug_OutOfProcessNodeJSService_BeforeWait, Thread.CurrentThread.ManagedThreadId.ToString()));
                                }

                                if (waitHandle.WaitOne(_options.TimeoutMS < 0 ? -1 : _options.TimeoutMS))
                                {
                                    _nodeJSProcess.SetConnected();
                                }
                                else
                                {
                                    // Kills and disposes
                                    _nodeJSProcess.Dispose();

                                    // Reset
                                    _outputDataStringBuilder.Length = 0;
                                    _errorDataStringBuilder.Length = 0;

                                    // We're unlikely to get to this point. If we do we want the issue to be logged.
                                    throw new InvocationException(string.Format(Strings.InvocationException_OutOfProcessNodeJSService_ConnectionAttemptTimedOut,
                                        _options.TimeoutMS,
                                        _nodeJSProcess.HasExited,
                                        _nodeJSProcess.ExitStatus));
                                }
                            }
                        }
                    }

                    // Create cancellation token
                    CancellationToken invokeCancellationToken;
                    (invokeCancellationToken, cancellationTokenSource) = CreateCts(cancellationToken);

                    return await TryInvokeAsync<T>(invocationRequest, invokeCancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // Invocation canceled, don't retry
                    throw;
                }
                catch (OperationCanceledException) when (numRetries == 0)
                {
                    // Invocation timed out and no more retries
                    throw new InvocationException(string.Format(Strings.InvocationException_OutOfProcessNodeJSService_InvocationTimedOut,
                        _options.TimeoutMS,
                        nameof(OutOfProcessNodeJSServiceOptions.TimeoutMS),
                        nameof(OutOfProcessNodeJSServiceOptions)));
                }
                catch (Exception exception) when (numRetries != 0)
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
                    waitHandle?.Dispose();
                }

                numRetries = numRetries > 0 ? numRetries - 1 : numRetries;
            }
        }

        internal virtual (CancellationToken, CancellationTokenSource) CreateCts(CancellationToken cancellationToken)
        {
            if (_options.TimeoutMS >= 0)
            {
                var cancellationTokenSource = new CancellationTokenSource(_options.TimeoutMS);

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

        internal virtual void CreateAndConnectToNodeJSProcess(EventWaitHandle waitHandle)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(OutOfProcessNodeJSService));
            }

            // Avoid orphan processes
            _nodeJSProcess?.Dispose();

            // Create new process
            string serverScript = _embeddedResourcesService.ReadAsString(_serverScriptAssembly, _serverScriptName);
            _nodeJSProcess = _nodeProcessFactory.Create(serverScript);

            // Connect through stdout
            _nodeJSProcess.AddOutputReceivedHandler((object sender, string message) => OutputReceivedHandler(sender, message, waitHandle));
            _nodeJSProcess.AddErrorReceivedHandler(ErrorReceivedHandler);
            _nodeJSProcess.BeginOutputReadLine();
            _nodeJSProcess.BeginErrorReadLine();
        }

        internal void OutputReceivedHandler(object _, string message, EventWaitHandle waitHandle)
        {
            if (!_nodeJSProcess.Connected && message.StartsWith(CONNECTION_ESTABLISHED_MESSAGE_START))
            {
                OnConnectionEstablishedMessageReceived(message);

                if (_debugLoggingEnabled)
                {
                    Logger.LogDebug(string.Format(Strings.LogDebug_OutOfProcessNodeJSService_BeforeSet, Thread.CurrentThread.ManagedThreadId.ToString()));
                }

                waitHandle.Set();
            }
            else
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
            }

            _disposed = true;
        }
    }
}
