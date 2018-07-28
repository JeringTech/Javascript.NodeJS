using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Jering.JavascriptUtils.NodeJS
{
    /// <summary>
    /// An abstraction for invoking code in NodeJS.
    /// </summary>
    public interface INodeJSService : IDisposable
    {
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
        Task<T> InvokeFromFileAsync<T>(string modulePath, string exportName = null, object[] args = null, CancellationToken cancellationToken = default(CancellationToken));

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
        Task<T> InvokeFromStringAsync<T>(string moduleString, string newCacheIdentifier = null, string exportName = null, object[] args = null, CancellationToken cancellationToken = default(CancellationToken));

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
        Task<T> InvokeFromStreamAsync<T>(Stream moduleStream, string newCacheIdentifier = null, string exportName = null, object[] args = null, CancellationToken cancellationToken = default(CancellationToken));

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
        Task<(bool, T)> TryInvokeFromCacheAsync<T>(string moduleCacheIdentifier, string exportName = null, object[] args = null, CancellationToken cancellationToken = default(CancellationToken));
    }
}