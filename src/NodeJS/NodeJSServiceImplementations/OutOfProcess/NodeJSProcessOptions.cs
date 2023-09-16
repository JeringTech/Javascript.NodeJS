using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.IO;

namespace Jering.Javascript.NodeJS
{
    /// <summary>
    /// Options for a NodeJS process.
    /// </summary>
    public class NodeJSProcessOptions
    {
        /// <summary>The base path for resolving NodeJS module paths.</summary>
        /// <remarks>
        /// <para>If this value is <c>null</c>, whitespace or an empty string and the application is an ASP.NET Core application, 
        /// project path is <see cref="IHostEnvironment.ContentRootPath"/>.</para>
        /// </remarks>
        public string ProjectPath { get; set; } = Directory.GetCurrentDirectory();

        /// <summary>The value used to locate the NodeJS executable.</summary>
        /// <remarks>
        /// <para>This value may be an absolute path, a relative path, or a file name.</para>
        /// <para>If this value is a relative path, the executable's path is resolved relative to <see cref="Directory.GetCurrentDirectory"/>.</para>
        /// <para>If this value is a file name, the executable's path is resolved using the path environment variable.</para>
        /// <para>If this value is <c>null</c>, whitespace or an empty string, it is overridden with the file name "node".</para>
        /// <para>Defaults to <c>null</c>.</para>
        /// </remarks>
        public string? ExecutablePath { get; set; }

        /// <summary>NodeJS and V8 options in the form &lt;NodeJS options&gt; &lt;V8 options&gt;.</summary>
        /// <remarks>
        /// <para>You can find the full list of NodeJS options <a href="https://nodejs.org/api/cli.html#cli_options">here</a>.</para>
        /// </remarks>
        public string? NodeAndV8Options { get; set; }

        /// <summary>The NodeJS server will listen on this port.</summary>
        /// <remarks>
        /// <para>If this value is 0, the OS will choose the port.</para>
        /// <para>Defaults to 0.</para>
        /// </remarks>
        public int Port { get; set; }

        /// <summary>The NodeJS process's environment variables.</summary>
        /// <remarks>
        /// <para>You can configure NodeJS by specifying environment variables for it. Find the full list of environment variables <a href="https://nodejs.org/api/cli.html#cli_environment_variables">here</a>.</para>
        /// <para>If this value doesn't contain an element with key "NODE_ENV" and the application is an ASP.NET Core application,
        /// an element with key "NODE_ENV" is added. The added element's value is "development" if <see cref="IHostEnvironment.EnvironmentName"/> is <see cref="Environments.Development"/>,
        /// and "production" otherwise.</para>
        /// </remarks>
        public IDictionary<string, string> EnvironmentVariables { get; set; } = new Dictionary<string, string>();
    }
}
