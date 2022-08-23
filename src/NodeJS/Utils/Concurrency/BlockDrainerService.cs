using System;
using System.Threading;
using System.Threading.Tasks;

namespace Jering.Javascript.NodeJS
{
    /// <summary>
    /// The default implementation of <see cref="IBlockDrainerService"/>.
    /// </summary>
    public class BlockDrainerService : IBlockDrainerService
    {
        /// <summary>
        /// The <see cref="TaskCompletionSource{TResult}"/> for the drain event.
        /// </summary>
        private TaskCompletionSource<bool> _drainTaskCompletionSource = new();

        /// <summary>
        /// The number of operations in the block of code or one less than if an operation is draining the block.
        /// </summary>
        private int _count = 0;

        /// <summary>
        /// The lock for entering the block of code.
        /// </summary>
        private readonly SemaphoreSlim _entranceLock = new(1, 1);

        /// <summary>
        /// Whether the instance is disposed.
        /// </summary>
        private bool _disposed = false;

        /// <inheritdoc/>
        public async Task EnterBlockAsync()
        {
            // Wait for entrance lock. Prevents entry while another operation is draining the block, see DrainBlockAndPreventEntryAsync.
            await _entranceLock.WaitAsync().ConfigureAwait(false);

            // Increment number of operations in block
            Interlocked.Increment(ref _count);

            // Entered block, release entrance lock
            _entranceLock.Release();
        }

        /// <inheritdoc/>
        public void ExitBlock()
        {
            // Decrement number of operations in block
            int newCount = Interlocked.Decrement(ref _count);

            // There are still operations in the block or no operation is draining the block
            if (newCount > -1)
            {
                return;
            }

            // If this occurs, ExitBlock is being called more than once for some EnterBlockAsync invocations.
            if (newCount < -1)
            {
                throw new InvalidOperationException(Strings.InvalidOperationException_BlockDrain_OperationCountBelowMinimum);
            }

            // newCount == -1, an operation is draining the block and all operations have exited
            _drainTaskCompletionSource.TrySetResult(true);
        }

        /// <inheritdoc/>
        public async Task DrainBlockAndPreventEntryAsync()
        {
            // Prevent threads from entering block
            await _entranceLock.WaitAsync().ConfigureAwait(false);

            // Decrement count so it can go < 0
            int newCount = Interlocked.Decrement(ref _count);

            // There are still operations in the block
            if (newCount > -1)
            {
                await _drainTaskCompletionSource.Task.ConfigureAwait(false);
                return;
            }

            // If this occurs, ExitBlock is being called more than once for some EnterBlockAsync invocations.
            if (newCount < -1)
            {
                throw new InvalidOperationException(Strings.InvalidOperationException_BlockDrain_OperationCountBelowMinimum);
            }

            // newCount == -1, no operations in block. Do nothing.
        }

        /// <inheritdoc/>
        public void ResetAfterDraining()
        {
            // Reset count
            _count = 0;

            // Create new task completion source
            _drainTaskCompletionSource = new TaskCompletionSource<bool>();

            // Allows threads to enter block again
            _entranceLock.Release();
        }

        /// <summary>
        /// Disposes this instance.
        /// </summary>
        /// <remarks>This method is not thread-safe. It should only be called after all other calls to this instance's methods have returned.</remarks>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this); // In case a sub class overrides Object.Finalize - https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-dispose#the-dispose-overload
        }

        /// <summary>
        /// Disposes the instance.
        /// </summary>
        /// <param name="disposing">True if the object is disposing or false if it is finalizing.</param>
        /// <remarks>This method is not thread-safe. It should only be called after all other calls to this instance's methods have returned.</remarks>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _entranceLock?.Dispose();
            }

            _disposed = true;
        }
    }
}
