using System.Collections.Generic;
using System.IO;

namespace Jering.JavascriptUtils.Node
{
    public class NodeJSProcessOptions
    {
        /// <summary>
        /// If set, overrides the path to the root of your application. This path is used when locating Node.js modules relative to your project.
        /// </summary>
        public string ProjectPath { get; set; } = Directory.GetCurrentDirectory();

        /// <summary>
        /// Node.js and V8 options in the form [Node.js options] [V8 options]. 
        /// The full list of Node.js options can be found here: https://nodejs.org/api/cli.html#cli_options
        /// </summary>
        public string NodeAndV8Options { get; set; }

        /// <summary>
        /// The port that the server running on Node.js will listen on. If set to 0, the OS will choose the port.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// If set, starts the Node.js instance with the specified environment variables.
        /// </summary>
        public IDictionary<string, string> EnvironmentVariables { get; set; }
    }
}
