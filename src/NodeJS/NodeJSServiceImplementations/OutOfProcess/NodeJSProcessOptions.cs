using System.Collections.Generic;
using System.IO;

namespace Jering.JavascriptUtils.NodeJS
{
    // TODO make configurable using serviceprovider
    // https://andrewlock.net/access-services-inside-options-and-startup-using-configureoptions/
    public class NodeJSProcessOptions
    {
        /// <summary>
        /// The path is used when locating Node.js modules. Defaults to the directory of the entry assembly for you application.
        /// </summary>
        public string ProjectPath { get; set; } = Directory.GetCurrentDirectory();

        /// <summary>
        /// <para>Node.js and V8 options in the form [Node.js options] [V8 options]. </para>
        /// <para>The full list of Node.js options can be found here: https://nodejs.org/api/cli.html#cli_options.</para>
        /// </summary>
        public string NodeAndV8Options { get; set; }

        /// <summary>
        /// The port that the server running on Node.js will listen on. If set to 0, the OS will choose the port. Defaults to 0.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// <para>If set, starts the Node.js instance with the specified environment variables.</para>
        /// <para>The full list of Node.js environment variables can be found here: https://nodejs.org/api/cli.html#cli_environment_variables.</para>
        /// </summary>
        public IDictionary<string, string> EnvironmentVariables { get; set; }
    }
}
