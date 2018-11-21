using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nito.AsyncEx.Interop;
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
        private readonly SemaphoreSlim _processSemaphore = new SemaphoreSlim(1, 1); // Use a semaphore since it doesn't have thread affinity
        private bool _disposed;
        private INodeJSProcess _nodeJSProcess;
        private readonly StringBuilder _outputDataStringBuilder = new StringBuilder();
        private readonly StringBuilder _errorDataStringBuilder = new StringBuilder();

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
        public async Task<T> InvokeFromFileAsync<T>(string modulePath, string exportName = null, object[] args = null, CancellationToken cancellationToken = default)
        {
            var invocationRequest = new InvocationRequest(ModuleSourceType.File,
                    modulePath,
                    exportName: exportName,
                    args: args);

            return (await TryInvokeCoreAsync<T>(invocationRequest, cancellationToken).ConfigureAwait(false)).Item2;
        }

        /// <inheritdoc />
        public async Task<T> InvokeFromStringAsync<T>(string moduleString, string newCacheIdentifier = null, string exportName = null, object[] args = null, CancellationToken cancellationToken = default)
        {
            var invocationRequest = new InvocationRequest(ModuleSourceType.String,
                    moduleString,
                    newCacheIdentifier,
                    exportName,
                    args);

            return (await TryInvokeCoreAsync<T>(invocationRequest, cancellationToken).ConfigureAwait(false)).Item2;
        }

        /// <inheritdoc />
        public async Task<T> InvokeFromStreamAsync<T>(Stream moduleStream, string newCacheIdentifier = null, string exportName = null, object[] args = null, CancellationToken cancellationToken = default)
        {
            var invocationRequest = new InvocationRequest(ModuleSourceType.Stream,
                    newCacheIdentifier: newCacheIdentifier,
                    exportName: exportName,
                    args: args,
                    moduleStreamSource: moduleStream);

            return (await TryInvokeCoreAsync<T>(invocationRequest, cancellationToken).ConfigureAwait(false)).Item2;
        }

        /// <inheritdoc />
        public Task<(bool, T)> TryInvokeFromCacheAsync<T>(string moduleCacheIdentifier, string exportName = null, object[] args = null, CancellationToken cancellationToken = default)
        {
            var invocationRequest = new InvocationRequest(ModuleSourceType.Cache,
                    moduleCacheIdentifier,
                    exportName: exportName,
                    args: args);

            return TryInvokeCoreAsync<T>(invocationRequest, cancellationToken);
        }

        internal virtual async Task<(bool, T)> TryInvokeCoreAsync<T>(InvocationRequest invocationRequest, CancellationToken cancellationToken)
        {
            CancellationTokenSource cancellationTokenSource = null;
            int numRetries = _options.NumRetries;

            while (true)
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(OutOfProcessNodeJSService));
                }

                try
                {
                    // If the the NodeJS process has not been instantiated or has been disconnected for some reason, attempt to create a 
                    // new process. Apart from the thread creating the process, all other threads will be blocked. If the new process 
                    // is created successfully, all threads will be released by OutputDataReceivedHandler.
                    if (_nodeJSProcess?.Connected != true)
                    {
                        if (_debugLoggingEnabled)
                        {
                            Logger.LogDebug(Strings.LogDebug_OutOfProcessNodeJSService_BeforeSemaphoreWait, _processSemaphore.CurrentCount, Thread.CurrentThread.ManagedThreadId.ToString());
                        }

                        await _processSemaphore.WaitAsync().ConfigureAwait(false);

                        // Double checked lock
                        if (_nodeJSProcess?.Connected != true)
                        {
                            CreateAndConnectToNodeJSProcess();

                            if (_debugLoggingEnabled)
                            {
                                Logger.LogDebug(string.Format(Strings.LogDebug_OutOfProcessNodeJSService_BeforeSempahoreAvailableWaitHandleWait, _processSemaphore.CurrentCount, Thread.CurrentThread.ManagedThreadId.ToString()));
                            }

                            // Use AvailableWaitHandle since in the event that the process connects before this line, AvailableWaitHandle doesn't decrement _processSemaphore.CurrentCount.
                            // Also, don't pass cancellationToken as an argument - by this point, we've already started a NodeJS process, even if this invocation is canceled, allow the NodeJS process to 
                            // connect for future invocations.
                            if (!await WaitHandleAsyncFactory.FromWaitHandle(_processSemaphore.AvailableWaitHandle, TimeSpan.FromMilliseconds(_options.TimeoutMS)).ConfigureAwait(false))
                            {
                                // Allow another thread to have a go at connecting
                                _processSemaphore.Release();

                                // We're unlikely to get to this point. If we do we want the issue to be logged.
                                throw new InvocationException(string.Format(Strings.InvocationException_OutOfProcessNodeJSService_ConnectionAttemptTimedOut,
                                    _options.TimeoutMS,
                                    _nodeJSProcess.HasExited,
                                    _nodeJSProcess.ExitCode));
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
                    if (_warningLoggingEnabled)
                    {
                        Logger.LogWarning(string.Format(Strings.LogWarning_InvocationAttemptFailed, numRetries < 0 ? "infinity" : numRetries.ToString(), exception.ToString()));
                    }
                }
                finally
                {
                    cancellationTokenSource?.Dispose();
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

        internal virtual void CreateAndConnectToNodeJSProcess()
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
            _nodeJSProcess.AddOutputDataReceivedHandler(OutputDataReceivedHandler);
            _nodeJSProcess.AddErrorDataReceivedHandler(ErrorDataReceivedHandler);
            _nodeJSProcess.BeginOutputReadLine();
            _nodeJSProcess.BeginErrorReadLine();
        }

        internal void OutputDataReceivedHandler(object sender, DataReceivedEventArgs evt)
        {
            if (evt.Data == null)
            {
                return;
            }

            if (!_nodeJSProcess.Connected && evt.Data.StartsWith(CONNECTION_ESTABLISHED_MESSAGE_START))
            {
                OnConnectionEstablishedMessageReceived(evt.Data);

                lock (_nodeJSProcess.Lock) // Prevent any threads from getting to _processSemaphore.WaitAsync in TryInvokeCoreAsync<T> until all threads have been released here.
                {
                    _nodeJSProcess.SetConnected();

                    // Release all threads by resetting CurrentCount to 1
                    while (_processSemaphore.CurrentCount < 1)
                    {
                        if (_debugLoggingEnabled)
                        {
                            Logger.LogDebug(string.Format(Strings.LogDebug_OutOfProcessNodeJSService_ReleasingSemaphore, _processSemaphore.CurrentCount, Thread.CurrentThread.ManagedThreadId.ToString()));
                        }

                        _processSemaphore.Release();
                    }
                }
            }
            else if (TryCreateMessage(_outputDataStringBuilder, evt.Data, out string message))
            {
                // Process output is received line by line. The last line of a message ends with a \0 (null character),
                // so we accumulate lines in a StringBuilder till the \0, then log the entire message in one go.
                Logger.LogInformation(message);
            }
        }

        internal void ErrorDataReceivedHandler(object sender, DataReceivedEventArgs evt)
        {
            if (evt.Data == null)
            {
                return;
            }

            if (TryCreateMessage(_errorDataStringBuilder, evt.Data, out string message))
            {
                Logger.LogError(message);
            }
        }

        internal virtual bool TryCreateMessage(StringBuilder stringBuilder, string data, out string message)
        {
            // OutputDataReceived is called everytime a newline character is read. The event data supplied to the callback
            // is a string containing all the characters between the previous newline character and the most recent one.
            // In other words, the stream is read line by line. The last line in each message ends with a null terminating 
            // character (see HttpServer).
            if (data[data.Length - 1] != '\0')
            {
                stringBuilder.AppendLine(data);
                message = null;
                return false;
            }

            stringBuilder.Append(data);
            stringBuilder.Length--; // Remove null terminating character
            message = stringBuilder.ToString();
            stringBuilder.Length = 0;

            return true;
        }

        /// <summary>
        /// Disposes this instance. This method is not thread-safe. It should only be called after all other calls to this instance's methods have returned.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
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

            // This call isn't thread-safe - https://docs.microsoft.com/en-us/dotnet/api/system.threading.semaphoreslim.dispose?view=netstandard-2.0.
            // It is the reason why this method and this class's Dispose() method are not thread-safe.
            // Since this class uses AvailableWaitHandle, dispose of the sempahore to release unmanaged resources
            _processSemaphore.Dispose();

            _nodeJSProcess?.Dispose();
            _disposed = true;
        }

        /// <summary>
        /// Implements the finalization part of the IDisposable pattern by calling Dispose(false).
        /// </summary>
        ~OutOfProcessNodeJSService()
        {
            Dispose(false);
        }
    }
}