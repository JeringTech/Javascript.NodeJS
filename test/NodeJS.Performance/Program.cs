using BenchmarkDotNet.Running;

namespace Jering.JavascriptUtils.NodeJS.Performance
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkRunner.Run<InvokeFromFile>();
        }

        public class DummyResult
        {
            public string Result { get; set; }
        }
    }
}
