using BenchmarkDotNet.Running;

namespace Jering.JavascriptUtils.NodeJS.Performance
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkRunner.Run<Benchmarks>();
        }
    }
}
