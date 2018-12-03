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
                        (_services ?? (_services = new ServiceCollection())).AddNodeJS();
                        _serviceProvider = _services.BuildServiceProvider();
                        _services = null;

                        _nodeJSService = _serviceProvider.GetRequiredService<INodeJSService>();
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
            (_services ?? (_services = new ServiceCollection())).Configure(configureOptions);
        }

        /// <summary>
        /// Invokes a function exported by a NodeJS module on disk.
        /// </summary>
        /// <typeparam name="T">The type of object this method will return. It can be a JSON-serializable type, <see cref="string"/>, or <see cref="Stream"/>.</typeparam>
        /// <param name="modulePath">The path to the module (i.e., JavaScript file) relative to <see cref="NodeJSProcessOptions.ProjectPath"/>.</param>
        /// <param name="exportName">The name of the function in the module's exports to be invoked. If unspecified, the module's exports object
        /// is assumed to be a function, and is invoked.</param>
        /// <param name="args">The sequence of JSON-serializable and/or string arguments to be passed to the function to invoke.</param>
        /// <param name="cancellationToken">The cancellation token for the asynchronous operation.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        /// <exception cref="InvocationException">Thrown if a NodeJS error occurs.</exception>
        /// <exception cref="InvocationException">Thrown if the invocation request times out.</exception>
        /// <exception cref="InvocationException">Thrown if NodeJS cannot be initialized.</exception>
        public static Task<T> InvokeFromFileAsync<T>(string modulePath, string exportName = null, object[] args = null, CancellationToken cancellationToken = default)
        {
            return GetOrCreateNodeJSService().InvokeFromFileAsync<T>(modulePath, exportName, args, cancellationToken);
        }

        /// <summary>
        /// Invokes a function exported by a NodeJS module in string form.
        /// </summary>
        /// <typeparam name="T">The type of object this method will return. It can be a JSON-serializable type, <see cref="string"/>, or <see cref="Stream"/>.</typeparam>
        /// <param name="moduleString">The module in string form.</param>
        /// <param name="newCacheIdentifier">The module's cache identifier in the NodeJS module cache. If unspecified, the module will not be cached.</param>
        /// <param name="exportName">The name of the function in the module's exports to be invoked. If unspecified, the module's exports object
        /// is assumed to be a function, and is invoked.</param>
        /// <param name="args">The sequence of JSON-serializable and/or string arguments to be passed to the function to invoke.</param>
        /// <param name="cancellationToken">The cancellation token for the asynchronous operation.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        /// <exception cref="InvocationException">Thrown if a NodeJS error occurs.</exception>
        /// <exception cref="InvocationException">Thrown if the invocation request times out.</exception>
        /// <exception cref="InvocationException">Thrown if NodeJS cannot be initialized.</exception>
        public static Task<T> InvokeFromStringAsync<T>(string moduleString, string newCacheIdentifier = null, string exportName = null, object[] args = null, CancellationToken cancellationToken = default)
        {
            return GetOrCreateNodeJSService().InvokeFromStringAsync<T>(moduleString, newCacheIdentifier, exportName, args, cancellationToken);
        }

        /// <summary>
        /// Invokes a function exported by a NodeJS module in Stream form.
        /// </summary>
        /// <typeparam name="T">The type of object this method will return. It can be a JSON-serializable type, <see cref="string"/>, or <see cref="Stream"/>.</typeparam>
        /// <param name="moduleStream">The module in Stream form.</param>
        /// <param name="newCacheIdentifier">The module's cache identifier in the NodeJS module cache. If unspecified, the module will not be cached.</param>
        /// <param name="exportName">The name of the function in the module's exports to be invoked. If unspecified, the module's exports object
        /// is assumed to be a function, and is invoked.</param>
        /// <param name="args">The sequence of JSON-serializable and/or string arguments to be passed to the function to invoke.</param>
        /// <param name="cancellationToken">The cancellation token for the asynchronous operation.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        /// <exception cref="InvocationException">Thrown if a NodeJS error occurs.</exception>
        /// <exception cref="InvocationException">Thrown if the invocation request times out.</exception>
        /// <exception cref="InvocationException">Thrown if NodeJS cannot be initialized.</exception>
        public static Task<T> InvokeFromStreamAsync<T>(Stream moduleStream, string newCacheIdentifier = null, string exportName = null, object[] args = null, CancellationToken cancellationToken = default)
        {
            return GetOrCreateNodeJSService().InvokeFromStreamAsync<T>(moduleStream, newCacheIdentifier, exportName, args, cancellationToken);
        }

        /// <summary>
        /// Attempts to invoke a function exported by a NodeJS module cached by NodeJS.
        /// </summary>
        /// <typeparam name="T">The type of object this method will return. It can be a JSON-serializable type, <see cref="string"/>, or <see cref="Stream"/>.</typeparam>
        /// <param name="moduleCacheIdentifier">The cache identifier of the module.</param>
        /// <param name="exportName">The name of the function in the module's exports to be invoked. If unspecified, the module's exports object
        /// is assumed to be a function, and is invoked.</param>
        /// <param name="args">The sequence of JSON-serializable and/or string arguments to be passed to the function to invoke.</param>
        /// <param name="cancellationToken">The cancellation token for the asynchronous operation.</param>
        /// <returns>The task object representing the asynchronous operation. On completion, the task returns a (bool, T) with the bool set to true on 
        /// success and false otherwise.</returns>
        /// <exception cref="InvocationException">Thrown if a NodeJS error occurs.</exception>
        /// <exception cref="InvocationException">Thrown if the invocation request times out.</exception>
        /// <exception cref="InvocationException">Thrown if NodeJS cannot be initialized.</exception>
        public static Task<(bool, T)> TryInvokeFromCacheAsync<T>(string moduleCacheIdentifier, string exportName = null, object[] args = null, CancellationToken cancellationToken = default)
        {
            return GetOrCreateNodeJSService().TryInvokeFromCacheAsync<T>(moduleCacheIdentifier, exportName, args, cancellationToken);
        }
    }
}
