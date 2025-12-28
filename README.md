# GaldrJson

**Blazing-fast, AOT-native JSON serialization for .NET**

GaldrJson is a high-performance JSON serialization library that uses source generation to provide zero-reflection, compile-time type safety. Designed for Native AOT scenarios but works seamlessly across all of .NET.

## Features

- **Zero Reflection** - All serialization code is generated at compile time
- **Native AOT Ready** - Full compatibility with Native AOT compilation
- **High Performance** - Competitive with System.Text.Json, faster than Newtonsoft.Json
- **Simple API** - Minimal configuration, maximum productivity
- **Flexible Naming Policies** - camelCase, snake_case, kebab-case, exact, or custom
- **ASP.NET Core Integration** - First-class support for Minimal APIs
- **Comprehensive Type Support** - Primitives, collections, dictionaries, nested objects, nullables, enums
- **Cycle Detection** - Optional circular reference detection

## Installation

Install via NuGet:

```bash
dotnet add package GaldrJson
```

For ASP.NET Core Minimal API support:

```bash
dotnet add package GaldrJson.AspNetCore
```

## Quick Start

### 1. Mark your types with `[GaldrJsonSerializable]`

```csharp
[GaldrJsonSerializable]
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public List<string> Tags { get; set; }
}
```

### 2. Serialize and deserialize

```csharp
var product = new Product
{
    Id = 1,
    Name = "Widget",
    Price = 29.99m,
    Tags = new List<string> { "new", "featured" }
};

// Serialize
string json = GaldrJson.Serialize(product);

// Deserialize
Product deserialized = GaldrJson.Deserialize<Product>(json);
```

That's it! The source generator handles everything else at compile time.

## Usage

### Basic Serialization & Deserialization

```csharp
using GaldrJson;

[GaldrJsonSerializable]
public class Person
{
    public string Name { get; set; }
    public int Age { get; set; }
    public Address Address { get; set; }
}

[GaldrJsonSerializable]
public class Address
{
    public string Street { get; set; }
    public string City { get; set; }
}

// Serialize
var person = new Person { Name = "John", Age = 30 };
string json = GaldrJson.Serialize(person);

// Deserialize
Person result = GaldrJson.Deserialize<Person>(json);
```

### Property Naming Policies

```csharp
var options = new GaldrJsonOptions
{
    PropertyNamingPolicy = PropertyNamingPolicy.CamelCase,
};

string json = GaldrJson.Serialize(person, options);
// Output: { "name": "John", "age": 30 }
```

Available policies:

-   `PropertyNamingPolicy.Exact` - Use exact property names (default)
-   `PropertyNamingPolicy.CamelCase` - camelCase
-   `PropertyNamingPolicy.SnakeCase` - snake_case
-   `PropertyNamingPolicy.KebabCase` - kebab-case

### Custom Property Names

```csharp
[GaldrJsonSerializable]
public class User
{
    [GaldrJsonPropertyName("user_id")]
    public int Id { get; set; }

    [GaldrJsonPropertyName("display_name")]
    public string Name { get; set; }
}
```

### Ignoring Properties

```csharp
[GaldrJsonSerializable]
public class Account
{
    public string Username { get; set; }

    [GaldrJsonIgnore]
    public string Password { get; set; }  // Never serialized
}
```

### Cycle Detection

```csharp
var options = new GaldrJsonOptions
{
    DetectCycles = true  // Throws JsonException if circular reference detected
};

string json = GaldrJson.Serialize(obj, options);
```

### ASP.NET Core Minimal API Integration

```csharp
using GaldrJson.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Register GaldrJson
builder.Services.AddGaldrJson();

var app = builder.Build();

[GaldrJsonSerializable]
public class ApiResponse
{
    public string Message { get; set; }
    public int StatusCode { get; set; }
}

app.MapGet("/api/data", () => new ApiResponse
{
    Message = "Success",
    StatusCode = 200
});

app.Run();
```

### Dependency Injection and Testability

GaldrJson provides the `IGaldrJsonSerializer` interface for easy testing and dependency injection:
```csharp
public class OrderService
{
    private readonly IGaldrJsonSerializer _serializer;
    
    public OrderService(IGaldrJsonSerializer serializer)
    {
        _serializer = serializer;
    }
    
    public string ExportOrder(Order order)
    {
        return _serializer.Serialize(order);
    }
}
```

The interface is automatically registered when calling `AddGaldrJson()`, making it easy to inject into your services or mock in unit tests.
```csharp
builder.Services.AddGaldrJson();

// Now you can inject IGaldrJsonSerializer anywhere
```

#### IGaldrJsonSerializer Methods
```
string Serialize<T>(T value, GaldrJsonOptions options = null);

bool TrySerialize(object value, Type actualType, out string json, GaldrJsonOptions options = null);

bool TrySerialize<T>(T value, out string json, GaldrJsonOptions options = null);

T Deserialize<T>(string json, GaldrJsonOptions options = null);

bool TryDeserialize(string json, Type targetType, out object value, GaldrJsonOptions options = null);

bool TryDeserialize<T>(string json, out T value, GaldrJsonOptions options = null);
```

## Supported Types

GaldrJson supports a comprehensive set of .NET types:

**Primitives**: `int`, `long`, `short`, `byte`, `sbyte`, `uint`, `ulong`, `ushort`, `float`, `double`, `decimal`, `bool`, `char`, `string`

**System Types**: `DateTime`, `DateTimeOffset`, `TimeSpan`, `Guid`

**Collections**: `List<T>`, `T[]`, `IList<T>`, `ICollection<T>`, `IEnumerable<T>`

**Dictionaries**: `Dictionary<TKey, TValue>`, `IDictionary<TKey, TValue>`, `IReadOnlyDictionary<TKey, TValue>`

**Special**: Nullable value types, Enums (as integers), Nested objects, `byte[]` (Base64)

## Performance

GaldrJson uses compile-time code generation to achieve performance competitive with System.Text.Json while maintaining full AOT compatibility.

With some settings, like when detecting infinite loops in serialization, GaldrJson significantly outperforms it.

_Benchmarks were performed with a complex object graph (50 products, 30 orders). See Tests/GaldrJson.PerformanceTests._

### Benchmark Results (Default Settings)

```
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26100.7462/24H2/2024Update/HudsonValley)
AMD Ryzen 9 9950X 4.30GHz, 1 CPU, 32 logical and 16 physical cores
.NET SDK 10.0.101
  [Host]     : .NET 10.0.1 (10.0.1, 10.0.125.57005), X64 RyuJIT x86-64-v4
  Job-FBVPNM : .NET 10.0.1 (10.0.1, 10.0.125.57005), X64 RyuJIT x86-64-v4

IterationCount=25

| Method                                      | Mean      | Error    | StdDev   | Rank | Gen0    | Gen1    | Gen2    | Allocated |
|-------------------------------------------- |----------:|---------:|---------:|-----:|--------:|--------:|--------:|----------:|
| 'Serialize (System.Text.Json Source Gen)'   |  56.00 us | 1.161 us | 1.550 us |    1 | 26.9775 | 26.9775 | 26.9775 |  83.21 KB |
| 'Deserialize (GaldrJson)'                   |  62.84 us | 0.850 us | 1.134 us |    2 |  4.1504 |  0.7324 |       - |  69.01 KB |
| 'Serialize (GaldrJson)'                     |  64.33 us | 1.068 us | 1.426 us |    2 | 26.9775 | 26.9775 | 26.9775 |  83.42 KB |
| 'Serialize (System.Text.Json)'              |  70.27 us | 1.710 us | 2.283 us |    3 | 26.9775 | 26.9775 | 26.9775 |  83.52 KB |
| 'Deserialize (System.Text.Json Source Gen)' |  78.46 us | 1.038 us | 1.386 us |    4 |  4.1504 |  0.7324 |       - |  69.27 KB |
| 'Deserialize (System.Text.Json)'            |  79.95 us | 1.272 us | 1.699 us |    4 |  4.1504 |  0.7324 |       - |  69.27 KB |
| 'Serialize (Newtonsoft.Json)'               | 122.01 us | 3.797 us | 4.937 us |    5 | 26.9775 | 26.9775 | 26.9775 | 231.88 KB |
| 'Deserialize (Newtonsoft.Json)'             | 146.07 us | 2.361 us | 3.151 us |    6 |  6.3477 |  1.2207 |       - | 106.24 KB |
```

### Benchmark Results (camelCase Naming)

```
BenchmarkDotNet v0.15.8, Linux Fedora Linux 43 (Workstation Edition)
AMD Ryzen AI 7 PRO 350 w/ Radeon 860M 1.41GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.101
  [Host]     : .NET 10.0.1 (10.0.1, 10.0.125.57005), X64 RyuJIT x86-64-v4 [AttachedDebugger]
  Job-FBVPNM : .NET 10.0.1 (10.0.1, 10.0.125.57005), X64 RyuJIT x86-64-v4

IterationCount=25  

| Method                                      | Mean     | Error   | StdDev   | Rank | Gen0    | Gen1    | Gen2    | Allocated |
|-------------------------------------------- |---------:|--------:|---------:|-----:|--------:|--------:|--------:|----------:|
| 'Serialize (GaldrJson)'                     | 169.6 us | 2.53 us |  3.38 us |    1 | 26.8555 | 26.8555 | 26.8555 |  83.42 KB |
| 'Deserialize (GaldrJson)'                   | 178.9 us | 2.41 us |  3.14 us |    2 |  8.0566 |  1.2207 |       - |  66.66 KB |
| 'Serialize (System.Text.Json)'              | 185.5 us | 6.38 us |  8.51 us |    2 | 26.8555 | 26.8555 | 26.8555 |  83.51 KB |
| 'Serialize (System.Text.Json Source Gen)'   | 189.4 us | 3.05 us |  3.74 us |    2 | 26.8555 | 26.8555 | 26.8555 |  83.52 KB |
| 'Deserialize (System.Text.Json Source Gen)' | 226.1 us | 2.19 us |  2.78 us |    3 |  8.3008 |  1.4648 |       - |  69.27 KB |
| 'Deserialize (System.Text.Json)'            | 232.0 us | 2.33 us |  2.94 us |    3 |  8.3008 |  1.4648 |       - |  69.27 KB |
| 'Serialize (Newtonsoft.Json)'               | 321.3 us | 8.29 us | 10.18 us |    4 | 26.8555 | 26.8555 | 26.8555 | 231.89 KB |
| 'Deserialize (Newtonsoft.Json)'             | 392.0 us | 3.71 us |  4.69 us |    5 | 12.6953 |  1.9531 |       - | 106.24 KB |
```

### Benchmark Results (Cycle Detection On)

```
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26100.7462/24H2/2024Update/HudsonValley)
AMD Ryzen 9 9950X 4.30GHz, 1 CPU, 32 logical and 16 physical cores
.NET SDK 10.0.101
  [Host]     : .NET 10.0.1 (10.0.1, 10.0.125.57005), X64 RyuJIT x86-64-v4
  Job-FBVPNM : .NET 10.0.1 (10.0.1, 10.0.125.57005), X64 RyuJIT x86-64-v4

IterationCount=25

| Method                                      | Mean      | Error    | StdDev   | Rank | Gen0    | Gen1    | Gen2    | Allocated |
|-------------------------------------------- |----------:|---------:|---------:|-----:|--------:|--------:|--------:|----------:|
| 'Deserialize (GaldrJson)'                   |  67.01 us | 1.957 us | 2.612 us |    1 |  4.1504 |  0.7324 |       - |  69.01 KB |
| 'Serialize (GaldrJson)'                     |  68.66 us | 1.232 us | 1.644 us |    1 | 26.9775 | 26.9775 | 26.9775 |  96.09 KB |
| 'Deserialize (System.Text.Json Source Gen)' |  83.79 us | 0.454 us | 0.523 us |    2 |  4.1504 |  0.8545 |       - |  69.39 KB |
| 'Deserialize (System.Text.Json)'            |  86.21 us | 1.084 us | 1.447 us |    2 |  3.9063 |  0.4883 |       - |  69.39 KB |
| 'Serialize (System.Text.Json)'              |  88.42 us | 1.863 us | 2.423 us |    2 | 30.2734 | 30.2734 | 30.2734 |  119.2 KB |
| 'Serialize (System.Text.Json Source Gen)'   |  93.11 us | 2.820 us | 3.765 us |    2 | 30.2734 | 30.2734 | 30.2734 | 119.21 KB |
| 'Serialize (Newtonsoft.Json)'               | 119.01 us | 3.687 us | 4.922 us |    3 | 26.8555 | 26.8555 | 26.8555 | 231.89 KB |
| 'Deserialize (Newtonsoft.Json)'             | 142.24 us | 2.221 us | 2.809 us |    4 |  6.3477 |  1.2207 |       - | 106.24 KB |
```

*Note: System.Text.Json uses `ReferenceHandler.Preserve` (writes $id/$ref), GaldrJson uses `DetectCycles = true` (throws on cycle). Both prevent infinite loops but with different approaches.*

## How It Works

GaldrJson uses Roslyn source generators to analyze types marked with `[GaldrJsonSerializable]` at compile time. For each type, it generates:

1. **Strongly-typed converters** - No reflection or dynamic code
2. **UTF-8 property name caches** - Fast property matching
3. **Optimized read/write methods** - Specialized for each type
4. **Helper methods** - For collections and dictionaries

All generated code is emitted as part of your assembly, making it compatible with Native AOT and trimming.

### Example Generated Code (Simplified)

```csharp
// For a type like:
[GaldrJsonSerializable]
public class Product { public int Id { get; set; } }

// GaldrJson generates:
internal sealed class ProductJsonConverter : JsonConverter<Product>
{
    private static readonly byte[] Prop_Id_Camel = UTF8("id");

    public override Product Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
    {
        // Optimized UTF-8 reading
    }

    public override void Write(Utf8JsonWriter writer, Product value, JsonSerializerOptions options)
    {
        // Optimized UTF-8 writing
    }
}
```

## Limitations

**Not Currently Supported:**

-   `DateOnly` and `TimeOnly` (requires multi-targeting, planned for future release)
-   Reflection-based serialization (by design)
-   Dynamic types or `ExpandoObject`
-   Polymorphic serialization with type discriminators
-   Custom converters

**By Design:**

-   Types must be known at compile time
-   Types must have a parameterless constructor (or use init properties)
-   Properties must be public with getters

## Configuration Options

```csharp
var options = new GaldrJsonOptions
{
    // Property naming policy (default: Exact)
    PropertyNamingPolicy = PropertyNamingPolicy.CamelCase,

    // Write indented/pretty JSON (default: false in Release, true in Debug)
    WriteIndented = true,

    // Case-insensitive property matching during deserialization (default: false)
    PropertyNameCaseInsensitive = false,

    // Detect and throw on circular references (default: false)
    DetectCycles = true
};
```

## License

This project is licensed under the MIT License - see the [LICENSE.txt](LICENSE.txt) file for details.

## Contributing

Contributions are welcome! This is an initial release, and while thoroughly tested, there may be edge cases or features that could be improved.

### Reporting Issues

If you encounter a bug or have a feature request, please open an issue on GitHub with:

-   A clear description of the problem
-   Sample code that reproduces the issue
-   Expected vs. actual behavior

### Building from Source

```bash
git clone https://github.com/rthomasv3/GaldrJson.git
cd GaldrJson
dotnet build
dotnet test
```

## Acknowledgments

GaldrJson uses System.Text.Json's `Utf8JsonReader` and `Utf8JsonWriter` for low-level JSON parsing and writing, ensuring correctness and performance.
