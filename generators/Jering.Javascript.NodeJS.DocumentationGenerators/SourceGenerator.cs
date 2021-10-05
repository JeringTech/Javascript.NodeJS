using Microsoft.CodeAnalysis;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace Jering.Javascript.NodeJS.DocumentationGenerators
{ 
    public abstract class SourceGenerator<T> : ISourceGenerator where T : ISyntaxReceiver, new()
    {
        protected static readonly DiagnosticDescriptor _unexpectedException = new("G0006",
            "UnexpectedException",
            "UnexpectedException: {0}",
            "Code generation",
            DiagnosticSeverity.Error,
            true);

        private string _generatorName = null;

        // Concurrency
        private Mutex _mutex;

        // Logging
        protected virtual bool Log { get; set; } = false;
        private volatile string _logFilePath = null;
        private int _executionCount = 0;

        protected string _projectDirectory;
        protected string _solutionDirectory;

        protected abstract void ExecuteCore(ref GeneratorExecutionContext context);

        protected virtual void InitializeCore() { }

        protected SourceGenerator()
        {
            _generatorName = GetType().Name;
            // Multiple processes may execute the same generator, sometimes concurrently.
            // We use a named mutex to synchronize across processes.
            _mutex = new(false, _generatorName);
        }

        public void Execute(GeneratorExecutionContext context)
        {
            try
            {
                _mutex.WaitOne();

                // Initialize directories and file paths. This can only be done when we receive the first syntax tree.
                // If these directories and paths change, VS has to be restarted, so this only needs to be done once.
                if (_logFilePath == null)
                {
                    _projectDirectory = Path.GetDirectoryName(context.Compilation.SyntaxTrees.First(tree => tree.FilePath.EndsWith("AssemblyInfo.cs")).FilePath);
                    _solutionDirectory = Path.Combine(_projectDirectory, "../..");
                    _logFilePath = Path.Combine(_projectDirectory, $"{GetType().Name}.txt");
                }

                if (Log)
                {
                    LogLine("");
                    LogLine($"Executing {_generatorName}...");
                    LogLine($"Instance execution count: {Interlocked.Increment(ref _executionCount)}");
                    LogLine($"Process ID: {Process.GetCurrentProcess().Id}");
                    LogLine($"Num syntax trees: {context.Compilation.SyntaxTrees.Count()}");
                }

                // Some calls occur for a single syntax tree.
                // It should be okay to skip them - if VS actually uses the results of these runs, our cs files would be invalid (e.g. incomplete enums),
                // but that is never the case, so we know VS doesn't use the output of these runs.
                if (context.Compilation.SyntaxTrees.Count() == 1)
                {
                    LogLine("Single syntax tree execution, terminating");
                    return;
                }

                ExecuteCore(ref context);
            }
            catch (Exception exception)
            {
                context.ReportDiagnostic(Diagnostic.Create(_unexpectedException, null, "Generator name: " + GetType().Name + ", Exception message: " + exception.Message));
            }
            finally
            {
                _mutex.ReleaseMutex();
            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new T());
            InitializeCore();
        }

#pragma warning disable IDE0060 // Unused when logging is off
        protected void LogLine(string message)
#pragma warning restore IDE0060
        {
            if (!Log)
            {
                return;
            }

            File.AppendAllText(_logFilePath, message + "\n");
        }

        protected void LogCancelled()
        {
            LogLine("Cancelled");
        }
    }
}
