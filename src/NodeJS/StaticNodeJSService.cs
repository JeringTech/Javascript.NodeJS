using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Jering.Javascript.NodeJS
{
    /// <summary>
    /// A class that provides static access to an instance of the default <see cref="INodeJSService"/> implementation's public methods.
    /// </summary>
    public static class StaticNodeJSService
    {
        private static volatile ServiceProvider _serviceProvider;
        private static volatile IServiceCollection _services;
        private static volatile INodeJSService _nodeJSService;
        private static readonly object _createLock = new object();

        private static INodeJSService GetOrCreateNodeJSService()
        {
            if (_nodeJSService == null || _services != null)
            {
                lock (_createLock)
                {
                    if (_nodeJSService == null || _services != null)
                    {
                        // Dispose of service provider
                        _serviceProvider?.Dispose();

                        // Create new service provider
                        _services ??= new ServiceCollection().AddNodeJS();
                        _serviceProvider = _services.BuildServiceProvider();
                        _nodeJSService = _serviceProvider.GetRequiredService<INodeJSService>();

                        _services = null;
                    }
                }
            }

            // NodeJSService already exists and no configuration pending
            return _nodeJSService;
        }

        /// <summary>
        /// <para>Disposes the underlying <see cref="IServiceProvider"/> used to resolve <see cref="INodeJSService"/>.</para>
        /// <para>This method is not thread safe.</para>
        /// </summary>
        public static void DisposeServiceProvider()
        {
            _serviceProvider?.Dispose();
            _serviceProvider = null;
            _nodeJSService = null;
        }

        /// <summary>
        /// <para>Configures options.</para>
        /// <para>This method is not thread safe.</para>
        /// </summary>
        /// <typeparam name="T">The type of options to configure.</typeparam>
        /// <param name="configureOptions">The action that configures the options.</param>
        public static void Configure<T>(Action<T> configureOptions) where T : class
        {
            _services = (_services ?? new ServiceCollection().AddNodeJS()).Configure(configureOptions);
        }

        /// <summary>
        /// <para>Sets the <see cref="ServiceCollection"/> used to create an <see cref="INodeJSService"/>.</para>
        /// <para>This method is not thread safe.</para>
        /// </summary>
        /// <param name="services">
        /// <para>The <see cref="ServiceCollection"/> used to create an <see cref="INodeJSService"/>.</para>
        /// <para>If this value doesn't contain a valid service for <see cref="INodeJSService"/>, <see cref="InvalidOperationException"/>s are
        /// thrown on subsequent invocations.</para>
        /// </param>
        public static void SetServices(ServiceCollection services)
        {
            _services = services;
        }

        /// <summary>
        /// <para>Invokes a function from a NodeJS module on disk.</para>
        /// <para>If <paramref name="exportName"/> is <c>null</c>, the module's exports is assumed to be a function and is invoked. Otherwise, invokes the function named <paramref name="exportName"/> in the module's exports.</para>
        /// <para>NodeJS caches the module using the module's path as cache identifier. This means subsequent invocations won't reread and recompile the module.</para>
        /// </summary>
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
        public static Task<T> InvokeFromFileAsync<T>(string modulePath, string exportName = null, object[] args = null, CancellationToken cancellationToken = default)
        {
            return GetOrCreateNodeJSService().InvokeFromFileAsync<T>(modulePath, exportName, args, cancellationToken);
        }

        /// <summary>
        /// <para>Invokes a function from a NodeJS module on disk.</para>
        /// <para>If <paramref name="exportName"/> is <c>null</c>, the module's exports is assumed to be a function and is invoked. Otherwise, invokes the function named <paramref name="exportName"/> in the module's exports.</para>
        /// <para>NodeJS caches the module using the module's path as cache identifier. This means subsequent invocations won't re-read and re-compile the module.</para>
        /// </summary>
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
        public static Task InvokeFromFileAsync(string modulePath, string exportName = null, object[] args = null, CancellationToken cancellationToken = default)
        {
            return GetOrCreateNodeJSService().InvokeFromFileAsync(modulePath, exportName, args, cancellationToken);
        }

        /// <summary>
        /// <para>Invokes a function from a NodeJS module in string form.</para>
        /// <para>If <paramref name="newCacheIdentifier"/> is <c>null</c>, the module string is sent to NodeJS and compiled for one time use.</para>
        /// <para>If <paramref name="newCacheIdentifier"/> isn't <c>null</c>, the module string and the cache identifier are both sent to NodeJS. If the module exists in NodeJS's cache, it's reused. Otherwise, the module string is compiled and cached.
        /// On subsequent invocations, you may use <see cref="TryInvokeFromCacheAsync{T}"/> to invoke directly from the cache, avoiding the overhead of sending the module string.</para>
        /// <para>If <paramref name="exportName"/> is <c>null</c>, the module's exports is assumed to be a function and is invoked. Otherwise, invokes the function named <paramref name="exportName"/> in the module's exports.</para>
        /// </summary>
        /// <typeparam name="T">The type of value returned. This may be a JSON-serializable type, <see cref="string"/>, or <see cref="Stream"/>.</typeparam>
        /// <param name="moduleString">The module in string form. This value mustn't be <c>null</c>, whitespace or an empty string.</param>
        /// <param name="newCacheIdentifier">The module's cache identifier. If this value is <c>null</c>, no attempt is made to retrieve or cache the module.</param>
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
        public static Task<T> InvokeFromStringAsync<T>(string moduleString, string newCacheIdentifier = null, string exportName = null, object[] args = null, CancellationToken cancellationToken = default)
        {
            return GetOrCreateNodeJSService().InvokeFromStringAsync<T>(moduleString, newCacheIdentifier, exportName, args, cancellationToken);
        }

        /// <summary>
        /// <para>Invokes a function from a NodeJS module in string form.</para>
        /// <para>If <paramref name="newCacheIdentifier"/> is <c>null</c>, the module string is sent to NodeJS and compiled for one time use.</para>
        /// <para>If <paramref name="newCacheIdentifier"/> isn't <c>null</c>, the module string and the cache identifier are both sent to NodeJS. If the module exists in NodeJS's cache, it's reused. Otherwise, the module string is compiled and cached.
        /// On subsequent invocations, you may use <see cref="TryInvokeFromCacheAsync{T}"/> to invoke directly from the cache, avoiding the overhead of sending the module string.</para>
        /// <para>If <paramref name="exportName"/> is <c>null</c>, the module's exports is assumed to be a function and is invoked. Otherwise, invokes the function named <paramref name="exportName"/> in the module's exports.</para>
        /// </summary>
        /// <param name="moduleString">The module in string form. This value mustn't be <c>null</c>, whitespace or an empty string.</param>
        /// <param name="newCacheIdentifier">The module's cache identifier. If this value is <c>null</c>, no attempt is made to retrieve or cache the module.</param>
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
        public static Task InvokeFromStringAsync(string moduleString, string newCacheIdentifier = null, string exportName = null, object[] args = null, CancellationToken cancellationToken = default)
        {
            return GetOrCreateNodeJSService().InvokeFromStringAsync(moduleString, newCacheIdentifier, exportName, args, cancellationToken);
        }

        /// <summary>
        /// <para>Invokes a function from a NodeJS module in string form.</para>
        /// <para>Initially, only sends the module's cache identifier to NodeJS. If the module exists in NodeJS's cache, it's reused. If the module doesn't exist in NodeJS's cache, creates the module string using 
        /// <paramref name="moduleFactory"/> and sends it, together with the module's cache identifier, to NodeJS for compilation and caching.</para>
        /// <para>If <paramref name="exportName"/> is <c>null</c>, the module's exports is assumed to be a function and is invoked. Otherwise, invokes the function named <paramref name="exportName"/> in the module's exports.</para>
        /// </summary>
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
        public static Task<T> InvokeFromStringAsync<T>(Func<string> moduleFactory, string cacheIdentifier, string exportName = null, object[] args = null, CancellationToken cancellationToken = default)
        {
            return GetOrCreateNodeJSService().InvokeFromStringAsync<T>(moduleFactory, cacheIdentifier, exportName, args, cancellationToken);
        }

        /// <summary>
        /// <para>Invokes a function from a NodeJS module in string form.</para>
        /// <para>Initially, only sends the module's cache identifier to NodeJS. If the module exists in NodeJS's cache, it's reused. If the module doesn't exist in NodeJS's cache, creates the module string using 
        /// <paramref name="moduleFactory"/> and sends it, together with the module's cache identifier, to NodeJS for compilation and caching.</para>
        /// <para>If <paramref name="exportName"/> is <c>null</c>, the module's exports is assumed to be a function and is invoked. Otherwise, invokes the function named <paramref name="exportName"/> in the module's exports.</para>
        /// </summary>
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
        public static Task InvokeFromStringAsync(Func<string> moduleFactory, string cacheIdentifier, string exportName = null, object[] args = null, CancellationToken cancellationToken = default)
        {
            return GetOrCreateNodeJSService().InvokeFromStringAsync(moduleFactory, cacheIdentifier, exportName, args, cancellationToken);
        }

        /// <summary>
        /// <para>Invokes a function from a NodeJS module in stream form.</para>
        /// <para>If <paramref name="newCacheIdentifier"/> is <c>null</c>, the module stream is sent to NodeJS and compiled for one time use.</para>
        /// <para>If <paramref name="newCacheIdentifier"/> isn't <c>null</c>, the module stream and the cache identifier are both sent to NodeJS. If the module exists in NodeJS's cache, it's reused. Otherwise, the module stream is compiled and cached.
        /// On subsequent invocations, you may use <see cref="TryInvokeFromCacheAsync{T}"/> to invoke directly from the cache, avoiding the overhead of sending the module stream to NodeJS.</para>
        /// <para>If <paramref name="exportName"/> is <c>null</c>, the module's exports is assumed to be a function and is invoked. Otherwise, invokes the function named <paramref name="exportName"/> in the module's exports.</para>
        /// </summary>
        /// <typeparam name="T">The type of value returned. This may be a JSON-serializable type, <see cref="string"/>, or <see cref="Stream"/>.</typeparam>
        /// <param name="moduleStream">The module in stream form. This value mustn't be <c>null</c>.</param>
        /// <param name="newCacheIdentifier">The module's cache identifier. If this value is <c>null</c>, no attempt is made to retrieve or cache the module.</param>
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
        public static Task<T> InvokeFromStreamAsync<T>(Stream moduleStream, string newCacheIdentifier = null, string exportName = null, object[] args = null, CancellationToken cancellationToken = default)
        {
            return GetOrCreateNodeJSService().InvokeFromStreamAsync<T>(moduleStream, newCacheIdentifier, exportName, args, cancellationToken);
        }

        /// <summary>
        /// <para>Invokes a function from a NodeJS module in stream form.</para>
        /// <para>If <paramref name="newCacheIdentifier"/> is <c>null</c>, the module stream is sent to NodeJS and compiled for one time use.</para>
        /// <para>If <paramref name="newCacheIdentifier"/> isn't <c>null</c>, the module stream and the cache identifier are both sent to NodeJS. If the module exists in NodeJS's cache, it's reused. Otherwise, the module stream is compiled and cached.
        /// On subsequent invocations, you may use <see cref="TryInvokeFromCacheAsync{T}"/> to invoke directly from the cache, avoiding the overhead of sending the module stream to NodeJS.</para>
        /// <para>If <paramref name="exportName"/> is <c>null</c>, the module's exports is assumed to be a function and is invoked. Otherwise, invokes the function named <paramref name="exportName"/> in the module's exports.</para>
        /// </summary>
        /// <param name="moduleStream">The module in stream form. This value mustn't be <c>null</c>.</param>
        /// <param name="newCacheIdentifier">The module's cache identifier. If this value is <c>null</c>, no attempt is made to retrieve or cache the module.</param>
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
        public static Task InvokeFromStreamAsync(Stream moduleStream, string newCacheIdentifier = null, string exportName = null, object[] args = null, CancellationToken cancellationToken = default)
        {
            return GetOrCreateNodeJSService().InvokeFromStreamAsync(moduleStream, newCacheIdentifier, exportName, args, cancellationToken);
        }

        /// <summary>
        /// <para>Invokes a function from a NodeJS module in stream form.</para>
        /// <para>Initially, only sends the module's cache identifier to NodeJS. If the module exists in NodeJS's cache, it's reused. If the module doesn't exist in NodeJS's cache, creates the module stream using 
        /// <paramref name="moduleFactory"/> and sends it, together with the module's cache identifier, to NodeJS for compilation and caching.</para>
        /// <para>If <paramref name="exportName"/> is <c>null</c>, the module's exports is assumed to be a function and is invoked. Otherwise, invokes the function named <paramref name="exportName"/> in the module's exports.</para>
        /// </summary>
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
        public static Task<T> InvokeFromStreamAsync<T>(Func<Stream> moduleFactory, string cacheIdentifier, string exportName = null, object[] args = null, CancellationToken cancellationToken = default)
        {
            return GetOrCreateNodeJSService().InvokeFromStreamAsync<T>(moduleFactory, cacheIdentifier, exportName, args, cancellationToken);
        }

        /// <summary>
        /// <para>Invokes a function from a NodeJS module in stream form.</para>
        /// <para>Initially, only sends the module's cache identifier to NodeJS. If the module exists in NodeJS's cache, it's reused. If the module doesn't exist in NodeJS's cache, creates the module stream using 
        /// <paramref name="moduleFactory"/> and sends it, together with the module's cache identifier, to NodeJS for compilation and caching.</para>
        /// <para>If <paramref name="exportName"/> is <c>null</c>, the module's exports is assumed to be a function and is invoked. Otherwise, invokes the function named <paramref name="exportName"/> in the module's exports.</para>
        /// </summary>
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
        public static Task InvokeFromStreamAsync(Func<Stream> moduleFactory, string cacheIdentifier, string exportName = null, object[] args = null, CancellationToken cancellationToken = default)
        {
            return GetOrCreateNodeJSService().InvokeFromStreamAsync(moduleFactory, cacheIdentifier, exportName, args, cancellationToken);
        }

        /// <summary>
        /// Attempts to invoke a function from a module in NodeJS's cache.
        /// </summary>
        /// <typeparam name="T">The type of value returned. This may be a JSON-serializable type, <see cref="string"/>, or <see cref="Stream"/>.</typeparam>
        /// <param name="moduleCacheIdentifier">The module's cache identifier. This value mustn't be <c>null</c>.</param>
        /// <param name="exportName">The name of the function in the module's exports to invoke. If this value is <c>null</c>, the module's exports is assumed to be a function and is invoked.</param>
        /// <param name="args">The sequence of JSON-serializable arguments to pass to the function to invoke. If this value is <c>null</c>, no arguments are passed.</param>
        /// <param name="cancellationToken">The cancellation token for the asynchronous operation.</param>
        /// <returns>The <see cref="Task"/> representing the asynchronous operation. On completion, the task returns a (bool, T) with the bool set to true on 
        /// success and false otherwise.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="moduleCacheIdentifier"/> is <c>null</c>.</exception>
        /// <exception cref="ConnectionException">Thrown if unable to connect to NodeJS.</exception>
        /// <exception cref="InvocationException">Thrown if the invocation request times out.</exception>
        /// <exception cref="InvocationException">Thrown if a NodeJS error occurs.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if this instance is disposed or if it attempts to use a disposed dependency.</exception>
        /// <exception cref="OperationCanceledException">Thrown if <paramref name="cancellationToken"/> is cancelled.</exception>
        public static Task<(bool, T)> TryInvokeFromCacheAsync<T>(string moduleCacheIdentifier, string exportName = null, object[] args = null, CancellationToken cancellationToken = default)
        {
            return GetOrCreateNodeJSService().TryInvokeFromCacheAsync<T>(moduleCacheIdentifier, exportName, args, cancellationToken);
        }

        /// <summary>
        /// Attempts to invoke a function from a module in NodeJS's cache.
        /// </summary>
        /// <param name="moduleCacheIdentifier">The module's cache identifier. This value mustn't be <c>null</c>.</param>
        /// <param name="exportName">The name of the function in the module's exports to invoke. If this value is <c>null</c>, the module's exports is assumed to be a function and is invoked.</param>
        /// <param name="args">The sequence of JSON-serializable arguments to pass to the function to invoke. If this value is <c>null</c>, no arguments are passed.</param>
        /// <param name="cancellationToken">The cancellation token for the asynchronous operation.</param>
        /// <returns>The <see cref="Task"/> representing the asynchronous operation. On completion, the task returns true on success and false otherwise.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="moduleCacheIdentifier"/> is <c>null</c>.</exception>
        /// <exception cref="ConnectionException">Thrown if unable to connect to NodeJS.</exception>
        /// <exception cref="InvocationException">Thrown if the invocation request times out.</exception>
        /// <exception cref="InvocationException">Thrown if a NodeJS error occurs.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if this instance is disposed or if it attempts to use a disposed dependency.</exception>
        /// <exception cref="OperationCanceledException">Thrown if <paramref name="cancellationToken"/> is cancelled.</exception>
        public static Task<bool> TryInvokeFromCacheAsync(string moduleCacheIdentifier, string exportName = null, object[] args = null, CancellationToken cancellationToken = default)
        {
            return GetOrCreateNodeJSService().TryInvokeFromCacheAsync(moduleCacheIdentifier, exportName, args, cancellationToken);
        }
    }
}
