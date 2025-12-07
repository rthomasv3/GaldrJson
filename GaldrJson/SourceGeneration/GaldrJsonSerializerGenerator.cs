using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using GaldrJson.SourceGeneration;
using TypeInfo = GaldrJson.SourceGeneration.TypeInfo;

[Generator]
public class GaldrJsonSerializerGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        //System.Diagnostics.Debugger.Launch();

        // Find all types marked with [GaldrJsonSerializable] attribute
        IncrementalValueProvider<ImmutableArray<TypeInfo>> typesWithAttribute = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: (node, _) => IsTypeWithGaldrJsonSerializableAttribute(node),
                transform: (ctx, _) => GetTypeInfoFromDeclaration(ctx))
            .Where(typeInfo => typeInfo != null)
            .Collect();

        // Generate serialization code
        context.RegisterSourceOutput(typesWithAttribute, GenerateSerializers);
    }

    private static bool IsTypeWithGaldrJsonSerializableAttribute(SyntaxNode node)
    {
        // Check if this is a type declaration (class, struct, or record)
        if (!(node is TypeDeclarationSyntax typeDeclaration))
            return false;

        // Check if it has any attributes
        return typeDeclaration.AttributeLists.Count > 0;
    }

    private static TypeInfo GetTypeInfoFromDeclaration(GeneratorSyntaxContext context)
    {
        TypeDeclarationSyntax typeDeclaration = (TypeDeclarationSyntax)context.Node;
        SemanticModel semanticModel = context.SemanticModel;

        // Get the type symbol
        INamedTypeSymbol typeSymbol = semanticModel.GetDeclaredSymbol(typeDeclaration) as INamedTypeSymbol;
        if (typeSymbol == null)
            return null;

        // Check if the type has the GaldrJsonSerializable attribute
        bool hasAttribute = typeSymbol.GetAttributes().Any(attr =>
            attr.AttributeClass?.Name == "GaldrJsonSerializable" ||
            attr.AttributeClass?.Name == "GaldrJsonSerializableAttribute");

        if (!hasAttribute)
            return null;

        // Extract type info
        return ExtractTypeInfo(typeSymbol);
    }

    private static TypeInfo ExtractTypeInfo(ITypeSymbol typeSymbol)
    {
        TypeInfo typeInfo = new TypeInfo
        {
            FullName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            Name = typeSymbol.Name,
            Namespace = typeSymbol.ContainingNamespace?.ToDisplayString() ?? "Global",
            Properties = new List<PropertyInfo>()
        };

        // Extract all public properties
        ImmutableArray<ISymbol> members = typeSymbol.GetMembers();
        foreach (ISymbol member in members)
        {
            if (member is IPropertySymbol property &&
                property.DeclaredAccessibility == Accessibility.Public &&
                property.GetMethod != null &&
                !property.IsStatic &&
                !property.IsIndexer &&
                !ShouldIgnoreProperty(property))
            {
                typeInfo.Properties.Add(new PropertyInfo
                {
                    Name = property.Name,
                    Type = property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    TypeSymbol = property.Type,
                    JsonName = GetJsonPropertyName(property),
                    CanWrite = property.SetMethod != null && property.SetMethod.DeclaredAccessibility == Accessibility.Public
                });
            }
        }

        return typeInfo;
    }

    private static string GetJsonPropertyName(IPropertySymbol property)
    {
        // Check for GaldrJsonPropertyName attribute first
        AttributeData galdrPropertyAttr = property.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "GaldrJsonPropertyNameAttribute" ||
                                 a.AttributeClass?.Name == "GaldrJsonPropertyName");

        if (galdrPropertyAttr?.ConstructorArguments.Length > 0)
        {
            return galdrPropertyAttr.ConstructorArguments[0].Value?.ToString() ?? property.Name;
        }

        // Fall back to System.Text.Json attribute for compatibility
        AttributeData jsonPropertyAttr = property.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "System.Text.Json.Serialization.JsonPropertyNameAttribute");

        if (jsonPropertyAttr?.ConstructorArguments.Length > 0)
        {
            return jsonPropertyAttr.ConstructorArguments[0].Value?.ToString() ?? property.Name;
        }

        // Default to camelCase
        return ToCamelCase(property.Name);
    }

    private static bool ShouldIgnoreProperty(IPropertySymbol property)
    {
        // Check for GaldrJsonIgnore attribute
        bool hasGaldrIgnore = property.GetAttributes()
            .Any(a => a.AttributeClass?.Name == "GaldrJsonIgnoreAttribute" ||
                      a.AttributeClass?.Name == "GaldrJsonIgnore");

        if (hasGaldrIgnore)
            return true;

        // Fall back to System.Text.Json attribute for compatibility
        bool hasJsonIgnore = property.GetAttributes()
            .Any(a => a.AttributeClass?.ToDisplayString() == "System.Text.Json.Serialization.JsonIgnoreAttribute");

        return hasJsonIgnore;
    }

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name) || char.IsLower(name[0]))
            return name;

        return char.ToLowerInvariant(name[0]) + name.Substring(1);
    }

    private static bool ShouldSerializeType(ITypeSymbol typeSymbol)
    {
        // Skip primitive types, enums, and strings
        if (typeSymbol.SpecialType != SpecialType.None ||
            typeSymbol.TypeKind == Microsoft.CodeAnalysis.TypeKind.Enum ||
            typeSymbol.Name == "String")
            return false;

        // Handle nullable types - unwrap and check the underlying type
        if (typeSymbol.NullableAnnotation == NullableAnnotation.Annotated &&
            typeSymbol is INamedTypeSymbol namedType &&
            namedType.TypeArguments.Length == 1)
        {
            return ShouldSerializeType(namedType.TypeArguments[0]);
        }

        // Skip system types like Guid, DateTime, etc. that have built-in JSON support
        string fullName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        if ((fullName.StartsWith("System.") || fullName.StartsWith("global::System.")) &&
            (fullName.Contains("Guid") || fullName.Contains("DateTime") || fullName.Contains("TimeSpan") || fullName.Contains("DateTimeOffset")))
            return false;

        return true;
    }

    private static bool IsCollectionType(ITypeSymbol typeSymbol)
    {
        // Handle arrays
        if (typeSymbol.TypeKind == Microsoft.CodeAnalysis.TypeKind.Array)
            return true;

        // Handle generic collections
        if (typeSymbol is INamedTypeSymbol namedTypeSymbol)
        {
            string fullName = namedTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            // Remove global:: prefix for consistent comparison
            string normalizedName = fullName.StartsWith("global::") ? fullName.Substring(8) : fullName;

            // Check for common collection types - use StartsWith to avoid matching Dictionary<string, List<int>>
            if (normalizedName.StartsWith("System.Collections.Generic.List<") ||
                normalizedName.StartsWith("System.Collections.Generic.IList<") ||
                normalizedName.StartsWith("System.Collections.Generic.ICollection<") ||
                normalizedName.StartsWith("System.Collections.Generic.IEnumerable<"))
                return true;
        }

        return false;
    }

    private static bool IsDictionaryType(ITypeSymbol typeSymbol)
    {
        if (typeSymbol is INamedTypeSymbol namedTypeSymbol)
        {
            string fullName = namedTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            // Remove global:: prefix for consistent comparison
            string normalizedName = fullName.StartsWith("global::") ? fullName.Substring(8) : fullName;

            return normalizedName.StartsWith("System.Collections.Generic.Dictionary<") ||
                   normalizedName.StartsWith("System.Collections.Generic.IDictionary<") ||
                   normalizedName.StartsWith("System.Collections.Generic.IReadOnlyDictionary<");
        }
        return false;
    }

    private static ITypeSymbol GetCollectionElementType(ITypeSymbol collectionType)
    {
        // Handle arrays
        if (collectionType is IArrayTypeSymbol arrayType)
            return arrayType.ElementType;

        // Handle generic collections
        if (collectionType is INamedTypeSymbol namedType && namedType.TypeArguments.Length == 1)
            return namedType.TypeArguments[0];

        return null;
    }

    private static (ITypeSymbol keyType, ITypeSymbol valueType)? GetDictionaryTypes(ITypeSymbol dictionaryType)
    {
        if (dictionaryType is INamedTypeSymbol namedType && namedType.TypeArguments.Length == 2)
            return (namedType.TypeArguments[0], namedType.TypeArguments[1]);

        return null;
    }

    private static List<TypeInfo> DiscoverAllTypes(ImmutableArray<TypeInfo> rootTypes)
    {
        var allTypes = new Dictionary<string, TypeInfo>();
        var typesToProcess = new Queue<TypeInfo>();

        // Start with the root types
        foreach (var rootType in rootTypes.Where(t => t != null))
        {
            if (!allTypes.ContainsKey(rootType.FullName))
            {
                allTypes[rootType.FullName] = rootType;
                typesToProcess.Enqueue(rootType);
            }
        }

        // Process types recursively
        while (typesToProcess.Count > 0)
        {
            TypeInfo currentType = typesToProcess.Dequeue();

            foreach (PropertyInfo property in currentType.Properties)
            {
                // Handle collections
                if (IsCollectionType(property.TypeSymbol))
                {
                    ITypeSymbol elementType = GetCollectionElementType(property.TypeSymbol);

                    if (elementType != null && ShouldSerializeType(elementType))
                    {
                        string elementTypeFullName = elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                        if (!allTypes.ContainsKey(elementTypeFullName))
                        {
                            TypeInfo elementTypeInfo = ExtractTypeInfo(elementType);

                            if (elementTypeInfo != null)
                            {
                                allTypes[elementTypeFullName] = elementTypeInfo;
                                typesToProcess.Enqueue(elementTypeInfo);
                            }
                        }
                    }
                }
                // Handle dictionaries
                else if (IsDictionaryType(property.TypeSymbol))
                {
                    var dictTypes = GetDictionaryTypes(property.TypeSymbol);
                    if (dictTypes.HasValue)
                    {
                        // Handle dictionary key type (if complex)
                        if (ShouldSerializeType(dictTypes.Value.keyType))
                        {
                            string keyTypeFullName = dictTypes.Value.keyType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                            if (!allTypes.ContainsKey(keyTypeFullName))
                            {
                                TypeInfo keyTypeInfo = ExtractTypeInfo(dictTypes.Value.keyType);
                                if (keyTypeInfo != null)
                                {
                                    allTypes[keyTypeFullName] = keyTypeInfo;
                                    typesToProcess.Enqueue(keyTypeInfo);
                                }
                            }
                        }

                        // Handle dictionary value type (if complex)
                        if (ShouldSerializeType(dictTypes.Value.valueType))
                        {
                            string valueTypeFullName = dictTypes.Value.valueType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                            if (!allTypes.ContainsKey(valueTypeFullName))
                            {
                                TypeInfo valueTypeInfo = ExtractTypeInfo(dictTypes.Value.valueType);
                                if (valueTypeInfo != null)
                                {
                                    allTypes[valueTypeFullName] = valueTypeInfo;
                                    typesToProcess.Enqueue(valueTypeInfo);
                                }
                            }
                        }
                    }
                }
                // Handle non-collection complex types
                else if (ShouldSerializeType(property.TypeSymbol))
                {
                    string propertyTypeFullName = property.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                    // If we haven't processed this type yet, extract its info and add to queue
                    if (!allTypes.ContainsKey(propertyTypeFullName))
                    {
                        TypeInfo propertyTypeInfo = ExtractTypeInfo(property.TypeSymbol);

                        if (propertyTypeInfo != null)
                        {
                            allTypes[propertyTypeFullName] = propertyTypeInfo;
                            typesToProcess.Enqueue(propertyTypeInfo);
                        }
                    }
                }
            }
        }

        return allTypes.Values.ToList();
    }

    private static void GenerateSerializers(SourceProductionContext context, ImmutableArray<TypeInfo> types)
    {
        // Create metadata cache for the entire generation process
        TypeMetadataCache metadataCache = new TypeMetadataCache();

        List<TypeInfo> allTypes = DiscoverAllTypes(types);

        if (allTypes.Count == 0)
            return;

        int fieldCounter = 0;
        foreach (var typeInfo in allTypes)
        {
            // Generate unique converter name based on full qualified name
            typeInfo.ConverterName = typeInfo.FullName
                .Replace("global::", "")
                .Replace(".", "_")
                .Replace("<", "_")
                .Replace(">", "_")
                .Replace(",", "_")
                .Replace(" ", "") + "JsonConverter";

            // Generate unique field name for cached instance
            typeInfo.FieldName = $"_converter{fieldCounter++}";
        }

        IndentedStringBuilder builder = new IndentedStringBuilder();

        // Generate the file header
        builder.AppendLine("// <auto-generated/>");
        builder.AppendLine("#nullable disable");
        builder.AppendLine("using System;");
        builder.AppendLine("using System.Collections.Generic;");
        builder.AppendLine("using System.Text.Json;");
        builder.AppendLine("using System.Text.Json.Serialization;");
        builder.AppendLine("using System.Runtime.CompilerServices;");
        builder.AppendLine("using GaldrJson;");
        builder.AppendLine();

        using (builder.Block("namespace GaldrJson.Generated"))
        {
            // Generate collection helper methods for all element types used in collections
            GenerateCollectionHelpers(builder, allTypes, metadataCache);

            // Generate dictionary helper methods for all element types used in dictionaries
            GenerateDictionaryHelpers(builder, allTypes, metadataCache);

            // Generate converter for each type
            foreach (TypeInfo typeInfo in allTypes)
            {
                GenerateTypeConverter(builder, typeInfo, metadataCache);
            }

            // Generate the serializer implementation
            GenerateSerializerImplementation(builder, allTypes);

            // Generate the module initializer
            GenerateModuleInitializer(builder);
        }

        context.AddSource("GaldrJsonSerializers.g.cs", SourceText.From(builder.ToString(), Encoding.UTF8));
    }

    private static void GenerateCollectionHelpers(IndentedStringBuilder builder, List<TypeInfo> allTypes, TypeMetadataCache metadataCache)
    {
        var elementTypesUsedInCollections = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);

        // Find all element types used in collections across all types
        foreach (var typeInfo in allTypes)
        {
            foreach (var property in typeInfo.Properties)
            {
                if (IsCollectionType(property.TypeSymbol))
                {
                    var elementType = GetCollectionElementType(property.TypeSymbol);
                    if (elementType != null)
                    {
                        elementTypesUsedInCollections.Add(elementType);
                    }
                }
            }
        }

        using (builder.Block("internal static class CollectionHelpers"))
        {
            // Generate helper methods for each element type
            foreach (var elementType in elementTypesUsedInCollections)
            {
                GenerateCollectionHelperMethods(builder, elementType, metadataCache);
            }
        }

        builder.AppendLine();
    }

    private static void GenerateCollectionHelperMethods(IndentedStringBuilder builder, ITypeSymbol elementTypeSymbol, TypeMetadataCache metadataCache)
    {
        // Get metadata for the element type
        var elementMetadata = metadataCache.GetOrCreate(elementTypeSymbol);
        string safeTypeName = elementMetadata.SafeName;
        string elementTypeDisplayName = elementMetadata.FullyQualifiedName;

        // Create emitter for the element type
        var elementEmitter = CodeEmitter.Create(elementMetadata);

        // Generate Read method
        using (builder.Block($"public static object ReadCollection_{safeTypeName}(ref Utf8JsonReader reader, JsonSerializerOptions options, bool isArray)"))
        {
            builder.AppendLine("if (reader.TokenType == JsonTokenType.Null)");
            using (builder.Indent())
                builder.AppendLine("return null;");
            builder.AppendLine();
            builder.AppendLine("if (reader.TokenType != JsonTokenType.StartArray)");
            using (builder.Indent())
                builder.AppendLine("throw new JsonException(\"Expected StartArray token\");");
            builder.AppendLine();
            builder.AppendLine($"var list = new List<{elementTypeDisplayName}>();");
            builder.AppendLine();

            using (builder.Block("while (reader.Read())"))
            {
                builder.AppendLine("if (reader.TokenType == JsonTokenType.EndArray)");
                using (builder.Indent())
                    builder.AppendLine("break;");
                builder.AppendLine();

                // Generate element reading code using CodeEmitter
                string elementReadCode = elementEmitter.EmitRead("reader");
                builder.AppendLine($"var element = {elementReadCode};");
                builder.AppendLine("list.Add(element);");
            }

            builder.AppendLine();
            builder.AppendLine("return isArray ? list.ToArray() : list;");
        }

        builder.AppendLine();

        // Generate Write method
        using (builder.Block($"public static void WriteCollection_{safeTypeName}(Utf8JsonWriter writer, object collection, JsonSerializerOptions options)"))
        {
            using (builder.Block("if (collection == null)"))
            {
                builder.AppendLine("writer.WriteNullValue();");
                builder.AppendLine("return;");
            }

            builder.AppendLine();
            builder.AppendLine("writer.WriteStartArray();");
            builder.AppendLine();

            using (builder.Block($"foreach (var item in (System.Collections.Generic.IEnumerable<{elementTypeDisplayName}>)collection)"))
            {
                // Generate element writing code using CodeEmitter (for array elements, no property name)
                string elementWriteCode = elementEmitter.EmitWrite("writer", "item", null);
                builder.AppendLine(elementWriteCode);
            }

            builder.AppendLine();
            builder.AppendLine("writer.WriteEndArray();");
        }

        builder.AppendLine();
    }

    private static void GenerateDictionaryHelpers(IndentedStringBuilder builder, List<TypeInfo> allTypes, TypeMetadataCache metadataCache)
    {
        var dictionaryTypesUsed = new HashSet<(ITypeSymbol keyType, ITypeSymbol valueType)>(new DictionaryTypesComparer());

        // Find all dictionary types used
        foreach (var typeInfo in allTypes)
        {
            foreach (var property in typeInfo.Properties)
            {
                if (IsDictionaryType(property.TypeSymbol))
                {
                    var dictTypes = GetDictionaryTypes(property.TypeSymbol);
                    if (dictTypes.HasValue)
                    {
                        dictionaryTypesUsed.Add((dictTypes.Value.keyType, dictTypes.Value.valueType));
                    }
                }
            }
        }

        if (dictionaryTypesUsed.Count > 0)
        {
            using (builder.Block("internal static class DictionaryHelpers"))
            {
                foreach (var (keyType, valueType) in dictionaryTypesUsed)
                {
                    GenerateDictionaryHelperMethods(builder, keyType, valueType, metadataCache);
                }
            }

            builder.AppendLine();
        }
    }

    // Comparer for dictionary type tuples using symbol equality
    private class DictionaryTypesComparer : IEqualityComparer<(ITypeSymbol keyType, ITypeSymbol valueType)>
    {
        public bool Equals((ITypeSymbol keyType, ITypeSymbol valueType) x, (ITypeSymbol keyType, ITypeSymbol valueType) y)
        {
            return SymbolEqualityComparer.Default.Equals(x.keyType, y.keyType) &&
                   SymbolEqualityComparer.Default.Equals(x.valueType, y.valueType);
        }

        public int GetHashCode((ITypeSymbol keyType, ITypeSymbol valueType) obj)
        {
            int hash1 = SymbolEqualityComparer.Default.GetHashCode(obj.keyType);
            int hash2 = SymbolEqualityComparer.Default.GetHashCode(obj.valueType);
            return hash1 ^ hash2;
        }
    }

    private static void GenerateDictionaryHelperMethods(IndentedStringBuilder builder, ITypeSymbol keyTypeSymbol, ITypeSymbol valueTypeSymbol, TypeMetadataCache metadataCache)
    {
        var keyMetadata = metadataCache.GetOrCreate(keyTypeSymbol);
        var valueMetadata = metadataCache.GetOrCreate(valueTypeSymbol);

        string safeTypeName = $"{keyMetadata.SafeName}_{valueMetadata.SafeName}";
        string keyTypeName = keyMetadata.FullyQualifiedName;
        string valueTypeName = valueMetadata.FullyQualifiedName;

        var valueEmitter = CodeEmitter.Create(valueMetadata);

        // Read method
        using (builder.Block($"public static Dictionary<{keyTypeName}, {valueTypeName}> ReadDictionary_{safeTypeName}(ref Utf8JsonReader reader, JsonSerializerOptions options)"))
        {
            builder.AppendLine("if (reader.TokenType == JsonTokenType.Null)");
            using (builder.Indent())
                builder.AppendLine("return null;");
            builder.AppendLine();
            builder.AppendLine("if (reader.TokenType != JsonTokenType.StartObject)");
            using (builder.Indent())
                builder.AppendLine("throw new JsonException(\"Expected StartObject token for dictionary\");");
            builder.AppendLine();
            builder.AppendLine($"var dictionary = new Dictionary<{keyTypeName}, {valueTypeName}>();");
            builder.AppendLine();

            using (builder.Block("while (reader.Read())"))
            {
                builder.AppendLine("if (reader.TokenType == JsonTokenType.EndObject)");
                using (builder.Indent())
                    builder.AppendLine("break;");
                builder.AppendLine();
                builder.AppendLine("if (reader.TokenType != JsonTokenType.PropertyName)");
                using (builder.Indent())
                    builder.AppendLine("continue;");
                builder.AppendLine();
                builder.AppendLine("var keyString = reader.GetString() ?? string.Empty;");

                // Key conversion logic
                string keyConversion = GetKeyConversionCode(keyMetadata);
                builder.AppendLine($"var key = {keyConversion};");

                builder.AppendLine("reader.Read();");

                // Value reading logic using CodeEmitter
                string valueReadCode = valueEmitter.EmitRead("reader");
                builder.AppendLine($"var value = {valueReadCode};");

                builder.AppendLine("dictionary[key] = value;");
            }

            builder.AppendLine();
            builder.AppendLine("return dictionary;");
        }

        builder.AppendLine();

        // Write method
        using (builder.Block($"public static void WriteDictionary_{safeTypeName}(Utf8JsonWriter writer, Dictionary<{keyTypeName}, {valueTypeName}> dictionary, JsonSerializerOptions options)"))
        {
            using (builder.Block("if (dictionary == null)"))
            {
                builder.AppendLine("writer.WriteNullValue();");
                builder.AppendLine("return;");
            }

            builder.AppendLine();
            builder.AppendLine("writer.WriteStartObject();");
            builder.AppendLine();

            using (builder.Block("foreach (var kvp in dictionary)"))
            {
                if (keyMetadata.IsEnum)
                {
                    builder.AppendLine("var keyString = ((int)kvp.Key).ToString();");
                }
                else
                {
                    builder.AppendLine("var keyString = kvp.Key.ToString() ?? string.Empty;");
                }

                builder.AppendLine("writer.WritePropertyName(keyString);");

                string valueWriteCode = valueEmitter.EmitWrite("writer", "kvp.Value", null);
                builder.AppendLine(valueWriteCode);
            }

            builder.AppendLine();
            builder.AppendLine("writer.WriteEndObject();");
        }

        builder.AppendLine();
    }

    private static string GetKeyConversionCode(TypeMetadata keyMetadata)
    {
        if (keyMetadata.IsEnum)
        {
            return $"({keyMetadata.FullyQualifiedName})int.Parse(keyString)";
        }

        // For primitive types, use SpecialType
        if (keyMetadata.IsPrimitive)
        {
            switch (keyMetadata.Symbol.SpecialType)
            {
                case SpecialType.System_String:
                    return "keyString";
                case SpecialType.System_Int16:
                    return "short.Parse(keyString)";
                case SpecialType.System_Int32:
                    return "int.Parse(keyString)";
                case SpecialType.System_Int64:
                    return "long.Parse(keyString)";
                case SpecialType.System_Byte:
                    return "byte.Parse(keyString)";
                default:
                    return "keyString";
            }
        }

        // For system types
        if (keyMetadata.IsSystemType)
        {
            var typeName = keyMetadata.FullyQualifiedName;
            if (typeName.Contains("Guid"))
                return "Guid.Parse(keyString)";
        }

        return "keyString";
    }

    private static void GenerateTypeConverter(IndentedStringBuilder builder, TypeInfo typeInfo, TypeMetadataCache metadataCache)
    {
        string typeName = typeInfo.FullName;

        using (builder.Block($"internal sealed class {typeInfo.ConverterName} : JsonConverter<{typeName}>"))
        {
            // Generate Read method
            using (builder.Block($"public override {typeName} Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)"))
            {
                builder.AppendLine("if (reader.TokenType == JsonTokenType.Null)");
                using (builder.Indent())
                    builder.AppendLine("return null;");
                builder.AppendLine();
                builder.AppendLine("if (reader.TokenType != JsonTokenType.StartObject)");
                using (builder.Indent())
                    builder.AppendLine("throw new JsonException(\"Expected StartObject token\");");
                builder.AppendLine();
                builder.AppendLine($"var result = new {typeName}();");
                builder.AppendLine();

                using (builder.Block("while (reader.Read())"))
                {
                    builder.AppendLine("if (reader.TokenType == JsonTokenType.EndObject)");
                    using (builder.Indent())
                        builder.AppendLine("return result;");
                    builder.AppendLine();
                    builder.AppendLine("if (reader.TokenType != JsonTokenType.PropertyName)");
                    using (builder.Indent())
                        builder.AppendLine("continue;");
                    builder.AppendLine();
                    builder.AppendLine("var propertyName = reader.GetString();");
                    builder.AppendLine("reader.Read();");
                    builder.AppendLine();

                    using (builder.Block("switch (propertyName)"))
                    {
                        // Generate case for each property
                        foreach (PropertyInfo prop in typeInfo.Properties.Where(p => p.CanWrite))
                        {
                            builder.AppendLine($"case \"{prop.JsonName}\":");
                            using (builder.Indent())
                            {
                                GeneratePropertyRead(builder, prop, "result", metadataCache);
                                builder.AppendLine("break;");
                            }
                        }

                        builder.AppendLine("default:");
                        using (builder.Indent())
                        {
                            builder.AppendLine("reader.Skip();");
                            builder.AppendLine("break;");
                        }
                    }
                }

                builder.AppendLine();
                builder.AppendLine("throw new JsonException(\"Expected EndObject token\");");
            }

            builder.AppendLine();

            // Generate Write method
            using (builder.Block($"public override void Write(Utf8JsonWriter writer, {typeName} value, JsonSerializerOptions options)"))
            {
                using (builder.Block("if (value == null)"))
                {
                    builder.AppendLine("writer.WriteNullValue();");
                    builder.AppendLine("return;");
                }

                builder.AppendLine();
                builder.AppendLine("writer.WriteStartObject();");

                foreach (PropertyInfo prop in typeInfo.Properties)
                {
                    GeneratePropertyWrite(builder, prop, metadataCache);
                }

                builder.AppendLine("writer.WriteEndObject();");
            }
        }

        builder.AppendLine();
    }

    private static void GeneratePropertyRead(IndentedStringBuilder builder, PropertyInfo property, string targetVariable, TypeMetadataCache metadataCache)
    {
        var propertyMetadata = metadataCache.GetOrCreate(property.TypeSymbol);
        var emitter = CodeEmitter.Create(propertyMetadata);
        var readCode = emitter.EmitRead("reader");

        builder.AppendLine($"{targetVariable}.{property.Name} = {readCode};");
    }

    private static void GeneratePropertyWrite(IndentedStringBuilder builder, PropertyInfo property, TypeMetadataCache metadataCache)
    {
        var propertyMetadata = metadataCache.GetOrCreate(property.TypeSymbol);
        var emitter = CodeEmitter.Create(propertyMetadata);
        var writeCode = emitter.EmitWrite("writer", $"value.{property.Name}", property.JsonName);

        builder.AppendLine(writeCode);
    }

    private static void GenerateSerializerImplementation(IndentedStringBuilder builder, List<TypeInfo> types)
    {
        using (builder.Block("internal class GeneratedJsonSerializer : IGaldrJsonTypeSerializer"))
        {
            // Generate cached converter fields
            foreach (TypeInfo type in types)
            {
                builder.AppendLine($"private static readonly {type.ConverterName} {type.FieldName} = new {type.ConverterName}();");
            }
            builder.AppendLine();

            // Add internal helper methods for reading/writing with converters
            using (builder.Block("internal static object ReadWithConverter(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)"))
            {
                using (builder.Block("switch (type)"))
                {
                    foreach (TypeInfo type in types)
                    {
                        builder.AppendLine($"case Type t when t == typeof({type.FullName}):");
                        using (builder.Indent())
                            builder.AppendLine($"return {type.FieldName}.Read(ref reader, typeof({type.FullName}), options);");
                    }

                    builder.AppendLine("default:");
                    using (builder.Indent())
                        builder.AppendLine("throw new NotSupportedException($\"Type {{type.FullName}} is not registered\");");
                }
            }

            builder.AppendLine();

            using (builder.Block("internal static void WriteWithConverter(Utf8JsonWriter writer, object value, Type type, JsonSerializerOptions options)"))
            {
                using (builder.Block("switch (type)"))
                {
                    foreach (TypeInfo type in types)
                    {
                        builder.AppendLine($"case Type t when t == typeof({type.FullName}):");
                        using (builder.Indent())
                        {
                            builder.AppendLine($"{type.FieldName}.Write(writer, ({type.FullName})value, options);");
                            builder.AppendLine("break;");
                        }
                    }

                    builder.AppendLine("default:");
                    using (builder.Indent())
                        builder.AppendLine("throw new NotSupportedException($\"Type {{type.FullName}} is not registered\");");
                }
            }

            builder.AppendLine();

            // CanSerialize method
            using (builder.Block("public bool CanSerialize(Type type)"))
            {
                foreach (TypeInfo type in types)
                {
                    builder.AppendLine($"if (type == typeof({type.FullName})) return true;");
                }
                builder.AppendLine("return false;");
            }

            builder.AppendLine();

            // Write Method
            using (builder.Block("public void Write(Utf8JsonWriter writer, object value, Type type, JsonSerializerOptions options)"))
            {
                builder.AppendLine("WriteWithConverter(writer, value, type, options);");
            }

            builder.AppendLine();

            // Serialize method
            using (builder.Block("public string Serialize(object value, Type type)"))
            {
                builder.AppendLine("using var stream = new System.IO.MemoryStream();");
                builder.AppendLine("using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });");
                builder.AppendLine();

                using (builder.Block("switch (type)"))
                {
                    foreach (TypeInfo type in types)
                    {
                        builder.AppendLine($"case Type t when t == typeof({type.FullName}):");
                        using (builder.Indent())
                        {
                            builder.AppendLine($"{type.FieldName}.Write(writer, ({type.FullName})value, JsonSerializerOptions.Default);");
                            builder.AppendLine("break;");
                        }
                    }

                    builder.AppendLine("default:");
                    using (builder.Indent())
                        builder.AppendLine("throw new NotSupportedException($\"Type {type} is not registered for serialization\");");
                }

                builder.AppendLine();
                builder.AppendLine("writer.Flush();");
                builder.AppendLine("return System.Text.Encoding.UTF8.GetString(stream.ToArray());");
            }

            builder.AppendLine();

            // Read Method
            using (builder.Block("public object Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)"))
            {
                builder.AppendLine("return ReadWithConverter(ref reader, type, options);");
            }

            builder.AppendLine();

            // Deserialize method
            using (builder.Block("public object Deserialize(string json, Type type)"))
            {
                builder.AppendLine("var bytes = System.Text.Encoding.UTF8.GetBytes(json);");
                builder.AppendLine("var reader = new Utf8JsonReader(bytes);");
                builder.AppendLine("reader.Read(); // Move to first token");
                builder.AppendLine();

                using (builder.Block("switch (type)"))
                {
                    foreach (TypeInfo type in types)
                    {
                        builder.AppendLine($"case Type t when t == typeof({type.FullName}):");
                        using (builder.Indent())
                            builder.AppendLine($"return {type.FieldName}.Read(ref reader, typeof({type.FullName}), JsonSerializerOptions.Default);");
                    }

                    builder.AppendLine("default:");
                    using (builder.Indent())
                        builder.AppendLine("throw new NotSupportedException($\"Type {type.FullName} is not registered for deserialization\");");
                }
            }
        }
    }

    private static void GenerateModuleInitializer(IndentedStringBuilder builder)
    {
        using (builder.Block("internal static class GaldrGeneratedInitializer"))
        {
            builder.AppendLine("[ModuleInitializer]");
            using (builder.Block("public static void Initialize()"))
            {
                builder.AppendLine("GaldrJsonSerializerRegistry.Register(new GeneratedJsonSerializer());");
            }
        }
    }
}
