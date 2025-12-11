# GaldrJson

**Blazing-fast, AOT-native JSON serialization for .NET**

GaldrJson is a high-performance JSON serialization library that uses source generation to provide zero-reflection, compile-time type safety. Designed for Native AOT scenarios but works seamlessly across all of .NET.

## Features

**Zero Reflection** All serialization code is generated at compile time
**Native AOT Ready** Full compatibility with Native AOT compilation
**High Performance** Competitive with System.Text.Json, faster than Newtonsoft.Json
**Simple API** Minimal configuration, maximum productivity
**Flexible Naming Policies** camelCase, snake_case, kebab-case, exact, or custom
**ASP.NET Core Integration** First-class support for Minimal APIs
**Comprehensive Type Support** Primitives, collections, dictionaries, nested objects, nullables, enums
**Cycle Detection** Optional circular reference detection

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

## Supported Types

GaldrJson supports a comprehensive set of .NET types:

**Primitives**: `int`, `long`, `short`, `byte`, `sbyte`, `uint`, `ulong`, `ushort`, `float`, `double`, `decimal`, `bool`, `char`, `string`

**System Types**: `DateTime`, `DateTimeOffset`, `TimeSpan`, `Guid`

**Collections**: `List<T>`, `T[]`, `IList<T>`, `ICollection<T>`, `IEnumerable<T>`

**Dictionaries**: `Dictionary<TKey, TValue>`, `IDictionary<TKey, TValue>`, `IReadOnlyDictionary<TKey, TValue>`

**Special**: Nullable value types, Enums (as integers), Nested objects, `byte[]` (Base64)

## Performance

GaldrJson uses compile-time code generation to achieve performance competitive with System.Text.Json while maintaining full AOT compatibility.

### Benchmark Results

| Method                           |      Mean |    Error |   StdDev |    Median | Rank |    Gen0 |    Gen1 |    Gen2 | Allocated |
| -------------------------------- | --------: | -------: | -------: | --------: | ---: | ------: | ------: | ------: | --------: |
| 'Deserialize (GaldrJson)'        |  62.58 us | 1.211 us | 1.812 us |  61.58 us |    1 |  4.1504 |  0.7324 |       - |  69.01 KB |
| 'Serialize (GaldrJson)'          |  70.20 us | 1.389 us | 1.600 us |  69.95 us |    2 | 26.9775 | 26.9775 | 26.9775 |  96.09 KB |
| 'Serialize (System.Text.Json)'   |  86.88 us | 1.734 us | 2.129 us |  86.41 us |    3 | 30.2734 | 30.2734 | 30.2734 | 119.21 KB |
| 'Deserialize (System.Text.Json)' |  87.88 us | 1.642 us | 1.536 us |  87.93 us |    3 |  3.9063 |  0.4883 |       - |  69.39 KB |
| 'Serialize (Newtonsoft.Json)'    | 120.66 us | 2.377 us | 5.317 us | 123.46 us |    4 | 26.9775 | 26.9775 | 26.9775 | 231.89 KB |
| 'Deserialize (Newtonsoft.Json)'  | 141.33 us | 2.802 us | 3.335 us | 140.94 us |    5 |  6.3477 |  1.2207 |       - | 106.24 KB |

_Benchmarks performed on .NET 10.0 with a complex object graph (50 products, 30 orders). See Tests/GaldrJson.PerformanceTests_

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

    // Case-insensitive property matching during deserialization (default: true)
    PropertyNameCaseInsensitive = true,

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
