using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Jering.Javascript.NodeJS
{
    /// <summary>
    /// An implementation of <see cref="INodeJSService"/> that uses Http for inter-process communication with a pool of NodeJS processes.
    /// </summary>
    public class HttpNodeJSPoolService : INodeJSService
    {
        private readonly ReadOnlyCollection<HttpNodeJSService> _httpNodeJSServices;

        private bool _disposed;
        // Does not need to be volatile since Interlocked.Increment has ordering guarantees
        private int _nextIndex;

        /// <summary>
        /// Gets the size of the <see cref="HttpNodeJSPoolService"/>.
        /// </summary>
        public int Size { get; }

        /// <summary>
        /// Creates a <see cref="HttpNodeJSPoolService"/> instance.
        /// </summary>
        public HttpNodeJSPoolService(ReadOnlyCollection<HttpNodeJSService> httpNodeJSServices)
        {
            _httpNodeJSServices = httpNodeJSServices;
            Size = httpNodeJSServices.Count;
        }

        /// <inheritdoc />
        public Task<T> InvokeFromFileAsync<T>(string modulePath, string exportName = null, object[] args = null, CancellationToken cancellationToken = default)
        {
            return GetHttpNodeJSService().InvokeFromFileAsync<T>(modulePath, exportName, args, cancellationToken);
        }

        /// <inheritdoc />
        public Task InvokeFromFileAsync(string modulePath, string exportName = null, object[] args = null, CancellationToken cancellationToken = default)
        {
            return GetHttpNodeJSService().InvokeFromFileAsync(modulePath, exportName, args, cancellationToken);
        }

        /// <inheritdoc />
        public Task<T> InvokeFromStringAsync<T>(string moduleString, string newCacheIdentifier = null, string exportName = null, object[] args = null, CancellationToken cancellationToken = default)
        {
            return GetHttpNodeJSService().InvokeFromStringAsync<T>(moduleString, newCacheIdentifier, exportName, args, cancellationToken);
        }

        /// <inheritdoc />
        public Task InvokeFromStringAsync(string moduleString, string newCacheIdentifier = null, string exportName = null, object[] args = null, CancellationToken cancellationToken = default)
        {
            return GetHttpNodeJSService().InvokeFromStringAsync(moduleString, newCacheIdentifier, exportName, args, cancellationToken);
        }

        /// <inheritdoc />
        public Task<T> InvokeFromStringAsync<T>(Func<string> moduleFactory, string cacheIdentifier, string exportName = null, object[] args = null, CancellationToken cancellationToken = default)
        {
            return GetHttpNodeJSService().InvokeFromStringAsync<T>(moduleFactory, cacheIdentifier, exportName, args, cancellationToken);
        }

        /// <inheritdoc />
        public Task InvokeFromStringAsync(Func<string> moduleFactory, string cacheIdentifier, string exportName = null, object[] args = null, CancellationToken cancellationToken = default)
        {
            return GetHttpNodeJSService().InvokeFromStringAsync(moduleFactory, cacheIdentifier, exportName, args, cancellationToken);
        }

        /// <inheritdoc />
        public Task<T> InvokeFromStreamAsync<T>(Stream moduleStream, string newCacheIdentifier = null, string exportName = null, object[] args = null, CancellationToken cancellationToken = default)
        {
            return GetHttpNodeJSService().InvokeFromStreamAsync<T>(moduleStream, newCacheIdentifier, exportName, args, cancellationToken);
        }

        /// <inheritdoc />
        public Task InvokeFromStreamAsync(Stream moduleStream, string newCacheIdentifier = null, string exportName = null, object[] args = null, CancellationToken cancellationToken = default)
        {
            return GetHttpNodeJSService().InvokeFromStreamAsync(moduleStream, newCacheIdentifier, exportName, args, cancellationToken);
        }

        /// <inheritdoc />
        public Task<T> InvokeFromStreamAsync<T>(Func<Stream> moduleFactory, string cacheIdentifier, string exportName = null, object[] args = null, CancellationToken cancellationToken = default)
        {
            return GetHttpNodeJSService().InvokeFromStreamAsync<T>(moduleFactory, cacheIdentifier, exportName, args, cancellationToken);
        }

        /// <inheritdoc />
        public Task InvokeFromStreamAsync(Func<Stream> moduleFactory, string cacheIdentifier, string exportName = null, object[] args = null, CancellationToken cancellationToken = default)
        {
            return GetHttpNodeJSService().InvokeFromStreamAsync(moduleFactory, cacheIdentifier, exportName, args, cancellationToken);
        }

        /// <inheritdoc />
        public Task<(bool, T)> TryInvokeFromCacheAsync<T>(string moduleCacheIdentifier, string exportName = null, object[] args = null, CancellationToken cancellationToken = default)
        {
            return GetHttpNodeJSService().TryInvokeFromCacheAsync<T>(moduleCacheIdentifier, exportName, args, cancellationToken);
        }

        /// <inheritdoc />
        public Task<bool> TryInvokeFromCacheAsync(string moduleCacheIdentifier, string exportName = null, object[] args = null, CancellationToken cancellationToken = default)
        {
            return GetHttpNodeJSService().TryInvokeFromCacheAsync(moduleCacheIdentifier, exportName, args, cancellationToken);
        }

        internal HttpNodeJSService GetHttpNodeJSService()
        {
            // Notes
            // - Interlocked.Increment wraps. This means if _nextIndex == Int32.MaxValue, it is set to Int32.MinValue - https://docs.microsoft.com/en-us/dotnet/api/system.threading.interlocked.increment?view=netstandard-2.0.
            // - unchecked((uint)number) means the bits representing the int are interpreted as uint - https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/numeric-conversions.
            // - Since .Net uses 2's complement to represent negative numbers, this means unchecked((uint)-1) == 4294967295 (UInt32.MaxValue), unchecked((uint)-2) == 4294967294 (UInt32.MaxValue - 1) and so on.
            // - This method will not return each HttpNodeJSService the same number of times when UInt32.MaxValue isn't divisible by Size. However, so long as between the 4 billion plus calls there is enough
            //   downtime for the NodeJS processes with extra invocations to complete them and catch up, we should be fine.
            uint index = unchecked((uint)Interlocked.Increment(ref _nextIndex));
            return _httpNodeJSServices[(int)(index % Size)];
        }

        /// <summary>
        /// Disposes this instance. This method is not thread-safe. It should only be called after all other calls to this instance's methods have returned.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Disposes the instance. This method is not thread-safe. It should only be called after all other calls to this instance's methods have returned.
        /// </summary>
        /// <param name="disposing">True if the object is disposing or false if it is finalizing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                foreach (HttpNodeJSService httpNodeJSService in _httpNodeJSServices)
                {
                    httpNodeJSService.Dispose();
                }
            }

            _disposed = true;
        }
    }
}
