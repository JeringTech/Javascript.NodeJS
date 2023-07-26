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
        /// Creates a <see cref="NodeJSProcessFactory"/>.
        /// </summary>
        /// <param name="optionsAccessor">The <see cref="NodeJSProcessOptions"/> accessor.</param>
        public NodeJSProcessFactory(IOptions<NodeJSProcessOptions> optionsAccessor)
        {
            _nodeJSProcessOptions = optionsAccessor.Value;
        }

        /// <inheritdoc />
        public INodeJSProcess Create(string serverScript)
        {
            ProcessStartInfo startInfo = CreateStartInfo(serverScript);

            return new NodeJSProcess(CreateProcess(startInfo));
        }

        /// <inheritdoc />
        public INodeJSProcess Create(string serverScript, EventHandler exitedEventHandler)
        {
            ProcessStartInfo startInfo = CreateStartInfo(serverScript);

            return new NodeJSProcess(CreateProcess(startInfo, exitedEventHandler));
        }

        internal ProcessStartInfo CreateStartInfo(string nodeServerScript)
        {
            nodeServerScript = EscapeCommandLineArg(nodeServerScript); // TODO can we escape before embedding? Would avoid an allocation every time we start a NodeJS process.

#if NET5_0_OR_GREATER
            int currentProcessPid = Environment.ProcessId;
#else
            int currentProcessPid = Process.GetCurrentProcess().Id;
#endif
            var startInfo = new ProcessStartInfo(_nodeJSProcessOptions.ExecutablePath!) // ConfigureNodeJSProcessOptions sets ExecutablePath to "node" if user specified value is null, whitespace or an empty string
            {
                Arguments = $"{_nodeJSProcessOptions.NodeAndV8Options} --input-type=module -e \"{nodeServerScript}\" -- --parentPid {currentProcessPid} --port {_nodeJSProcessOptions.Port}",
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = _nodeJSProcessOptions.ProjectPath,
                // If this option isn't set to true, in certain situations, e.g. in windows forms projects, a Node.js console window pops up when scripts are invoked.
                CreateNoWindow = true
            };

            // Append environment Variables
            if (_nodeJSProcessOptions.EnvironmentVariables != null)
            {
                foreach (string envVarKey in _nodeJSProcessOptions.EnvironmentVariables.Keys)
                {
                    string envVarValue = _nodeJSProcessOptions.EnvironmentVariables[envVarKey];
                    startInfo.Environment[envVarKey] = envVarValue;
                }
            }

            return startInfo;
        }

        internal static Process CreateProcess(ProcessStartInfo startInfo, EventHandler? exitedEventHandler = null)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = startInfo,
                    // On Mac at least, a killed child process is left open as a zombie until the parent
                    // captures its exit code. We don't need the exit code for this process, and don't want
                    // to use process.WaitForExit() explicitly (we'd have to block the thread until it really
                    // has exited), but we don't want to leave zombies lying around either. It's sufficient
                    // to use process.EnableRaisingEvents so that .NET will grab the exit code and let the
                    // zombie be cleaned away without having to block our thread.
                    EnableRaisingEvents = true,
                };
                if (exitedEventHandler != null)
                {
                    process.Exited += exitedEventHandler;
                }
                process.Start();

                return process;
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(string.Format(Strings.InvalidOperationException_NodeJSProcessFactory_FailedToStartNodeProcess, Environment.GetEnvironmentVariable("PATH")), exception);
            }

            throw new InvalidOperationException(string.Format(Strings.InvalidOperationException_NodeJSProcessFactory_FailedToStartNodeProcess, Environment.GetEnvironmentVariable("PATH")));
        }

        internal static string EscapeCommandLineArg(string arg)
        {
            var stringBuilder = new StringBuilder();
            int slashSequenceLength = 0;
            for (int i = 0; i < arg.Length; i++)
            {
                char currentChar = arg[i];

                if (currentChar == '\\')
                {
                    slashSequenceLength++;

                    // If the last character in the argument is \, it must be escaped, together with any \ that immediately preceed it.
                    // This prevents situations like: SomeExecutable.exe "SomeArg\", where the quote meant to demarcate the end of the
                    // argument gets escaped.
                    if (i == arg.Length - 1)
                    {
                        for (int j = 0; j < slashSequenceLength; j++)
                        {
                            stringBuilder.
                                Append('\\').
                                Append('\\');
                        }
                    }
                }
                else if (currentChar == '"')
                {
                    // Every \ or sequence of \ that preceed a " must be escaped.
                    for (int j = 0; j < slashSequenceLength; j++)
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
