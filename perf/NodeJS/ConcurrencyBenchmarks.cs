using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using Microsoft.AspNetCore.NodeServices;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Jering.Javascript.NodeJS.Performance
{
    [MemoryDiagnoser]
    public class ConcurrencyBenchmarks
    {
        private const string DUMMY_WARMUP_MODULE = "module.exports = (callback) => callback()";
        private const string DUMMY_CONCURRENCY_MODULE_FILE = "dummyConcurrencyModule.js";

        private ServiceProvider _serviceProvider;
        private INodeJSService _nodeJSService;
        [Obsolete]
        private INodeServices _nodeServices;

        [GlobalSetup(Target = nameof(INodeJSService_Concurrency_MultiProcess))]
        public void INodeJSService_Concurrency_MultiProcess_Setup()
        {
            var services = new ServiceCollection();
            services.AddNodeJS();
            services.Configure<NodeJSProcessOptions>(options => options.ProjectPath = Path.Combine(Directory.GetCurrentDirectory(), "../../../.."));
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
        public async Task<string[]> INodeJSService_Concurrency_MultiProcess()
        {
            // Act
            const int numTasks = 25;
            var results = new Task<string>[numTasks];
            for (int i = 0; i < numTasks; i++)
            {
                results[i] = _nodeJSService.InvokeFromFileAsync<string>(DUMMY_CONCURRENCY_MODULE_FILE);
            }

            return await Task.WhenAll(results);
        }

        [GlobalSetup(Target = nameof(INodeJSService_Concurrency_None))]
        public void INodeJSService_Concurrency_None_Setup()
        {
            var services = new ServiceCollection();
            services.AddNodeJS();
            services.Configure<NodeJSProcessOptions>(options => options.ProjectPath = Path.Combine(Directory.GetCurrentDirectory(), "../../../.."));
            _serviceProvider = services.BuildServiceProvider();
            _nodeJSService = _serviceProvider.GetRequiredService<INodeJSService>();

            // Warmup. First run starts a Node.js processes.
            _nodeJSService.InvokeFromStringAsync(DUMMY_WARMUP_MODULE).GetAwaiter().GetResult();
        }

        [Benchmark]
        public async Task<string[]> INodeJSService_Concurrency_None()
        {
            // Act
            const int numTasks = 25;
            var results = new Task<string>[numTasks];
            for (int i = 0; i < numTasks; i++)
            {
                results[i] = _nodeJSService.InvokeFromFileAsync<string>(DUMMY_CONCURRENCY_MODULE_FILE);
            }

            return await Task.WhenAll(results);
        }

        [Obsolete]
        [GlobalSetup(Target = nameof(INodeServices_Concurrency))]
        public void INodeServices_Concurrency_Setup()
        {
            var services = new ServiceCollection();
            services.AddNodeServices(options =>
            {
                options.ProjectPath = Path.Combine(Directory.GetCurrentDirectory(), "../../../..");
                options.WatchFileExtensions = null;
            });
            _serviceProvider = services.BuildServiceProvider();
            _nodeServices = _serviceProvider.GetRequiredService<INodeServices>();

            // Warmup. First run starts a Node.js processes.
            _nodeServices.InvokeAsync<DummyResult>("dummyLatencyModule.js", 0).GetAwaiter().GetResult();
        }

        [Obsolete]
        [Benchmark]
        public async Task<string[]> INodeServices_Concurrency()
        {
            // Act
            const int numTasks = 25;
            var results = new Task<string>[numTasks];
            for (int i = 0; i < numTasks; i++)
            {
                results[i] = _nodeServices.InvokeAsync<string>(DUMMY_CONCURRENCY_MODULE_FILE);
            }

            return await Task.WhenAll(results);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _serviceProvider.Dispose();
        }

        public class DummyResult
        {
            public string Result { get; set; }
        }
    }
}
