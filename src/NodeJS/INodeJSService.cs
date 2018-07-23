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
        /// Invokes a function exported by a NodeJS module.
        /// </summary>
        /// <typeparam name="T">The type of object this method will return. It can be a JSON-serializable type, <see cref="string"/>, or <see cref="Stream"/>.</typeparam>
        /// <param name="modulePath">The path to the NodeJS module (i.e., JavaScript file) relative to <see cref="NodeJSProcessOptions.ProjectPath"/>.</param>
        /// <param name="exportName">If set, specifies the name of the function in the module's exports to be invoked. If unspecified, the module's exports object
        /// is assumed to be a function, and is invoked.</param>
        /// <param name="args">The sequence of JSON-serializable arguments to be passed to the NodeJS function to invoke.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the invocation.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        Task<T> InvokeFromFileAsync<T>(string modulePath, string exportName = null, object[] args = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Invokes a function exported by a NodeJS module.
        /// </summary>
        /// <typeparam name="T">The type of object this method will return. It can be a JSON-serializable type, <see cref="string"/>, or <see cref="Stream"/>.</typeparam>
        /// <param name="moduleString">The NodeJS module as a string.</param>
        /// <param name="newCacheIdentifier">If specified, the NodeJS module's exports will be cached by the NodeJS process, using this string as its identifier. If unspecified,
        /// the NodeJS module's exports will not be cached.</param>
        /// <param name="exportName">If set, specifies the name of the function in the module's exports to be invoked. If unspecified, the module's exports object
        /// is assumed to be a function, and is invoked.</param>
        /// <param name="args">The sequence of JSON-serializable arguments to be passed to the NodeJS function to invoke.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the invocation.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        Task<T> InvokeFromStringAsync<T>(string moduleString, string newCacheIdentifier = null, string exportName = null, object[] args = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Invokes a function exported by a NodeJS module.
        /// </summary>
        /// <typeparam name="T">The type of object this function will return. It can be a JSON-serializable type, <see cref="string"/>, or <see cref="Stream"/>.</typeparam>
        /// <param name="moduleStream">The NodeJS module as a <see cref="Stream"/>.</param>
        /// <param name="newCacheIdentifier">If specified, the NodeJS module's exports will be cached by the NodeJS process, using this string as its identifier. If unspecified,
        /// the NodeJS module's exports will not be cached.</param>
        /// <param name="exportName">If set, specifies the name of the function in the module's exports to be invoked. If unspecified, the module's exports object
        /// is assumed to be a function, and is invoked.</param>
        /// <param name="args">The sequence of JSON-serializable arguments to be passed to the NodeJS function to invoke.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the invocation.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        Task<T> InvokeFromStreamAsync<T>(Stream moduleStream, string newCacheIdentifier = null, string exportName = null, object[] args = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Attempts to invoke a function exported by a NodeJS module.
        /// </summary>
        /// <typeparam name="T">The type of object this method will return. It can be a JSON-serializable type, <see cref="string"/>, or <see cref="Stream"/>.</typeparam>
        /// <param name="moduleCacheIdentifier">The cache identifier of the NodeJS module.</param>
        /// <param name="exportName">If set, specifies the name of the function in the module's exports to be invoked. If unspecified, the module's exports object
        /// is assumed to be a function, and is invoked.</param>
        /// <param name="args">The sequence of JSON-serializable arguments to be passed to the NodeJS function to invoke.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the invocation.</param>
        /// <returns>The task object representing the asynchronous operation. The bool in the value tuple is true on success.</returns>
        Task<(bool, T)> TryInvokeFromCacheAsync<T>(string moduleCacheIdentifier, string exportName = null, object[] args = null, CancellationToken cancellationToken = default(CancellationToken));
    }
}