using Jering.JavascriptUtils.Node.HostingModels;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for setting up NodeServices in an <see cref="IServiceCollection" />.
    /// </summary>
    public static class NodeServiceCollectionExtensions
    {
        /// <summary>
        /// Adds NodeServices support to the <paramref name="services"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        public static void AddNodeServices(this IServiceCollection services)
        {
            services.AddSingleton<INodeHost, HttpNodeHost>();
        }
    }
}
