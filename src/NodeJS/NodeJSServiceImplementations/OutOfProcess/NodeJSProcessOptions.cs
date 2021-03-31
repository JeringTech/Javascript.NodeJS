using Microsoft.AspNetCore.Hosting;
using System.Collections.Generic;
using System.IO;

namespace Jering.Javascript.NodeJS
{
    /// <summary>
    /// Options for a NodeJS process.
    /// </summary>
    public class NodeJSProcessOptions
    {
        /// <summary>
        /// <para>The base path for resolving paths of NodeJS modules on disk.</para>
        /// <para>If this value is <c>null</c>, whitespace or an empty string and the application is an ASP.NET Core application, 
        /// project path is <see cref="IHostingEnvironment.ContentRootPath"/>.</para>
        /// </summary>
        public string ProjectPath { get; set; } = Directory.GetCurrentDirectory();

        /// <summary>
        /// <para>The value used to locate the NodeJS executable.</para>
        /// <para>This value may be an absolute path, a relative path, or a file name.</para>
        /// <para>If this value is a relative path, the executable's path is resolved relative to <see cref="Directory.GetCurrentDirectory"/>.</para>
        /// <para>If this value is a file name, the executable's path is resolved using the path environment variable.</para>
        /// <para>If this value is <c>null</c>, whitespace or an empty string, it is overridden with the file name "node".</para>
        /// <para>Defaults to <c>null</c>.</para>
        /// </summary>
        public string ExecutablePath { get; set; }

        /// <summary>
        /// <para>NodeJS and V8 options in the form [NodeJS options] [V8 options].</para>
        /// <para>The full list of NodeJS options can be found here: https://nodejs.org/api/cli.html#cli_options.</para>
        /// </summary>
        public string NodeAndV8Options { get; set; }

        /// <summary>
        /// <para>The port that the server running on NodeJS will listen on.</para>
        /// <para>If set to 0, the OS will choose the port. This value defaults to 0.</para>
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// <para>The environment variables for the NodeJS process.</para>
        /// <para>The full list of NodeJS environment variables can be found here: https://nodejs.org/api/cli.html#cli_environment_variables.</para>
        /// <para>If this value doesn't contain an element with key "NODE_ENV" and the application is an ASP.NET Core application,
        /// an element with key "NODE_ENV" is added. The added element's value is "development" if <see cref="IHostingEnvironment.EnvironmentName"/> is <see cref="EnvironmentName.Development"/>,
        /// and "production" otherwise.</para>
        /// </summary>
        public IDictionary<string, string> EnvironmentVariables { get; set; } = new Dictionary<string, string>();
    }
}
