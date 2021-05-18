using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Xunit;

namespace Jering.Javascript.NodeJS.Tests
{
    public class NodeJSProcessFactoryUnitTests
    {
        private readonly MockRepository _mockRepository = new(MockBehavior.Default);

        [Fact]
        public void CreateStartInfo_CreatesStartInfo()
        {
            // Arrange
            const string dummyNodeServerScript = "dummyNodeServerScript";
            const string dummyNodeAndV8Options = "dummyNodeAndV8Options";
            const int dummyPort = 123; // Arbitrary
            const string dummyProjectPath = "dummyProjectPath";
            const string dummyEnvironmentVariable = "dummyEnvironmentVariable";
            const string dummyEnvironmentVariableValue = "dummyEnvironmentVariableValue";
            var dummyEnvironmentVariables = new Dictionary<string, string> { { dummyEnvironmentVariable, dummyEnvironmentVariableValue } };
            var dummyNodeJSProcessOptions = new NodeJSProcessOptions
            {
                NodeAndV8Options = dummyNodeAndV8Options,
                Port = dummyPort,
                ProjectPath = dummyProjectPath,
                EnvironmentVariables = dummyEnvironmentVariables
            };
            Mock<IOptions<NodeJSProcessOptions>> mockOptionsAccessor = _mockRepository.Create<IOptions<NodeJSProcessOptions>>();
            mockOptionsAccessor.Setup(o => o.Value).Returns(dummyNodeJSProcessOptions);
            NodeJSProcessFactory testSubject = CreateNodeJSProcessFactory(mockOptionsAccessor.Object);

            // Act
            ProcessStartInfo result = testSubject.CreateStartInfo(dummyNodeServerScript);

            // Assert
#if NET5_0
            int currentProcessPid = Environment.ProcessId;
#else
            int currentProcessPid = Process.GetCurrentProcess().Id;
#endif
            Assert.Equal($"{dummyNodeAndV8Options} -e \"{dummyNodeServerScript}\" -- --parentPid {currentProcessPid} --port {dummyPort}", result.Arguments);
            Assert.False(result.UseShellExecute);
            Assert.True(result.RedirectStandardInput);
            Assert.True(result.RedirectStandardOutput);
            Assert.True(result.RedirectStandardError);
            Assert.Equal(dummyProjectPath, result.WorkingDirectory);
            result.Environment.TryGetValue(dummyEnvironmentVariable, out string? resultEnvironmentVariableValue);
            Assert.NotNull(resultEnvironmentVariableValue);
            Assert.Equal(dummyEnvironmentVariableValue, resultEnvironmentVariableValue);
        }

        [Theory]
        [MemberData(nameof(EscapeCommandLineArg_EscapesCommandLineArgs_Data))]
        public void EscapeCommandLineArg_EscapesCommandLineArgs(string dummyArg, string expectedResult)
        {
            // Act
            string result = NodeJSProcessFactory.EscapeCommandLineArg(dummyArg);

            // Assert
            Assert.Equal(expectedResult, result);
        }

        public static IEnumerable<object[]> EscapeCommandLineArg_EscapesCommandLineArgs_Data()
        {
            return new object[][]
            {
                new object[]{@"""", @"\"""}, // Quotes must be escaped
                new object[]{@"\""", @"\\\"""}, // Escaped quotes must be escaped along with their preceding blackslash: \" escaped to \\"
                new object[]{@"\\""", @"\\\\\"""}, // \\" escaped to \\\\\"
                new object[]{@"test\", @"test\\"} // Backslashes at the end of the arg must be escaped to avoid the closing quote being escaped in the command line (e.g node "test\")
            };
        }

        private NodeJSProcessFactory CreateNodeJSProcessFactory(IOptions<NodeJSProcessOptions>? optionsAccessor = null)
        {
            return new NodeJSProcessFactory(optionsAccessor ?? _mockRepository.Create<IOptions<NodeJSProcessOptions>>().Object);
        }
    }
}
