﻿using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Jering.Javascript.NodeJS.Performance
{
    [MemoryDiagnoser]
    public class FileWatchingBenchmarks
    {
        private const string DUMMY_WARMUP_MODULE = "module.exports = (callback) => callback()";
        private const string DUMMY_PATH = "dummyPath";

        private ServiceProvider? _serviceProvider;
        private HttpNodeJSService? _httpNodeJSService;

        [GlobalSetup(Target = nameof(HttpNodeJSService_FileWatching_GracefulShutdownEnabled_MoveToNewProcess))]
        public void HttpNodeJSService_FileWatching_GracefulShutdownEnabled_MoveToNewProcess_Setup()
        {
            var services = new ServiceCollection();
            services.AddNodeJS();
            services.Configure<OutOfProcessNodeJSServiceOptions>(options => options.EnableFileWatching = true);
            _serviceProvider = services.BuildServiceProvider();
            _httpNodeJSService = _serviceProvider.GetRequiredService<INodeJSService>() as HttpNodeJSService;

            // Warmup. First run starts a Node.js process.
            _httpNodeJSService!.InvokeFromStringAsync(DUMMY_WARMUP_MODULE).GetAwaiter().GetResult();
        }

        [Benchmark]
        public void HttpNodeJSService_FileWatching_GracefulShutdownEnabled_MoveToNewProcess()
        {
            _httpNodeJSService!.FileChangedHandler();
        }

        [GlobalSetup(Target = nameof(HttpNodeJSService_FileWatching_GracefulShutdownDisabled_MoveToNewProcess))]
        public void HttpNodeJSService_FileWatching_GracefulShutdownDisabled_MoveToNewProcess_Setup()
        {
            var services = new ServiceCollection();
            services.AddNodeJS();
            services.Configure<OutOfProcessNodeJSServiceOptions>(options =>
            {
                options.EnableFileWatching = true;
                options.GracefulProcessShutdown = false;
            });
            _serviceProvider = services.BuildServiceProvider();
            _httpNodeJSService = _serviceProvider.GetRequiredService<INodeJSService>() as HttpNodeJSService;

            // Warmup. First run starts a Node.js process.
            _httpNodeJSService!.InvokeFromStringAsync(DUMMY_WARMUP_MODULE).GetAwaiter().GetResult();
        }

        [Benchmark]
        public void HttpNodeJSService_FileWatching_GracefulShutdownDisabled_MoveToNewProcess()
        {
            _httpNodeJSService!.FileChangedHandler();
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _serviceProvider?.Dispose();
        }
    }
}
