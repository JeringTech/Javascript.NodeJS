using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.Text;

namespace Jering.Javascript.NodeJS
{
    /// <summary>
    /// The default <see cref="INodeJSProcessFactory"/> implementation.
    /// </summary>
    public class NodeJSProcessFactory : INodeJSProcessFactory
    {
        private readonly NodeJSProcessOptions _nodeJSProcessOptions;

        /// <summary>
        /// Creates a <see cref="NodeJSProcessFactory"/> instance.
        /// </summary>
        /// <param name="optionsAccessor"></param>
        public NodeJSProcessFactory(IOptions<NodeJSProcessOptions> optionsAccessor)
        {
            _nodeJSProcessOptions = optionsAccessor?.Value ?? new NodeJSProcessOptions();
        }

        /// <inheritdoc />
        public INodeJSProcess Create(string serverScript)
        {
            ProcessStartInfo startInfo = CreateStartInfo(serverScript);

            return new NodeJSProcess(CreateProcess(startInfo));
        }

        internal ProcessStartInfo CreateStartInfo(string nodeServerScript)
        {
            nodeServerScript = EscapeCommandLineArg(nodeServerScript);

            int currentProcessPid = Process.GetCurrentProcess().Id;
            var startInfo = new ProcessStartInfo("node")
            {
                Arguments = $"{_nodeJSProcessOptions.NodeAndV8Options} -e \"{nodeServerScript}\" -- --parentPid {currentProcessPid} --port {_nodeJSProcessOptions.Port}",
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = _nodeJSProcessOptions.ProjectPath
            };

            // Append environment Variables
            if (_nodeJSProcessOptions.EnvironmentVariables != null)
            {
                foreach (var envVarKey in _nodeJSProcessOptions.EnvironmentVariables.Keys)
                {
                    string envVarValue = _nodeJSProcessOptions.EnvironmentVariables[envVarKey];
                    if (envVarValue != null)
                    {
                        startInfo.Environment[envVarKey] = envVarValue;
                    }
                }
            }

            return startInfo;
        }

        internal Process CreateProcess(ProcessStartInfo startInfo)
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
                throw new InvalidOperationException(string.Format(Strings.InvalidOperationException_NodeJSProcessFactory_FailedToStartNodeProcess, Environment.GetEnvironmentVariable("PATH")), ex);
            }
        }

        internal string EscapeCommandLineArg(string arg)
        {
            var stringBuilder = new StringBuilder();
            int slashSequenceLength = 0;
            for(int i = 0; i < arg.Length; i++)
            {
                char currentChar = arg[i];

                if(currentChar == '\\')
                {
                    slashSequenceLength++;

                    // If the last character in the argument is \, it must be escaped, together with any \ that immediately preceed it.
                    // This prevents situations like: SomeExecutable.exe "SomeArg\", where the quote meant to demarcate the end of the
                    // argument gets escaped.
                    if(i == arg.Length - 1)
                    {
                        for (int j = 0; j < slashSequenceLength; j++)
                        {
                            stringBuilder.
                                Append('\\').
                                Append('\\');
                        }
                    }
                }
                else if(currentChar == '"')
                {
                    // Every \ or sequence of \ that preceed a " must be escaped.
                    for(int j = 0; j < slashSequenceLength; j++)
                    {
                        stringBuilder.
                            Append('\\').
                            Append('\\');
                    }
                    slashSequenceLength = 0;

                    stringBuilder.
                        Append('\\').
                        Append('"');
                }
                else
                {
                    for (int j = 0; j < slashSequenceLength; j++)
                    {
                        stringBuilder.Append('\\');
                    }
                    slashSequenceLength = 0;

                    stringBuilder.Append(currentChar);
                }
            }

            return stringBuilder.ToString();
        }
    }
}
