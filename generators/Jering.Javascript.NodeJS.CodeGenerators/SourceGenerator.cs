using Microsoft.CodeAnalysis;
using System;
using System.IO;
using System.Linq;

namespace Jering.Javascript.NodeJS.Generators
{
    public abstract class SourceGenerator<T> : ISourceGenerator where T : ISyntaxReceiver, new()
    {
        protected static readonly DiagnosticDescriptor _unexpectedException = new("G0006",
            "UnexpectedException",
            "UnexpectedException: {0}",
            "Code generation",
            DiagnosticSeverity.Error,
            true);

        private volatile string _logFilePath = string.Empty;

        protected string _projectDirectory;
        protected string _solutionDirectory;

        protected abstract void ExecuteCore(ref GeneratorExecutionContext context);

        protected virtual void InitializeCore() { }

        public void Execute(GeneratorExecutionContext context)
        {
            try
            {
                // Initialize directories and file paths. This can only be done when we receive the first syntax tree.
                // If these directories and paths change, VS has to be restarted, so this only needs to be done once.
                if (_logFilePath == string.Empty)
                {
                    lock (this)
                    {
                        if (_logFilePath == string.Empty)
                        {
                            _projectDirectory = Path.GetDirectoryName(context.Compilation.SyntaxTrees.First(tree => tree.FilePath.EndsWith("AssemblyInfo.cs")).FilePath);
                            _solutionDirectory = Path.Combine(_projectDirectory, "../..");
                            _logFilePath = Path.Combine(_projectDirectory, $"{GetType().Name}.txt");
                        }
                    }
                }

                ExecuteCore(ref context);
            }
            catch (Exception exception)
            {
                context.ReportDiagnostic(Diagnostic.Create(_unexpectedException, null, exception.Message));
            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            // Register a factory that can create our custom syntax receiver
            context.RegisterForSyntaxNotifications(() => new T());
            InitializeCore();
        }

#pragma warning disable IDE0060 // Unused when logging is off
        protected void LogLine(string message)
#pragma warning restore IDE0060
        {
            //File.AppendAllText(_logFilePath, message + "\n");
        }
    }
}
