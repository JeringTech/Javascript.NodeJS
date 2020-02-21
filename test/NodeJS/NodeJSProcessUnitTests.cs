using Moq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using Xunit;

namespace Jering.Javascript.NodeJS.Tests
{
    /// <summary>
    /// <para>Logic in some simple functions (no branching) is covered by HttpNodeJSServiceIntegrationTests.</para>
    /// <para>Because this class wraps a <see cref="Process"/>, some tests here require starting a procees.</para>
    /// </summary>
    public class NodeJSProcessUnitTests
    {
        private readonly MockRepository _mockRepository = new MockRepository(MockBehavior.Default);
        private const string DUMMY_LONG_RUNNING_SCRIPT_NAME = "dummyLongRunningScript.js";
        private static readonly string _projectPath = Path.Combine(Directory.GetCurrentDirectory(), "../../../Javascript"); // Current directory is <test project path>/bin/debug/<framework>
        private static readonly string _dummyLongRunningScript = File.ReadAllText(Path.Combine(_projectPath, DUMMY_LONG_RUNNING_SCRIPT_NAME));

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
        public void Constructor_ThrowsArgumentExceptionIfProcessHasExited()
        {
            // Arrange
            using (Process dummyProcess = CreateProcess())
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
        public void Constructor_ThrowsArgumentExceptionIfProcessHasBeenDisposed()
        {
            // Arrange
            using (Process dummyProcess = CreateProcess())
            {
                dummyProcess.Kill();
                dummyProcess.WaitForExit();
                dummyProcess.Dispose();

                // Act and assert
                ArgumentException result = Assert.Throws<ArgumentException>(() => new NodeJSProcess(dummyProcess));
                Assert.IsType<InvalidOperationException>(result.InnerException);
                Assert.Equal(Strings.ArgumentException_NodeJSProcess_ProcessHasNotBeenStartedOrHasBeenDisposed + "\nParameter name: process",
                    result.Message,
                    ignoreLineEndingDifferences: true);
            }
        }

        // Indirectly tests InternalOutputDataReceivedHandler
        [Fact]
        public void AddOutputReceivedHandler_AddsHandlers()
        {
            // Arrange
            var dummySender = new object();
            var dummyStringBuilder = new StringBuilder();
            DataReceivedEventArgs dummyDataReceivedEventArgs = CreateDataReceivedEventArgs();
            MessageReceivedEventHandler dummyHandler = (object _, string __) => { };
            Mock<NodeJSProcess> mockTestSubject = CreateMockNodeJSProcess(outputDataStringBuilder: dummyStringBuilder);
            mockTestSubject.CallBase = true;
            mockTestSubject.Setup(n => n.DataReceivedHandler(dummyStringBuilder, dummyHandler, dummySender, dummyDataReceivedEventArgs)); // Output received handler added
            mockTestSubject.Setup(n => n.AddOutputDataReceivedHandler(mockTestSubject.Object.InternalOutputDataReceivedHandler)); // Output data received handler added

            // Act
            mockTestSubject.Object.AddOutputReceivedHandler(dummyHandler);

            // Assert
            mockTestSubject.Object.InternalOutputDataReceivedHandler(dummySender, dummyDataReceivedEventArgs);
            mockTestSubject.VerifyAll();
        }

        [Fact]
        public void AddOutputReceivedHandler_OnlyAddsInternalOutputDataReceivedHandlerOnce()
        {
            // Arrange
            Mock<NodeJSProcess> mockTestSubject = CreateMockNodeJSProcess();
            mockTestSubject.CallBase = true;
            mockTestSubject.Setup(t => t.AddOutputDataReceivedHandler(mockTestSubject.Object.InternalOutputDataReceivedHandler));
            MessageReceivedEventHandler dummyHandler = (object _, string __) => { };

            // Act
            mockTestSubject.Object.AddOutputReceivedHandler(dummyHandler);
            mockTestSubject.Object.AddOutputReceivedHandler(dummyHandler);

            // Assert
            mockTestSubject.Verify(n => n.AddOutputDataReceivedHandler(mockTestSubject.Object.InternalOutputDataReceivedHandler), Times.Once);
        }

        // Indirectly tests InternalErrorDataReceivedHandler
        [Fact]
        public void AddErrorReceivedHandler_AddsHandlers()
        {
            // Arrange
            var dummySender = new object();
            var dummyStringBuilder = new StringBuilder();
            DataReceivedEventArgs dummyDataReceivedEventArgs = CreateDataReceivedEventArgs();
            MessageReceivedEventHandler dummyHandler = (object _, string __) => { };
            Mock<NodeJSProcess> mockTestSubject = CreateMockNodeJSProcess(errorDataStringBuilder: dummyStringBuilder);
            mockTestSubject.CallBase = true;
            mockTestSubject.Setup(n => n.DataReceivedHandler(dummyStringBuilder, dummyHandler, dummySender, dummyDataReceivedEventArgs)); // Error received handler added
            mockTestSubject.Setup(n => n.AddErrorDataReceivedHandler(mockTestSubject.Object.InternalErrorDataReceivedHandler)); // Error data received handler added

            // Act
            mockTestSubject.Object.AddErrorReceivedHandler(dummyHandler);

            // Assert
            mockTestSubject.Object.InternalErrorDataReceivedHandler(dummySender, dummyDataReceivedEventArgs);
            mockTestSubject.VerifyAll();
        }

        [Fact]
        public void AddErrorReceivedHandler_OnlyAddsInternalErrorDataReceivedHandlerOnce()
        {
            // Arrange
            Mock<NodeJSProcess> mockTestSubject = CreateMockNodeJSProcess();
            mockTestSubject.CallBase = true;
            mockTestSubject.Setup(t => t.AddErrorDataReceivedHandler(mockTestSubject.Object.InternalErrorDataReceivedHandler));
            MessageReceivedEventHandler dummyHandler = (object _, string __) => { };

            // Act
            mockTestSubject.Object.AddErrorReceivedHandler(dummyHandler);
            mockTestSubject.Object.AddErrorReceivedHandler(dummyHandler);

            // Assert
            mockTestSubject.Verify(n => n.AddErrorDataReceivedHandler(mockTestSubject.Object.InternalErrorDataReceivedHandler), Times.Once);
        }

        [Fact]
        public void ExitStatus_ReturnsExitStatusNotExitedMessageIfProcessHasNotExited()
        {
            Process dummyProcess = null;
            try
            {
                // Arrange
                dummyProcess = CreateProcess();
                NodeJSProcess testSubject = CreateNodeJSProcess(dummyProcess);

                // Act
                string result = testSubject.ExitStatus;

                // Assert
                Assert.Equal(NodeJSProcess.EXIT_STATUS_NOT_EXITED, result);
            }
            finally
            {
                try
                {
                    dummyProcess?.Kill();
                }
                catch
                {
                    // If Kill throws, process has already terminated
                }
                finally
                {
                    dummyProcess?.Dispose();
                }
            }
        }

        [Fact]
        public void ExitStatus_ReturnsExitStatusDisposedIfProcessHasBeenDisposed()
        {
            // Arrange
            using (NodeJSProcess testSubject = CreateNodeJSProcess())
            {
                // Act
                testSubject.Dispose();
                string result = testSubject.ExitStatus;

                // Assert
                Assert.Equal(NodeJSProcess.EXIT_STATUS_DISPOSED, result);
            }
        }

        [Fact]
        public void ExitStatus_ReturnsExitCodeIfProcessHasExitedButNotBeenDisposedOf()
        {
            Process dummyProcess = null;
            try
            {
                // Arrange
                dummyProcess = CreateProcess();
                NodeJSProcess testSubject = CreateNodeJSProcess(dummyProcess);

                // Act
                testSubject.Kill();
                dummyProcess.WaitForExit();
                string result = testSubject.ExitStatus;

                // Assert
                Assert.NotEmpty(result); // Can't gaurantee what exit code will be returned on all platforms
            }
            finally
            {
                try
                {
                    dummyProcess?.Kill();
                }
                catch
                {
                    // If Kill throws, process has already terminated
                }
                finally
                {
                    dummyProcess?.Dispose();
                }
            }
        }

        [Fact]
        public void DataReceivedHandler_DoesNothingIfEventDataIsNull()
        {
            // Arrange
            Mock<NodeJSProcess> mockTestSubject = CreateMockNodeJSProcess();
            mockTestSubject.CallBase = true;

            // Act
            mockTestSubject.Object.DataReceivedHandler(null, null, null, CreateDataReceivedEventArgs());

            // Assert
            string dummyOutMessage = null;
            mockTestSubject.Verify(t => t.TryCreateMessage(It.IsAny<StringBuilder>(), It.IsAny<string>(), out dummyOutMessage), Times.Never());
        }

        [Fact]
        public void DataReceivedHandler_IfDataIsNotNullAndMessageIsCreatedCallsMessageReceivedEventHandler()
        {
            // Arrange
            var dummySender = new object();
            const string dummyData = "dummyData";
            var dummyStringBuilder = new StringBuilder();
            Mock<NodeJSProcess> mockTestSubject = CreateMockNodeJSProcess();
            mockTestSubject.CallBase = true;
            string dummyMessage = "dummyMessage";
            mockTestSubject.Setup(t => t.TryCreateMessage(dummyStringBuilder, dummyData, out dummyMessage)).Returns(true);
            object resultSender = null;
            string resultMessage = null;
            MessageReceivedEventHandler dummyMessageReceivedEventHandler = (object sender, string message) => { resultSender = sender; resultMessage = message; };

            // Act
            mockTestSubject.Object.DataReceivedHandler(dummyStringBuilder, dummyMessageReceivedEventHandler, dummySender, CreateDataReceivedEventArgs(dummyData));

            // Assert
            _mockRepository.VerifyAll();
            Assert.Same(dummySender, resultSender);
            Assert.Equal(dummyMessage, resultMessage);
        }

        [Fact]
        public void DataReceivedHandler_IfDataIsNotNullButMessageIsNotCreatedDoesNotCallMessageReceivedEventHandler()
        {
            // Arrange
            var dummySender = new object();
            const string dummyData = "dummyData";
            var dummyStringBuilder = new StringBuilder();
            Mock<NodeJSProcess> mockTestSubject = CreateMockNodeJSProcess();
            mockTestSubject.CallBase = true;
            string dummyMessage = null;
            mockTestSubject.Setup(t => t.TryCreateMessage(dummyStringBuilder, dummyData, out dummyMessage)).Returns(false);

            // Act
            mockTestSubject.Object.DataReceivedHandler(dummyStringBuilder, null, dummySender, CreateDataReceivedEventArgs(dummyData));

            // Assert
            _mockRepository.VerifyAll();
        }

        [Theory]
        [MemberData(nameof(TryCreateMessage_AppendsDataToStringBuilderAndReturnsFalseIfTheDataDoesNotEndWithANullTerminatingCharacter_Data))]
        public void TryCreateMessage_AppendsDataToStringBuilderAndReturnsFalseIfTheDataDoesNotEndWithANullTerminatingCharacter(string dummyData)
        {
            // Arrange
            var dummyStringBuilder = new StringBuilder();
            NodeJSProcess testSubject = CreateNodeJSProcess();

            // Act
            bool result = testSubject.TryCreateMessage(dummyStringBuilder, dummyData, out string resultMessage);

            // Assert
            Assert.False(result);
            Assert.Null(resultMessage);
            Assert.Equal(dummyData + "\n", dummyStringBuilder.ToString(), ignoreLineEndingDifferences: true);
        }

        public static IEnumerable<object[]> TryCreateMessage_AppendsDataToStringBuilderAndReturnsFalseIfTheDataDoesNotEndWithANullTerminatingCharacter_Data()
        {
            return new object[][]
            {
                new object[] {"dummyData"},
                new object[] {string.Empty}
            };
        }

        [Fact]
        public void TryCreateMessage_ResetsStringBuilderReturnsTrueAndAMessageIfTheDataEndsWithANullTerminatingCharacter()
        {
            // Arrange
            var dummyStringBuilder = new StringBuilder();
            const string dummyData = "dummyData";
            string dummyDataWithNullTerminatingCharacter = $"{dummyData}\0";
            NodeJSProcess testSubject = CreateNodeJSProcess();

            // Act
            bool result = testSubject.TryCreateMessage(dummyStringBuilder, dummyDataWithNullTerminatingCharacter, out string resultMessage);

            // Assert
            Assert.True(result);
            Assert.Equal(dummyData, resultMessage);
            Assert.Equal(0, dummyStringBuilder.Length);
        }

        // https://stackoverflow.com/questions/1354308/how-to-instantiate-datareceivedeventargs-or-be-able-to-fill-it-with-data
        private DataReceivedEventArgs CreateDataReceivedEventArgs(string TestData = null)
        {
            var MockEventArgs =
                (DataReceivedEventArgs)System.Runtime.Serialization.FormatterServices
                 .GetUninitializedObject(typeof(DataReceivedEventArgs));

            FieldInfo[] EventFields = typeof(DataReceivedEventArgs)
                .GetFields(
                    BindingFlags.NonPublic |
                    BindingFlags.Instance |
                    BindingFlags.DeclaredOnly);

            EventFields[0].SetValue(MockEventArgs, TestData);

            return MockEventArgs;
        }

        private Process CreateProcess()
        {
            var dummyNodeJSProcessFactory = new NodeJSProcessFactory(null);
            ProcessStartInfo dummyProcessStartInfo = dummyNodeJSProcessFactory.CreateStartInfo(_dummyLongRunningScript);

            return dummyNodeJSProcessFactory.CreateProcess(dummyProcessStartInfo);
        }

        private NodeJSProcess CreateNodeJSProcess(Process dummyProcess = null,
            StringBuilder outputDataStringBuilder = null,
            StringBuilder errorDataStringBuilder = null)
        {
            return new NodeJSProcess(dummyProcess, outputDataStringBuilder, errorDataStringBuilder);
        }

        private Mock<NodeJSProcess> CreateMockNodeJSProcess(Process dummyProcess = null,
            StringBuilder outputDataStringBuilder = null,
            StringBuilder errorDataStringBuilder = null)
        {
            return _mockRepository.Create<NodeJSProcess>(dummyProcess, outputDataStringBuilder, errorDataStringBuilder);
        }
    }
}
