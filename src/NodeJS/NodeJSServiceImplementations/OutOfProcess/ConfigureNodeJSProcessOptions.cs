using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;

namespace Jering.Javascript.NodeJS
{
    /// <summary>
    /// A <see cref="IConfigureOptions{TOptions}"/> implementation for configuring the singleton <see cref="NodeJSProcessOptions"/> for an application.
    /// </summary>
    public class ConfigureNodeJSProcessOptions : IConfigureOptions<NodeJSProcessOptions>
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        /// <summary>
        /// Creates a <see cref="ConfigureNodeJSProcessOptions"/> instance.
        /// </summary>
        /// <param name="serviceScopeFactory"></param>
        public ConfigureNodeJSProcessOptions(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        /// <summary>
        /// Configures a <see cref="NodeJSProcessOptions"/> using data from the application's <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="options">The target <see cref="NodeJSProcessOptions"/> to configure.</param>
        public void Configure(NodeJSProcessOptions options)
        {
            // Create a scope to avoid leaking unintended singletons - https://wildermuth.com/2016/08/07/ASP-NET-Core-Dependency-Injection
            using (IServiceScope scope = _serviceScopeFactory.CreateScope())
            {
                IServiceProvider serviceProvider = scope.ServiceProvider;

                IHostingEnvironment hostingEnvironment = serviceProvider.GetService<IHostingEnvironment>();
                if (hostingEnvironment == null)
                {
                    return;
                }

                options.ProjectPath = hostingEnvironment.ContentRootPath;
                if (options.EnvironmentVariables == null)
                {
                    options.EnvironmentVariables = new Dictionary<string, string>();
                }
                options.EnvironmentVariables["NODE_ENV"] = hostingEnvironment.IsDevelopment() ? "development" : "production"; // De-facto standard values for Node
            }
        }
    }
}
