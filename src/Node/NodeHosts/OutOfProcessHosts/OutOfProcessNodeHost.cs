using Jering.JavascriptUtils.Node.Node.OutOfProcessHosts;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Jering.JavascriptUtils.Node.HostingModels
{
    /// <summary>
    /// Class responsible for launching a Node child process on the local machine, determining when it is ready to
    /// accept invocations, detecting if it dies on its own, and finally terminating it on disposal.
    ///
    /// This abstract base class uses the input/output streams of the child process to perform a simple handshake
    /// to determine when the child process is ready to accept invocations. This is agnostic to the mechanism that
    /// derived classes use to actually perform the invocations (e.g., they could use HTTP-RPC, or a binary TCP
    /// protocol, or any other RPC-type mechanism).
    /// </summary>
    /// <seealso cref="INodeHost" />
    public abstract class OutOfProcessNodeHost : INodeHost
    {
        private const string CONNECTION_ESTABLISHED_MESSAGE = "[Jering.JavascriptUtils.Node:Listening]";

        /// <summary>
        /// The <see cref="ILogger"/> to which the Node.js instance's stdout/stderr is being redirected.
        /// </summary>
        private readonly ILogger _nodeOutputLogger;
        private readonly INodeProcessFactory _nodeProcessFactory;
        private readonly TaskCompletionSource<object> _connectionIsReadySource = new TaskCompletionSource<object>();
        private readonly OutOfProcessNodeHostOptions _options;
        private readonly string _nodeServerScript;

        private object _nodeProcessAccessLock = new object();
        private Process _nodeProcess;
        private bool _disposed;

        /// <summary>
        /// Creates a new instance of <see cref="OutOfProcessNodeHost"/>.
        /// </summary>
        /// <param name="nodeProcess">The Node.js process.</param>
        /// <param name="nodeOutputLogger">The <see cref="ILogger"/> to which the Node.js instance's stdout/stderr (and other log information) should be written.</param>
        /// <param name="invocationTimeoutMilliseconds">The maximum duration, in milliseconds, to wait for RPC calls to complete.</param>
        protected OutOfProcessNodeHost(INodeProcessFactory nodeProcessFactory, string nodeServerScript, ILogger nodeOutputLogger, OutOfProcessNodeHostOptions options)
        {
            _nodeProcessFactory = nodeProcessFactory;
            _nodeServerScript = nodeServerScript;
            _nodeOutputLogger = nodeOutputLogger ?? throw new ArgumentNullException(nameof(nodeOutputLogger));
            _options = options;
        }

        /// <summary>
        /// Asynchronously invokes code in the Node.js instance.
        /// </summary>
        /// <typeparam name="T">The JSON-serializable data type that the Node.js code will asynchronously return.</typeparam>
        /// <param name="invocationInfo">Specifies the Node.js function to be invoked and arguments to be passed to it.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the invocation.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the completion of the RPC call.</returns>
        protected abstract Task<T> InvokeExportAsync<T>(NodeInvocationInfo invocationInfo, CancellationToken cancellationToken);

        protected abstract void OnConnectionEstablishedMessageReceived(string connectionEstablishedMessage);

        // TODO implement - or abstract?
        public Task<T> InvokeExportFromFileAsync<T>(string relativePath, bool cache = true, string export = null, object[] args = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<T> InvokeExportFromStringAsync<T>(string module, string cacheIdentifier = null, string export = null, object[] args = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<T> InvokeExportFromStreamAsync<T>(Stream module, string cacheIdentifier = null, string export = null, object[] args = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<bool> TryInvokeExportFromCacheAsync<T>(string identifier, out T result, string export = null, object[] args = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Asynchronously invokes code in the Node.js instance.
        /// </summary>
        /// <typeparam name="T">The JSON-serializable data type that the Node.js code will asynchronously return.</typeparam>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the invocation.</param>
        /// <param name="moduleName">The path to the Node.js module (i.e., JavaScript file) relative to your project root that contains the code to be invoked.</param>
        /// <param name="exportNameOrNull">If set, specifies the CommonJS export to be invoked. If not set, the module's default CommonJS export itself must be a function to be invoked.</param>
        /// <param name="args">Any sequence of JSON-serializable arguments to be passed to the Node.js function.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the completion of the RPC call.</returns>
        public async Task<T> InvokeExportAsync<T>(CancellationToken cancellationToken, string moduleName, string exportNameOrNull, params object[] args)
        {
            if (_nodeProcess?.HasExited != false)
            {
                lock (_nodeProcessAccessLock)
                {
                    _nodeProcess?.Dispose();
                    _nodeProcess = _nodeProcessFactory.Create(_nodeServerScript);
                }
            }

            // Construct a new cancellation token that combines the supplied token with the configured invocation
            // timeout. Technically we could avoid wrapping the cancellationToken if no timeout is configured,
            // but that's not really a major use case, since timeouts are enabled by default.
            using (var timeoutSource = new CancellationTokenSource())
            using (CancellationTokenSource combinedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token))
            {
                if (_options.InvocationTimeoutMS > 0)
                {
                    timeoutSource.CancelAfter(_options.InvocationTimeoutMS);
                }

                // By overwriting the supplied cancellation token, we ensure that it isn't accidentally used
                // below. We only want to pass through the token that respects timeouts.
                cancellationToken = combinedCancellationTokenSource.Token;
                bool connectionSuccessful = false;

                try
                {
                    // Wait until the connection is established. This will throw if the connection fails to initialize,
                    // or if cancellation is requested first. Note that we can't really cancel the "establishing connection"
                    // task because that's shared with all callers, but we can stop waiting for it if this call is cancelled.
                    await _connectionIsReadySource.Task.OrThrowOnCancellation(cancellationToken).ConfigureAwait(false);
                    connectionSuccessful = true;

                    return await InvokeExportAsync<T>(new NodeInvocationInfo
                    {
                        ModuleName = moduleName,
                        ExportedFunctionName = exportNameOrNull,
                        Args = args
                    }, cancellationToken).ConfigureAwait(false);
                }
                catch (TaskCanceledException)
                {
                    if (timeoutSource.IsCancellationRequested)
                    {
                        // It was very common for developers to report 'TaskCanceledException' when encountering almost any
                        // trouble when using NodeServices. Now we have a default invocation timeout, and attempt to give
                        // a more descriptive exception message if it happens.
                        if (!connectionSuccessful)
                        {
                            // This is very unlikely, but for debugging, it's still useful to differentiate it from the
                            // case below.
                            throw new NodeInvocationException(
                                $"Attempt to connect to Node timed out after {_options.InvocationTimeoutMS}ms.",
                                string.Empty);
                        }
                        else
                        {
                            // Developers encounter this fairly often (if their Node code fails without invoking the callback,
                            // all that the .NET side knows is that the invocation eventually times out). Previously, this surfaced
                            // as a TaskCanceledException, but this led to a lot of issue reports. Now we throw the following
                            // descriptive error.
                            throw new NodeInvocationException(
                                $"The Node invocation timed out after {_options.InvocationTimeoutMS}ms.",
                                $"You can change the timeout duration by setting the {OutOfProcessNodeHostOptions.TimeoutConfigPropertyName} "
                                + $"property on {nameof(OutOfProcessNodeHostOptions)}.\n\n"
                                + "The first debugging step is to ensure that your Node.js function always invokes the supplied "
                                + "callback (or throws an exception synchronously), even if it encounters an error. Otherwise, "
                                + "the .NET code has no way to know that it is finished or has failed."
                            );
                        }
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        protected virtual void ConnectToInputOutputStreams(Process nodeProcess)
        {
            bool initializationIsCompleted = false;

            nodeProcess.OutputDataReceived += (sender, evt) =>
            {
                if (evt.Data.StartsWith(CONNECTION_ESTABLISHED_MESSAGE) && !initializationIsCompleted)
                {
                    OnConnectionEstablishedMessageReceived(evt.Data);
                    _connectionIsReadySource.SetResult(null);
                    initializationIsCompleted = true;
                }
                else if (evt.Data != null)
                {
                    _nodeOutputLogger.LogInformation(UnencodeNewlines(evt.Data));
                }
            };

            nodeProcess.ErrorDataReceived += (sender, evt) =>
            {
                if (evt.Data != null)
                {
                    if (IsDebuggerMessage(evt.Data))
                    {
                        _nodeOutputLogger.LogWarning(evt.Data);
                    }
                    else
                    {
                        _nodeOutputLogger.LogError(UnencodeNewlines(evt.Data));
                    }
                }
            };

            nodeProcess.BeginOutputReadLine();
            nodeProcess.BeginErrorReadLine();
        }

        private static string UnencodeNewlines(string str)
        {
            // TODO this is real ugly, just use a message terminator string like "\0"
            // use a string builder to accumulate till terminator is reached then construct and log output.
            if (str != null)
            {
                // The token here needs to match the const in OverrideStdOutputs.ts.
                // See the comment there for why we're doing this.
                str = str.Replace("__ns_newline__", Environment.NewLine);
            }

            return str;
        }

        private static bool IsDebuggerMessage(string message)
        {
            return message.StartsWith("Debugger attached", StringComparison.Ordinal) ||
                message.StartsWith("Debugger listening ", StringComparison.Ordinal) ||
                message.StartsWith("To start debugging", StringComparison.Ordinal) ||
                message.Equals("Warning: This is an experimental feature and could change at any time.", StringComparison.Ordinal) ||
                message.Equals("For help see https://nodejs.org/en/docs/inspector", StringComparison.Ordinal) ||
                message.Contains("chrome-devtools:");
        }

        /// <summary>
        /// Disposes this instance.
        /// </summary>
        public void Dispose()
        {
            DisposeCore();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the instance.
        /// </summary>
        /// <param name="disposing">True if the object is disposing or false if it is finalizing.</param>
        protected virtual void DisposeCore()
        {
            if (!_disposed)
            {
                // Make sure the Node process is finished
                // TODO: Is there a more graceful way to end it? Or does this still let it perform any cleanup?
                if (_nodeProcess?.HasExited == false)
                {
                    _nodeProcess.Kill();
                }

                _disposed = true;
            }
        }

        /// <summary>
        /// Implements the finalization part of the IDisposable pattern by calling Dispose(false).
        /// </summary>
        ~OutOfProcessNodeHost()
        {
            DisposeCore();
        }
    }
}