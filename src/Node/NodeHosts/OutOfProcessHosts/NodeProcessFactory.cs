using Jering.JavascriptUtils.Node.NodeHosts;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.IO;

namespace Jering.JavascriptUtils.Node.Node.OutOfProcessHosts
{
    public abstract class NodeProcessFactory : INodeProcessFactory
    {
        private readonly NodeProcessOptions _nodeProcessOptions;

        protected NodeProcessFactory(IOptions<NodeProcessOptions> optionsAccessor)
        {
            _nodeProcessOptions = optionsAccessor.Value;
        }

        public Process Create(string nodeServerScript)
        {
            ProcessStartInfo startInfo = CreateNodeProcessStartInfo(nodeServerScript);

            return CreateAndStartNodeProcess(startInfo);
        }

        protected virtual ProcessStartInfo CreateNodeProcessStartInfo(string nodeServerScript)
        {
            // This method is virtual, as it provides a way to override the NODE_PATH or the path to node.exe
            int currentProcessPid = Process.GetCurrentProcess().Id;
            var startInfo = new ProcessStartInfo("node")
            {
                Arguments = $"{_nodeProcessOptions.NodeAndV8Options} -e \"{nodeServerScript}\" -- --parentPid {currentProcessPid} --port {_nodeProcessOptions.Port}",
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = _nodeProcessOptions.ProjectPath
            };

            // Append environment Variables
            if (_nodeProcessOptions.EnvironmentVariables != null)
            {
                foreach (var envVarKey in _nodeProcessOptions.EnvironmentVariables.Keys)
                {
                    string envVarValue = _nodeProcessOptions.EnvironmentVariables[envVarKey];
                    if (envVarValue != null)
                    {
                        startInfo.Environment[envVarKey] = envVarValue;
                    }
                }
            }

            // Append projectPath to NODE_PATH so it can locate node_modules
            string existingNodePath = Environment.GetEnvironmentVariable("NODE_PATH") ?? string.Empty;
            if (existingNodePath != string.Empty)
            {
                existingNodePath += Path.PathSeparator;
            }

            startInfo.Environment["NODE_PATH"] = existingNodePath + Path.Combine(_nodeProcessOptions.ProjectPath, "node_modules");

            return startInfo;
        }

        protected virtual Process CreateAndStartNodeProcess(ProcessStartInfo startInfo)
        {
            try
            {
                Process process = Process.Start(startInfo);

                // On Mac at least, a killed child process is left open as a zombie until the parent
                // captures its exit code. We don't need the exit code for this process, and don't want
                // to use process.WaitForExit() explicitly (we'd have to block the thread until it really
                // has exited), but we don't want to leave zombies lying around either. It's sufficient
                // to use process.EnableRaisingEvents so that .NET will grab the exit code and let the
                // zombie be cleaned away without having to block our thread.
                process.EnableRaisingEvents = true;

                return process;
            }
            catch (Exception ex)
            {
                string message = "Failed to start Node process. To resolve this:.\n\n"
                            + "[1] Ensure that Node.js is installed and can be found in one of the PATH directories.\n"
                            + $"    Current PATH enviroment variable is: { Environment.GetEnvironmentVariable("PATH") }\n"
                            + "    Make sure the Node executable is in one of those directories, or update your PATH.\n\n"
                            + "[2] See the InnerException for further details of the cause.";
                throw new InvalidOperationException(message, ex);
            }
        }
    }
}
