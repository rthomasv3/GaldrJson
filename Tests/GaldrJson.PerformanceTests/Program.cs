using System;
using System.Text.Json.Serialization;
using BenchmarkDotNet.Running;

namespace GaldrJson.PerformanceTests;

internal class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("GaldrJson Performance Benchmarks");
        Console.WriteLine("=================================");
        Console.WriteLine();

        BenchmarkRunner.Run<JsonSerializationBenchmarks>();

        Console.WriteLine();
        Console.WriteLine("Benchmark complete!");
    }
}

[JsonSerializable(typeof(ProductCatalog))]
internal partial class SourceGenerationContext : JsonSerializerContext { }
