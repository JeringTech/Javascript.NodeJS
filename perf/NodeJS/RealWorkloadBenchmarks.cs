using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using Microsoft.AspNetCore.NodeServices;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;

namespace Jering.Javascript.NodeJS.Performance
{
    [MemoryDiagnoser]
    public class RealWorkloadBenchmarks
    {
        private const string DUMMY_CACHE_IDENTIFIER = "dummyRealWorkloadModuleIdentifier";
        private const string DUMMY_REAL_WORKLOAD_MODULE_FILE = "dummyRealWorkloadModule.js";
        // Realistically, you aren't going to pass the same string for highlighting every time, so use a format that we alter slightly every interation
        private const string DUMMY_CODE_FORMAT = @"public class HelloWorld
{{
    public static void Main(string[] args)
    {{
        // Hello world
        Console.WriteLine(""Hello world {0}!"");
    }}
}}";
        private static readonly string _projectPath = Path.Combine(Directory.GetCurrentDirectory(), "../../../../../../../Javascript"); // BenchmarkDotNet creates a project nested deep in bin

        private int _counter;
        private readonly object[] _args = new object[1];

        private ServiceProvider? _serviceProvider;
        private INodeJSService? _nodeJSService;
        [Obsolete]
        private INodeServices? _nodeServices;

        private const int NUM_INVOCATIONS = 25;

        private readonly ConcurrentQueue<Task<string?>> _invocationNullableResultTasks = new();
        private readonly ConcurrentQueue<Task<string>> _invocationResultTasks = new();

        [GlobalSetup(Target = nameof(INodeJSService_RealWorkload))]
        public void INodeJSService_RealWorkload_Setup()
        {
            var services = new ServiceCollection();
            services.AddNodeJS();
            services.Configure<NodeJSProcessOptions>(options => options.ProjectPath = _projectPath); // Module loads prismjs from node_modules
            services.Configure<OutOfProcessNodeJSServiceOptions>(options => options.Concurrency = Concurrency.MultiProcess);
            _serviceProvider = services.BuildServiceProvider();
            _nodeJSService = _serviceProvider.GetRequiredService<INodeJSService>();
            _counter = 0;

            // Warmup/cache. First few runs start Node.js processes, so they take longer. If we don't manually warm up, BenchmarkDotNet erroneously complains
            // about iteration time being too low
            for (int i = 0; i < Environment.ProcessorCount; i++)
            {
                _args[0] = string.Format(DUMMY_CODE_FORMAT, _counter++);
                _nodeJSService.InvokeFromStringAsync(DummyModuleFactory, DUMMY_CACHE_IDENTIFIER, args: _args).GetAwaiter().GetResult();
            }
        }

        [Benchmark]
        public async Task<string?[]> INodeJSService_RealWorkload()
        {
            _args[0] = string.Format(DUMMY_CODE_FORMAT, _counter++);
            // The module uses Prism.js to perform syntax highlighting
            Parallel.For(0, NUM_INVOCATIONS, key => _invocationNullableResultTasks.Enqueue(_nodeJSService!.InvokeFromStringAsync<string>(DummyModuleFactory, DUMMY_CACHE_IDENTIFIER, args: _args)));
            return await Task.WhenAll(_invocationNullableResultTasks).ConfigureAwait(false);
        }

        private string DummyModuleFactory()
        {
            return File.ReadAllText(Path.Combine(_projectPath, DUMMY_REAL_WORKLOAD_MODULE_FILE));
        }

        [IterationSetup(Target = nameof(INodeJSService_RealWorkload))]
        public void INodeJSService_IterationSetup()
        {
            _invocationNullableResultTasks.Clear();
        }

        [Obsolete("NodeServices is obsolete")]
        [GlobalSetup(Target = nameof(INodeServices_RealWorkload))]
        public void INodeServices_RealWorkload_Setup()
        {
            var services = new ServiceCollection();
            services.AddNodeServices(options =>
            {
                options.ProjectPath = _projectPath;
                options.WatchFileExtensions = null;
            });
            _serviceProvider = services.BuildServiceProvider();
            _nodeServices = _serviceProvider.GetRequiredService<INodeServices>();
            _counter = 0;

            // Warmup. First run starts a Node.js process.
            _nodeServices!.InvokeAsync<DummyResult>("dummyLatencyModule.js", 0).GetAwaiter().GetResult(); // Doesn't support invoke from string, so this is the simplest/quickest
        }

        [Obsolete("NodeServices is obsolete")]
        [Benchmark]
        public async Task<string[]> INodeServices_RealWorkload()
        {
            _args[0] = string.Format(DUMMY_CODE_FORMAT, _counter++);
            Parallel.For(0, NUM_INVOCATIONS, key => _invocationResultTasks.Enqueue(_nodeServices!.InvokeAsync<string>(DUMMY_REAL_WORKLOAD_MODULE_FILE, _args)));
            return await Task.WhenAll(_invocationResultTasks).ConfigureAwait(false);
        }

        [IterationSetup(Target = nameof(INodeServices_RealWorkload))]
        public void INodeServices_IterationSetup()
        {
            _invocationResultTasks.Clear();
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _serviceProvider?.Dispose();
        }

        public class DummyResult
        {
            public string? Result { get; set; }
        }
    }
}
