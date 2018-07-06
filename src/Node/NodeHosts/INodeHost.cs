using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Jering.JavascriptUtils.Node.HostingModels
{
    /// <summary>
    /// Represents an instance of Node.js to which Remote Procedure Calls (RPC) may be sent.
    /// </summary>
    public interface INodeHost : IDisposable
    {
        /// <summary>
        /// Asynchronously invokes code in the Node.js instance.
        /// </summary>
        /// <typeparam name="T">The JSON-serializable data type that the Node.js code will asynchronously return.</typeparam>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the invocation.</param>
        /// <param name="moduleName">The path to the Node.js module (i.e., JavaScript file) relative to your project root that contains the code to be invoked.</param>
        /// <param name="exportNameOrNull">If set, specifies the CommonJS export to be invoked. If not set, the module's default CommonJS export itself must be a function to be invoked.</param>
        /// <param name="args">Any sequence of JSON-serializable arguments to be passed to the Node.js function.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the completion of the RPC call.</returns>
        //TaskAsync<T> CacheExportsAsyncAsync<T>(CancellationToken cancellationToken, string identifier, object source);

        //TaskAsync<T> InvokeExportAsyncAsync<T>(CancellationToken cancellationToken, string identifier, object source);

        Task<T> InvokeExportFromFileAsync<T>(string relativePath,
            bool cache = true,
            string export = null,
            object[] args = null,
            CancellationToken cancellationToken = default(CancellationToken));

        Task<T> InvokeExportFromStringAsync<T>(string module,
            string cacheIdentifier = null,
            string export = null,
            object[] args = null,
            CancellationToken cancellationToken = default(CancellationToken));

        Task<T> InvokeExportFromStreamAsync<T>(Stream module,
            string cacheIdentifier = null,
            string export = null,
            object[] args = null,
            CancellationToken cancellationToken = default(CancellationToken));

        Task<bool> TryInvokeExportFromCacheAsync<T>(string identifier, out T result, string export = null, object[] args = null, CancellationToken cancellationToken = default(CancellationToken));
    }
}