using System;
using System.Diagnostics;

namespace Jering.Javascript.NodeJS
{
    /// <summary>
    /// The default implementation of <see cref="INodeJSProcess"/>.
    /// </summary>
    public class NodeJSProcess : INodeJSProcess
    {
        internal const string EXIT_STATUS_NOT_EXITED = "Process has not exited";
        internal const string EXIT_STATUS_DISPOSED = "Process has been disposed";
        private readonly Process _process;
        private bool _connected;
        private bool _disposed;

        /// <summary>
        /// Creates a <see cref="NodeJSProcess"/> instance.
        /// </summary>
        /// <param name="process">The NodeJS process.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="process"/> is null.</exception>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="process"/> has exited.</exception>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="process"/> has not started or has been disposed of.</exception>
        public NodeJSProcess(Process process)
        {
            if(process == null)
            {
                throw new ArgumentNullException(nameof(process));
            }

            try
            {
                if (process.HasExited)
                {
                    throw new ArgumentException(Strings.ArgumentException_NodeJSProcess_ProcessHasExited, nameof(process));
                }
            }
            catch (InvalidOperationException exception)
            {
                throw new ArgumentException(Strings.ArgumentException_NodeJSProcess_ProcessHasNotBeenStartedOrHasBeenDisposed, nameof(process), exception);
            }

            _process = process;
        }

        /// <inheritdoc />
        public void AddOutputDataReceivedHandler(DataReceivedEventHandler dataReceivedEventHandler)
        {
            _process.OutputDataReceived += dataReceivedEventHandler;
        }

        /// <inheritdoc />
        public void AddErrorDataReceivedHandler(DataReceivedEventHandler dataReceivedEventHandler)
        {
            _process.ErrorDataReceived += dataReceivedEventHandler;
        }

        /// <inheritdoc />
        public void BeginOutputReadLine()
        {
            _process.BeginOutputReadLine();
        }

        /// <inheritdoc />
        public void BeginErrorReadLine()
        {
            _process.BeginErrorReadLine();
        }

        /// <inheritdoc />
        public virtual void SetConnected()
        {
            lock (Lock)
            {
                _connected = true;
            }
        }

        /// <inheritdoc />
        public virtual string ExitStatus
        {
            get
            {
                lock (Lock)
                {
                    if (HasExited)
                    {
                        return _disposed ? EXIT_STATUS_DISPOSED : _process.ExitCode.ToString();
                    }

                    return EXIT_STATUS_NOT_EXITED;
                }
            }
        }

        /// <inheritdoc />
        public virtual bool HasExited
        {
            get
            {
                lock (Lock)
                {
                    return _disposed || _process.HasExited;
                }
            }
        }

        /// <inheritdoc />
        public virtual bool Connected
        {
            get
            {
                lock (Lock)
                {
                    return !HasExited && _connected;
                }
            }
        }

        /// <inheritdoc />
        public object Lock { get; } = new object();

        /// <inheritdoc />
        public void Kill()
        {
            _process?.Kill();
        }

        /// <summary>
        /// Kills and disposes of this instance's underlying NodeJS <see cref="Process"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Kills and disposes of this instance's underlying NodeJS <see cref="Process"/>.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            lock (Lock)
            {
                if (_disposed)
                {
                    return;
                }

                // Unmanaged resource, so always call, even if called by finalizer
                try
                {
                    Kill();
                    // Give async output some time to push its messages
                    _process?.WaitForExit(500);
                }
                catch
                {
                    // Do nothing, since if kill fails, we can assume that the process is already dead
                }

                // After process has exited, dispose of instance to release handle - https://docs.microsoft.com/en-sg/dotnet/api/system.diagnostics.process?view=netframework-4.7.1
                _process?.Dispose();

                _disposed = true;
                _connected = false;
            }
        }

        /// <summary>
        /// Implements the finalization part of the IDisposable pattern by calling Dispose(false).
        /// </summary>
        ~NodeJSProcess()
        {
            Dispose(false);
        }
    }
}
