using BenchmarkDotNet.Running;

namespace Jering.Javascript.NodeJS.Performance
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkRunner.Run<Benchmarks>();
        }
    }
}
