using System;
using System.Threading.Tasks;

namespace Jering.Javascript.NodeJS
{
    /// <summary>
    /// Synchronization type used to drain operations from a code block.
    /// </summary>
    /// <remarks>In order to drain operations from a block, we must keep track of operations in the block. This is done using <see cref="EnterBlockAsync"/> and
    /// <see cref="ExitBlock"/>. Draining is done using <see cref="DrainBlockAndPreventEntryAsync"/>.</remarks>
    public interface IBlockDrainerService : IDisposable
    {
        /// <summary>
        /// Enter the block of code.
        /// </summary>
        /// <returns>The <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <remarks>The calling operation must call <see cref="ExitBlock"/> in a finally block after the block of code.</remarks>
        Task EnterBlockAsync();

        /// <summary>
        /// Exit the block of code.
        /// </summary>
        /// <remarks>Each call to this method must be preceded by exactly one call to <see cref="EnterBlockAsync"/>.</remarks>
        void ExitBlock();

        /// <summary>
        /// Drain the block of code of operations.
        /// </summary>
        /// <remarks>To drain the block, this method prevents new operations from entering the block.</remarks>
        /// <remarks>The calling operation must call <see cref="ResetAfterDraining"/> in a finally block after the logic that required the draining.</remarks>
        Task DrainBlockAndPreventEntryAsync();

        /// <summary>
        /// Resets the <see cref="BlockDrainerService"/> after draining.
        /// </summary>
        /// <remarks>Each call to this method must be preceded by exactly one call to <see cref="DrainBlockAndPreventEntryAsync"/>.</remarks>
        void ResetAfterDraining();
    }
}
