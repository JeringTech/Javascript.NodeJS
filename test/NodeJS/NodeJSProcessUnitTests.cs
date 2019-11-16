using Moq;
using System;
using System.Diagnostics;
using Xunit;

namespace Jering.Javascript.NodeJS.Tests
{
    // Most of the logic in this class is covered by HttpNodeJSServiceIntegrationTests.
    public class NodeJSProcessUnitTests
    {
        private readonly MockRepository _mockRepository = new MockRepository(MockBehavior.Default);

        [Fact]
        public void Constructor_ThrowsArgumentNullExceptionIfProcessIsNull()
        {
            // Act and assert
            Assert.Throws<ArgumentNullException>(() => new NodeJSProcess(null));
        }

        [Fact]
        public void Constructor_ThrowsArgumentExceptionIfProcessHasNotBeenStarted()
        {
            // Arrange
            using (var dummyProcess = new Process())
            {
                // Act and assert
                ArgumentException result = Assert.Throws<ArgumentException>(() => new NodeJSProcess(dummyProcess));
                Assert.IsType<InvalidOperationException>(result.InnerException);
                Assert.Equal(Strings.ArgumentException_NodeJSProcess_ProcessHasNotBeenStartedOrHasBeenDisposed + "\nParameter name: process",
                    result.Message,
                    ignoreLineEndingDifferences: true);
            }
        }

        [Fact]
        public void Constructor_ThrowsArgumentExceptionIfProcessHasBeenDisposed()
        {
            // Arrange
            Process dummyProcess = CreateNodeJSProcess();
            dummyProcess.Dispose();

            // Act and assert
            ArgumentException result = Assert.Throws<ArgumentException>(() => new NodeJSProcess(dummyProcess));
            Assert.IsType<InvalidOperationException>(result.InnerException);
            Assert.Equal(Strings.ArgumentException_NodeJSProcess_ProcessHasNotBeenStartedOrHasBeenDisposed + "\nParameter name: process",
                result.Message,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void Constructor_ThrowsArgumentExceptionIfProcessHasExited()
        {
            // Arrange
            using (Process dummyProcess = CreateNodeJSProcess())
            {
                dummyProcess.Kill();
                dummyProcess.WaitForExit();

                // Act and assert
                ArgumentException result = Assert.Throws<ArgumentException>(() => new NodeJSProcess(dummyProcess));
                Assert.Equal(Strings.ArgumentException_NodeJSProcess_ProcessHasExited + "\nParameter name: process",
                    result.Message,
                    ignoreLineEndingDifferences: true);
            }
        }

        [Fact]
        public void ExitStatus_ReturnsExitStatusNotExitedMessageIfProcessHasNotExited()
        {
            // Arrange
            Process dummyProcess = CreateNodeJSProcess();
            Mock<NodeJSProcess> mockTestSubject = _mockRepository.Create<NodeJSProcess>(dummyProcess);
            mockTestSubject.CallBase = true;
            using (NodeJSProcess testSubject = mockTestSubject.Object)
            {
                // Act
                string result = mockTestSubject.Object.ExitStatus;

                // Assert
                Assert.Equal(NodeJSProcess.EXIT_STATUS_NOT_EXITED, result);
            }
        }

        [Fact]
        public void ExitStatus_ReturnsExitStatusDisposedIfProcessHasBeenDisposed()
        {
            // Arrange
            Process dummyProcess = CreateNodeJSProcess();
            Mock<NodeJSProcess> mockTestSubject = _mockRepository.Create<NodeJSProcess>(dummyProcess);
            mockTestSubject.CallBase = true;
            using (NodeJSProcess testSubject = mockTestSubject.Object)
            {
                // Act
                testSubject.Dispose();
                string result = mockTestSubject.Object.ExitStatus;

                // Assert
                Assert.Equal(NodeJSProcess.EXIT_STATUS_DISPOSED, result);
            }
        }

        [Fact]
        public void ExitStatus_ReturnsExitCodeIfProcessHasExitedButNotBeenDisposedOf()
        {
            // Arrange
            Process dummyProcess = CreateNodeJSProcess();
            Mock<NodeJSProcess> mockTestSubject = _mockRepository.Create<NodeJSProcess>(dummyProcess);
            mockTestSubject.CallBase = true;
            using (NodeJSProcess testSubject = mockTestSubject.Object)
            {
                // Act
                testSubject.Kill();
                dummyProcess.WaitForExit();
                string result = mockTestSubject.Object.ExitStatus;

                // Assert
                Assert.NotEmpty(result); // Can't gaurantee what exit code will be returned on all platforms
            }
        }

        private Process CreateNodeJSProcess()
        {
            // Get server script
            var embeddedResourcesService = new EmbeddedResourcesService();
            string serverScript = embeddedResourcesService.ReadAsString(typeof(HttpNodeJSService), HttpNodeJSService.SERVER_SCRIPT_NAME);

            // Create process
            var nodeJSProcessFactory = new NodeJSProcessFactory(null);
            ProcessStartInfo processStartInfo = nodeJSProcessFactory.CreateStartInfo(serverScript);
            return nodeJSProcessFactory.CreateProcess(processStartInfo);
        }
    }
}
