using System.Collections.Generic;

namespace Jering.JavascriptUtils.Node.NodeHosts
{
    public class NodeProcessOptions
    {
        /// <summary>
        /// If set, overrides the path to the root of your application. This path is used when locating Node.js modules relative to your project.
        /// </summary>
        public string ProjectPath { get; set; }


        public string CommandLineOptions { get; set; }

        public int Port { get; set; }

        /// <summary>
        /// If set, starts the Node.js instance with the specified environment variables.
        /// </summary>
        public IDictionary<string, string> EnvironmentVariables { get; set; }
    }
}
