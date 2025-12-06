using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using GaldrJson.SourceGeneration;

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

        //System.Diagnostics.Debugger.Launch();

        StringBuilder sb = new StringBuilder();

        // Generate the file header
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable disable");
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Text.Json;");
        sb.AppendLine("using System.Text.Json.Serialization;");
        sb.AppendLine("using System.Runtime.CompilerServices;");
        sb.AppendLine("using GaldrJson;");
        sb.AppendLine();
        sb.AppendLine("namespace GaldrJson.Generated");
        sb.AppendLine("{");

        // Generate collection helper methods for all element types used in collections
        GenerateCollectionHelpers(sb, allTypes, metadataCache);

        // Generate dictionary helper methods for all element types used in dictionaries
        GenerateDictionaryHelpers(sb, allTypes, metadataCache);

        // Generate converter for each type
        foreach (TypeInfo typeInfo in allTypes)
        {
            GenerateTypeConverter(sb, typeInfo, metadataCache);
        }

        // Generate the serializer implementation
        GenerateSerializerImplementation(sb, allTypes);

        // Generate the module initializer
        GenerateModuleInitializer(sb);

        sb.AppendLine("}");

        context.AddSource("GaldrJsonSerializers.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    private static void GenerateCollectionHelpers(StringBuilder sb, List<TypeInfo> allTypes, TypeMetadataCache metadataCache)
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

        sb.AppendLine("    internal static class CollectionHelpers");
        sb.AppendLine("    {");

        // Generate helper methods for each element type
        foreach (var elementType in elementTypesUsedInCollections)
        {
            GenerateCollectionHelperMethods(sb, elementType, metadataCache);
        }

        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void GenerateCollectionHelperMethods(StringBuilder sb, ITypeSymbol elementTypeSymbol, TypeMetadataCache metadataCache)
    {
        // Get metadata for the element type
        var elementMetadata = metadataCache.GetOrCreate(elementTypeSymbol);
        string safeTypeName = elementMetadata.SafeName;
        string elementTypeDisplayName = elementMetadata.FullyQualifiedName;

        // Create emitter for the element type
        var elementEmitter = CodeEmitter.Create(elementMetadata);

        // Generate Read method
        sb.AppendLine($"        public static object ReadCollection_{safeTypeName}(ref Utf8JsonReader reader, JsonSerializerOptions options, bool isArray)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (reader.TokenType == JsonTokenType.Null)");
        sb.AppendLine("                return null;");
        sb.AppendLine();
        sb.AppendLine("            if (reader.TokenType != JsonTokenType.StartArray)");
        sb.AppendLine("                throw new JsonException(\"Expected StartArray token\");");
        sb.AppendLine();
        sb.AppendLine($"            var list = new List<{elementTypeDisplayName}>();");
        sb.AppendLine();
        sb.AppendLine("            while (reader.Read())");
        sb.AppendLine("            {");
        sb.AppendLine("                if (reader.TokenType == JsonTokenType.EndArray)");
        sb.AppendLine("                    break;");
        sb.AppendLine();

        // Generate element reading code using CodeEmitter
        string elementReadCode = elementEmitter.EmitRead("reader");
        sb.AppendLine($"                var element = {elementReadCode};");
        sb.AppendLine("                list.Add(element);");

        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            return isArray ? list.ToArray() : list;");
        sb.AppendLine("        }");
        sb.AppendLine();

        // Generate Write method
        sb.AppendLine($"        public static void WriteCollection_{safeTypeName}(Utf8JsonWriter writer, object collection, JsonSerializerOptions options)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (collection == null)");
        sb.AppendLine("            {");
        sb.AppendLine("                writer.WriteNullValue();");
        sb.AppendLine("                return;");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            writer.WriteStartArray();");
        sb.AppendLine();
        sb.AppendLine($"            foreach (var item in (System.Collections.Generic.IEnumerable<{elementTypeDisplayName}>)collection)");
        sb.AppendLine("            {");

        // Generate element writing code using CodeEmitter (for array elements, no property name)
        string elementWriteCode = elementEmitter.EmitWrite("writer", "item", null);
        sb.AppendLine($"                {elementWriteCode}");

        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            writer.WriteEndArray();");
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    private static void GenerateDictionaryHelpers(StringBuilder sb, List<TypeInfo> allTypes, TypeMetadataCache metadataCache)
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
            sb.AppendLine("    internal static class DictionaryHelpers");
            sb.AppendLine("    {");

            foreach (var (keyType, valueType) in dictionaryTypesUsed)
            {
                GenerateDictionaryHelperMethods(sb, keyType, valueType, metadataCache);
            }

            sb.AppendLine("    }");
            sb.AppendLine();
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

    private static void GenerateDictionaryHelperMethods(StringBuilder sb, ITypeSymbol keyTypeSymbol, ITypeSymbol valueTypeSymbol, TypeMetadataCache metadataCache)
    {
        var keyMetadata = metadataCache.GetOrCreate(keyTypeSymbol);
        var valueMetadata = metadataCache.GetOrCreate(valueTypeSymbol);

        string safeTypeName = $"{keyMetadata.SafeName}_{valueMetadata.SafeName}";
        string keyTypeName = keyMetadata.FullyQualifiedName;
        string valueTypeName = valueMetadata.FullyQualifiedName;

        var valueEmitter = CodeEmitter.Create(valueMetadata);

        // Read method
        sb.AppendLine($"        public static Dictionary<{keyTypeName}, {valueTypeName}> ReadDictionary_{safeTypeName}(ref Utf8JsonReader reader, JsonSerializerOptions options)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (reader.TokenType == JsonTokenType.Null)");
        sb.AppendLine("                return null;");
        sb.AppendLine();
        sb.AppendLine("            if (reader.TokenType != JsonTokenType.StartObject)");
        sb.AppendLine("                throw new JsonException(\"Expected StartObject token for dictionary\");");
        sb.AppendLine();
        sb.AppendLine($"            var dictionary = new Dictionary<{keyTypeName}, {valueTypeName}>();");
        sb.AppendLine();
        sb.AppendLine("            while (reader.Read())");
        sb.AppendLine("            {");
        sb.AppendLine("                if (reader.TokenType == JsonTokenType.EndObject)");
        sb.AppendLine("                    break;");
        sb.AppendLine();
        sb.AppendLine("                if (reader.TokenType != JsonTokenType.PropertyName)");
        sb.AppendLine("                    continue;");
        sb.AppendLine();
        sb.AppendLine("                var keyString = reader.GetString() ?? string.Empty;");

        // Key conversion logic
        string keyConversion = GetKeyConversionCode(keyMetadata);
        sb.AppendLine($"                var key = {keyConversion};");

        sb.AppendLine("                reader.Read();");

        // Value reading logic using CodeEmitter
        string valueReadCode = valueEmitter.EmitRead("reader");
        sb.AppendLine($"                var value = {valueReadCode};");

        sb.AppendLine("                dictionary[key] = value;");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            return dictionary;");
        sb.AppendLine("        }");
        sb.AppendLine();

        // Write method
        sb.AppendLine($"        public static void WriteDictionary_{safeTypeName}(Utf8JsonWriter writer, Dictionary<{keyTypeName}, {valueTypeName}> dictionary, JsonSerializerOptions options)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (dictionary == null)");
        sb.AppendLine("            {");
        sb.AppendLine("                writer.WriteNullValue();");
        sb.AppendLine("                return;");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            writer.WriteStartObject();");
        sb.AppendLine();
        sb.AppendLine("            foreach (var kvp in dictionary)");
        sb.AppendLine("            {");

        if (keyMetadata.IsEnum)
        {
            sb.AppendLine("                var keyString = ((int)kvp.Key).ToString();");
        }
        else
        {
            sb.AppendLine("                var keyString = kvp.Key.ToString() ?? string.Empty;");
        }

        sb.AppendLine("                writer.WritePropertyName(keyString);");

        string valueWriteCode = valueEmitter.EmitWrite("writer", "kvp.Value", null);
        sb.AppendLine($"                {valueWriteCode}");

        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            writer.WriteEndObject();");
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    private static string GetKeyConversionCode(GaldrJson.SourceGeneration.TypeMetadata keyMetadata)
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



    private static void GenerateTypeConverter(StringBuilder sb, TypeInfo typeInfo, TypeMetadataCache metadataCache)
    {
        string converterName = $"{typeInfo.Name}JsonConverter";
        string typeName = typeInfo.FullName;

        sb.AppendLine($"    internal sealed class {converterName} : JsonConverter<{typeName}>");
        sb.AppendLine("    {");

        // Generate Read method
        sb.AppendLine($"        public override {typeName} Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (reader.TokenType == JsonTokenType.Null)");
        sb.AppendLine("                return null;");
        sb.AppendLine();
        sb.AppendLine("            if (reader.TokenType != JsonTokenType.StartObject)");
        sb.AppendLine("                throw new JsonException(\"Expected StartObject token\");");
        sb.AppendLine();
        sb.AppendLine($"            var result = new {typeName}();");
        sb.AppendLine();
        sb.AppendLine("            while (reader.Read())");
        sb.AppendLine("            {");
        sb.AppendLine("                if (reader.TokenType == JsonTokenType.EndObject)");
        sb.AppendLine("                    return result;");
        sb.AppendLine();
        sb.AppendLine("                if (reader.TokenType != JsonTokenType.PropertyName)");
        sb.AppendLine("                    continue;");
        sb.AppendLine();
        sb.AppendLine("                var propertyName = reader.GetString();");
        sb.AppendLine("                reader.Read();");
        sb.AppendLine();
        sb.AppendLine("                switch (propertyName)");
        sb.AppendLine("                {");

        // Generate case for each property
        foreach (PropertyInfo prop in typeInfo.Properties.Where(p => p.CanWrite))
        {
            sb.AppendLine($"                    case \"{prop.JsonName}\":");
            GeneratePropertyRead(sb, prop, "result", metadataCache);
            sb.AppendLine("                        break;");
        }

        sb.AppendLine("                    default:");
        sb.AppendLine("                        reader.Skip();");
        sb.AppendLine("                        break;");
        sb.AppendLine("                }");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            throw new JsonException(\"Expected EndObject token\");");
        sb.AppendLine("        }");
        sb.AppendLine();

        // Generate Write method
        sb.AppendLine($"        public override void Write(Utf8JsonWriter writer, {typeName} value, JsonSerializerOptions options)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (value == null)");
        sb.AppendLine("            {");
        sb.AppendLine("                writer.WriteNullValue();");
        sb.AppendLine("                return;");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            writer.WriteStartObject();");

        foreach (PropertyInfo prop in typeInfo.Properties)
        {
            GeneratePropertyWrite(sb, prop, metadataCache);
        }

        sb.AppendLine("            writer.WriteEndObject();");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void GeneratePropertyRead(StringBuilder sb, PropertyInfo property, string targetVariable, TypeMetadataCache metadataCache)
    {
        var propertyMetadata = metadataCache.GetOrCreate(property.TypeSymbol);
        var emitter = CodeEmitter.Create(propertyMetadata);
        var readCode = emitter.EmitRead("reader");

        sb.AppendLine($"                        {targetVariable}.{property.Name} = {readCode};");
    }

    private static string GetBaseTypeName(ITypeSymbol typeSymbol)
    {
        // Handle nullable types - get the underlying type name
        if (typeSymbol.NullableAnnotation == NullableAnnotation.Annotated &&
            typeSymbol is INamedTypeSymbol namedType &&
            namedType.TypeArguments.Length == 1)
        {
            return namedType.TypeArguments[0].Name;
        }

        return typeSymbol.Name;
    }

    private static void GeneratePropertyWrite(StringBuilder sb, PropertyInfo property, TypeMetadataCache metadataCache)
    {
        var propertyMetadata = metadataCache.GetOrCreate(property.TypeSymbol);
        var emitter = CodeEmitter.Create(propertyMetadata);
        var writeCode = emitter.EmitWrite("writer", $"value.{property.Name}", property.JsonName);

        sb.AppendLine($"            {writeCode}");
    }

    private static void GenerateSerializerImplementation(StringBuilder sb, List<TypeInfo> types)
    {
        sb.AppendLine("    internal class GeneratedJsonSerializer : IGaldrJsonTypeSerializer");
        sb.AppendLine("    {");
        sb.AppendLine("        public bool CanSerialize(Type type)");
        sb.AppendLine("        {");
        foreach (TypeInfo type in types)
        {
            sb.AppendLine($"            if (type == typeof({type.FullName})) return true;");
        }
        sb.AppendLine("            return false;");
        sb.AppendLine("        }");
        sb.AppendLine();

        sb.AppendLine("        public string Serialize(object value, Type type)");
        sb.AppendLine("        {");
        sb.AppendLine("            using var stream = new System.IO.MemoryStream();");
        sb.AppendLine("            using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });");
        sb.AppendLine();
        sb.AppendLine("            switch (type)");
        sb.AppendLine("            {");

        foreach (TypeInfo type in types)
        {
            sb.AppendLine($"                case Type t when t == typeof({type.FullName}):");
            sb.AppendLine($"                    new {type.Name}JsonConverter().Write(writer, ({type.FullName})value, JsonSerializerOptions.Default);");
            sb.AppendLine("                    break;");
        }

        sb.AppendLine("                default:");
        sb.AppendLine("                    throw new NotSupportedException($\"Type {type} is not registered for serialization\");");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            writer.Flush();");
        sb.AppendLine("            return System.Text.Encoding.UTF8.GetString(stream.ToArray());");
        sb.AppendLine("        }");
        sb.AppendLine();

        sb.AppendLine("        public object Deserialize(string json, Type type)");
        sb.AppendLine("        {");
        sb.AppendLine("            var bytes = System.Text.Encoding.UTF8.GetBytes(json);");
        sb.AppendLine("            var reader = new Utf8JsonReader(bytes);");
        sb.AppendLine("            reader.Read(); // Move to first token");
        sb.AppendLine();
        sb.AppendLine("            switch (type)");
        sb.AppendLine("            {");

        foreach (TypeInfo type in types)
        {
            sb.AppendLine($"                case Type t when t == typeof({type.FullName}):");
            sb.AppendLine($"                    return new {type.Name}JsonConverter().Read(ref reader, typeof({type.FullName}), JsonSerializerOptions.Default);");
        }

        sb.AppendLine("                default:");
        sb.AppendLine("                    throw new NotSupportedException($\"Type {type.FullName} is not registered for deserialization\");");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
    }

    private static void GenerateModuleInitializer(StringBuilder sb)
    {
        sb.AppendLine("    internal static class GaldrGeneratedInitializer");
        sb.AppendLine("    {");
        sb.AppendLine("        [ModuleInitializer]");
        sb.AppendLine("        public static void Initialize()");
        sb.AppendLine("        {");
        sb.AppendLine("            GaldrJsonSerializerRegistry.Register(new GeneratedJsonSerializer());");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
    }

    // Helper classes
    private class TypeInfo
    {
        public string FullName { get; set; } = "";
        public string Name { get; set; } = "";
        public string Namespace { get; set; } = "";
        public List<PropertyInfo> Properties { get; set; } = new List<PropertyInfo>();
    }

    private class PropertyInfo
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public ITypeSymbol TypeSymbol { get; set; } = null;
        public string JsonName { get; set; } = "";
        public bool CanWrite { get; set; }
    }
}
