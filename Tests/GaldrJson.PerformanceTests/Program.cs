using System;
using BenchmarkDotNet.Running;

namespace GaldrJson.PerformanceTests;

internal class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("GaldrJson Performance Benchmarks");
        Console.WriteLine("=================================");
        Console.WriteLine();

        var summary = BenchmarkRunner.Run<JsonSerializationBenchmarks>();

        Console.WriteLine();
        Console.WriteLine("Benchmark complete!");
    }
}
