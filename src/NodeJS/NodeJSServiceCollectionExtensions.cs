using Jering.IocServices.System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System;
using System.Collections.ObjectModel;
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
            services.TryAddSingleton(typeof(IHttpClientService), IHttpClientServiceFactory);

            // Services defined in this project
            services.AddSingleton<IConfigureOptions<NodeJSProcessOptions>, ConfigureNodeJSProcessOptions>();
            services.AddSingleton<IHttpContentFactory, InvocationContentFactory>();
            services.AddSingleton<IEmbeddedResourcesService, EmbeddedResourcesService>();
            services.AddSingleton<INodeJSProcessFactory, NodeJSProcessFactory>();
            services.AddSingleton(typeof(INodeJSService), INodeJSServiceFactory);
            services.AddSingleton<IJsonService, JsonService>();
            services.AddSingleton<IEnvironmentService, EnvironmentService>();
        }

        internal static IHttpClientService IHttpClientServiceFactory(IServiceProvider serviceProvider)
        {
            OutOfProcessNodeJSServiceOptions outOfProcessNodeJSServiceOptions = serviceProvider.GetRequiredService<IOptions<OutOfProcessNodeJSServiceOptions>>().Value;

            return new HttpClientService
            {
                Timeout = outOfProcessNodeJSServiceOptions.TimeoutMS == -1 ? Timeout.InfiniteTimeSpan : TimeSpan.FromMilliseconds(outOfProcessNodeJSServiceOptions.TimeoutMS + 1000)
            };
        }

        internal static INodeJSService INodeJSServiceFactory(IServiceProvider serviceProvider)
        {
            OutOfProcessNodeJSServiceOptions outOfProcessNodeJSServiceOptions = serviceProvider.GetRequiredService<IOptions<OutOfProcessNodeJSServiceOptions>>().Value;
            IEnvironmentService environmentService = serviceProvider.GetRequiredService<IEnvironmentService>();

            int concurrencyDegree = outOfProcessNodeJSServiceOptions.ConcurrencyDegree;
            int processorCount = environmentService.ProcessorCount; // TODO to be safe we should ensure that this is >= 1

            if(outOfProcessNodeJSServiceOptions.Concurrency == Concurrency.None ||
                concurrencyDegree == 1 || // MultiProcess mode but only 1 process
                concurrencyDegree <= 0 && processorCount == 1) // Machine has only 1 logical processor
            {
                return ActivatorUtilities.CreateInstance<HttpNodeJSService>(serviceProvider);
            }
            else
            {
                if (concurrencyDegree <= 0)
                {
                    concurrencyDegree = processorCount;
                }

                var httpNodeJSServices = new HttpNodeJSService[concurrencyDegree];
                for (int i = 0; i < concurrencyDegree; i++)
                {
                    httpNodeJSServices[i] = ActivatorUtilities.CreateInstance<HttpNodeJSService>(serviceProvider);
                }

                return new HttpNodeJSPoolService(new ReadOnlyCollection<HttpNodeJSService>(httpNodeJSServices));
            }
        }
    }
}
