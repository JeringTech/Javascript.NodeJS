using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Jering.JavascriptUtils.NodeJS
{
    /// <summary>
    /// <para>The primary responsibilities of this class are launching and maintaining a Node.js process.</para>
    /// <para>
    /// This abstract base class uses the input/output streams of the child process to perform a simple handshake
    /// to determine when the child process is ready to accept invocations. This is agnostic to the mechanism that
    /// derived classes use to actually perform the invocations (e.g., they could use HTTP-RPC, or a binary TCP
    /// protocol, or any other RPC-type mechanism).
    /// </para>
    /// </summary>
    /// <seealso cref="INodeJSService" />
    public abstract class OutOfProcessNodeJSService : INodeJSService
    {
        protected const string CONNECTION_ESTABLISHED_MESSAGE_START = "[Jering.JavascriptUtils.NodeJS: Listening on ";

        protected readonly ILogger NodeJSServiceLogger;
        private readonly IEmbeddedResourcesService _embeddedResourcesService;
        private readonly INodeJSProcessFactory _nodeProcessFactory;
        private readonly string _serverScriptName;
        private readonly Assembly _serverScriptAssembly;
        private readonly OutOfProcessNodeJSServiceOptions _options;
        private readonly SemaphoreSlim _processSemaphore = new SemaphoreSlim(1, 1);

        private Process _nodeProcess;
        private bool _connected;
        private bool _disposed;

        /// <summary>
        /// Creates a new instance of <see cref="OutOfProcessNodeJSService"/>.
        /// </summary>
        /// <param name="nodeProcessFactory"></param>
        /// <param name="nodeJSServiceLogger">The <see cref="ILogger"/> to which the Node.js process's stdout/stderr (and other log information) will be redirected to.</param>
        /// <param name="optionsAccessor"></param>
        /// <param name="embeddedResourcesService"></param>
        /// <param name="serverScriptAssembly"></param>
        /// <param name="serverScriptName"></param>
        protected OutOfProcessNodeJSService(INodeJSProcessFactory nodeProcessFactory,
            ILogger nodeJSServiceLogger,
            IOptions<OutOfProcessNodeJSServiceOptions> optionsAccessor,
            IEmbeddedResourcesService embeddedResourcesService,
            Assembly serverScriptAssembly,
            string serverScriptName)
        {
            _nodeProcessFactory = nodeProcessFactory;
            NodeJSServiceLogger = nodeJSServiceLogger;
            _options = optionsAccessor?.Value ?? new OutOfProcessNodeJSServiceOptions();
            _embeddedResourcesService = embeddedResourcesService;
            _serverScriptName = serverScriptName;
            _serverScriptAssembly = serverScriptAssembly;
        }

        /// <summary>
        /// Asynchronously invokes code in the Node.js instance.
        /// </summary>
        /// <typeparam name="T">The JSON-serializable data type that the Node.js code will asynchronously return.</typeparam>
        /// <param name="invocationRequest">Contains the data to be sent to the Node.js process.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the invocation.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the completion of the RPC call.</returns>
        protected abstract Task<(bool, T)> TryInvokeAsync<T>(InvocationRequest invocationRequest, CancellationToken cancellationToken);

        /// <summary>
        /// Called when the connection established message from the Node.js process is received. The server script can be used to customize the message to provide
        /// information on the server, such as the port is is listening on.
        /// </summary>
        /// <param name="connectionEstablishedMessage"></param>
        protected abstract void OnConnectionEstablishedMessageReceived(string connectionEstablishedMessage);

        public async Task<T> InvokeFromFileAsync<T>(string modulePath, string exportName = null, object[] args = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var invocationRequest = new InvocationRequest(ModuleSourceType.File,
                    modulePath,
                    exportName: exportName,
                    args: args);

            return (await TryInvokeCoreAsync<T>(invocationRequest, cancellationToken).ConfigureAwait(false)).Item2;
        }

        public async Task<T> InvokeFromStringAsync<T>(string moduleString, string newCacheIdentifier = null, string exportName = null, object[] args = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var invocationRequest = new InvocationRequest(ModuleSourceType.String,
                    moduleString,
                    newCacheIdentifier,
                    exportName,
                    args);

            return (await TryInvokeCoreAsync<T>(invocationRequest, cancellationToken).ConfigureAwait(false)).Item2;
        }

        public async Task<T> InvokeFromStreamAsync<T>(Stream moduleStream, string newCacheIdentifier = null, string exportName = null, object[] args = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var invocationRequest = new InvocationRequest(ModuleSourceType.Stream,
                    newCacheIdentifier: newCacheIdentifier,
                    exportName: exportName,
                    args: args,
                    moduleStreamSource: moduleStream);

            return (await TryInvokeCoreAsync<T>(invocationRequest, cancellationToken).ConfigureAwait(false)).Item2;
        }

        public Task<(bool, T)> TryInvokeFromCacheAsync<T>(string moduleCacheIdentifier, string exportName = null, object[] args = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var invocationRequest = new InvocationRequest(ModuleSourceType.Cache,
                    moduleCacheIdentifier,
                    exportName: exportName,
                    args: args);

            return TryInvokeCoreAsync<T>(invocationRequest, cancellationToken);
        }

        internal async Task<(bool, T)> TryInvokeCoreAsync<T>(InvocationRequest invocationRequest, CancellationToken cancellationToken)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(OutOfProcessNodeJSService));
            }

            // Disposables
            CancellationTokenSource timeoutCTS = null;
            CancellationTokenSource combinedCTS = null;

            try
            {
                // Create combined CancellationToken only as required.
                if (_options.TimeoutMS > 0)
                {
                    timeoutCTS = new CancellationTokenSource();
                    timeoutCTS.CancelAfter(_options.TimeoutMS);

                    if (cancellationToken != CancellationToken.None)
                    {
                        combinedCTS = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCTS.Token);
                        cancellationToken = combinedCTS.Token;
                    }
                    else
                    {
                        cancellationToken = timeoutCTS.Token;
                    }
                }

                // Once ConnectToInputOutStreams is called, _nodeProcess will not be null and _nodeProcess.HasExited will be false.
                // Therefore, there is no risk of _connected being set to false while waiting for or after receiving the connection established
                // message.
                if (_nodeProcess?.HasExited != false)
                {
                    _connected = false;
                }

                // If the the Node.js process has not been started has has been terminated for some reason, attempt to create a 
                // new process. Apart from the thread creating the process, all other threads will be blocked. If the new process 
                // is created successfully, all threads will be released by the OutputDataReceived delegate in 
                // ConnectToInputOutputStreams.
                if (!_connected)
                {
                    if (NodeJSServiceLogger.IsEnabled(LogLevel.Debug))
                    {
                        NodeJSServiceLogger.LogDebug($"Before first semaphore wait, count: {_processSemaphore.CurrentCount}. Thread ID: {Thread.CurrentThread.ManagedThreadId.ToString()}");
                    }

                    await _processSemaphore.WaitAsync().ConfigureAwait(false);

                    // Double checked lock
                    if (!_connected)
                    {
                        // Even though process has exited, dispose instance to release handle - https://docs.microsoft.com/en-sg/dotnet/api/system.diagnostics.process?view=netframework-4.7.1
                        _nodeProcess?.Dispose();
                        string serverScript = _embeddedResourcesService.ReadAsString(_serverScriptAssembly, _serverScriptName);
                        _nodeProcess = _nodeProcessFactory.Create(serverScript);
                        ConnectToInputOutputStreams(_nodeProcess);

                        if (NodeJSServiceLogger.IsEnabled(LogLevel.Debug))
                        {
                            NodeJSServiceLogger.LogDebug($"Before second semaphore wait, count: {_processSemaphore.CurrentCount}. Thread ID: {Thread.CurrentThread.ManagedThreadId.ToString()}");
                        }

                        await _processSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                    }
                }

                return await TryInvokeAsync<T>(invocationRequest, cancellationToken).ConfigureAwait(false);

            }
            catch (Exception exception)
            {
                timeoutCTS?.Dispose();
                combinedCTS?.Dispose();

                if (!(exception is OperationCanceledException))
                {
                    throw;
                }

                if (_nodeProcess == null)
                {
                    // This is very unlikely
                    throw new InvocationException(
                        $"Attempt to connect to Node timed out after {_options.TimeoutMS}ms.",
                        string.Empty);
                }

                // Developers encounter this fairly often (if their Node code fails without invoking the callback,
                // all that the .NET side knows is that the invocation eventually times out). Previously, this surfaced
                // as a TaskCanceledException, but this led to a lot of issue reports. Now we throw the following
                // descriptive error.
                throw new InvocationException(
                    $"The Node invocation timed out after {_options.TimeoutMS}ms.",
                    $"You can change the timeout duration by setting the {nameof(OutOfProcessNodeJSServiceOptions.TimeoutMS)} "
                    + $"property on {nameof(OutOfProcessNodeJSServiceOptions)}.\n\n"
                    + "The first debugging step is to ensure that your Node.js function always invokes the supplied "
                    + "callback (or throws an exception synchronously), even if it encounters an error. Otherwise, "
                    + "the .NET code has no way to know that it is finished or has failed."
                );
            }
        }

        protected virtual void ConnectToInputOutputStreams(Process nodeProcess)
        {
            var outputStringBuilder = new StringBuilder();
            var errorStringBuilder = new StringBuilder();

            nodeProcess.OutputDataReceived += (sender, evt) =>
            {
                if (evt.Data == null)
                {
                    return;
                }

                if (evt.Data.StartsWith(CONNECTION_ESTABLISHED_MESSAGE_START) && !_connected)
                {
                    OnConnectionEstablishedMessageReceived(evt.Data);
                    _connected = true;

                    // Release all threads by resetting CurrentCount to 1
                    while (_processSemaphore.CurrentCount < 1)
                    {
                        if (NodeJSServiceLogger.IsEnabled(LogLevel.Debug))
                        {
                            NodeJSServiceLogger.LogDebug($"Releasing semaphore, count: {_processSemaphore.CurrentCount}. Thread ID: {Thread.CurrentThread.ManagedThreadId.ToString()}");
                        }

                        _processSemaphore.Release();
                    }
                }
                else
                {
                    // Process output is received line by line. The last line of a message ends with a \0 (null character),
                    // so we accumulate lines in a StringBuilder till the \0, then log the entire message in one go.
                    if (TryCreateMessage(outputStringBuilder, evt.Data, out string message))
                    {
                        NodeJSServiceLogger.LogDebug(message);
                    }
                }
            };

            nodeProcess.ErrorDataReceived += (sender, evt) =>
            {
                if (evt.Data == null)
                {
                    return;
                }

                if (TryCreateMessage(errorStringBuilder, evt.Data, out string message))
                {
                    NodeJSServiceLogger.LogError(message);
                }
            };

            nodeProcess.BeginOutputReadLine();
            nodeProcess.BeginErrorReadLine();
        }

        private bool TryCreateMessage(StringBuilder stringBuilder, string newLine, out string message)
        {
            stringBuilder.AppendLine(newLine);

            if (stringBuilder[stringBuilder.Length - 1] != '\0')
            {
                message = null;
                return false;
            }

            stringBuilder.Length--;
            message = stringBuilder.ToString();
            stringBuilder.Length = 0;

            return true;
        }

        /// <summary>
        /// Disposes this instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the instance.
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
                _processSemaphore.Dispose();
            }

            // Ensure that node process gets killed
            if (_nodeProcess?.HasExited == false)
            {
                _nodeProcess.Kill();
                // Give async output some time to push its messages
                // TODO this can throw, is it safe to call in the finalizer?
                _nodeProcess.WaitForExit(500);
                _nodeProcess.Dispose();
            }

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