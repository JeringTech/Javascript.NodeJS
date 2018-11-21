using System;
using System.Diagnostics;

namespace Jering.Javascript.NodeJS
{
    /// <summary>
    /// The default implementation of <see cref="INodeJSProcess"/>.
    /// </summary>
    public class NodeJSProcess : INodeJSProcess
    {
        private readonly Process _process;
        private bool _connected;
        private bool _disposed;

        /// <summary>
        /// Creates a <see cref="NodeJSProcess"/> instance.
        /// </summary>
        /// <param name="process">The NodeJS process.</param>
        public NodeJSProcess(Process process)
        {
            _process = process ?? throw new ArgumentNullException(nameof(process));
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
        public void SetConnected()
        {
            lock (Lock)
            {
                _connected = true;
            }
        }

        /// <inheritdoc />
        public string ExitCode
        {
            get
            {
                lock (Lock)
                {
                    if (HasExited)
                    {
                        return _disposed ? "Process has been disposed" : _process.ExitCode.ToString();
                    }

                    return "Process has not exited";
                }
            }
        }

        /// <inheritdoc />
        public bool HasExited
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
        public bool Connected
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

                // Unmanaged resource, so always call, even if finalizer is parent method.
                _process.Kill();
                // Give async output some time to push its messages
                _process.WaitForExit(500);
                // After process has exited, dispose of instance to release handle - https://docs.microsoft.com/en-sg/dotnet/api/system.diagnostics.process?view=netframework-4.7.1
                _process.Dispose();

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
