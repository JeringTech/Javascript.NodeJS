using System;
using System.Threading;
using System.Threading.Tasks;

namespace Jering.JavascriptUtils.Node
{
    /// <summary>
    /// Represents the ability to invoke code in a Node.js environment. Although the underlying Node.js instance
    /// might change over time (e.g., the process might be restarted), the <see cref="INodeServices"/> instance
    /// will remain constant.
    /// </summary>
    public interface INodeServices : IDisposable
    {
        /// <summary>
        /// Asynchronously invokes code in the Node.js instance.
        /// </summary>
        /// <typeparam name="T">The JSON-serializable data type that the Node.js code will asynchronously return.</typeparam>
        /// <param name="moduleName">The path to the Node.js module (i.e., JavaScript file) relative to your project root.</param>
        /// <param name="args">Any sequence of JSON-serializable arguments to be passed to the Node.js function.</param>
        /// <param name="exportedFunctionName">If non-null, specifies the function exported by the Node.js module to invoke. 
        /// If null, the Node.js module must export a single function and that function will be invoked.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the invocation.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the completion of the RPC call.</returns>
        Task<T> InvokeAsync<T>(string moduleName, object[] args = null, string exportedFunctionName = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Asynchronously invokes code in the Node.js instance.
        /// </summary>
        /// <typeparam name="T">The JSON-serializable data type that the Node.js code will asynchronously return.</typeparam>
        /// <param name="module">The Node.js module.</param>
        /// <param name="args">Any sequence of JSON-serializable arguments to be passed to the Node.js function.</param>
        /// <param name="exportedFunctionName">If non-null, specifies the function exported by the Node.js module to invoke. 
        /// If null, the Node.js module must export a single function and that function will be invoked.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the invocation.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the completion of the RPC call.</returns>
        Task<T> InvokeFromMemoryAsync<T>(string module, object[] args = null, string exportedFunctionName = null, CancellationToken cancellationToken = default(CancellationToken));
    }
}