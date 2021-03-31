using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Jering.Javascript.NodeJS
{
    /// <summary>
    /// An <see cref="IConfigureOptions{TOptions}"/> implementation for configuring <see cref="NodeJSProcessOptions"/>.
    /// </summary>
    public class ConfigureNodeJSProcessOptions : IConfigureOptions<NodeJSProcessOptions>
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        /// <summary>
        /// Creates a <see cref="ConfigureNodeJSProcessOptions"/>.
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
            // Set executable path if unspecified
            if (string.IsNullOrWhiteSpace(options.ExecutablePath))
            {
                options.ExecutablePath = "node";
            }

            // Check whether project path already specified
            bool projectPathSpecified = !string.IsNullOrWhiteSpace(options.ProjectPath);

            // Check whether NODE_ENV already specified
            bool nodeEnvSpecified;
            if (options.EnvironmentVariables == null)
            {
                options.EnvironmentVariables = new Dictionary<string, string>();
                nodeEnvSpecified = false;
            }
            else
            {
                nodeEnvSpecified = options.EnvironmentVariables.ContainsKey("NODE_ENV");
            }

            // Return if both project path and NODE_ENV already specified
            if (projectPathSpecified && nodeEnvSpecified)
            {
                return;
            }

            // Create a scope to avoid leaking unintended singletons - https://wildermuth.com/2016/08/07/ASP-NET-Core-Dependency-Injection
            using IServiceScope scope = _serviceScopeFactory.CreateScope();
            IServiceProvider serviceProvider = scope.ServiceProvider;

            IHostingEnvironment hostingEnvironment = serviceProvider.GetService<IHostingEnvironment>();
            if (hostingEnvironment == null)
            {
                return;
            }

            if (!projectPathSpecified)
            {
                options.ProjectPath = hostingEnvironment.ContentRootPath;
            }

            if (!nodeEnvSpecified)
            {
                options.EnvironmentVariables["NODE_ENV"] = hostingEnvironment.IsDevelopment() ? "development" : "production"; // De-facto standard values for Node
            }
        }
    }
}
