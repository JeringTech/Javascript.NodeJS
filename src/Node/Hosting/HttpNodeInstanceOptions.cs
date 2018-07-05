using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Jering.JavascriptUtils.Node.HostingModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging.Console;

namespace Jering.JavascriptUtils.Node
{
    /// <summary>
    /// Describes options used to configure an <see cref="INodeServices"/> instance.
    /// </summary>
    public class HttpNodeInstanceOptions
    {
        internal const string TimeoutConfigPropertyName = nameof(InvocationTimeoutMilliseconds);
        private const int DefaultInvocationTimeoutMilliseconds = 60 * 1000;
        private const string LogCategoryName = "Jering.JavascriptUtils.Node";
        private static readonly string[] DefaultWatchFileExtensions = { ".js", ".jsx", ".ts", ".tsx", ".json", ".html" };

        /// <summary>
        /// Creates a new instance of <see cref="HttpNodeInstanceOptions"/>.
        /// </summary>
        /// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
        public HttpNodeInstanceOptions()
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            EnvironmentVariables = new Dictionary<string, string>();
            InvocationTimeoutMilliseconds = DefaultInvocationTimeoutMilliseconds;
            WatchFileExtensions = (string[])DefaultWatchFileExtensions.Clone();

            IHostingEnvironment hostEnv = serviceProvider.GetService<IHostingEnvironment>();
            if (hostEnv != null)
            {
                // In an ASP.NET environment, we can use the IHostingEnvironment data to auto-populate a few
                // things that you'd otherwise have to specify manually
                ProjectPath = hostEnv.ContentRootPath;
                EnvironmentVariables["NODE_ENV"] = hostEnv.IsDevelopment() ? "development" : "production"; // De-facto standard values for Node
            }
            else
            {
                ProjectPath = Directory.GetCurrentDirectory();
            }

            IApplicationLifetime applicationLifetime = serviceProvider.GetService<IApplicationLifetime>();
            if (applicationLifetime != null)
            {
                ApplicationStoppingToken = applicationLifetime.ApplicationStopping;
            }

            // If the DI system gives us a logger, use it. Otherwise, set up a default one.
            ILoggerFactory loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            NodeInstanceOutputLogger = loggerFactory != null
                ? loggerFactory.CreateLogger(LogCategoryName)
                : new ConsoleLogger(LogCategoryName, null, false);

            // By default, we use this package's built-in out-of-process-via-HTTP hosting/transport
            this.UseHttpHosting();
        }

        /// <summary>
        /// Specifies how to construct Node.js instances. An <see cref="INodeInstance"/> encapsulates all details about
        /// how Node.js instances are launched and communicated with. A new <see cref="INodeInstance"/> will be created
        /// automatically if the previous instance has terminated (e.g., because a source file changed).
        /// </summary>
        public Func<INodeInstance> NodeInstanceFactory { get; set; }

        /// <summary>
        /// If set, overrides the path to the root of your application. This path is used when locating Node.js modules relative to your project.
        /// </summary>
        public string ProjectPath { get; set; }

        /// <summary>
        /// If set, the Node.js instance should restart when any matching file on disk within your project changes.
        /// </summary>
        public string[] WatchFileExtensions { get; set; }

        /// <summary>
        /// The Node.js instance's stdout/stderr will be redirected to this <see cref="ILogger"/>.
        /// </summary>
        public ILogger NodeInstanceOutputLogger { get; set; }

        /// <summary>
        /// If set, starts the Node.js instance with the specified environment variables.
        /// </summary>
        public IDictionary<string, string> EnvironmentVariables { get; set; }

        /// <summary>
        /// Specifies the maximum duration, in milliseconds, that your .NET code should wait for Node.js RPC calls to return.
        /// </summary>
        public int InvocationTimeoutMilliseconds { get; set; }

        /// <summary>
        /// A token that indicates when the host application is stopping.
        /// </summary>
        public CancellationToken ApplicationStoppingToken { get; set; }
    }
}