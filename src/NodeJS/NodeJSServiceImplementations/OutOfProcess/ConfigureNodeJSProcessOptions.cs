using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;

namespace Jering.JavascriptUtils.NodeJS
{
    // These configurations correspond to some of the configurations in the Microsoft.AspNetCore.NodeServices.NodeServiceOptions constructor.
    // The goal of providing these configurations is to make it easier to switch between this library and the NodeServices library.
    public class ConfigureNodeJSProcessOptions : IConfigureOptions<NodeJSProcessOptions>
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public ConfigureNodeJSProcessOptions(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        public void Configure(NodeJSProcessOptions options)
        {
            // Create a scope to avoid leaking unintended singletons - https://wildermuth.com/2016/08/07/ASP-NET-Core-Dependency-Injection
            using (IServiceScope scope = _serviceScopeFactory.CreateScope())
            {
                IServiceProvider serviceProvider = scope.ServiceProvider;

                IHostingEnvironment hostingEnvironment = serviceProvider.GetService<IHostingEnvironment>();
                if (hostingEnvironment != null)
                {
                    options.ProjectPath = hostingEnvironment.ContentRootPath;
                    if(options.EnvironmentVariables == null)
                    {
                        options.EnvironmentVariables = new Dictionary<string, string>();
                    }
                    options.EnvironmentVariables["NODE_ENV"] = hostingEnvironment.IsDevelopment() ? "development" : "production"; // De-facto standard values for Node
                }
            }
        }
    }
}
