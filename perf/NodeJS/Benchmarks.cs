using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using Microsoft.AspNetCore.NodeServices;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Threading.Tasks;

namespace Jering.Javascript.NodeJS.Performance
{
    [MemoryDiagnoser]
    public class Benchmarks
    {
        private ServiceProvider _serviceProvider;

        private const string DUMMY_MODULE_IDENTIFIER = "dummyModuleIdentifier";
        private int _counter;
        private INodeJSService _nodeJSService;
        private INodeServices _nodeServices;

        [GlobalSetup(Target = nameof(INodeJSService_InvokeFromFile))]
        public void INodeJSService_InvokeFromFile_Setup()
        {
            var services = new ServiceCollection();
            services.AddNodeJS();
            services.Configure<NodeJSProcessOptions>(options => options.ProjectPath = Path.Combine(Directory.GetCurrentDirectory(), "../../../.."));
            _serviceProvider = services.BuildServiceProvider();
            _nodeJSService = _serviceProvider.GetRequiredService<INodeJSService>(); // Default INodeJSService is HttpNodeJSService
            _counter = 0;
        }

        [Benchmark]
        public async Task<DummyResult> INodeJSService_InvokeFromFile()
        {
            DummyResult result = await _nodeJSService.InvokeFromFileAsync<DummyResult>("dummyModule.js", args: new object[] { $"success {_counter++}" });
            return result;
        }

        [GlobalSetup(Target = nameof(INodeJSService_InvokeFromCache))]
        public void INodeJSService_InvokeFromCache_Setup()
        {
            var services = new ServiceCollection();
            services.AddNodeJS();
            _serviceProvider = services.BuildServiceProvider();
            _nodeJSService = _serviceProvider.GetRequiredService<INodeJSService>(); // Default INodeJSService is HttpNodeJSService
            _counter = 0;

            // Cache module
            DummyResult _ = _nodeJSService.InvokeFromStringAsync<DummyResult>("module.exports = (callback, resultString) => callback(null, { result: resultString });", DUMMY_MODULE_IDENTIFIER, args: new object[] { $"success {_counter++}" }).Result;
        }

        [Benchmark]
        public async Task<DummyResult> INodeJSService_InvokeFromCache()
        {
            (bool _, DummyResult result) = await _nodeJSService.TryInvokeFromCacheAsync<DummyResult>(DUMMY_MODULE_IDENTIFIER, args: new object[] { $"success {_counter++}" });
            return result;
        }

        [GlobalSetup(Target = nameof(INodeServices))]
        public void INodeServices_Setup()
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
        }

        [Benchmark]
        public async Task<DummyResult> INodeServices()
        {
            DummyResult result = await _nodeServices.InvokeAsync<DummyResult>("dummyModule.js", $"success {_counter++}");
            return result;
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
