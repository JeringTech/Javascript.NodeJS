using System;
using System.Threading.Tasks;
using Xunit;

namespace Jering.Javascript.NodeJS.Tests
{
    /// <summary>
    /// Covers edge cases that may not be tested in <see cref="BlockDrainerServiceIntegrationTests"/>.
    /// </summary>
    public class BlockDrainerServiceUnitTests
    {
        [Fact]
        public async void ExitBlock_DoesNothingIfNewCountIsLargerThanMinus1()
        {
            // Arrange
            var testSubject = new BlockDrainerService();
            await testSubject.EnterBlockAsync().ConfigureAwait(false); // Increment count from 0 to 1

            // Act and assert
            testSubject.ExitBlock(); // Does not throw
        }

        [Fact]
        public void ExitBlock_ThrowsInvalidOperationExceptionIfNewCountIsLessThanMinus1()
        {
            // Arrange
            var testSubject = new BlockDrainerService();

            // Act and assert
            testSubject.ExitBlock(); // Decrement count from 0 to -1
            Assert.Throws<InvalidOperationException>(() => testSubject.ExitBlock());
        }

        [Fact]
        public async void DrainBlockAndPreventEntryAsync_ReturnsUncompletedTaskIfNewCountIsLargerThanMinus1()
        {
            // Arrange
            var testSubject = new BlockDrainerService();
            await testSubject.EnterBlockAsync().ConfigureAwait(false); // Increment count from 0 to 1

            // Act
            Task result = testSubject.DrainBlockAndPreventEntryAsync();

            // Assert
            Assert.False(result.IsCompleted);
        }

        [Fact]
        public async void DrainBlockAndPreventEntryAsync_ThrowsInvalidOperationExceptionIfNewCountIsLessThanMinus1()
        {
            // Arrange
            var testSubject = new BlockDrainerService();
            testSubject.ExitBlock(); // Decrement count from 0 to -1

            // Act and assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => testSubject.DrainBlockAndPreventEntryAsync()).ConfigureAwait(false);
        }

        [Fact]
        public void DrainBlockAndPreventEntryAsync_ReturnsCompletedTaskIfNewCountIsMinus1()
        {
            // Arrange
            var testSubject = new BlockDrainerService();

            // Act
            Task result = testSubject.DrainBlockAndPreventEntryAsync(); // Synchronously returns completed task

            // Assert
            Assert.True(result.IsCompleted);
        }
    }
}
