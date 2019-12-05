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
    public class LatencyBenchmarks
    {
        private const string DUMMY_WARMUP_MODULE = "module.exports = (callback) => callback()";
        private const string DUMMY_LATENCY_MODULE_FILE = "dummyLatencyModule.js";
        private const string DUMMY_MODULE_IDENTIFIER = "dummyLatencyModuleIdentifier";
        private static readonly string _projectPath = Path.Combine(Directory.GetCurrentDirectory(), "../../../../../../../Javascript"); // BenchmarkDotNet creates a project nested deep in bin

        private ServiceProvider _serviceProvider;
        private int _counter;
        private INodeJSService _nodeJSService;
        [Obsolete]
        private INodeServices _nodeServices;

        [GlobalSetup(Target = nameof(INodeJSService_Latency_InvokeFromFile))]
        public void INodeJSService_Latency_InvokeFromFile_Setup()
        {
            var services = new ServiceCollection();
            services.AddNodeJS();
            services.Configure<NodeJSProcessOptions>(options => options.ProjectPath = _projectPath);
            _serviceProvider = services.BuildServiceProvider();
            _nodeJSService = _serviceProvider.GetRequiredService<INodeJSService>();
            _counter = 0;

            // Warmup. First run starts a Node.js process.
            _nodeJSService.InvokeFromStringAsync(DUMMY_WARMUP_MODULE).GetAwaiter().GetResult();
        }

        [Benchmark]
        public async Task<DummyResult> INodeJSService_Latency_InvokeFromFile()
        {
            return await _nodeJSService.InvokeFromFileAsync<DummyResult>(DUMMY_LATENCY_MODULE_FILE, args: new object[] { _counter++ });
        }

        [GlobalSetup(Target = nameof(INodeJSService_Latency_InvokeFromCache))]
        public void INodeJSService_Latency_InvokeFromCache_Setup()
        {
            var services = new ServiceCollection();
            services.AddNodeJS();
            _serviceProvider = services.BuildServiceProvider();
            _nodeJSService = _serviceProvider.GetRequiredService<INodeJSService>();
            _counter = 0;

            // Warmup/cache.
            _nodeJSService.InvokeFromStringAsync<DummyResult>(DummyModuleFactory, DUMMY_MODULE_IDENTIFIER, args: new object[] { _counter++ }).GetAwaiter().GetResult();
        }

        [Benchmark]
        public async Task<DummyResult> INodeJSService_Latency_InvokeFromCache()
        {
            return await _nodeJSService.InvokeFromStringAsync<DummyResult>(DummyModuleFactory, DUMMY_MODULE_IDENTIFIER, args: new object[] { _counter++ });
        }

        private string DummyModuleFactory()
        {
            return File.ReadAllText(Path.Combine(_projectPath, DUMMY_LATENCY_MODULE_FILE));
        }

        [Obsolete]
        [GlobalSetup(Target = nameof(INodeServices_Latency))]
        public void INodeServices_Latency_Setup()
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
            _nodeServices.InvokeAsync<DummyResult>(DUMMY_LATENCY_MODULE_FILE, 0).GetAwaiter().GetResult();
        }

        [Obsolete]
        [Benchmark]
        public async Task<DummyResult> INodeServices_Latency()
        {
            DummyResult result = await _nodeServices.InvokeAsync<DummyResult>(DUMMY_LATENCY_MODULE_FILE, _counter++);
            return result;
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _serviceProvider.Dispose();
        }

        public class DummyResult
        {
            public int Result { get; set; }
        }
    }
}
