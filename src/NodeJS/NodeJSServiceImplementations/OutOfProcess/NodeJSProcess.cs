using System;
using System.Diagnostics;
using System.Text;
using System.Diagnostics.CodeAnalysis;

namespace Jering.Javascript.NodeJS
{
    /// <summary>
    /// <para>Represents the method that will handle the message received event of a process.</para>
    /// <para>This method is a convenience-alternative to <see cref="DataReceivedEventHandler"/> which handles each line of a message.</para>
    /// </summary>
    /// <param name="message">The message.</param>
    public delegate void MessageReceivedEventHandler(string message);

    /// <summary>
    /// The default implementation of <see cref="INodeJSProcess"/>.
    /// </summary>
    public class NodeJSProcess : INodeJSProcess
    {
        internal const string EXIT_STATUS_NOT_EXITED = "Process has not exited";
        internal const string EXIT_STATUS_DISPOSED = "Process has been disposed";

        private readonly Process _process;
        private readonly object _lock = new();
        private bool _connected;
        private volatile bool _disposed; // Used in double checked lock
        private readonly StringBuilder _outputDataStringBuilder;
        private MessageReceivedEventHandler? _outputReceivedHandler;
        private bool _internalOutputDataReceivedHandlerAdded;
        private readonly StringBuilder _errorDataStringBuilder;
        private MessageReceivedEventHandler? _errorReceivedHandler;
        private bool _internalErrorDataReceivedHandlerAdded;

        /// <summary>
        /// Creates a <see cref="NodeJSProcess"/>.
        /// </summary>
        /// <param name="process">The NodeJS process.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="process"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if the <paramref name="process"/> has exited.</exception>
        /// <exception cref="ArgumentException">Thrown if the <paramref name="process"/> has not started or has been disposed of.</exception>
        public NodeJSProcess(Process process) : this(process, new StringBuilder(), new StringBuilder()) // TODO consider using a stringbuilder pool
        {
            if (process == null)
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
        }

        // Without this we can't test without spawning lots of processes
        internal NodeJSProcess(Process process,
            StringBuilder outputDataStringBuilder,
            StringBuilder errorDataStringBuilder)
        {
            _process = process;

            _outputDataStringBuilder = outputDataStringBuilder;
            _internalOutputDataReceivedHandlerAdded = false;
            _errorDataStringBuilder = errorDataStringBuilder;
            _internalErrorDataReceivedHandlerAdded = false;
        }

        /// <inheritdoc />
        public virtual void AddOutputReceivedHandler(MessageReceivedEventHandler messageReceivedEventHandler)
        {
            _outputReceivedHandler += messageReceivedEventHandler;

            if (!_internalOutputDataReceivedHandlerAdded)
            {
                AddOutputDataReceivedHandler(InternalOutputDataReceivedHandler);
                _internalOutputDataReceivedHandlerAdded = true;
            }
        }

        /// <inheritdoc />
        public virtual void AddErrorReceivedHandler(MessageReceivedEventHandler messageReceivedEventHandler)
        {
            _errorReceivedHandler += messageReceivedEventHandler;

            if (!_internalErrorDataReceivedHandlerAdded)
            {
                AddErrorDataReceivedHandler(InternalErrorDataReceivedHandler);
                _internalErrorDataReceivedHandlerAdded = true;
            }
        }

        /// <inheritdoc />
        public virtual void AddOutputDataReceivedHandler(DataReceivedEventHandler dataReceivedEventHandler)
        {
            _process.OutputDataReceived += dataReceivedEventHandler;
        }

        /// <inheritdoc />
        public virtual void AddErrorDataReceivedHandler(DataReceivedEventHandler dataReceivedEventHandler)
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
            lock (_lock)
            {
                _connected = true;
            }
        }

        /// <inheritdoc />
        public virtual string ExitStatus
        {
            get
            {
                lock (_lock)
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
                lock (_lock)
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
                lock (_lock)
                {
                    return !HasExited && _connected;
                }
            }
        }

        /// <inheritdoc />
        public void Kill()
        {
            _process.Kill();
        }

        /// <inheritdoc />
        public int SafeID
        {
            get
            {
                try
                {
                    return _process.Id;
                }
                catch
                {
                    return -1;
                }
            }
        }

        internal virtual void InternalOutputDataReceivedHandler(object _, DataReceivedEventArgs dataReceivedEventArgs)
        {
            DataReceivedHandler(_outputDataStringBuilder, _outputReceivedHandler!, dataReceivedEventArgs); // _outputReceivedHandler is assigned a value before this method is called
        }

        internal virtual void InternalErrorDataReceivedHandler(object _, DataReceivedEventArgs dataReceivedEventArgs)
        {
            DataReceivedHandler(_errorDataStringBuilder, _errorReceivedHandler!, dataReceivedEventArgs); // _errorReceivedHandler is assigned a value before this method is called
        }

        internal virtual void DataReceivedHandler(StringBuilder stringBuilder,
            MessageReceivedEventHandler messageReceivedEventHandler,
            DataReceivedEventArgs dataReceivedEventArgs)
        {
            string? data = dataReceivedEventArgs.Data;

            // Process output is received line by line. The last line of a message ends with a \0 (null character),
            // so we accumulate lines in a StringBuilder till the \0, then log the entire message in one go.
            if (TryCreateMessage(stringBuilder, data, out string? result))
            {
                messageReceivedEventHandler(result);
            }
        }

        // OutputDataReceivedHandler and ErrorDataReceivedHandler are called every time a newline character is read in the stdout and stderr streams respectively.
        // The event data supplied to callbacks is a string containing all the characters between the previous newline character and the most recent one.
        // In other words, streams are read line by line. The last line in each message ends with a null terminating character (see HttpServer.ts).
        internal virtual bool TryCreateMessage(StringBuilder stringBuilder, string? data, [NotNullWhen(true)] out string? message)
        {
            message = null;

            // When the stream is closed, an event with data = null is received - https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.datareceivedeventargs.data?view=netframework-4.8#remarks
            if (data != null)
            {
                int dataLength = data.Length;

                if (dataLength == 0) // Empty line
                {
                    stringBuilder.AppendLine();
                    return false;
                }

                if (data[dataLength - 1] != '\0')
                {
                    stringBuilder.AppendLine(data);
                    return false;
                }

                stringBuilder.Append(data);
                stringBuilder.Length--; // Remove null terminating character
            }
            else if (stringBuilder.Length == 0)
            {
                return false;
            }

            message = stringBuilder.ToString();
            stringBuilder.Length = 0;

            return true;
        }

        /// <summary>
        /// Kills and disposes of the NodeJS process.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this); // In case a sub class overrides Object.Finalize - https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-dispose#the-dispose-overload
        }

        /// <summary>
        /// Kills and disposes of the NodeJS process.
        /// </summary>
        /// <remarks>This method is thread-safe.</remarks>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            lock (_lock)
            {
                if (_disposed)
                {
                    return;
                }

                if (disposing) // If this method was called by a finalizer, we shouldn't try to release managed resources - https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-dispose#the-disposeboolean-overload
                {
                    // A finalizer can run even if an object's constructor never completed, to be safe, use null conditional operator.
                    if (_process?.HasExited == false)
                    {
                        try
                        {
                            _process?.Kill();
                        }
                        catch
                        {
                            // Throws if process is already dead, note that process could die between HasExited check and Kill
                        }
                    }
                    _process?.Dispose();
                }

                _disposed = true;
                _connected = false;
            }
        }
    }
}
