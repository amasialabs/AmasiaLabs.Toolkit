using BenchmarkDotNet.Running;

namespace AmasiaLabs.Toolkit.FlowflakeId.Benchmarks;

// ReSharper disable once ClassNeverInstantiated.Global
public class Program
{
    public static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<FlowflakeIdBenchmarks>();
        Console.WriteLine(summary);
    }
}