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
        /// <para>Adds a handler for output messages.</para>
        /// <para>The added handler is more convenient than what you'd add with <see cref="AddOutputDataReceivedHandler(DataReceivedEventHandler)"/>:
        /// when the NodeJS process writes to stdout, the <see cref="DataReceivedEventHandler"/> fires once for each line written.
        /// The handler added by this method fires only when a complete message has been built. This way subscribers don't need to manually build messages.</para>
        /// <para>This method is not thread-safe.</para>
        /// </summary>
        /// <param name="messageReceivedHandler">The handler to add.</param>
        void AddOutputReceivedHandler(MessageReceivedEventHandler messageReceivedHandler);

        /// <summary>
        /// <para>Adds a handler for error messages.</para>
        /// <para>The added handler is more convenient than what you'd add with <see cref="AddErrorDataReceivedHandler(DataReceivedEventHandler)"/>:
        /// when the NodeJS process writes to stderr, the <see cref="DataReceivedEventHandler"/> fires once for each line written.
        /// The handler added by this method fires only when a complete message has been built. This way subscribers don't need to manually build messages.</para>
        /// <para>This method is not thread-safe.</para>
        /// </summary>
        /// <param name="messageReceivedHandler">The handler to add.</param>
        void AddErrorReceivedHandler(MessageReceivedEventHandler messageReceivedHandler);

        /// <summary>
        /// Adds a <see cref="Process.OutputDataReceived"/> handler to the NodeJS process.
        /// </summary>
        /// <param name="dataReceivedEventHandler">The handler to add.</param>
        void AddOutputDataReceivedHandler(DataReceivedEventHandler dataReceivedEventHandler);

        /// <summary>
        /// Adds a <see cref="Process.ErrorDataReceived"/> handler to the NodeJS process.
        /// </summary>
        /// <param name="dataReceivedEventHandler">The handler to add.</param>
        void AddErrorDataReceivedHandler(DataReceivedEventHandler dataReceivedEventHandler);

        /// <summary>
        /// Gets a boolean value indicating whether or not the NodeJS process is connected.
        /// </summary>
        bool Connected { get; }

        /// <summary>
        /// Gets a boolean value indicating whether or not the NodeJS process has been exited.
        /// </summary>
        bool HasExited { get; }

        /// <summary>
        /// <para>Gets a string describing the NodeJS process's exit status.</para>
        /// <para>If the process has not exited, returns "Process has not exited".</para>
        /// <para>If the process has been disposed, returns "Process has been disposed".</para>
        /// <para>If the process has exit but has not been disposed, returns its exit code.</para>
        /// </summary>
        string ExitStatus { get; }

        /// <summary>
        /// Sets the NodeJS process's connected state to true.
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

        /// <summary>
        /// Kills the underlying NodeJS process.
        /// </summary>
        void Kill();
    }
}
