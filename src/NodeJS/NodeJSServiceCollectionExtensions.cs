using Jering.IocServices.System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System;
using System.Threading;

namespace Jering.Javascript.NodeJS
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
            // Third party services
            services.AddLogging();
            services.AddOptions();
            services.TryAddSingleton(typeof(IHttpClientService), serviceProvider =>
            {
                OutOfProcessNodeJSServiceOptions outOfProcessNodeJSServiceOptions = serviceProvider.GetRequiredService<IOptions<OutOfProcessNodeJSServiceOptions>>().Value;

                return new HttpClientService
                {
                    Timeout = outOfProcessNodeJSServiceOptions.TimeoutMS == -1 ? Timeout.InfiniteTimeSpan : TimeSpan.FromMilliseconds(outOfProcessNodeJSServiceOptions.TimeoutMS + 1000)
                };
            });

            // Services defined in this project
            services.AddSingleton<IConfigureOptions<NodeJSProcessOptions>, ConfigureNodeJSProcessOptions>();
            services.AddSingleton<IHttpContentFactory, InvocationContentFactory>();
            services.AddSingleton<IJsonService, JsonService>();
            services.AddSingleton<IEmbeddedResourcesService, EmbeddedResourcesService>();
            services.AddSingleton<INodeJSProcessFactory, NodeJSProcessFactory>();
            services.AddSingleton<INodeJSService, HttpNodeJSService>();
        }
    }
}
