namespace Jering.JavascriptUtils.Node
{
    /// <summary>
    /// Describes options used to configure an <see cref="INodeServices"/> instance.
    /// </summary>
    public class OutOfProcessNodeServiceOptions
    {
        //internal const string TimeoutConfigPropertyName = nameof(InvocationTimeoutMS);
        //private const int DefaultInvocationTimeoutMilliseconds = 60 * 1000;
        //private const string LogCategoryName = "Jering.JavascriptUtils.Node";

        /// <summary>
        /// Creates a new instance of <see cref="OutOfProcessNodeServiceOptions"/>.
        /// </summary>
        /// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
        //public OutOfProcessNodeHostOptions()
        //{
        //    if (serviceProvider == null)
        //    {
        //        throw new ArgumentNullException(nameof(serviceProvider));
        //    }

        //    EnvironmentVariables = new Dictionary<string, string>();
        //    InvocationTimeoutMilliseconds = DefaultInvocationTimeoutMilliseconds;

        //    IHostingEnvironment hostEnv = serviceProvider.GetService<IHostingEnvironment>();
        //    if (hostEnv != null)
        //    {
        //        // In an ASP.NET environment, we can use the IHostingEnvironment data to auto-populate a few
        //        // things that you'd otherwise have to specify manually
        //        ProjectPath = hostEnv.ContentRootPath;
        //        EnvironmentVariables["NODE_ENV"] = hostEnv.IsDevelopment() ? "development" : "production"; // De-facto standard values for Node
        //    }
        //    else
        //    {
        //        ProjectPath = Directory.GetCurrentDirectory();
        //    }

        //    IApplicationLifetime applicationLifetime = serviceProvider.GetService<IApplicationLifetime>();
        //    if (applicationLifetime != null)
        //    {
        //        ApplicationStoppingToken = applicationLifetime.ApplicationStopping;
        //    }

        //    // If the DI system gives us a logger, use it. Otherwise, set up a default one.
        //    ILoggerFactory loggerFactory = serviceProvider.GetService<ILoggerFactory>();
        //    NodeInstanceOutputLogger = loggerFactory != null
        //        ? loggerFactory.CreateLogger(LogCategoryName)
        //        : new ConsoleLogger(LogCategoryName, null, false);

        //    // By default, we use this package's built-in out-of-process-via-HTTP hosting/transport
        //    this.UseHttpHosting();
        //}


        /// <summary>
        /// Specifies the maximum duration, in milliseconds, that your .NET code should wait for Node.js RPC calls to return.
        /// </summary>
        public int InvocationTimeoutMS { get; set; } = 1000;
    }
}