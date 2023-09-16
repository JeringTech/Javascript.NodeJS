using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using System.Collections.Generic;
using Xunit;

namespace Jering.Javascript.NodeJS.Tests
{
    public class ConfigureNodeJSProcessOptionsUnitTests
    {
        private readonly MockRepository _mockRepository = new(MockBehavior.Default);

        [Theory]
        [MemberData(nameof(Configure_SetsExecutablePathIfItIsNullWhitespaceOrAnEmptyString_Data))]
        public void Configure_SetsExecutablePathIfItIsNullWhitespaceOrAnEmptyString(string dummyExecutablePath)
        {
            // Arrange
            Mock<IServiceProvider> mockServiceProvider = _mockRepository.Create<IServiceProvider>();
            mockServiceProvider.Setup(s => s.GetService(typeof(IHostEnvironment))).Returns(null); // Called by the extension method GetService<T>
            Mock<IServiceScope> mockServiceScope = _mockRepository.Create<IServiceScope>();
            mockServiceScope.Setup(s => s.ServiceProvider).Returns(mockServiceProvider.Object);
            Mock<IServiceScopeFactory> mockServiceScopeFactory = _mockRepository.Create<IServiceScopeFactory>();
            mockServiceScopeFactory.Setup(s => s.CreateScope()).Returns(mockServiceScope.Object);
            var dummyOptions = new NodeJSProcessOptions { ExecutablePath = dummyExecutablePath };
            ConfigureNodeJSProcessOptions testSubject = CreateConfigureNodeJSProcessOptions(mockServiceScopeFactory.Object);

            // Act
            testSubject.Configure(dummyOptions);

            // AssertS
            Assert.Equal("node", dummyOptions.ExecutablePath);
        }

        public static IEnumerable<object?[]> Configure_SetsExecutablePathIfItIsNullWhitespaceOrAnEmptyString_Data()
        {
            return new object?[][]
            {
                new object?[]{null},
                new object?[]{" "},
                new object?[]{string.Empty}
            };
        }

        [Fact]
        public void Configure_DoesNothingIfProjectPathAndNodeEnvAlreadySpecified()
        {
            // Arrange
            const string dummyProjectPath = "dummyProjectPath";
            const string nodeEnvVarName = "NODE_ENV";
            const string dummyEnvironmentState = "dummyEnvironmentState";
            var dummyEnvironmentVariables = new Dictionary<string, string> { { nodeEnvVarName, dummyEnvironmentState } };
            var dummyOptions = new NodeJSProcessOptions { ProjectPath = dummyProjectPath, EnvironmentVariables = dummyEnvironmentVariables };
            ConfigureNodeJSProcessOptions testSubject = CreateConfigureNodeJSProcessOptions();

            // Act
            testSubject.Configure(dummyOptions);

            // Assert
            Assert.Equal(dummyProjectPath, dummyOptions.ProjectPath); // Unchanged
            Assert.Equal(dummyEnvironmentState, dummyOptions.EnvironmentVariables[nodeEnvVarName]); // Unchanged
        }

        [Fact]
        public void Configure_DoesNothingIfThereIsNoHostingEnvironmentService()
        {
            // Arrange
            const string dummyProjectPath = "dummyProjectPath";
            Mock<IServiceProvider> mockServiceProvider = _mockRepository.Create<IServiceProvider>();
            mockServiceProvider.Setup(s => s.GetService(typeof(IHostEnvironment))).Returns(null); // Called by the extension method GetService<T>
            Mock<IServiceScope> mockServiceScope = _mockRepository.Create<IServiceScope>();
            mockServiceScope.Setup(s => s.ServiceProvider).Returns(mockServiceProvider.Object);
            Mock<IServiceScopeFactory> mockServiceScopeFactory = _mockRepository.Create<IServiceScopeFactory>();
            mockServiceScopeFactory.Setup(s => s.CreateScope()).Returns(mockServiceScope.Object);
            var dummyOptions = new NodeJSProcessOptions { ProjectPath = dummyProjectPath };
            ConfigureNodeJSProcessOptions testSubject = CreateConfigureNodeJSProcessOptions(mockServiceScopeFactory.Object);

            // Act
            testSubject.Configure(dummyOptions);

            // Assert
            _mockRepository.VerifyAll();
            Assert.Equal(dummyProjectPath, dummyOptions.ProjectPath);
            Assert.Empty(dummyOptions.EnvironmentVariables);
        }

        [Theory]
        [MemberData(nameof(Configure_ConfiguresProjectPathIfItIsNullWhitespaceOrAnEmptyString_Data))]
        public void Configure_ConfiguresProjectPathIfItIsNullWhitespaceOrAnEmptyString(string dummyProjectPath)
        {
            // Arrange
            const string dummyContentRootPath = "dummyContentRootPath";
            Mock<IHostEnvironment> mockHostingEnvironment = _mockRepository.Create<IHostEnvironment>();
            mockHostingEnvironment.Setup(h => h.ContentRootPath).Returns(dummyContentRootPath);
            Mock<IServiceProvider> mockServiceProvider = _mockRepository.Create<IServiceProvider>();
            mockServiceProvider.Setup(s => s.GetService(typeof(IHostEnvironment))).Returns(mockHostingEnvironment.Object); // Called by the extension method GetService<T>
            Mock<IServiceScope> mockServiceScope = _mockRepository.Create<IServiceScope>();
            mockServiceScope.Setup(s => s.ServiceProvider).Returns(mockServiceProvider.Object);
            Mock<IServiceScopeFactory> mockServiceScopeFactory = _mockRepository.Create<IServiceScopeFactory>();
            mockServiceScopeFactory.Setup(s => s.CreateScope()).Returns(mockServiceScope.Object);
            var dummyOptions = new NodeJSProcessOptions
            {
                ProjectPath = dummyProjectPath, // Since it's null, whitespace or an empty string, we won't return early
                EnvironmentVariables = new Dictionary<string, string> { { "NODE_ENV", "dummyEnvironmentState" } }
            };
            ConfigureNodeJSProcessOptions testSubject = CreateConfigureNodeJSProcessOptions(mockServiceScopeFactory.Object);

            // Act
            testSubject.Configure(dummyOptions);

            // Assert
            _mockRepository.VerifyAll();
            Assert.Equal(dummyContentRootPath, dummyOptions.ProjectPath);
        }

        public static IEnumerable<object?[]> Configure_ConfiguresProjectPathIfItIsNullWhitespaceOrAnEmptyString_Data()
        {
            return new object?[][]
            {
                new object ?[]{null},
                new object ?[]{" "},
                new object ?[]{string.Empty}
            };
        }

        [Fact]
        public void Configure_DoesNotOverwriteProjectPathIfItIsNotNullWhitespaceOrAnEmptyString()
        {
            // Arrange
            const string dummyProjectPath = "dummyProjectPath";
            Mock<IHostEnvironment> mockHostingEnvironment = _mockRepository.Create<IHostEnvironment>();
            mockHostingEnvironment.Setup(h => h.EnvironmentName).Returns("dummyEnvironments"); // Called by IsDevelopment()
            Mock<IServiceProvider> mockServiceProvider = _mockRepository.Create<IServiceProvider>();
            mockServiceProvider.Setup(s => s.GetService(typeof(IHostEnvironment))).Returns(mockHostingEnvironment.Object); // Called by the extension method GetService<T>
            Mock<IServiceScope> mockServiceScope = _mockRepository.Create<IServiceScope>();
            mockServiceScope.Setup(s => s.ServiceProvider).Returns(mockServiceProvider.Object);
            Mock<IServiceScopeFactory> mockServiceScopeFactory = _mockRepository.Create<IServiceScopeFactory>();
            mockServiceScopeFactory.Setup(s => s.CreateScope()).Returns(mockServiceScope.Object);
            var dummyOptions = new NodeJSProcessOptions
            {
                ProjectPath = dummyProjectPath,
                EnvironmentVariables = null! // So we don't return early
            };
            ConfigureNodeJSProcessOptions testSubject = CreateConfigureNodeJSProcessOptions(mockServiceScopeFactory.Object);

            // Act
            testSubject.Configure(dummyOptions);

            // Assert
            _mockRepository.VerifyAll();
            Assert.Equal(dummyProjectPath, dummyOptions.ProjectPath); // Unchanged
        }

        [Theory]
        [MemberData(nameof(Configure_ConfiguresNodeEnvIfItIsUnspecified_Data))]
        public void Configure_ConfiguresNodeEnvIfItIsUnspecified(Dictionary<string, string> dummyExistingEnvironmentVariables,
            string dummyEnvironments,
            Dictionary<string, string> expectedEnvironmentVariables)
        {
            // Arrange
            Mock<IHostEnvironment> mockHostingEnvironment = _mockRepository.Create<IHostEnvironment>();
            mockHostingEnvironment.Setup(h => h.EnvironmentName).Returns(dummyEnvironments); // Called by IsDevelopment()
            Mock<IServiceProvider> mockServiceProvider = _mockRepository.Create<IServiceProvider>();
            mockServiceProvider.Setup(s => s.GetService(typeof(IHostEnvironment))).Returns(mockHostingEnvironment.Object); // Called by the extension method GetService<T>
            Mock<IServiceScope> mockServiceScope = _mockRepository.Create<IServiceScope>();
            mockServiceScope.Setup(s => s.ServiceProvider).Returns(mockServiceProvider.Object);
            Mock<IServiceScopeFactory> mockServiceScopeFactory = _mockRepository.Create<IServiceScopeFactory>();
            mockServiceScopeFactory.Setup(s => s.CreateScope()).Returns(mockServiceScope.Object);
            var dummyOptions = new NodeJSProcessOptions { ProjectPath = "dummyProjectPath", EnvironmentVariables = dummyExistingEnvironmentVariables };
            ConfigureNodeJSProcessOptions testSubject = CreateConfigureNodeJSProcessOptions(mockServiceScopeFactory.Object);

            // Act
            testSubject.Configure(dummyOptions);

            // Assert
            _mockRepository.VerifyAll();
            Assert.Equal(expectedEnvironmentVariables, dummyOptions.EnvironmentVariables);
        }

        public static IEnumerable<object?[]> Configure_ConfiguresNodeEnvIfItIsUnspecified_Data()
        {
            const string nodeEnvVarName = "NODE_ENV";
            const string expectedProductionNodeEnvValue = "production";
            const string expectedDevelopmentNodeEnvValue = "development";
            const string dummyEnvVarName = "DUMMY_ENV_VAR";
            const string dummyEnvVarValue = "dummyEnvVarValue";

            return new object?[][]
            {
                // Sets NODE_ENV to development if Environments is Development
                new object ?[]{
                    null,
                    Environments.Development,
                    new Dictionary<string, string>{ { nodeEnvVarName, expectedDevelopmentNodeEnvValue } }
                },
                // Sets NODE_ENV to production if Environments is Production
                new object ?[]{
                    null,
                    Environments.Production,
                    new Dictionary<string, string>{ { nodeEnvVarName, expectedProductionNodeEnvValue } }
                },
                // Defaults to "production"
                new object ?[]{
                    null,
                    Environments.Staging,
                    new Dictionary<string, string>{ { nodeEnvVarName, expectedProductionNodeEnvValue } }
                },
                // Keeps existing environment variables
                new object ?[]{
                    new Dictionary<string, string> { { dummyEnvVarName, dummyEnvVarValue } },
                    Environments.Development,
                    new Dictionary<string, string>
                    {
                        { dummyEnvVarName, dummyEnvVarValue },
                        { nodeEnvVarName, expectedDevelopmentNodeEnvValue }
                    }
                }
            };
        }

        [Fact]
        public void Configure_DoesNotOverwriteNodeEnvIfItIsSpecified()
        {
            // Arrange
            const string nodeEnvVarName = "NODE_ENV";
            const string dummyNodeEnvValue = "dummyEnvironmentState";
            var dummyEnvironmentVariables = new Dictionary<string, string> { { nodeEnvVarName, dummyNodeEnvValue } };
            Mock<IHostEnvironment> mockHostingEnvironment = _mockRepository.Create<IHostEnvironment>();
            mockHostingEnvironment.Setup(h => h.ContentRootPath).Returns("dummyContentRootPath");
            Mock<IServiceProvider> mockServiceProvider = _mockRepository.Create<IServiceProvider>();
            mockServiceProvider.Setup(s => s.GetService(typeof(IHostEnvironment))).Returns(mockHostingEnvironment.Object); // Called by the extension method GetService<T>
            Mock<IServiceScope> mockServiceScope = _mockRepository.Create<IServiceScope>();
            mockServiceScope.Setup(s => s.ServiceProvider).Returns(mockServiceProvider.Object);
            Mock<IServiceScopeFactory> mockServiceScopeFactory = _mockRepository.Create<IServiceScopeFactory>();
            mockServiceScopeFactory.Setup(s => s.CreateScope()).Returns(mockServiceScope.Object);
            var dummyOptions = new NodeJSProcessOptions
            {
                ProjectPath = null!, // Project path should not be null, but user can still specify null. We're testing the situation where nullable reference type warning are ignored.
                EnvironmentVariables = dummyEnvironmentVariables
            };
            ConfigureNodeJSProcessOptions testSubject = CreateConfigureNodeJSProcessOptions(mockServiceScopeFactory.Object);

            // Act
            testSubject.Configure(dummyOptions);

            // Assert
            _mockRepository.VerifyAll();
            Assert.Equal(dummyNodeEnvValue, dummyOptions.EnvironmentVariables[nodeEnvVarName]); // Unchanged
        }

        private ConfigureNodeJSProcessOptions CreateConfigureNodeJSProcessOptions(IServiceScopeFactory? serviceScopeFactory = null)
        {
            return new ConfigureNodeJSProcessOptions(serviceScopeFactory ?? _mockRepository.Create<IServiceScopeFactory>().Object);
        }
    }
}
