using BenchmarkDotNet.Running;

namespace Jering.Javascript.NodeJS.Performance
{
    public class Program
    {
        public static void Main(string[] _)
        {
            BenchmarkRunner.Run<LatencyBenchmarks>();
            BenchmarkRunner.Run<ConcurrencyBenchmarks>();
            BenchmarkRunner.Run<RealWorkloadBenchmarks>();
            BenchmarkRunner.Run<PoolThreadingBenchmarks>();
        }
    }
}
