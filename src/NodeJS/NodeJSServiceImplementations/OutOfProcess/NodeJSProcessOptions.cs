using Microsoft.AspNetCore.Hosting;
using System.Collections.Generic;
using System.IO;

namespace Jering.JavascriptUtils.NodeJS
{
    /// <summary>
    /// Options for a NodeJS process.
    /// </summary>
    public class NodeJSProcessOptions
    {
        /// <summary>
        /// <para>The path used for resolving NodeJS modules.</para>
        /// <para>If the application is an ASP.NET Core application, this value defaults to <see cref="IHostingEnvironment.ContentRootPath"/>.
        /// Otherwise, it defaults to the current working directory.</para>
        /// </summary>
        public string ProjectPath { get; set; } = Directory.GetCurrentDirectory();

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
        /// <para>The environment variables to be set for the NodeJS process.</para>
        /// <para>The full list of NodeJS environment variables can be found here: https://nodejs.org/api/cli.html#cli_environment_variables.</para>
        /// </summary>
        public IDictionary<string, string> EnvironmentVariables { get; set; }
    }
}
