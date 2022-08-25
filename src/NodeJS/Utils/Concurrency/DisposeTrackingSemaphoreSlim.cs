using System;
using System.Threading;

namespace Jering.Javascript.NodeJS
{
    /// <summary>
    /// Light wrapper of <see cref="SemaphoreSlim"/> that tracks whether or not it is disposed.
    /// </summary>
    internal class DisposeTrackingSemaphoreSlim : SemaphoreSlim
    {
        private bool _disposed;

        public DisposeTrackingSemaphoreSlim(int initialCount, int maxCount) : base(initialCount, maxCount)
        {
        }


        /// <summary>
        /// Calls <see cref="SemaphoreSlim.Release()"/> if the <see cref="SemaphoreSlim"/> is not disposed.
        /// </summary>
        /// <remarks>Allows us to avoid an <see cref="ObjectDisposedException"/>.</remarks>
        public void ReleaseIfNotDisposed()
        {
            if (!_disposed)
            {
                Release();
            }
        }

        protected override void Dispose(bool explicitDisposing)
        {
            _disposed = true;
            base.Dispose(explicitDisposing);
        }
    }
}
