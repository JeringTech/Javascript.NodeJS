using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using Xunit;

namespace Jering.JavascriptUtils.Node.Tests
{
    public class HttpNodeServiceIntegrationTests : IDisposable
    {
        private ServiceProvider _serviceProvider;

        [Fact]
        public async void InvokeFromStringAsync_InvokesJavascript()
        {
            // Arrange
            HttpNodeService httpNodeService = CreateHttpNodeService();

            // Act
            string result = await httpNodeService.InvokeFromStringAsync<string>("module.exports = () => 'success';").ConfigureAwait(false);
        }

        private HttpNodeService CreateHttpNodeService()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddNode(); // Default INodeService is HttpNodeService
            services.Configure<NodeProcessOptions>(options => options.NodeAndV8Options = "--inspect-brk");
            services.Configure<OutOfProcessNodeServiceOptions>(options => options.InvocationTimeoutMS = -1);
            _serviceProvider = services.BuildServiceProvider();

            ILoggerFactory loggerFactory = _serviceProvider.GetRequiredService<ILoggerFactory>();
            loggerFactory.AddConsole();

            return _serviceProvider.GetRequiredService<INodeService>() as HttpNodeService;
        }

        public void Dispose()
        {
            _serviceProvider.Dispose();
        }
    }
}
