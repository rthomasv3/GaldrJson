using System.Text.Json;
using System.Text.Json.Serialization;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Newtonsoft.Json;

namespace GaldrJson.PerformanceTests;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
[SimpleJob(iterationCount: 25)]
public class JsonSerializationBenchmarks
{
    private ProductCatalog _testData;
    private string _jsonString;
    private GaldrJsonOptions _galdrJsonOptions;
    private JsonSerializerOptions _jsonSerializerOptions;
    private JsonSerializerOptions _jsonSerializerOptionsSourceGen;
    private JsonSerializerSettings _jsonSerializerSettings;

    [GlobalSetup]
    public void Setup()
    {
        // Test data: 50 products with nested details (manufacturer, dimensions)
        //            30 orders with 1-5 items each, addresses, status enums
        //            Dictionaries, lists, nullable types, dates, decimals
        _testData = TestDataGenerator.GenerateCatalog(productCount: 50, orderCount: 30);

        // Pre-serialize for deserialization tests
        // Using System.Text.Json as the baseline serializer for fairness
        _jsonString = System.Text.Json.JsonSerializer.Serialize(_testData);

        _galdrJsonOptions = new GaldrJsonOptions()
        {
            PropertyNameCaseInsensitive = false,
            PropertyNamingPolicy = PropertyNamingPolicy.Exact,
            WriteIndented = false,
            DetectCycles = true,
        };

        _jsonSerializerOptions = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = false,
            PropertyNamingPolicy = null,
            WriteIndented = false,
            ReferenceHandler = ReferenceHandler.Preserve,
        };

        _jsonSerializerOptionsSourceGen = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = false,
            PropertyNamingPolicy = null,
            WriteIndented = false,
            ReferenceHandler = ReferenceHandler.Preserve,
            TypeInfoResolver = SourceGenerationContext.Default
        };

        _jsonSerializerSettings = new JsonSerializerSettings()
        {
            Formatting = Formatting.None,
            ReferenceLoopHandling = ReferenceLoopHandling.Error,
        };
    }

    // ========================================================================
    // SERIALIZATION BENCHMARKS
    // ========================================================================

    [Benchmark(Description = "Serialize (Newtonsoft.Json)")]
    public string Serialize_NewtonsoftJson()
    {
        return JsonConvert.SerializeObject(_testData, _jsonSerializerSettings);
    }

    [Benchmark(Description = "Serialize (System.Text.Json)")]
    public string Serialize_SystemTextJson()
    {
        return System.Text.Json.JsonSerializer.Serialize(_testData, _jsonSerializerOptions);
    }

    [Benchmark(Description = "Serialize (System.Text.Json Source Gen)")]
    public string Serialize_SystemTextJson_SourceGen()
    {
        return System.Text.Json.JsonSerializer.Serialize(_testData, _jsonSerializerOptionsSourceGen);
    }

    [Benchmark(Description = "Serialize (GaldrJson)")]
    public string Serialize_GaldrJson()
    {
        return GaldrJson.Serialize(_testData, _galdrJsonOptions);
    }

    // ========================================================================
    // DESERIALIZATION BENCHMARKS
    // ========================================================================

    [Benchmark(Description = "Deserialize (Newtonsoft.Json)")]
    public ProductCatalog Deserialize_NewtonsoftJson()
    {
        return JsonConvert.DeserializeObject<ProductCatalog>(_jsonString, _jsonSerializerSettings);
    }

    [Benchmark(Description = "Deserialize (System.Text.Json)")]
    public ProductCatalog Deserialize_SystemTextJson()
    {
        return System.Text.Json.JsonSerializer.Deserialize<ProductCatalog>(_jsonString, _jsonSerializerOptions);
    }

    [Benchmark(Description = "Deserialize (System.Text.Json Source Gen)")]
    public ProductCatalog Deserialize_SystemTextJson_SourceGen()
    {
        return System.Text.Json.JsonSerializer.Deserialize<ProductCatalog>(_jsonString, _jsonSerializerOptionsSourceGen);
    }

    [Benchmark(Description = "Deserialize (GaldrJson)")]
    public ProductCatalog Deserialize_GaldrJson()
    {
        return GaldrJson.Deserialize<ProductCatalog>(_jsonString, _galdrJsonOptions);
    }
}
