using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Jering.JavascriptUtils.NodeJS.Tests
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
            ServiceProvider serviceProvider = services.BuildServiceProvider();
            INodeJSService result = serviceProvider.GetRequiredService<INodeJSService>(); // As long as this doesn't throw, the dependency graph is valid
        }
    }
}
