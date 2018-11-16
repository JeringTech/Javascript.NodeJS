using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using System.Collections.Generic;
using Xunit;

namespace Jering.Javascript.NodeJS.Tests
{
    public class ConfigureNodeJSProcessOptionsUnitTests
    {
        private readonly MockRepository _mockRepository = new MockRepository(MockBehavior.Default);

        [Fact]
        public void Configure_DoesNothingIfThereIsNoHostingEnvironmentService()
        {
            // Arrange
            const string dummyProjectPath = "dummyProjectPath";
            Mock<IServiceProvider> mockServiceProvider = _mockRepository.Create<IServiceProvider>();
            mockServiceProvider.Setup(s => s.GetService(typeof(IHostingEnvironment))).Returns(null); // Called by the extension method GetService<T>
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
        [MemberData(nameof(Configure_ConfiguresOptionsUsingHostingEnvironmentState_Data))]
        public void Configure_ConfiguresOptionsUsingHostingEnvironmentState(SerializableWrapper<Dictionary<string, string>> dummyExistingEnvironmentVariablesWrapper,
            string dummyEnvironmentName,
            SerializableWrapper<Dictionary<string, string>> expectedEnvironmentVariablesWrapper)
        {
            // Arrange
            const string dummyContentRootPath = "dummyContentRootPath";
            Mock<IHostingEnvironment> mockHostingEnvironment = _mockRepository.Create<IHostingEnvironment>();
            mockHostingEnvironment.Setup(h => h.ContentRootPath).Returns(dummyContentRootPath);
            mockHostingEnvironment.Setup(h => h.EnvironmentName).Returns(dummyEnvironmentName); // Called by IsDevelopment()
            Mock<IServiceProvider> mockServiceProvider = _mockRepository.Create<IServiceProvider>();
            mockServiceProvider.Setup(s => s.GetService(typeof(IHostingEnvironment))).Returns(mockHostingEnvironment.Object); // Called by the extension method GetService<T>
            Mock<IServiceScope> mockServiceScope = _mockRepository.Create<IServiceScope>();
            mockServiceScope.Setup(s => s.ServiceProvider).Returns(mockServiceProvider.Object);
            Mock<IServiceScopeFactory> mockServiceScopeFactory = _mockRepository.Create<IServiceScopeFactory>();
            mockServiceScopeFactory.Setup(s => s.CreateScope()).Returns(mockServiceScope.Object);
            var dummyOptions = new NodeJSProcessOptions { ProjectPath = dummyContentRootPath, EnvironmentVariables = dummyExistingEnvironmentVariablesWrapper?.Value };
            ConfigureNodeJSProcessOptions testSubject = CreateConfigureNodeJSProcessOptions(mockServiceScopeFactory.Object);

            // Act
            testSubject.Configure(dummyOptions);

            // Assert
            _mockRepository.VerifyAll();
            Assert.Equal(dummyContentRootPath, dummyOptions.ProjectPath);
            Assert.Equal(expectedEnvironmentVariablesWrapper.Value, dummyOptions.EnvironmentVariables);
        }

        public static IEnumerable<object[]> Configure_ConfiguresOptionsUsingHostingEnvironmentState_Data()
        {
            const string expectedProductionNodeEnvValue = "production";
            const string expectedDevelopmentNodeEnvValue = "development";
            const string expectedNodeEnvVarName = "NODE_ENV";
            const string dummyEnvVarName = "DUMMY_ENV_VAR";
            const string dummyEnvVarValue = "dummyEnvVarValue";

            return new object[][]
            {
                // Sets NODE_ENV to development if EnvironmentName is Development
                new object[]{
                    null,
                    EnvironmentName.Development,
                    new SerializableWrapper<Dictionary<string, string>>(
                        new Dictionary<string, string>
                        {
                            { expectedNodeEnvVarName, expectedDevelopmentNodeEnvValue }
                        }
                    )
                },
                // Sets NODE_ENV to production if EnvironmentName is Production
                new object[]{
                    null,
                    EnvironmentName.Production,
                    new SerializableWrapper<Dictionary<string, string>>(
                        new Dictionary<string, string>
                        {
                            { expectedNodeEnvVarName, expectedProductionNodeEnvValue }
                        }
                    )
                },
                // Defaults to "production"
                new object[]{
                    null,
                    EnvironmentName.Staging,
                    new SerializableWrapper<Dictionary<string, string>>(
                        new Dictionary<string, string>
                        {
                            { expectedNodeEnvVarName, expectedProductionNodeEnvValue }
                        }
                    )
                },
                // Keeps existing environment variables
                new object[]{
                    new SerializableWrapper<Dictionary<string, string>>(
                        new Dictionary<string, string> { { dummyEnvVarName, dummyEnvVarValue } }
                    ),
                    EnvironmentName.Development,
                    new SerializableWrapper<Dictionary<string, string>>(
                    new Dictionary<string, string>
                        {
                            { dummyEnvVarName, dummyEnvVarValue },
                            { expectedNodeEnvVarName, expectedDevelopmentNodeEnvValue }
                        }
                    )
                }
            };
        }

        private ConfigureNodeJSProcessOptions CreateConfigureNodeJSProcessOptions(IServiceScopeFactory serviceScopeFactory = null)
        {
            return new ConfigureNodeJSProcessOptions(serviceScopeFactory);
        }
    }
}
