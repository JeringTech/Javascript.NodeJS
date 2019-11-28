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
        private const string DUMMY_LATENCY_MODULE = "dummyLatencyModule.js";
        private const string DUMMY_MODULE_IDENTIFIER = "dummyLatencyModuleIdentifier";

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
            services.Configure<NodeJSProcessOptions>(options => options.ProjectPath = Path.Combine(Directory.GetCurrentDirectory(), "../../../.."));
            _serviceProvider = services.BuildServiceProvider();
            _nodeJSService = _serviceProvider.GetRequiredService<INodeJSService>();
            _counter = 0;

            // Warm up. First run starts a Node.js process.
            _nodeJSService.InvokeFromStringAsync<string>("module.exports = (callback) => callback(null, null)", "warmup").GetAwaiter().GetResult();
        }

        [Benchmark]
        public async Task<DummyResult> INodeJSService_Latency_InvokeFromFile()
        {
            DummyResult result = await _nodeJSService.InvokeFromFileAsync<DummyResult>(DUMMY_LATENCY_MODULE, args: new object[] { _counter++ });
            return result;
        }

        [GlobalSetup(Target = nameof(INodeJSService_Latency_InvokeFromCache))]
        public void INodeJSService_Latency_InvokeFromCache_Setup()
        {
            var services = new ServiceCollection();
            services.AddNodeJS();
            _serviceProvider = services.BuildServiceProvider();
            _nodeJSService = _serviceProvider.GetRequiredService<INodeJSService>();
            _counter = 0;

            // Cache module/warmup
            _nodeJSService.InvokeFromStringAsync<DummyResult>("module.exports = (callback, result) => callback(null, { result: result });", DUMMY_MODULE_IDENTIFIER, args: new object[] { _counter++ }).GetAwaiter().GetResult();
        }

        [Benchmark]
        public async Task<DummyResult> INodeJSService_Latency_InvokeFromCache()
        {
            (bool _, DummyResult result) = await _nodeJSService.TryInvokeFromCacheAsync<DummyResult>(DUMMY_MODULE_IDENTIFIER, args: new object[] { _counter++ });
            return result;
        }

        [Obsolete]
        [GlobalSetup(Target = nameof(INodeServices_Latency))]
        public void INodeServices_Latency_Setup()
        {
            var services = new ServiceCollection();
            services.AddNodeServices(options =>
            {
                options.ProjectPath = Path.Combine(Directory.GetCurrentDirectory(), "../../../..");
                options.WatchFileExtensions = null;
            });
            _serviceProvider = services.BuildServiceProvider();
            _nodeServices = _serviceProvider.GetRequiredService<INodeServices>();
            _counter = 0;

            // Warm up. First run starts a Node.js process.
            _nodeServices.InvokeAsync<DummyResult>(DUMMY_LATENCY_MODULE, 0).GetAwaiter().GetResult();
        }

        [Obsolete]
        [Benchmark]
        public async Task<DummyResult> INodeServices_Latency()
        {
            DummyResult result = await _nodeServices.InvokeAsync<DummyResult>(DUMMY_LATENCY_MODULE, _counter++);
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
