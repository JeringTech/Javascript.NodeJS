using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jering.JavascriptUtils.NodeJS
{
    /// <summary>
    /// Extension methods for setting up NodeJS in an <see cref="IServiceCollection" />.
    /// </summary>
    public static class NodeJSServiceCollectionExtensions
    {
        /// <summary>
        /// Adds NodeJS services to the an <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The target <see cref="IServiceCollection"/>.</param>
        public static void AddNodeJS(this IServiceCollection services)
        {
            services.AddLogging(loggingBuilder => loggingBuilder.AddConsole());
            services.AddOptions();
            services.AddSingleton<IConfigureOptions<NodeJSProcessOptions>, ConfigureNodeJSProcessOptions>();

            services.AddSingleton<IHttpContentFactory, InvocationContentFactory>();
            services.AddSingleton<IJsonService, JsonService>();
            services.AddSingleton<IEmbeddedResourcesService, EmbeddedResourcesService>();
            services.AddSingleton<IHttpClientService, HttpClientService>();
            services.AddSingleton<INodeJSProcessFactory, NodeJSProcessFactory>();
            services.AddSingleton<INodeJSService, HttpNodeJSService>();
        }
    }
}
