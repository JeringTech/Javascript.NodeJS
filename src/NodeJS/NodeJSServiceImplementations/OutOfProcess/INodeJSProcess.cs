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
#if NET5_0 || NET6_0 || NET7_0
    public interface INodeJSProcess : IAsyncDisposable, IDisposable
#else
    public interface INodeJSProcess : IDisposable
#endif
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
        /// Begins reading of stdout and stderr using standalone threads.
        /// </summary>
        /// <remarks>
        /// <para>You must register at least one <see cref="MessageReceivedEventHandler"/> for each of <see cref="AddOutputDataReceivedHandler(DataReceivedEventHandler)"/>
        /// and <see cref="AddErrorReceivedHandler(MessageReceivedEventHandler)"/> before calling this method.</para>
        /// <para>Read content is passed to the registered <see cref="MessageReceivedEventHandler"/>s.</para>
        /// <para>If <see cref="MessageReceivedEventHandler"/> are missing, the standalone threads will throw <see cref="NullReferenceException"/>s.</para>
        /// <para>Use this method over <see cref="BeginOutputReadLine"/> and <see cref="BeginErrorReadLine"/>. Both of those methods require ThreadPool threads to read.
        /// This means that if ThreadPool threads are limited in number, it will take some time for reads to start.</para>
        /// </remarks>
        void BeginOutputAndErrorReading();

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
        /// Gets the value indicating whether the NodeJS process is connected.
        /// </summary>
        bool Connected { get; }

        /// <summary>
        /// Gets the value indicating whether the NodeJS process has exited.
        /// </summary>
        bool HasExited { get; }

        /// <summary>
        /// <para>Gets the string describing the NodeJS process's exit status.</para>
        /// <para>If the process has not exited, returns "Process has not exited".</para>
        /// <para>If the process has been disposed, returns "Process has been disposed".</para>
        /// <para>If the process has exit but has not been disposed, returns its exit code.</para>
        /// </summary>
        string ExitStatus { get; }

        /// <summary>
        /// <para>Gets the NodeJS process's ID.</para>
        /// <para>If the ID can't be retrieved, returns -1.</para>
        /// </summary>
        int SafeID { get; }

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
