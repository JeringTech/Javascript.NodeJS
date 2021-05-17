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
    public class ConcurrencyBenchmarks
    {
        private const string DUMMY_WARMUP_MODULE = "module.exports = (callback) => callback()";
        private const string DUMMY_CONCURRENCY_MODULE_FILE = "dummyConcurrencyModule.js";
        private static readonly string _projectPath = Path.Combine(Directory.GetCurrentDirectory(), "../../../../../../../Javascript");  // BenchmarkDotNet creates a project nested deep in bin

        private ServiceProvider? _serviceProvider;
        private INodeJSService? _nodeJSService;
        [Obsolete]
        private INodeServices? _nodeServices;

        private const int NUM_INVOCATIONS = 25;
        private readonly ConcurrentQueue<Task<string?>> _invocationNullableResultTasks = new();
        private readonly ConcurrentQueue<Task<string>> _invocationResultTasks = new();

        [GlobalSetup(Target = nameof(INodeJSService_Concurrency_MultiProcess))]
        public void INodeJSService_Concurrency_MultiProcess_Setup()
        {
            var services = new ServiceCollection();
            services.AddNodeJS();
            services.Configure<NodeJSProcessOptions>(options => options.ProjectPath = _projectPath);
            services.Configure<OutOfProcessNodeJSServiceOptions>(options => options.Concurrency = Concurrency.MultiProcess);
            _serviceProvider = services.BuildServiceProvider();
            _nodeJSService = _serviceProvider.GetRequiredService<INodeJSService>();

            // Warmup. First few runs start Node.js processes, so they take longer. If we don't manually warm up, BenchmarkDotNet erroneously complains
            // about iteration time being too low
            for (int i = 0; i < Environment.ProcessorCount; i++)
            {
                _nodeJSService.InvokeFromStringAsync(DUMMY_WARMUP_MODULE).GetAwaiter().GetResult();
            }
        }

        [Benchmark]
        public async Task<string?[]> INodeJSService_Concurrency_MultiProcess()
        {
            Parallel.For(0, NUM_INVOCATIONS, key => _invocationNullableResultTasks.Enqueue(_nodeJSService!.InvokeFromFileAsync<string>(DUMMY_CONCURRENCY_MODULE_FILE)));
            return await Task.WhenAll(_invocationNullableResultTasks).ConfigureAwait(false);
        }

        [GlobalSetup(Target = nameof(INodeJSService_Concurrency_None))]
        public void INodeJSService_Concurrency_None_Setup()
        {
            var services = new ServiceCollection();
            services.AddNodeJS();
            services.Configure<NodeJSProcessOptions>(options => options.ProjectPath = _projectPath);
            _serviceProvider = services.BuildServiceProvider();
            _nodeJSService = _serviceProvider.GetRequiredService<INodeJSService>();

            // Warmup. First run starts a Node.js processes.
            _nodeJSService.InvokeFromStringAsync(DUMMY_WARMUP_MODULE).GetAwaiter().GetResult();
        }

        [Benchmark]
        public async Task<string?[]> INodeJSService_Concurrency_None()
        {
            Parallel.For(0, NUM_INVOCATIONS, key => _invocationNullableResultTasks.Enqueue(_nodeJSService!.InvokeFromFileAsync<string>(DUMMY_CONCURRENCY_MODULE_FILE)));
            return await Task.WhenAll(_invocationNullableResultTasks).ConfigureAwait(false);
        }

        [IterationSetup(Targets = new[] { nameof(INodeJSService_Concurrency_MultiProcess), nameof(INodeJSService_Concurrency_None), })]
        public void INodeJSService_IterationSetup()
        {
            _invocationNullableResultTasks.Clear();
        }

        [Obsolete("NodeServices is obsolete")]
        [GlobalSetup(Target = nameof(INodeServices_Concurrency))]
        public void INodeServices_Concurrency_Setup()
        {
            var services = new ServiceCollection();
            services.AddNodeServices(options =>
            {
                options.ProjectPath = _projectPath;
                options.WatchFileExtensions = null;
            });
            _serviceProvider = services.BuildServiceProvider();
            _nodeServices = _serviceProvider.GetRequiredService<INodeServices>();

            // Warmup. First run starts a Node.js processes.
            _nodeServices.InvokeAsync<DummyResult>("dummyLatencyModule.js", 0).GetAwaiter().GetResult();
        }

        [Obsolete("NodeServices is obsolete")]
        [Benchmark]
        public async Task<string?[]> INodeServices_Concurrency()
        {
            Parallel.For(0, NUM_INVOCATIONS, key => _invocationResultTasks.Enqueue(_nodeServices!.InvokeAsync<string>(DUMMY_CONCURRENCY_MODULE_FILE)));
            return await Task.WhenAll(_invocationResultTasks).ConfigureAwait(false);
        }

        [IterationSetup(Target = nameof(INodeServices_Concurrency))]
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
