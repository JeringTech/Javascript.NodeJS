using Microsoft.Extensions.DependencyInjection;
using System;
using Xunit;

namespace Jering.Javascript.NodeJS.Tests
{
    public class NodeJSServiceCollectionExtensionsUnitTests
    {
        [Fact]
        public void AddNodeJS_AddsServices()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddNodeJS();

            // Assert
            IServiceProvider serviceProvider = services.BuildServiceProvider();
            INodeJSService _ = serviceProvider.GetRequiredService<INodeJSService>(); // As long as this doesn't throw, the dependency graph is valid
        }
    }
}
