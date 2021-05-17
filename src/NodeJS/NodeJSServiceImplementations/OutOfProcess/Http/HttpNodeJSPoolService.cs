using System;
using System.Collections.ObjectModel;
using System.Threading;

namespace Jering.Javascript.NodeJS
{
    /// <summary>
    /// An implementation of <see cref="INodeJSService"/> that uses Http for inter-process communication with a pool of NodeJS processes.
    /// </summary>
    public partial class HttpNodeJSPoolService : INodeJSService
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
        /// Creates a <see cref="HttpNodeJSPoolService"/>.
        /// </summary>
        public HttpNodeJSPoolService(ReadOnlyCollection<HttpNodeJSService> httpNodeJSServices)
        {
            _httpNodeJSServices = httpNodeJSServices;
            Size = httpNodeJSServices.Count;
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
            GC.SuppressFinalize(this);
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
