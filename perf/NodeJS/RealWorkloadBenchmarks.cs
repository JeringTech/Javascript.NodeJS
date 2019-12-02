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

        private int _counter;
        private ServiceProvider _serviceProvider;
        private INodeJSService _nodeJSService;
        [Obsolete]
        private INodeServices _nodeServices;

        [GlobalSetup(Target = nameof(INodeJSService_RealWorkload))]
        public void INodeJSService_RealWorkload_Setup()
        {
            var services = new ServiceCollection();
            services.AddNodeJS();
            services.Configure<NodeJSProcessOptions>(options => options.ProjectPath = Path.Combine(Directory.GetCurrentDirectory(), "../../../.."));
            services.Configure<OutOfProcessNodeJSServiceOptions>(options => options.Concurrency = Concurrency.MultiProcess);
            _serviceProvider = services.BuildServiceProvider();
            _nodeJSService = _serviceProvider.GetRequiredService<INodeJSService>();
            _counter = 0;

            // Warmup/cache. First few runs start Node.js processes, so they take longer. If we don't manually warm up, BenchmarkDotNet erroneously complains
            // about iteration time being too low
            for (int i = 0; i < Environment.ProcessorCount; i++)
            {
                _nodeJSService.InvokeFromStringAsync(DummyModuleFactory, DUMMY_CACHE_IDENTIFIER, args: new object[] { string.Format(DUMMY_CODE_FORMAT, _counter++) }).GetAwaiter().GetResult();
            }
        }

        [Benchmark]
        public async Task<string[]> INodeJSService_RealWorkload()
        {
            // Act
            const int numTasks = 25;
            var results = new Task<string>[numTasks];
            for (int i = 0; i < numTasks; i++)
            {
                // The module uses Prism.js to perform syntax highlighting
                results[i] = _nodeJSService.InvokeFromStringAsync<string>(DummyModuleFactory, DUMMY_CACHE_IDENTIFIER, args: new object[] { string.Format(DUMMY_CODE_FORMAT, _counter++) });
            }

            return await Task.WhenAll(results);
        }

        private string DummyModuleFactory()
        {
            return File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "../../../..", DUMMY_REAL_WORKLOAD_MODULE_FILE));
        }

        [Obsolete]
        [GlobalSetup(Target = nameof(INodeServices_RealWorkload))]
        public void INodeServices_RealWorkload_Setup()
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

            // Warmup. First run starts a Node.js process.
            _nodeServices.InvokeAsync<DummyResult>("dummyLatencyModule.js", 0).GetAwaiter().GetResult(); // Doesn't support invoke from string, so this is the simplest/quickest
        }

        [Obsolete]
        [Benchmark]
        public async Task<string[]> INodeServices_RealWorkload()
        {
            // Act
            const int numTasks = 25;
            var results = new Task<string>[numTasks];
            for (int i = 0; i < numTasks; i++)
            {
                results[i] = _nodeServices.InvokeAsync<string>(DUMMY_REAL_WORKLOAD_MODULE_FILE, string.Format(DUMMY_CODE_FORMAT, _counter++));
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
