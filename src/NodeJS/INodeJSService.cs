using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Jering.JavascriptUtils.NodeJS
{
    /// <summary>
    /// A service that provides ways to invoke code in NodeJS.
    /// </summary>
    public interface INodeJSService : IDisposable
    {
        /// <summary>
        /// Asynchronously invokes code contained in a file, in NodeJS. The module is cached by the NodeJS process if it isn't already cached. If it is already cached, it is invoked from the 
        /// cache.
        /// </summary>
        /// <typeparam name="T">The type of the object this function will return. It can be a JSON-serializable type, <see cref="string"/>, or <see cref="Stream"/>.</typeparam>
        /// <param name="modulePath">The path to the NodeJS module (i.e., JavaScript file) relative to your project root.</param>
        /// <param name="exportName">If set, specifies the name of the property (in the module's exports) to be invoked. If not set, the module's exports
        /// is assumed to be a function, and is invoked.</param>
        /// <param name="args">The sequence of JSON-serializable arguments to be passed to the NodeJS function to invoke.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the invocation.</param>
        /// <returns>A <see cref="Task{T}"/> representing the completion of the invocation.</returns>
        Task<T> InvokeFromFileAsync<T>(string modulePath, string exportName = null, object[] args = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Asynchronously invokes code in NodeJS, optionally caching the module in the NodeJS process. A module cached using this function can be invoked using <see cref="TryInvokeFromCacheAsync"/>.
        /// </summary>
        /// <typeparam name="T">The type of the object this function will return. It can be a JSON-serializable type, <see cref="string"/>, or <see cref="Stream"/>.</typeparam>
        /// <param name="moduleString">The NodeJS module as a string.</param>
        /// <param name="newCacheIdentifier">If specified, the NodeJS module's exports will be cached by the NodeJS process, using this string as its identifier. If unspecified,
        /// the NodeJS module's exports will not be cached.</param>
        /// <param name="exportName">If set, specifies the name of the property (in the module's exports) to be invoked. If not set, the module's exports
        /// is assumed to be a function, and is invoked.</param>
        /// <param name="args">The sequence of JSON-serializable arguments to be passed to the NodeJS function to invoke.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the invocation.</param>
        /// <returns>A <see cref="Task{T}"/> representing the completion of the invocation.</returns>
        Task<T> InvokeFromStringAsync<T>(string moduleString, string newCacheIdentifier = null, string exportName = null, object[] args = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Asynchronously invokes code in NodeJS, optionally caching the module in the NodeJS process. A module cached using this function can be invoked using <see cref="TryInvokeFromCacheAsync"/>.
        /// </summary>
        /// <typeparam name="T">The type of the object this function will return. It can be a JSON-serializable type, <see cref="string"/>, or <see cref="Stream"/>.</typeparam>
        /// <param name="moduleStream">The NodeJS module as a <see cref="Stream"/>.</param>
        /// <param name="newCacheIdentifier">If specified, the NodeJS module's exports will be cached by the NodeJS process, using this string as its identifier. If unspecified,
        /// the NodeJS module's exports will not be cached.</param>
        /// <param name="exportName">If set, specifies the name of the property (in the module's exports) to be invoked. If not set, the module's exports
        /// is assumed to be a function, and is invoked.</param>
        /// <param name="args">The sequence of JSON-serializable arguments to be passed to the NodeJS function to invoke.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the invocation.</param>
        /// <returns>A <see cref="Task{T}"/> representing the completion of the invocation.</returns>
        Task<T> InvokeFromStreamAsync<T>(Stream moduleStream, string newCacheIdentifier = null, string exportName = null, object[] args = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Asynchronously attempts to invoke code from the NodeJS process's cache.
        /// </summary>
        /// <typeparam name="T">The type of the object this function will return. It can be a JSON-serializable type, <see cref="string"/>, or <see cref="Stream"/>.</typeparam>
        /// <param name="moduleCacheIdentifier">The cache identifier of the NodeJS module to be invoked.</param>
        /// <param name="exportName">If set, specifies the name of the property (in the module's exports) to be invoked. If not set, the module's exports
        /// is assumed to be a function, and is invoked.</param>
        /// <param name="args">The sequence of JSON-serializable arguments to be passed to the NodeJS function to invoke.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the invocation.</param>
        /// <returns>A <see cref="Task{(bool, T)}"/> representing the completion of the invocation. The bool in the value tuple is true on success.</returns>
        Task<(bool, T)> TryInvokeFromCacheAsync<T>(string moduleCacheIdentifier, string exportName = null, object[] args = null, CancellationToken cancellationToken = default(CancellationToken));
    }
}