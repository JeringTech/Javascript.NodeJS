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
        private readonly object _lock = new object();
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
            lock (_lock)
            {
                _connected = true;
            }
        }

        /// <inheritdoc />
        public string ExitCode
        {
            get
            {
                lock (_lock)
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
                lock (_lock)
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
                lock (_lock)
                {
                    return !HasExited && _connected;
                }
            }
        }

        /// <inheritdoc />
        public void DisposeIfNotConnected()
        {
            lock (_lock)
            {
                if (!Connected)
                {
                    Dispose();
                }
            }
        }

        /// <summary>
        /// Kills and disposes of this instance's underlying NodeJS <see cref="Process"/>.
        /// </summary>
        public void Dispose()
        {
            lock (_lock)
            {
                _process.Kill();
                // Give async output some time to push its messages
                _process.WaitForExit(500);
                // After process has exited, dispose of instance to release handle - https://docs.microsoft.com/en-sg/dotnet/api/system.diagnostics.process?view=netframework-4.7.1
                _process.Dispose();

                _disposed = true;
                _connected = false;
            }
        }
    }
}
