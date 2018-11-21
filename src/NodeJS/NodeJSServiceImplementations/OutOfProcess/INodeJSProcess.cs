using System;
using System.Diagnostics;

namespace Jering.Javascript.NodeJS
{
    /// <summary>
    /// <para>A thread-safe abstraction for a NodeJS process.</para>
    /// <para>This abstraction exists for these reasons:</para>
    /// <list type="bullet">
    /// <item>Accessing properties of disposed process objects causes exceptions. For example, after calling MyProcess.Dispose(), 
    /// calling MyProcess.HasExited throws an InvalidOperationException. This abstraction prevents such exceptions by tracking
    /// the disposed status of the process in a thread safe manner.</item>
    /// <item>NodeJS processes have an extra state: connected. This abstraction wraps the state together with the process.</item>
    /// <item>This abstraction allows for more simpler and more thorough tests through mocking.</item>
    /// </list>
    /// </summary>
    public interface INodeJSProcess : IDisposable
    {
        /// <summary>
        /// Gets the lock used to synchronize operations in this NodeJS process.
        /// </summary>
        object Lock { get; }

        /// <summary>
        /// Gets a boolean value indicating whether or not this NodeJS process is connected.
        /// </summary>
        bool Connected { get; }

        /// <summary>
        /// Gets a boolean value indicating whether or not this NodeJS process has been exited.
        /// </summary>
        bool HasExited { get; }

        /// <summary>
        /// <para>Gets a string describing this NodeJS process's exit code.</para>
        /// <para>If the process has not exited, returns "Process has not exited".</para>
        /// <para>If the process has been disposed, returns "Process has been disposed".</para>
        /// </summary>
        string ExitCode { get; }

        /// <summary>
        /// Adds a <see cref="Process.OutputDataReceived"/> handler to this NodeJS process.
        /// </summary>
        /// <param name="dataReceivedEventHandler">The handler to add.</param>
        void AddOutputDataReceivedHandler(DataReceivedEventHandler dataReceivedEventHandler);

        /// <summary>
        /// Adds a <see cref="Process.ErrorDataReceived"/> handler to this NodeJS process.
        /// </summary>
        /// <param name="dataReceivedEventHandler">The handler to add.</param>
        void AddErrorDataReceivedHandler(DataReceivedEventHandler dataReceivedEventHandler);

        /// <summary>
        /// Sets this NodeJS process's connected state to true.
        /// </summary>
        void SetConnected();

        /// <summary>
        /// Begins asynchronous read operations on the redirected <see cref="Process.StandardError"/> stream of the application.
        /// </summary>
        void BeginErrorReadLine();

        /// <summary>
        /// Begins asynchronous read operations on the redirected <see cref="Process.StandardOutput"/> stream of the application.
        /// </summary>
        void BeginOutputReadLine();
    }
}
