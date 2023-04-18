using Microsoft.Extensions.DependencyInjection;
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
        public static IServiceCollection AddNodeJS(this IServiceCollection services)
        {
            // Third party services
            services.
                AddLogging().
                AddOptions();

            // Services defined in this project
            services.
                AddSingleton<IConfigureOptions<NodeJSProcessOptions>, ConfigureNodeJSProcessOptions>().
                AddSingleton<IHttpContentFactory, InvocationContentFactory>().
                AddSingleton<IEmbeddedResourcesService, EmbeddedResourcesService>().
                AddSingleton<INodeJSProcessFactory, NodeJSProcessFactory>().
                AddSingleton(typeof(INodeJSService), INodeJSServiceFactory).
                AddSingleton<IJsonService, JsonService>().
                AddSingleton<IEnvironmentService, EnvironmentService>().
                AddSingleton<IFileWatcherService, FileWatcherService>().
                AddSingleton<ITaskService, TaskService>().
                AddTransient<IBlockDrainerService, BlockDrainerService>();
#if NETCOREAPP3_1
            // If not called, framework forces HTTP/1.1 so long as origin isn't https
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
#endif
            services.
                AddHttpClient<IHttpClientService, HttpClientService>().
                SetHandlerLifetime(Timeout.InfiniteTimeSpan); // No DNS changes, handler lifetime can be infinite

            return services;
        }

        internal static INodeJSService INodeJSServiceFactory(IServiceProvider serviceProvider)
        {
            OutOfProcessNodeJSServiceOptions outOfProcessNodeJSServiceOptions = serviceProvider.GetRequiredService<IOptions<OutOfProcessNodeJSServiceOptions>>().Value;
            IEnvironmentService environmentService = serviceProvider.GetRequiredService<IEnvironmentService>();

            int concurrencyDegree = outOfProcessNodeJSServiceOptions.ConcurrencyDegree;
            int processorCount = environmentService.ProcessorCount; // TODO to be safe we should ensure that this is >= 1

            if (outOfProcessNodeJSServiceOptions.Concurrency == Concurrency.None ||
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
