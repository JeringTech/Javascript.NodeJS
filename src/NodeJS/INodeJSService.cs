using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Jering.Javascript.NodeJS
{
    /// <summary>
    /// An abstraction for invoking code in NodeJS.
    /// </summary>
    public interface INodeJSService : IDisposable
    {
        /// <summary>Invokes a function from a NodeJS module on disk.</summary>
        /// <typeparam name="T">The type of value returned. This may be a JSON-serializable type, <see cref="string"/>, or <see cref="Stream"/>.</typeparam>
        /// <param name="modulePath">The path to the module relative to <see cref="NodeJSProcessOptions.ProjectPath"/>. This value mustn't be <c>null</c>, whitespace or an empty string.</param>
        /// <param name="exportName">The name of the function in the module's exports to invoke. If this value is <c>null</c>, the module's exports is assumed to be a function and is invoked.</param>
        /// <param name="args">The sequence of JSON-serializable arguments to pass to the function to invoke. If this value is <c>null</c>, no arguments are passed.</param>
        /// <param name="cancellationToken">The cancellation token for the asynchronous operation.</param>
        /// <returns>The <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="modulePath"/> is <c>null</c>, whitespace or an empty string.</exception>
        /// <exception cref="ConnectionException">Thrown if unable to connect to NodeJS.</exception>
        /// <exception cref="InvocationException">Thrown if the invocation request times out.</exception>
        /// <exception cref="InvocationException">Thrown if a NodeJS error occurs.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if this instance is disposed or if it attempts to use a disposed dependency.</exception>
        /// <exception cref="OperationCanceledException">Thrown if <paramref name="cancellationToken"/> is cancelled.</exception>
        /// <remarks>
        /// <para>If <paramref name="exportName"/> is <c>null</c>, we assume <c>Module.exports</c> is a function and invoke it. Otherwise, we invoke <c>Module.exports[<paramref name="exportName"/>]</c>.</para>
        /// <para>To avoid rereads and recompilations on subsequent invocations, NodeJS caches the module using the its absolute path as cache identifier. </para>
        /// </remarks>
        /// <example>
        /// If we have a file named exampleModule.js (located in <c>NodeJSProcessOptions.ProjectPath</c>), with contents:
        /// <code language="javascript">module.exports = (callback, message) =&gt; callback(null, { resultMessage: message });</code>
        /// And we have the class <c>Result</c>:
        /// <code language="csharp">public class Result
        /// {
        ///     public string Message { get; set; }
        /// }</code>
        /// The following assertion will pass:
        /// <code language="csharp">Result result = await nodeJSService.InvokeFromFileAsync&lt;Result&gt;("exampleModule.js", args: new[] { "success" });
        /// 
        /// Assert.Equal("success", result.Message);</code>
        /// </example>
        Task<T?> InvokeFromFileAsync<T>(string modulePath, string? exportName = null, object[]? args = null, CancellationToken cancellationToken = default);

        /// <summary>Invokes a function from a NodeJS module on disk.</summary>
        /// <param name="modulePath">The path to the module relative to <see cref="NodeJSProcessOptions.ProjectPath"/>. This value mustn't be <c>null</c>, whitespace or an empty string.</param>
        /// <param name="exportName">The name of the function in the module's exports to invoke. If this value is <c>null</c>, the module's exports is assumed to be a function and is invoked.</param>
        /// <param name="args">The sequence of JSON-serializable arguments to pass to the function to invoke. If this value is <c>null</c>, no arguments are passed.</param>
        /// <param name="cancellationToken">The cancellation token for the asynchronous operation.</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="modulePath"/> is <c>null</c>, whitespace or an empty string.</exception>
        /// <exception cref="ConnectionException">Thrown if unable to connect to NodeJS.</exception>
        /// <exception cref="InvocationException">Thrown if the invocation request times out.</exception>
        /// <exception cref="InvocationException">Thrown if a NodeJS error occurs.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if this instance is disposed or if it attempts to use a disposed dependency.</exception>
        /// <exception cref="OperationCanceledException">Thrown if <paramref name="cancellationToken"/> is cancelled.</exception>
        /// <remarks>
        /// <para>If <paramref name="exportName"/> is <c>null</c>, we assume <c>module.exports</c> is a function and invoke it. Otherwise, we invoke <c>module.exports[<paramref name="exportName"/>]</c>.</para>
        /// <para>To avoid rereads and recompilations on subsequent invocations, NodeJS caches the module using the its absolute path as cache identifier. </para>
        /// </remarks>
        Task InvokeFromFileAsync(string modulePath, string? exportName = null, object[]? args = null, CancellationToken cancellationToken = default);

        /// <summary>Invokes a function from a NodeJS module in string form.</summary>
        /// <typeparam name="T">The type of value returned. This may be a JSON-serializable type, <see cref="string"/>, or <see cref="Stream"/>.</typeparam>
        /// <param name="moduleString">The module in string form. This value mustn't be <c>null</c>, whitespace or an empty string.</param>
        /// <param name="cacheIdentifier">The module's cache identifier. If this value is <c>null</c>, NodeJS does not attempt to retrieve or cache the module.</param>
        /// <param name="exportName">The name of the function in the module's exports to invoke. If this value is <c>null</c>, the module's exports is assumed to be a function and is invoked.</param>
        /// <param name="args">The sequence of JSON-serializable arguments to pass to the function to invoke. If this value is <c>null</c>, no arguments are passed.</param>
        /// <param name="cancellationToken">The cancellation token for the asynchronous operation.</param>
        /// <returns>The <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="moduleString"/> is <c>null</c>, whitespace or an empty string.</exception>
        /// <exception cref="ConnectionException">Thrown if unable to connect to NodeJS.</exception>
        /// <exception cref="InvocationException">Thrown if the invocation request times out.</exception>
        /// <exception cref="InvocationException">Thrown if a NodeJS error occurs.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if this instance is disposed or if it attempts to use a disposed dependency.</exception>
        /// <exception cref="OperationCanceledException">Thrown if <paramref name="cancellationToken"/> is cancelled.</exception>
        /// <remarks>
        /// <para>If <paramref name="cacheIdentifier"/> is <c>null</c>, we send <paramref name="moduleString"/> to NodeJS where it's compiles it for one-time use.</para>
        /// <para>If <paramref name="cacheIdentifier"/> isn't <c>null</c>, we send both <paramref name="moduleString"/> and <paramref name="cacheIdentifier"/> to NodeJS. NodeJS reuses the module if it's already cached. Otherwise, it compiles and caches the module.</para>
        /// <para>Once the module is cached, you may use <see cref="TryInvokeFromCacheAsync{T}"/> to invoke directly from the cache, avoiding the overhead of sending <paramref name="moduleString"/>.</para>
        /// <para>If <paramref name="exportName"/> is <c>null</c>, we assume <c>module.exports</c> is a function and invoke it. Otherwise, we invoke <c>module.exports[<paramref name="exportName"/>]</c>.</para>
        /// </remarks>
        /// <example>
        /// Using the class <c>Result</c>:
        /// <code language="csharp">public class Result
        /// {
        ///     public string Message { get; set; }
        /// }</code>
        /// The following assertion will pass:
        /// <code language="csharp">Result result = await nodeJSService.InvokeFromStringAsync&lt;Result&gt;("module.exports = (callback, message) =&gt; callback(null, { resultMessage: message });", 
        ///     args: new[] { "success" });
        /// 
        /// Assert.Equal("success", result.Message);</code>
        /// </example>
        Task<T?> InvokeFromStringAsync<T>(string moduleString, string? cacheIdentifier = null, string? exportName = null, object[]? args = null, CancellationToken cancellationToken = default);

        /// <summary>Invokes a function from a NodeJS module in string form.</summary>
        /// <param name="moduleString">The module in string form. This value mustn't be <c>null</c>, whitespace or an empty string.</param>
        /// <param name="cacheIdentifier">The module's cache identifier. If this value is <c>null</c>, NodeJS does not attempt to retrieve or cache the module.</param>
        /// <param name="exportName">The name of the function in the module's exports to invoke. If this value is <c>null</c>, the module's exports is assumed to be a function and is invoked.</param>
        /// <param name="args">The sequence of JSON-serializable arguments to pass to the function to invoke. If this value is <c>null</c>, no arguments are passed.</param>
        /// <param name="cancellationToken">The cancellation token for the asynchronous operation.</param>
        /// <returns>The <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="moduleString"/> is <c>null</c>, whitespace or an empty string.</exception>
        /// <exception cref="ConnectionException">Thrown if unable to connect to NodeJS.</exception>
        /// <exception cref="InvocationException">Thrown if the invocation request times out.</exception>
        /// <exception cref="InvocationException">Thrown if a NodeJS error occurs.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if this instance is disposed or if it attempts to use a disposed dependency.</exception>
        /// <exception cref="OperationCanceledException">Thrown if <paramref name="cancellationToken"/> is cancelled.</exception>
        /// <remarks>
        /// <para>If <paramref name="cacheIdentifier"/> is <c>null</c>, we send <paramref name="moduleString"/> to NodeJS where it's compiled for one-time use.</para>
        /// <para>If <paramref name="cacheIdentifier"/> isn't <c>null</c>, we send both <paramref name="moduleString"/> and <paramref name="cacheIdentifier"/> to NodeJS. NodeJS reuses the module if it's already cached. Otherwise, it compiles and caches the module.</para>
        /// <para>Once the module is cached, you may use <see cref="TryInvokeFromCacheAsync{T}"/> to invoke directly from the cache, avoiding the overhead of sending <paramref name="moduleString"/>.</para>
        /// <para>If <paramref name="exportName"/> is <c>null</c>, we assume <c>module.exports</c> is a function and invoke it. Otherwise, we invoke <c>module.exports[<paramref name="exportName"/>]</c>.</para>
        /// </remarks>
        Task InvokeFromStringAsync(string moduleString, string? cacheIdentifier = null, string? exportName = null, object[]? args = null, CancellationToken cancellationToken = default);

        /// <summary>Invokes a function from a NodeJS module in string form.</summary>
        /// <typeparam name="T">The type of value returned. This may be a JSON-serializable type, <see cref="string"/>, or <see cref="Stream"/>.</typeparam>
        /// <param name="moduleFactory">The factory that creates the module string. This value mustn't be <c>null</c> and it mustn't return <c>null</c>, whitespace or an empty string.</param>
        /// <param name="cacheIdentifier">The module's cache identifier. This value mustn't be <c>null</c>.</param>
        /// <param name="exportName">The name of the function in the module's exports to invoke. If this value is <c>null</c>, the module's exports is assumed to be a function and is invoked.</param>
        /// <param name="args">The sequence of JSON-serializable arguments to pass to the function to invoke. If this value is <c>null</c>, no arguments are passed.</param>
        /// <param name="cancellationToken">The cancellation token for the asynchronous operation.</param>
        /// <returns>The <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if module is not cached but <paramref name="moduleFactory"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="cacheIdentifier"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="moduleFactory"/> returns <c>null</c>, whitespace or an empty string.</exception>
        /// <exception cref="ConnectionException">Thrown if unable to connect to NodeJS.</exception>
        /// <exception cref="InvocationException">Thrown if the invocation request times out.</exception>
        /// <exception cref="InvocationException">Thrown if a NodeJS error occurs.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if this instance is disposed or if it attempts to use a disposed dependency.</exception>
        /// <exception cref="OperationCanceledException">Thrown if <paramref name="cancellationToken"/> is cancelled.</exception>
        /// <remarks>
        /// <para>When this method is called, we first send <paramref name="cacheIdentifier"/> to NodeJS. NodeJS reuses the module if it's already cached. Otherwise, it informs us that the module isn't cached. 
        /// When that happens, we create the module string using <paramref name="moduleFactory"/> and send it to NodeJS, together with <paramref name="cacheIdentifier"/>. NodeJS then compiles and caches the module.</para>
        /// <para>If <paramref name="exportName"/> is <c>null</c>, the module's exports is assumed to be a function and is invoked. Otherwise, invokes the function named <paramref name="exportName"/> in the module's exports.</para>
        /// </remarks>
        Task<T?> InvokeFromStringAsync<T>(Func<string> moduleFactory, string cacheIdentifier, string? exportName = null, object[]? args = null, CancellationToken cancellationToken = default);

        /// <summary>Invokes a function from a NodeJS module in string form.</summary>
        /// <param name="moduleFactory">The factory that creates the module string. This value mustn't be <c>null</c> and it mustn't return <c>null</c>, whitespace or an empty string.</param>
        /// <param name="cacheIdentifier">The module's cache identifier. This value mustn't be <c>null</c>.</param>
        /// <param name="exportName">The name of the function in the module's exports to invoke. If this value is <c>null</c>, the module's exports is assumed to be a function and is invoked.</param>
        /// <param name="args">The sequence of JSON-serializable arguments to pass to the function to invoke. If this value is <c>null</c>, no arguments are passed.</param>
        /// <param name="cancellationToken">The cancellation token for the asynchronous operation.</param>
        /// <returns>The <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if module is not cached but <paramref name="moduleFactory"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="cacheIdentifier"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="moduleFactory"/> returns <c>null</c>, whitespace or an empty string.</exception>
        /// <exception cref="ConnectionException">Thrown if unable to connect to NodeJS.</exception>
        /// <exception cref="InvocationException">Thrown if the invocation request times out.</exception>
        /// <exception cref="InvocationException">Thrown if a NodeJS error occurs.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if this instance is disposed or if it attempts to use a disposed dependency.</exception>
        /// <exception cref="OperationCanceledException">Thrown if <paramref name="cancellationToken"/> is cancelled.</exception>
        /// <remarks>
        /// <para>When this method is called, we first send <paramref name="cacheIdentifier"/> to NodeJS. NodeJS reuses the module if it's already cached. Otherwise, it informs us that the module isn't cached. 
        /// When that happens, we create the module string using <paramref name="moduleFactory"/> and send it to NodeJS, together with <paramref name="cacheIdentifier"/>. NodeJS then compiles and caches the module.</para>
        /// <para>If <paramref name="exportName"/> is <c>null</c>, the module's exports is assumed to be a function and is invoked. Otherwise, invokes the function named <paramref name="exportName"/> in the module's exports.</para>
        /// </remarks>
        Task InvokeFromStringAsync(Func<string> moduleFactory, string cacheIdentifier, string? exportName = null, object[]? args = null, CancellationToken cancellationToken = default);

        /// <summary>Invokes a function from a NodeJS module in stream form.</summary>
        /// <typeparam name="T">The type of value returned. This may be a JSON-serializable type, <see cref="string"/>, or <see cref="Stream"/>.</typeparam>
        /// <param name="moduleStream">The module in stream form. This value mustn't be <c>null</c>.</param>
        /// <param name="cacheIdentifier">The module's cache identifier. If this value is <c>null</c>, NodeJS does not attempt to retrieve or cache the module.</param>
        /// <param name="exportName">The name of the function in the module's exports to invoke. If this value is <c>null</c>, the module's exports is assumed to be a function and is invoked.</param>
        /// <param name="args">The sequence of JSON-serializable arguments to pass to the function to invoke. If this value is <c>null</c>, no arguments are passed.</param>
        /// <param name="cancellationToken">The cancellation token for the asynchronous operation.</param>
        /// <returns>The <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="moduleStream"/> is <c>null</c>.</exception>
        /// <exception cref="ConnectionException">Thrown if unable to connect to NodeJS.</exception>
        /// <exception cref="InvocationException">Thrown if the invocation request times out.</exception>
        /// <exception cref="InvocationException">Thrown if a NodeJS error occurs.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if this instance is disposed or if it attempts to use a disposed dependency.</exception>
        /// <exception cref="OperationCanceledException">Thrown if <paramref name="cancellationToken"/> is cancelled.</exception>
        /// <remarks>
        /// <para>If <paramref name="cacheIdentifier"/> is <c>null</c>, we send the stream to NodeJS where it's compiled for one-time use.</para>
        /// <para>If <paramref name="cacheIdentifier"/> isn't <c>null</c>, we send both the stream and <paramref name="cacheIdentifier"/> to NodeJS. NodeJS reuses the module if it's already cached. Otherwise, it compiles and caches the module.</para>
        /// <para>Once the module is cached, you may use <see cref="TryInvokeFromCacheAsync{T}"/> to invoke directly from the cache, avoiding the overhead of sending the module stream.</para>
        /// <para>If <paramref name="exportName"/> is <c>null</c>, we assume <c>module.exports</c> is a function and invoke it. Otherwise, we invoke <c>module.exports[<paramref name="exportName"/>]</c>.</para>
        /// </remarks>
        /// <example>
        /// Using the class <c>Result</c>:
        /// <code language="csharp">public class Result
        /// {
        ///     public string Message { get; set; }
        /// }</code>
        /// The following assertion will pass:
        /// <code language="csharp">using (var memoryStream = new MemoryStream())
        /// using (var streamWriter = new StreamWriter(memoryStream))
        /// {
        ///     // Write the module to a MemoryStream for demonstration purposes.
        ///     streamWriter.Write("module.exports = (callback, message) =&gt; callback(null, {resultMessage: message});");
        ///     streamWriter.Flush();
        ///     memoryStream.Position = 0;
        /// 
        ///     Result result = await nodeJSService.InvokeFromStreamAsync&lt;Result&gt;(memoryStream, args: new[] { "success" });
        ///     
        ///     Assert.Equal("success", result.Message);
        /// }</code>
        /// </example>
        Task<T?> InvokeFromStreamAsync<T>(Stream moduleStream, string? cacheIdentifier = null, string? exportName = null, object[]? args = null, CancellationToken cancellationToken = default);

        /// <summary>Invokes a function from a NodeJS module in stream form.</summary>
        /// <param name="moduleStream">The module in stream form. This value mustn't be <c>null</c>.</param>
        /// <param name="cacheIdentifier">The module's cache identifier. If this value is <c>null</c>, NodeJS does not attempt to retrieve or cache the module.</param>
        /// <param name="exportName">The name of the function in the module's exports to invoke. If this value is <c>null</c>, the module's exports is assumed to be a function and is invoked.</param>
        /// <param name="args">The sequence of JSON-serializable arguments to pass to the function to invoke. If this value is <c>null</c>, no arguments are passed.</param>
        /// <param name="cancellationToken">The cancellation token for the asynchronous operation.</param>
        /// <returns>The <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="moduleStream"/> is <c>null</c>.</exception>
        /// <exception cref="ConnectionException">Thrown if unable to connect to NodeJS.</exception>
        /// <exception cref="InvocationException">Thrown if the invocation request times out.</exception>
        /// <exception cref="InvocationException">Thrown if a NodeJS error occurs.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if this instance is disposed or if it attempts to use a disposed dependency.</exception>
        /// <exception cref="OperationCanceledException">Thrown if <paramref name="cancellationToken"/> is cancelled.</exception>
        /// <remarks>
        /// <para>If <paramref name="cacheIdentifier"/> is <c>null</c>, we send the stream to NodeJS where it's compiled for one-time use.</para>
        /// <para>If <paramref name="cacheIdentifier"/> isn't <c>null</c>, we send both the stream and <paramref name="cacheIdentifier"/> to NodeJS. NodeJS reuses the module if it's already cached. Otherwise, it compiles and caches the module.</para>
        /// <para>Once the module is cached, you may use <see cref="TryInvokeFromCacheAsync{T}"/> to invoke directly from the cache, avoiding the overhead of sending the module stream.</para>
        /// <para>If <paramref name="exportName"/> is <c>null</c>, we assume <c>module.exports</c> is a function and invoke it. Otherwise, we invoke <c>module.exports[<paramref name="exportName"/>]</c>.</para>
        /// </remarks>
        Task InvokeFromStreamAsync(Stream moduleStream, string? cacheIdentifier = null, string? exportName = null, object[]? args = null, CancellationToken cancellationToken = default);

        /// <summary>Invokes a function from a NodeJS module in stream form.</summary>
        /// <typeparam name="T">The type of value returned. This may be a JSON-serializable type, <see cref="string"/>, or <see cref="Stream"/>.</typeparam>
        /// <param name="moduleFactory">The factory that creates the module stream. This value mustn't be <c>null</c> and it mustn't return <c>null</c>.</param>
        /// <param name="cacheIdentifier">The module's cache identifier. This value mustn't be <c>null</c>.</param>
        /// <param name="exportName">The name of the function in the module's exports to invoke. If this value is <c>null</c>, the module's exports is assumed to be a function and is invoked.</param>
        /// <param name="args">The sequence of JSON-serializable arguments to pass to the function to invoke. If this value is <c>null</c>, no arguments are passed.</param>
        /// <param name="cancellationToken">The cancellation token for the asynchronous operation.</param>
        /// <returns>The <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if module is not cached but <paramref name="moduleFactory"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="cacheIdentifier"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="moduleFactory"/> returns <c>null</c>.</exception>
        /// <exception cref="ConnectionException">Thrown if unable to connect to NodeJS.</exception>
        /// <exception cref="InvocationException">Thrown if the invocation request times out.</exception>
        /// <exception cref="InvocationException">Thrown if a NodeJS error occurs.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if this instance is disposed or if it attempts to use a disposed dependency.</exception>
        /// <exception cref="OperationCanceledException">Thrown if <paramref name="cancellationToken"/> is cancelled.</exception>
        /// <remarks>
        /// <para>When this method is called, we first send <paramref name="cacheIdentifier"/> to NodeJS. NodeJS reuses the module if it's already cached. Otherwise, it informs us that the module isn't cached. 
        /// When that happens, we create the module stream using <paramref name="moduleFactory"/> and send it to NodeJS, together with <paramref name="cacheIdentifier"/>. NodeJS then compiles and caches the module.</para>
        /// <para>If <paramref name="exportName"/> is <c>null</c>, the module's exports is assumed to be a function and is invoked. Otherwise, invokes the function named <paramref name="exportName"/> in the module's exports.</para>
        /// </remarks>
        Task<T?> InvokeFromStreamAsync<T>(Func<Stream> moduleFactory, string cacheIdentifier, string? exportName = null, object[]? args = null, CancellationToken cancellationToken = default);

        /// <summary>Invokes a function from a NodeJS module in stream form.</summary>
        /// <param name="moduleFactory">The factory that creates the module stream. This value mustn't be <c>null</c> and it mustn't return <c>null</c>.</param>
        /// <param name="cacheIdentifier">The module's cache identifier. This value mustn't be <c>null</c>.</param>
        /// <param name="exportName">The name of the function in the module's exports to invoke. If this value is <c>null</c>, the module's exports is assumed to be a function and is invoked.</param>
        /// <param name="args">The sequence of JSON-serializable arguments to pass to the function to invoke. If this value is <c>null</c>, no arguments are passed.</param>
        /// <param name="cancellationToken">The cancellation token for the asynchronous operation.</param>
        /// <returns>The <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if module is not cached but <paramref name="moduleFactory"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="cacheIdentifier"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="moduleFactory"/> returns <c>null</c>.</exception>
        /// <exception cref="ConnectionException">Thrown if unable to connect to NodeJS.</exception>
        /// <exception cref="InvocationException">Thrown if the invocation request times out.</exception>
        /// <exception cref="InvocationException">Thrown if a NodeJS error occurs.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if this instance is disposed or if it attempts to use a disposed dependency.</exception>
        /// <exception cref="OperationCanceledException">Thrown if <paramref name="cancellationToken"/> is cancelled.</exception>
        /// <remarks>
        /// <para>When this method is called, we first send <paramref name="cacheIdentifier"/> to NodeJS. NodeJS reuses the module if it's already cached. Otherwise, it informs us that the module isn't cached. 
        /// When that happens, we create the module stream using <paramref name="moduleFactory"/> and send it to NodeJS, together with <paramref name="cacheIdentifier"/>. NodeJS then compiles and caches the module.</para>
        /// <para>If <paramref name="exportName"/> is <c>null</c>, the module's exports is assumed to be a function and is invoked. Otherwise, invokes the function named <paramref name="exportName"/> in the module's exports.</para>
        /// </remarks>
        Task InvokeFromStreamAsync(Func<Stream> moduleFactory, string cacheIdentifier, string? exportName = null, object[]? args = null, CancellationToken cancellationToken = default);

        /// <summary>Attempts to invoke a function from a module in NodeJS's cache.</summary>
        /// <typeparam name="T">The type of value returned. This may be a JSON-serializable type, <see cref="string"/>, or <see cref="Stream"/>.</typeparam>
        /// <param name="cacheIdentifier">The module's cache identifier. This value mustn't be <c>null</c>.</param>
        /// <param name="exportName">The name of the function in the module's exports to invoke. If this value is <c>null</c>, the module's exports is assumed to be a function and is invoked.</param>
        /// <param name="args">The sequence of JSON-serializable arguments to pass to the function to invoke. If this value is <c>null</c>, no arguments are passed.</param>
        /// <param name="cancellationToken">The cancellation token for the asynchronous operation.</param>
        /// <returns>The <see cref="Task"/> representing the asynchronous operation. On completion, the task returns a (bool, T) with the bool set to true on 
        /// success and false otherwise.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="cacheIdentifier"/> is <c>null</c>.</exception>
        /// <exception cref="ConnectionException">Thrown if unable to connect to NodeJS.</exception>
        /// <exception cref="InvocationException">Thrown if the invocation request times out.</exception>
        /// <exception cref="InvocationException">Thrown if a NodeJS error occurs.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if this instance is disposed or if it attempts to use a disposed dependency.</exception>
        /// <exception cref="OperationCanceledException">Thrown if <paramref name="cancellationToken"/> is cancelled.</exception>
        /// <example>
        /// Using the class <c>Result</c>:
        /// <code language="csharp">public class Result
        /// {
        ///     public string Message { get; set; }
        /// }</code>
        /// The following assertion will pass:
        /// <code language="csharp">// Cache the module
        /// string cacheIdentifier = "exampleModule";
        /// await nodeJSService.InvokeFromStringAsync&lt;Result&gt;("module.exports = (callback, message) =&gt; callback(null, { resultMessage: message });", 
        ///     cacheIdentifier,
        ///     args: new[] { "success" });
        ///
        /// // Invoke from cache
        /// (bool success, Result result) = await nodeJSService.TryInvokeFromCacheAsync&lt;Result&gt;(cacheIdentifier, args: new[] { "success" });
        ///
        /// Assert.True(success);
        /// Assert.Equal("success", result.Message);</code>
        /// </example>
        Task<(bool, T?)> TryInvokeFromCacheAsync<T>(string cacheIdentifier, string? exportName = null, object[]? args = null, CancellationToken cancellationToken = default);

        /// <summary>Attempts to invoke a function from a module in NodeJS's cache.</summary>
        /// <param name="cacheIdentifier">The module's cache identifier. This value mustn't be <c>null</c>.</param>
        /// <param name="exportName">The name of the function in the module's exports to invoke. If this value is <c>null</c>, the module's exports is assumed to be a function and is invoked.</param>
        /// <param name="args">The sequence of JSON-serializable arguments to pass to the function to invoke. If this value is <c>null</c>, no arguments are passed.</param>
        /// <param name="cancellationToken">The cancellation token for the asynchronous operation.</param>
        /// <returns>The <see cref="Task"/> representing the asynchronous operation. On completion, the task returns true on success and false otherwise.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="cacheIdentifier"/> is <c>null</c>.</exception>
        /// <exception cref="ConnectionException">Thrown if unable to connect to NodeJS.</exception>
        /// <exception cref="InvocationException">Thrown if the invocation request times out.</exception>
        /// <exception cref="InvocationException">Thrown if a NodeJS error occurs.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if this instance is disposed or if it attempts to use a disposed dependency.</exception>
        /// <exception cref="OperationCanceledException">Thrown if <paramref name="cancellationToken"/> is cancelled.</exception>
        Task<bool> TryInvokeFromCacheAsync(string cacheIdentifier, string? exportName = null, object[]? args = null, CancellationToken cancellationToken = default);
    }
}
