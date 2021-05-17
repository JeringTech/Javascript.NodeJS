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

        private ServiceProvider? _serviceProvider;
        private int _counter;
        private readonly object[] _args = new object[1];
        private INodeJSService? _nodeJSService;
        [Obsolete]
        private INodeServices? _nodeServices;

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
        public async Task<DummyResult?> INodeJSService_Latency_InvokeFromFile()
        {
            _args[0] = _counter++;
            return await _nodeJSService!.InvokeFromFileAsync<DummyResult>(DUMMY_LATENCY_MODULE_FILE, args: _args).ConfigureAwait(false);
        }

        // If file watching is enabled but graceful shutdown is disabled, latency is the same as if file watching is disabled,
        // so don't need a separate benchmark for that scenario. There is slightly higher latency when file watching and
        // graceful shutdown are both enabled though.
        [GlobalSetup(Target = nameof(INodeJSService_Latency_InvokeFromFile_GracefulShutdownEnabled))]
        public void INodeJSService_Latency_InvokeFromFile_GracefulShutdownEnabled_Setup()
        {
            var services = new ServiceCollection();
            services.AddNodeJS();
            services.Configure<NodeJSProcessOptions>(options => options.ProjectPath = _projectPath);
            services.Configure<OutOfProcessNodeJSServiceOptions>(options => options.EnableFileWatching = true);
            _serviceProvider = services.BuildServiceProvider();
            _nodeJSService = _serviceProvider.GetRequiredService<INodeJSService>();
            _counter = 0;

            // Warmup. First run starts a Node.js process.
            _nodeJSService.InvokeFromStringAsync(DUMMY_WARMUP_MODULE).GetAwaiter().GetResult();
        }

        [Benchmark]
        public async Task<DummyResult?> INodeJSService_Latency_InvokeFromFile_GracefulShutdownEnabled()
        {
            _args[0] = _counter++;
            return await _nodeJSService!.InvokeFromFileAsync<DummyResult>(DUMMY_LATENCY_MODULE_FILE, args: _args).ConfigureAwait(false);
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
            _args[0] = _counter++;
            _nodeJSService.InvokeFromStringAsync<DummyResult>(DummyModuleFactory, DUMMY_MODULE_IDENTIFIER, args: _args).GetAwaiter().GetResult();
        }

        [Benchmark]
        public async Task<DummyResult?> INodeJSService_Latency_InvokeFromCache()
        {
            _args[0] = _counter++;
            return await _nodeJSService!.InvokeFromStringAsync<DummyResult>(DummyModuleFactory, DUMMY_MODULE_IDENTIFIER, args: _args).ConfigureAwait(false);
        }

        private string DummyModuleFactory()
        {
            return File.ReadAllText(Path.Combine(_projectPath, DUMMY_LATENCY_MODULE_FILE));
        }

        [Obsolete("NodeServices is obsolete")]
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

        [Obsolete("NodeServices is obsolete")]
        [Benchmark]
        public async Task<DummyResult> INodeServices_Latency()
        {
            _args[0] = _counter++;
            DummyResult result = await _nodeServices!.InvokeAsync<DummyResult>(DUMMY_LATENCY_MODULE_FILE, _args).ConfigureAwait(false);
            return result;
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _serviceProvider?.Dispose();
        }

        public class DummyResult
        {
            public int Result { get; set; }
        }
    }
}
