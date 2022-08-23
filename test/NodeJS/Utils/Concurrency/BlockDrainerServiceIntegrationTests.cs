using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Jering.Javascript.NodeJS.Tests
{
    /// <summary>
    /// Covers real-world usage of <see cref="BlockDrainerService"/>.
    /// </summary>
    public class BlockDrainerServiceIntegrationTests
    {
        private const int TIMEOUT_MS = 60000;

        [Fact(Timeout = TIMEOUT_MS)]
        public async void BlockDrainerService_DrainsBlocks()
        {
            // Arrange
            var testSubject = new BlockDrainerService();
            int numOperationsPerGroup = 5;
            int numResets = 5;
            for (int i = 0; i < numResets; i++) // To verify that BlockDrainer is reusable after resetting
            {
                var dummyTaskCompletionSource = new TaskCompletionSource<bool>();
                Task<bool> dummyTask = dummyTaskCompletionSource.Task;
                List<Task> dummyOperationGroup1 = new(numOperationsPerGroup);
                List<Task> dummyOperationGroup2 = new(numOperationsPerGroup);
                var dummyCounter = new SemaphoreSlim(0, numOperationsPerGroup);

                // Act
                for (int j = 0; j < numOperationsPerGroup; j++) // Start first group of operations, these should stop at dummyTask
                {
                    dummyOperationGroup1.Add(Task.Run(async () =>
                    {
                        await testSubject.EnterBlockAsync().ConfigureAwait(false);
                        try
                        {
                            dummyCounter.Release();
                            await dummyTask.ConfigureAwait(false);
                        }
                        finally
                        {
                            testSubject.ExitBlock();
                        }
                    }));
                }
                for (int j = 0; j < numOperationsPerGroup; j++) // Wait for all operations to enter block
                {
                    await dummyCounter.WaitAsync().ConfigureAwait(false);
                }
                Task dummyDrainTask = testSubject.DrainBlockAndPreventEntryAsync(); // Start draining
                for (int j = 0; j < numOperationsPerGroup; j++) // Start second group of operations, these should not make it into the block initially
                {
                    dummyOperationGroup2.Add(Task.Run(async () =>
                    {
                        await testSubject.EnterBlockAsync().ConfigureAwait(false);
                        try
                        {
                            await dummyTask.ConfigureAwait(false);
                        }
                        finally
                        {
                            testSubject.ExitBlock();
                        }
                    }));
                }
                dummyTaskCompletionSource.SetResult(true); // Release dummyTask, first group of operations can now run to completion

                // Assert 
                await Task.WhenAll(dummyOperationGroup1).ConfigureAwait(false); // First group of operations can complete, test times out if they do not complete
                await dummyDrainTask.ConfigureAwait(false);  // Block drained, test times out if it does not complete
                for (int j = 0; j < numOperationsPerGroup; j++) // Second group not completed (waiting for BlockDrainer to release entrance lock)
                {
                    Assert.False(dummyOperationGroup2[j].IsCompleted);
                }

                // Act
                testSubject.ResetAfterDraining(); // Reset BlockDrainer, releases second group

                // Assert
                await Task.WhenAll(dummyOperationGroup2).ConfigureAwait(false); // Test times out if they do not complete
            }
        }
    }
}
