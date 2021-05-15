using Microsoft.Extensions.DependencyInjection;
using System;

namespace Jering.Javascript.NodeJS
{
    /// <summary>
    /// A class that provides static access to an instance of the default <see cref="INodeJSService"/> implementation's public methods.
    /// </summary>
    public static partial class StaticNodeJSService
    {
        private static volatile ServiceProvider? _serviceProvider;
        private static volatile IServiceCollection? _services;
        private static volatile INodeJSService? _nodeJSService;
        private static readonly object _createLock = new();

        private static INodeJSService GetOrCreateNodeJSService()
        {
            if (_nodeJSService == null || _services != null)
            {
                lock (_createLock)
                {
                    if (_nodeJSService == null || _services != null)
                    {
                        // Dispose of service provider
                        _serviceProvider?.Dispose();

                        // Create new service provider
                        _services ??= new ServiceCollection().AddNodeJS();
                        _serviceProvider = _services.BuildServiceProvider();
                        _nodeJSService = _serviceProvider.GetRequiredService<INodeJSService>();

                        _services = null;
                    }
                }
            }

            // NodeJSService already exists and no configuration pending
            return _nodeJSService;
        }

        /// <summary>
        /// <para>Disposes the underlying <see cref="IServiceProvider"/> used to resolve <see cref="INodeJSService"/>.</para>
        /// <para>This method is not thread safe.</para>
        /// </summary>
        public static void DisposeServiceProvider()
        {
            _serviceProvider?.Dispose();
            _serviceProvider = null;
            _nodeJSService = null;
        }

        /// <summary>
        /// <para>Configures options.</para>
        /// <para>This method is not thread safe.</para>
        /// </summary>
        /// <typeparam name="T">The type of options to configure.</typeparam>
        /// <param name="configureOptions">The action that configures the options.</param>
        public static void Configure<T>(Action<T> configureOptions) where T : class
        {
            _services = (_services ?? new ServiceCollection().AddNodeJS()).Configure(configureOptions);
        }

        /// <summary>
        /// <para>Sets the <see cref="ServiceCollection"/> used to create an <see cref="INodeJSService"/>.</para>
        /// <para>This method is not thread safe.</para>
        /// </summary>
        /// <param name="services">
        /// <para>The <see cref="ServiceCollection"/> used to create an <see cref="INodeJSService"/>.</para>
        /// <para>If this value doesn't contain a valid service for <see cref="INodeJSService"/>, <see cref="InvalidOperationException"/>s are
        /// thrown on subsequent invocations.</para>
        /// </param>
        public static void SetServices(ServiceCollection services)
        {
            _services = services;
        }
    }
}
