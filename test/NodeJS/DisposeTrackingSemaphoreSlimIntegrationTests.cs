using Xunit;

namespace Jering.Javascript.NodeJS.Tests
{
    public class DisposeTrackingSemaphoreSlimIntegrationTests
    {
        [Fact]
        public void ReleaseIfNotDisposed_DoesNotThrowObjectDisposedExceptionIfCalledAfterObjectIsDisposed()
        {
            // Arrange
            DisposeTrackingSemaphoreSlim testSubject = new(1, 1);
            testSubject.Dispose();

            // Act and assert
            testSubject.ReleaseIfNotDisposed(); // Successfuly if this does not throw
        }

        [Fact]
        public void ReleaseIfNotDisposed_ReleasesIfObjectHasNotBeenDisposed()
        {
            // Arrange
            DisposeTrackingSemaphoreSlim testSubject = new(0, 1);

            // Act
            testSubject.ReleaseIfNotDisposed();

            // Assert
            Assert.Equal(1, testSubject.CurrentCount);
        }
    }
}
