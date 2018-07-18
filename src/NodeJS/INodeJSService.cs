using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Jering.JavascriptUtils.NodeJS
{
    /// <summary>
    /// Represents an instance of Node.js to which Remote Procedure Calls (RPC) may be sent.
    /// </summary>
    public interface INodeJSService : IDisposable
    {
        /// <summary>
        /// Asynchronously invokes code in the Node.js instance. 
        /// </summary>
        /// <typeparam name="T">The JSON-serializable data type that the Node.js code will asynchronously return.</typeparam>
        /// <param name="modulePath">The path to the Node.js module (i.e., JavaScript file) relative to your project root that contains the code to be invoked.</param>
        /// <param name="exportName">If set, specifies the name of the property in the Node.js module's exports to be invoked. If not set, the module's exports
        /// is assumed to be a function, and is invoked.</param>
        /// <param name="args">The sequence of JSON-serializable arguments to be passed to the invoked Node.js function.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the invocation.</param>
        /// <returns>A <see cref="Task{T}"/> representing the completion of the RPC call.</returns>
        Task<T> InvokeFromFileAsync<T>(string modulePath, string exportName = null, object[] args = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Asynchronously invokes code in the Node.js instance.
        /// </summary>
        /// <typeparam name="T">The JSON-serializable data type that the Node.js code will asynchronously return.</typeparam>
        /// <param name="moduleString">In string form, the Node.js module (i.e., JavaScript file) to be invoked.</param>
        /// <param name="newCacheIdentifier">If specified, the Node.js module's exports will be cached by the Node.js process, using this string as its identifier. </param>
        /// <param name="exportName">If specified, used as the name of the property in the Node.js module's exports to be invoked. Otherwise, the module's exports object
        /// is assumed to be a function, and is invoked.</param>
        /// <param name="args">The sequence of JSON-serializable arguments to be passed to the invoked Node.js function.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the invocation.</param>
        /// <returns>A <see cref="Task{T}"/> representing the completion of the RPC call.</returns>
        Task<T> InvokeFromStringAsync<T>(string moduleString, string newCacheIdentifier = null, string exportName = null, object[] args = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Asynchronously invokes code in the Node.js instance.
        /// </summary>
        /// <typeparam name="T">The JSON-serializable data type that the Node.js code will asynchronously return.</typeparam>
        /// <param name="moduleStream">In <see cref="Stream"/> form,, the Node.js module (i.e., JavaScript file) to be invoked.</param>
        /// <param name="newCacheIdentifier">If specified, the Node.js module's exports will be cached by the Node.js process, using this string as its identifier. </param>
        /// <param name="exportName">If specified, used as the name of the property in the Node.js module's exports to be invoked. Otherwise, the module's exports object
        /// is assumed to be a function, and is invoked.</param>
        /// <param name="args">The sequence of JSON-serializable arguments to be passed to the invoked Node.js function.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the invocation.</param>
        /// <returns>A <see cref="Task{T}"/> representing the completion of the RPC call.</returns>
        Task<T> InvokeFromStreamAsync<T>(Stream moduleStream, string newCacheIdentifier = null, string exportName = null, object[] args = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Asynchronously attempts to invoke code from the Node.js process's cache.
        /// </summary>
        /// <typeparam name="T">The JSON-serializable data type that the Node.js code will asynchronously return.</typeparam>
        /// <param name="moduleCacheIdentifier">The cache identifier of the Node.js module (i.e., JavaScript file) to be invoked.</param>
        /// <param name="exportName">If specified, used as the name of the property in the Node.js module's exports to be invoked. Otherwise, the module's exports object
        /// is assumed to be a function, and is invoked.</param>
        /// <param name="args">The sequence of JSON-serializable arguments to be passed to the invoked Node.js function.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the invocation.</param>
        /// <returns>A <see cref="Task{InvocationResult{T}}"/> representing the completion of the RPC call. Since this method is asynchronous, it cannot have an out parameter.
        /// Therefore, it returns both a bool indicating success or failure and the resulting value of the invocation wrapped in <see cref="InvocationResult{T}"/>.</returns>
        Task<(bool, T)> TryInvokeFromCacheAsync<T>(string moduleCacheIdentifier, string exportName = null, object[] args = null, CancellationToken cancellationToken = default(CancellationToken));
    }
}