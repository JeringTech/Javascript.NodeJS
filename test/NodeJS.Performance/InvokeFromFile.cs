using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using Microsoft.AspNetCore.NodeServices;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Threading.Tasks;

namespace Jering.JavascriptUtils.NodeJS.Performance
{
    /// <summary>
    /// Invokes code from a file using NodeJSService and NodeServices.
    /// </summary>
    [MemoryDiagnoser]
    public class InvokeFromFile
    {
        private ServiceProvider _serviceProvider;

        private int _counter;
        private INodeJSService _nodeJSService;
        private INodeServices _nodeServices;

        [GlobalSetup(Target = nameof(NodeJSService))]
        public void NodeJSServiceSetup()
        {
            ServiceCollection services = new ServiceCollection();
            services.AddNodeJS();
            services.Configure<NodeJSProcessOptions>(options => options.ProjectPath = Path.Combine(Directory.GetCurrentDirectory(), "../../../.."));
            _serviceProvider = services.BuildServiceProvider();
            _nodeJSService = _serviceProvider.GetRequiredService<INodeJSService>(); // Default INodeJSService is HttpNodeJSService
            _counter = 0;
        }

        [GlobalCleanup(Target = nameof(NodeJSService))]
        public void NodeJSServiceCleanup()
        {
            _serviceProvider.Dispose();
        }

        [Benchmark]
        public Task<DummyResult> NodeJSService()
        {
            return _nodeJSService.InvokeFromFileAsync<DummyResult>("dummyModule.js", args: new object[] { $"success {_counter++}" });
        }

        [GlobalSetup(Target = nameof(NodeServices))]
        public void NodeServicesSetup()
        {
            ServiceCollection services = new ServiceCollection();
            services.AddNodeServices(options =>
            {
                options.ProjectPath = Path.Combine(Directory.GetCurrentDirectory(), "../../../..");
                options.WatchFileExtensions = null;
            });
            _serviceProvider = services.BuildServiceProvider();
            _nodeServices = _serviceProvider.GetRequiredService<INodeServices>();
            _counter = 0;
        }

        [GlobalCleanup(Target = nameof(NodeServices))]
        public void NodeServicesCleanup()
        {
            _serviceProvider.Dispose();
        }

        [Benchmark]
        public Task<DummyResult> NodeServices()
        {
            return _nodeServices.InvokeAsync<DummyResult>("dummyModule.js", $"success {_counter++}");
        }

        public class DummyResult
        {
            public string Result { get; set; }
        }
    }
}
