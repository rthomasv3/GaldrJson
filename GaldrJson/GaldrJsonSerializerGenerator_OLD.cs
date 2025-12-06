using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

//[Generator]
public class GaldrJsonSerializerGenerator_OLD : IIncrementalGenerator
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
            typeSymbol.TypeKind == TypeKind.Enum ||
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
        if (typeSymbol.TypeKind == TypeKind.Array)
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
        GenerateCollectionHelpers(sb, allTypes);

        // Generate dictionary helper methods for all element types used in dictionaries
        GenerateDictionaryHelpers(sb, allTypes);

        // Generate converter for each type
        foreach (TypeInfo typeInfo in allTypes)
        {
            GenerateTypeConverter(sb, typeInfo);
        }

        // Generate the serializer implementation
        GenerateSerializerImplementation(sb, allTypes);

        // Generate the module initializer
        GenerateModuleInitializer(sb);

        sb.AppendLine("}");

        context.AddSource("GaldrJsonSerializers.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    private static void GenerateCollectionHelpers(StringBuilder sb, List<TypeInfo> allTypes)
    {
        var elementTypesUsedInCollections = new HashSet<string>();
        var enumElementTypes = new HashSet<string>();
        var nullableElementTypes = new HashSet<string>();

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
                        string elementTypeName = elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        elementTypesUsedInCollections.Add(elementTypeName);

                        if (elementType.TypeKind == TypeKind.Enum)
                        {
                            enumElementTypes.Add(elementTypeName);
                        }

                        if (IsNullableValueType(elementType, out _))
                        {
                            nullableElementTypes.Add(elementTypeName);
                        }
                    }
                }
            }
        }

        sb.AppendLine("    internal static class CollectionHelpers");
        sb.AppendLine("    {");

        // Generate helper methods for each element type
        foreach (var elementTypeName in elementTypesUsedInCollections)
        {
            bool isEnum = enumElementTypes.Contains(elementTypeName);
            bool isNullable = nullableElementTypes.Contains(elementTypeName);
            GenerateCollectionHelperMethods(sb, elementTypeName, isEnum, isNullable);
        }

        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void GenerateCollectionHelperMethods(StringBuilder sb, string elementTypeName, bool isEnum, bool isNullable)
    {
        string safeTypeName = GetSafeTypeName(elementTypeName);
        string elementTypeDisplayName = elementTypeName;

        // Determine how to read/write the element
        bool isPrimitive = IsPrimitiveTypeName(elementTypeName);
        bool isSystemType = IsSystemTypeName(elementTypeName);

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

        // Generate element reading code
        string elementReadCode = GetElementReadCode(elementTypeName, isPrimitive, isSystemType, isEnum, isNullable);
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

        // Generate element writing code
        string elementWriteCode = GetElementWriteCode(elementTypeName, isPrimitive, isSystemType, isEnum, isNullable);
        sb.AppendLine($"                {elementWriteCode}");

        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            writer.WriteEndArray();");
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    private static void GenerateDictionaryHelpers(StringBuilder sb, List<TypeInfo> allTypes)
    {
        var dictionaryTypesUsed = new HashSet<(string keyType, string valueType)>();
        var enumKeyTypes = new HashSet<string>();
        var enumValueTypes = new HashSet<string>();
        var nullableElementTypes = new HashSet<string>();

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
                        string keyTypeName = dictTypes.Value.keyType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        string valueTypeName = dictTypes.Value.valueType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        dictionaryTypesUsed.Add((keyTypeName, valueTypeName));

                        if (dictTypes.Value.keyType.TypeKind == TypeKind.Enum)
                        {
                            enumKeyTypes.Add(keyTypeName);
                        }

                        if (dictTypes.Value.valueType.TypeKind == TypeKind.Enum)
                        {
                            enumValueTypes.Add(valueTypeName);
                        }

                        if (IsNullableValueType(dictTypes.Value.valueType, out _))
                        {
                            nullableElementTypes.Add(valueTypeName);
                        }
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
                bool isKeyEnum = enumKeyTypes.Contains(keyType);
                bool isValueEnum = enumValueTypes.Contains(valueType);
                bool isValueNullable = nullableElementTypes.Contains(valueType);

                GenerateDictionaryHelperMethods(sb, keyType, valueType, isKeyEnum, isValueEnum, isValueNullable);
            }

            sb.AppendLine("    }");
            sb.AppendLine();
        }
    }

    private static void GenerateDictionaryHelperMethods(StringBuilder sb, string keyTypeName, string valueTypeName, bool isKeyEnum, bool isValueEnum, bool isValueNullable)
    {
        string safeTypeName = GetSafeTypeName($"{keyTypeName}_{valueTypeName}");

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
        string keyConversion = GetKeyConversionCode(keyTypeName, isKeyEnum);
        sb.AppendLine($"                var key = {keyConversion};");

        sb.AppendLine("                reader.Read();");

        // Value reading logic
        string valueReadCode = GetElementReadCode(valueTypeName, IsPrimitiveTypeName(valueTypeName), IsSystemTypeName(valueTypeName), isValueEnum, isValueNullable);
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

        if (isKeyEnum)
        {
            sb.AppendLine("                var keyString = ((int)kvp.Key).ToString();");
        }
        else
        {
            sb.AppendLine("                var keyString = kvp.Key.ToString() ?? string.Empty;");
        }

        sb.AppendLine("                writer.WritePropertyName(keyString);");

        string valueWriteCode = GetElementWriteCode(valueTypeName, IsPrimitiveTypeName(valueTypeName), IsSystemTypeName(valueTypeName), isValueEnum, isValueNullable);
        sb.AppendLine($"                {valueWriteCode.Replace("item", "kvp.Value")}");

        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            writer.WriteEndObject();");
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    private static string GetKeyConversionCode(string keyTypeName, bool isEnum)
    {
        switch (keyTypeName)
        {
            case "System.String":
            case "global::System.String":
            case "string":
                return "keyString";

            case "System.Int16":
            case "global::System.Int16":
            case "short":
                return "short.Parse(keyString)";

            case "System.Int32":
            case "global::System.Int32":
            case "int":
                return "int.Parse(keyString)";

            case "System.Int64":
            case "global::System.Int64":
            case "long":
                return "long.Parse(keyString)";

            case "System.Guid":
            case "global::System.Guid":
                return "Guid.Parse(keyString)";

            case "System.Byte":
            case "global::System.Byte":
            case "byte":
                return "byte.Parse(keyString)";

            default:
                if (isEnum)
                {
                    return $"({keyTypeName})int.Parse(keyString)";
                }
                else
                {
                    return "keyString"; // Default to string for complex key types
                }
        }
    }

    private static bool IsPrimitiveTypeName(string typeName)
    {
        // Remove global:: prefix if present
        string normalizedTypeName = typeName.StartsWith("global::") ? typeName.Substring(8) : typeName;

        // Handle both full .NET type names and C# aliases
        return normalizedTypeName == "System.Int32" || normalizedTypeName == "int" ||
               normalizedTypeName == "System.Int64" || normalizedTypeName == "long" ||
               normalizedTypeName == "System.Int16" || normalizedTypeName == "short" ||
               normalizedTypeName == "System.UInt32" || normalizedTypeName == "uint" ||
               normalizedTypeName == "System.UInt64" || normalizedTypeName == "ulong" ||
               normalizedTypeName == "System.UInt16" || normalizedTypeName == "ushort" ||
               normalizedTypeName == "System.Byte" || normalizedTypeName == "byte" ||
               normalizedTypeName == "System.Single" || normalizedTypeName == "float" ||
               normalizedTypeName == "System.Double" || normalizedTypeName == "double" ||
               normalizedTypeName == "System.Decimal" || normalizedTypeName == "decimal" ||
               normalizedTypeName == "System.Boolean" || normalizedTypeName == "bool" ||
               normalizedTypeName == "System.String" || normalizedTypeName == "string" ||
               normalizedTypeName == "System.DateTime" ||
               normalizedTypeName == "System.TimeSpan" ||
               normalizedTypeName == "System.Char" || normalizedTypeName == "char";
    }

    private static bool IsSystemTypeName(string typeName)
    {
        string normalizedTypeName = typeName.StartsWith("global::") ? typeName.Substring(8) : typeName;

        return normalizedTypeName.StartsWith("System.") &&
               (typeName.Contains("Guid") || 
                typeName.Contains("DateTime") || 
                typeName.Contains("TimeSpan") || 
                typeName.Contains("DateTimeOffset"));
    }

    private static bool IsNullableValueType(ITypeSymbol typeSymbol, out ITypeSymbol underlyingType)
    {
        underlyingType = null;

        // Check if it's Nullable<T>
        if (typeSymbol is INamedTypeSymbol namedType &&
            namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            // Get the underlying type (T in Nullable<T>)
            underlyingType = namedType.TypeArguments[0];
            return true;
        }

        return false;
    }

    private static string GetElementReadCode(string elementTypeName, bool isPrimitive, bool isSystemType, bool isEnum, bool isNullable)
    {
        if (isNullable)
        {
            return GenerateNullableElementReadCode(elementTypeName);
        }

        if (isPrimitive)
        {
            // Remove global:: prefix for comparison
            string normalizedTypeName = elementTypeName.StartsWith("global::") ? elementTypeName.Substring(8) : elementTypeName;

            switch (normalizedTypeName)
            {
                case "System.Int32":
                case "int":
                    return "reader.GetInt32()";
                case "System.Int64":
                case "long":
                    return "reader.GetInt64()";
                case "System.Int16":
                case "short":
                    return "reader.GetInt16()";
                case "System.Byte":
                case "byte":
                    return "reader.GetByte()";
                case "System.Single":
                case "float":
                    return "reader.GetSingle()";
                case "System.Double":
                case "double":
                    return "reader.GetDouble()";
                case "System.Decimal":
                case "decimal":
                    return "reader.GetDecimal()";
                case "System.Boolean":
                case "bool":
                    return "reader.GetBoolean()";
                case "System.String":
                case "string":
                    return "reader.GetString()";
                case "System.DateTime":
                    return "reader.GetDateTime()";
                case "System.DateTimeOffset":
                    return "reader.GetDateTimeOffset()";
                case "System.TimeSpan":
                    return "System.TimeSpan.FromTicks(reader.GetInt64())";
                case "System.Char":
                case "char":
                    return "reader.GetString()?[0] ?? '\\0'";
                default:
                    throw new NotSupportedException($"Primitive type {elementTypeName} not supported");
            }
        }

        if (isSystemType && elementTypeName.Contains("Guid"))
        {
            return "reader.GetGuid()";
        }

        if (isEnum)
        {
            return $"({elementTypeName})reader.GetInt32()";
        }

        // For complex types, use the generated converter
        string baseTypeName = GetBaseTypeNameFromFullName(elementTypeName);
        string converterName = $"{baseTypeName}JsonConverter";
        return $"new {converterName}().Read(ref reader, typeof({elementTypeName}), options) ?? new {elementTypeName}()";
    }

    private static string GenerateNullableElementReadCode(string nullableTypeName)
    {
        // Extract the underlying type from the nullable type name
        // e.g., "System.Nullable<System.Int32>" -> "System.Int32"
        string underlyingTypeName = ExtractUnderlyingTypeFromNullable(nullableTypeName);

        // Generate: reader.TokenType == JsonTokenType.Null ? null : (Nullable<int>)reader.GetInt32()
        string underlyingReadCode = GetUnderlyingTypeReadCode(underlyingTypeName);

        return $"reader.TokenType == JsonTokenType.Null ? null : ({nullableTypeName}){underlyingReadCode}";
    }

    private static string ExtractUnderlyingTypeFromNullable(string nullableTypeName)
    {
        // If it's in the form "int?" or "MyEnum?", just remove the '?'
        if (nullableTypeName.EndsWith("?"))
        {
            return nullableTypeName.Substring(0, nullableTypeName.Length - 1);
        }

        // Otherwise try to parse "System.Nullable<System.Int32>" format
        int startIndex = nullableTypeName.IndexOf('<');
        int endIndex = nullableTypeName.LastIndexOf('>');

        if (startIndex > 0 && endIndex > startIndex)
        {
            return nullableTypeName.Substring(startIndex + 1, endIndex - startIndex - 1);
        }

        return nullableTypeName; // Fallback
    }

    private static string GetUnderlyingTypeReadCode(string typeName)
    {
        // Remove the nullable suffix if present
        string underlyingType = typeName.TrimEnd('?');
        string normalizedTypeName = underlyingType.StartsWith("global::") ? underlyingType.Substring(8) : underlyingType;

        switch (normalizedTypeName)
        {
            case "System.Int32":
            case "int":
                return "reader.GetInt32()";
            case "System.Int64":
            case "long":
                return "reader.GetInt64()";
            case "System.Int16":
            case "short":
                return "reader.GetInt16()";
            case "System.Byte":
            case "byte":
                return "reader.GetByte()";
            case "System.SByte":
            case "sbyte":
                return "reader.GetSByte()";
            case "System.UInt16":
            case "ushort":
                return "reader.GetUInt16()";
            case "System.UInt32":
            case "uint":
                return "reader.GetUInt32()";
            case "System.UInt64":
            case "ulong":
                return "reader.GetUInt64()";
            case "System.Single":
            case "float":
                return "reader.GetSingle()";
            case "System.Double":
            case "double":
                return "reader.GetDouble()";
            case "System.Decimal":
            case "decimal":
                return "reader.GetDecimal()";
            case "System.Boolean":
            case "bool":
                return "reader.GetBoolean()";
            case "System.DateTime":
                return "reader.GetDateTime()";
            case "System.DateTimeOffset":
                return "reader.GetDateTimeOffset()";
            case "System.TimeSpan":
                return "System.TimeSpan.FromTicks(reader.GetInt64())";
            case "System.Char":
            case "char":
                return "reader.GetString()?[0] ?? '\\0'";
            case "System.Guid":
            case "Guid":
                return "reader.GetGuid()";
            default:
                // Assume it's an enum
                return $"({underlyingType})reader.GetInt32()";
        }
    }

    private static string GetElementWriteCode(string elementTypeName, bool isPrimitive, bool isSystemType, bool isEnum, bool isNullable)
    {
        if (isNullable)
        {
            return GenerateNullableElementWriteCode(elementTypeName);
        }

        if (isPrimitive)
        {
            // Remove global:: prefix for comparison
            string normalizedTypeName = elementTypeName.StartsWith("global::") ? elementTypeName.Substring(8) : elementTypeName;

            switch (normalizedTypeName)
            {
                case "System.Int32":
                case "int":
                case "System.Int64":
                case "long":
                case "System.Int16":
                case "short":
                case "System.Byte":
                case "byte":
                case "System.Single":
                case "float":
                case "System.Double":
                case "double":
                case "System.Decimal":
                case "decimal":
                    return "writer.WriteNumberValue(item);";
                case "System.TimeSpan":
                    return "writer.WriteNumberValue(item.Ticks);";
                case "System.Boolean":
                case "bool":
                    return "writer.WriteBooleanValue(item);";
                case "System.String":
                case "string":
                    return "writer.WriteStringValue(item);";
                case "System.DateTime":
                case "System.DateTimeOffset":
                    return "writer.WriteStringValue(item);";
                case "System.Char":
                case "char":
                    return "writer.WriteStringValue(item.ToString());";
                default:
                    throw new NotSupportedException($"Primitive type {elementTypeName} not supported");
            }
        }

        if (isSystemType && elementTypeName.Contains("Guid"))
        {
            return "writer.WriteStringValue(item);";
        }

        if (isEnum)
        {
            return "writer.WriteNumberValue((int)item);";
        }

        // For complex types, use the generated converter
        string baseTypeName = GetBaseTypeNameFromFullName(elementTypeName);
        string converterName = $"{baseTypeName}JsonConverter";
        return $"new {converterName}().Write(writer, item, options);";
    }

    private static string GenerateNullableElementWriteCode(string nullableTypeName)
    {
        string underlyingTypeName = ExtractUnderlyingTypeFromNullable(nullableTypeName);
        string underlyingWriteCode = GetUnderlyingTypeWriteCode(underlyingTypeName);

        return $"if (item.HasValue) {{ {underlyingWriteCode.Replace("item", "item.Value")} }} else {{ writer.WriteNullValue(); }}";
    }

    private static string GetUnderlyingTypeWriteCode(string typeName)
    {
        // Remove the nullable suffix if present
        string underlyingType = typeName.TrimEnd('?');
        string normalizedTypeName = underlyingType.StartsWith("global::") ? underlyingType.Substring(8) : underlyingType;

        switch (normalizedTypeName)
        {
            case "System.Int32":
            case "int":
            case "System.Int64":
            case "long":
            case "System.Int16":
            case "short":
            case "System.Byte":
            case "byte":
            case "System.SByte":
            case "sbyte":
            case "System.UInt16":
            case "ushort":
            case "System.UInt32":
            case "uint":
            case "System.UInt64":
            case "ulong":
            case "System.Single":
            case "float":
            case "System.Double":
            case "double":
            case "System.Decimal":
            case "decimal":
                return "writer.WriteNumberValue(item);";
            case "System.TimeSpan":
                return "writer.WriteNumberValue(item.Ticks);";
            case "System.Boolean":
            case "bool":
                return "writer.WriteBooleanValue(item);";
            case "System.DateTime":
            case "System.DateTimeOffset":
            case "System.Guid":
                return "writer.WriteStringValue(item);";
            case "System.Char":
            case "char":
                return "writer.WriteStringValue(item.ToString());";
            default:
                // Assume it's an enum
                return "writer.WriteNumberValue((int)item);";
        }
    }

    private static string GetBaseTypeNameFromFullName(string fullTypeName)
    {
        var parts = fullTypeName.Split('.');
        return parts[parts.Length - 1];
    }

    private static void GenerateTypeConverter(StringBuilder sb, TypeInfo typeInfo)
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
            GeneratePropertyRead(sb, prop, "result");
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
            GeneratePropertyWrite(sb, prop);
        }

        sb.AppendLine("            writer.WriteEndObject();");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void GeneratePropertyRead(StringBuilder sb, PropertyInfo property, string targetVariable)
    {
        string typeStr = property.Type;
        string readCode = String.Empty;

        if (IsNullableValueType(property.TypeSymbol, out ITypeSymbol underlyingType))
        {
            readCode = GenerateNullableValueTypeReadCode(underlyingType, property.Type);
            sb.AppendLine($"                        {targetVariable}.{property.Name} = {readCode};");
            return;
        }

        switch (property.TypeSymbol.SpecialType)
        {
            case SpecialType.System_Byte:
                readCode = "reader.GetByte()";
                break;

            case SpecialType.System_SByte:
                readCode = "reader.GetSByte()";
                break;

            case SpecialType.System_Char:
                readCode = "reader.GetString()?[0] ?? '\\0'";
                break;

            case SpecialType.System_Int16:
                readCode = "reader.GetInt16()";
                break;

            case SpecialType.System_UInt16:
                readCode = "reader.GetUInt16()";
                break;

            case SpecialType.System_Int32:
                readCode = "reader.GetInt32()";
                break;

            case SpecialType.System_UInt32:
                readCode = "reader.GetUInt32()";
                break;

            case SpecialType.System_Int64:
                readCode = "reader.GetInt64()";
                break;

            case SpecialType.System_UInt64:
                readCode = "reader.GetUInt64()";
                break;

            case SpecialType.System_Decimal:
                readCode = "reader.GetDecimal()";
                break;

            case SpecialType.System_Single:
                readCode = "reader.GetSingle()";
                break;

            case SpecialType.System_Double:
                readCode = "reader.GetDouble()";
                break;

            case SpecialType.System_Boolean:
                readCode = "reader.GetBoolean()";
                break;

            case SpecialType.System_String:
                readCode = "reader.GetString()";
                break;

            case SpecialType.System_DateTime:
                readCode = "reader.GetDateTime()";
                break;

            default:
                if (property.Type == "System.DateTimeOffset" || property.Type == "global::System.DateTimeOffset")
                {
                    readCode = "reader.GetDateTimeOffset()";
                }
                else if (property.Type == "System.TimeSpan" || property.Type == "global::System.TimeSpan")
                {
                    readCode = "System.TimeSpan.FromTicks(reader.GetInt64())";
                }
                else if (property.TypeSymbol.TypeKind == TypeKind.Enum)
                {
                    readCode = $"({property.Type})reader.GetInt32()";
                }
                else
                {
                    readCode = GetComplexTypeReadCode(property);
                }
                break;
        }

        sb.AppendLine($"                        {targetVariable}.{property.Name} = {readCode};");
    }

    private static string GenerateNullableValueTypeReadCode(ITypeSymbol underlyingType, string fullNullableType)
    {
        string nullCheck = "reader.TokenType == JsonTokenType.Null ? null : ";

        string underlyingReadCode;

        switch (underlyingType.SpecialType)
        {
            case SpecialType.System_Byte:
                underlyingReadCode = "reader.GetByte()";
                break;
            case SpecialType.System_SByte:
                underlyingReadCode = "reader.GetSByte()";
                break;
            case SpecialType.System_Int16:
                underlyingReadCode = "reader.GetInt16()";
                break;
            case SpecialType.System_UInt16:
                underlyingReadCode = "reader.GetUInt16()";
                break;
            case SpecialType.System_Int32:
                underlyingReadCode = "reader.GetInt32()";
                break;
            case SpecialType.System_UInt32:
                underlyingReadCode = "reader.GetUInt32()";
                break;
            case SpecialType.System_Int64:
                underlyingReadCode = "reader.GetInt64()";
                break;
            case SpecialType.System_UInt64:
                underlyingReadCode = "reader.GetUInt64()";
                break;
            case SpecialType.System_Decimal:
                underlyingReadCode = "reader.GetDecimal()";
                break;
            case SpecialType.System_Single:
                underlyingReadCode = "reader.GetSingle()";
                break;
            case SpecialType.System_Double:
                underlyingReadCode = "reader.GetDouble()";
                break;
            case SpecialType.System_Boolean:
                underlyingReadCode = "reader.GetBoolean()";
                break;
            case SpecialType.System_Char:
                underlyingReadCode = "reader.GetString()?[0] ?? '\\0'";
                break;
            case SpecialType.System_DateTime:
                underlyingReadCode = "reader.GetDateTime()";
                break;
            default:
                // Handle nullable enums
                if (underlyingType.TypeKind == TypeKind.Enum)
                {
                    string underlyingTypeName = underlyingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    underlyingReadCode = $"({underlyingTypeName})reader.GetInt32()";
                }
                else if (underlyingType.Name == "DateTimeOffset" || underlyingType.Name == "System.DateTimeOffset" || underlyingType.Name == "global::System.DateTimeOffset")
                {
                    underlyingReadCode = "reader.GetDateTimeOffset()";
                }
                else if (underlyingType.Name == "TimeSpan" || underlyingType.Name == "System.TimeSpan" || underlyingType.Name == "global::System.TimeSpan")
                {
                    underlyingReadCode = "System.TimeSpan.FromTicks(reader.GetInt64())";
                }
                else if (underlyingType.Name == "Guid" || underlyingType.Name == "System.Guid" || underlyingType.Name == "global::System.Guid")
                {
                    underlyingReadCode = "reader.GetGuid()";
                }
                else
                {
                    // Shouldn't happen for value types, but handle it
                    underlyingReadCode = "default";
                }
                break;
        }

        return $"({nullCheck}{underlyingReadCode})";
    }

    private static string GetComplexTypeReadCode(PropertyInfo property)
    {
        // Handle Guid
        if (property.Type == "System.Guid" || property.Type == "global::System.Guid")
            return "reader.GetGuid()";

        if (property.Type == "System.DateTimeOffset" || property.Type == "global::System.DateTimeOffset")
            return "reader.GetDateTimeOffset()";

        if (property.Type == "System.TimeSpan" || property.Type == "global::System.TimeSpan")
            return "System.TimeSpan.FromTicks(reader.GetInt64())";

        if (IsCollectionType(property.TypeSymbol))
        {
            return GenerateCollectionReadCode(property);
        }

        if (IsDictionaryType(property.TypeSymbol))
        {
            return GenerateDictionaryReadCode(property);
        }

        // For complex types that we're generating converters for, use the generated converter directly
        string baseTypeName = GetBaseTypeName(property.TypeSymbol);
        string converterName = $"{baseTypeName}JsonConverter";

        // Handle nullable types
        if (property.TypeSymbol.NullableAnnotation == NullableAnnotation.Annotated)
        {
            return $"new {converterName}().Read(ref reader, typeof({property.Type}), options)";
        }

        // Default for complex types - use generated converter
        //return $"new {converterName}().Read(ref reader, typeof({property.Type}), options) ?? new {property.Type}()";
        return $"new {converterName}().Read(ref reader, typeof({property.Type}), options)";
    }

    private static string GenerateCollectionReadCode(PropertyInfo property)
    {
        var elementType = GetCollectionElementType(property.TypeSymbol);
        string elementTypeName = elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        // Determine the collection type
        bool isArray = property.TypeSymbol.TypeKind == TypeKind.Array;

        return $@"({property.Type})CollectionHelpers.ReadCollection_{GetSafeTypeName(elementTypeName)}(ref reader, options, {isArray.ToString().ToLower()})";
    }

    private static string GenerateDictionaryReadCode(PropertyInfo property)
    {
        var dictTypes = GetDictionaryTypes(property.TypeSymbol);
        if (!dictTypes.HasValue) return "null";

        string keyTypeName = dictTypes.Value.keyType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        string valueTypeName = dictTypes.Value.valueType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        return $"DictionaryHelpers.ReadDictionary_{GetSafeTypeName($"{keyTypeName}_{valueTypeName}")}(ref reader, options)";
    }

    private static string GetSafeTypeName(string typeName)
    {
        return typeName
            .Replace("global::", "")
            .Replace(".", "_")
            .Replace("<", "_")
            .Replace(">", "_")
            .Replace("?", "_Nullable");
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

    private static void GeneratePropertyWrite(StringBuilder sb, PropertyInfo property)
    {
        string writeCode = String.Empty;

        if (IsNullableValueType(property.TypeSymbol, out ITypeSymbol underlyingType))
        {
            writeCode = GenerateNullableValueTypeWriteCode(property, underlyingType);
            sb.AppendLine($"            {writeCode}");
            return;
        }

        switch (property.TypeSymbol.SpecialType)
        {
            case SpecialType.System_Byte:
            case SpecialType.System_SByte:
            case SpecialType.System_Int16:
            case SpecialType.System_Int32:
            case SpecialType.System_Int64:
            case SpecialType.System_UInt16:
            case SpecialType.System_UInt32:
            case SpecialType.System_UInt64:
            case SpecialType.System_Single:
            case SpecialType.System_Double:
            case SpecialType.System_Decimal:
                writeCode = $"writer.WriteNumber(\"{property.JsonName}\", value.{property.Name});";
                break;

            case SpecialType.System_Char:
                writeCode = $"writer.WriteString(\"{property.JsonName}\", value.{property.Name}.ToString());";
                break;

            case SpecialType.System_Boolean:
                writeCode = $"writer.WriteBoolean(\"{property.JsonName}\", value.{property.Name});";
                break;

            case SpecialType.System_String:
                writeCode = GenerateStringWriteCode(property);
                break;

            case SpecialType.System_DateTime:
                writeCode = $"writer.WriteString(\"{property.JsonName}\", value.{property.Name});";
                break;

            default:
                if (property.Type == "System.DateTimeOffset" || property.Type == "global::System.DateTimeOffset")
                {
                    writeCode = $"writer.WriteString(\"{property.JsonName}\", value.{property.Name});";
                }
                else if (property.Type == "System.TimeSpan" || property.Type == "global::System.TimeSpan")
                {
                    writeCode = $"writer.WriteNumber(\"{property.JsonName}\", value.{property.Name}.Ticks);";
                }
                else if (property.TypeSymbol.TypeKind == TypeKind.Enum)
                {
                    writeCode = $"writer.WriteNumber(\"{property.JsonName}\", (int)value.{property.Name});";
                }
                else
                {
                    writeCode = GenerateComplexTypeWriteCode(property);
                }
                break;
        }

        sb.AppendLine($"            {writeCode}");
    }

    private static string GenerateStringWriteCode(PropertyInfo property)
    {
        return $@"if (value.{property.Name} != null)
                writer.WriteString(""{property.JsonName}"", value.{property.Name});
            else
                writer.WriteNull(""{property.JsonName}"");";
    }

    private static string GenerateNullableValueTypeWriteCode(PropertyInfo property, ITypeSymbol underlyingType)
    {
        StringBuilder code = new StringBuilder();

        code.AppendLine($"if (value.{property.Name}.HasValue)");
        code.Append("            {");
        code.AppendLine();

        string valueAccess = $"value.{property.Name}.Value";

        switch (underlyingType.SpecialType)
        {
            case SpecialType.System_Byte:
            case SpecialType.System_SByte:
            case SpecialType.System_Int16:
            case SpecialType.System_UInt16:
            case SpecialType.System_Int32:
            case SpecialType.System_UInt32:
            case SpecialType.System_Int64:
            case SpecialType.System_UInt64:
            case SpecialType.System_Single:
            case SpecialType.System_Double:
            case SpecialType.System_Decimal:
                code.AppendLine($"                writer.WriteNumber(\"{property.JsonName}\", {valueAccess});");
                break;
            case SpecialType.System_Boolean:
                code.AppendLine($"                writer.WriteBoolean(\"{property.JsonName}\", {valueAccess});");
                break;
            case SpecialType.System_Char:
                code.AppendLine($"                writer.WriteString(\"{property.JsonName}\", {valueAccess}.ToString());");
                break;
            case SpecialType.System_DateTime:
                code.AppendLine($"                writer.WriteString(\"{property.JsonName}\", {valueAccess});");
                break;
            default:
                // Handle nullable enums
                if (underlyingType.TypeKind == TypeKind.Enum)
                {
                    code.AppendLine($"                writer.WriteNumber(\"{property.JsonName}\", (int){valueAccess});");
                }
                else if (underlyingType.Name == "DateTimeOffset" || underlyingType.Name == "System.DateTimeOffset" || underlyingType.Name == "global::System.DateTimeOffset")
                {
                    code.AppendLine($"                writer.WriteString(\"{property.JsonName}\", {valueAccess});");
                }
                else if (underlyingType.Name == "TimeSpan" || underlyingType.Name == "System.TimeSpan" || underlyingType.Name == "global::System.TimeSpan")
                {
                    code.AppendLine($"                writer.WriteNumber(\"{property.JsonName}\", {valueAccess}.Ticks);");
                }
                else if (underlyingType.Name == "Guid" || underlyingType.Name == "System.Guid" || underlyingType.Name == "global::System.Guid")
                {
                    code.AppendLine($"                writer.WriteString(\"{property.JsonName}\", {valueAccess});");
                }
                break;
        }

        code.AppendLine("            }");
        code.AppendLine("            else");
        code.AppendLine("            {");
        code.AppendLine($"                writer.WriteNull(\"{property.JsonName}\");");
        code.AppendLine("            }");

        return code.ToString().TrimEnd();
    }

    private static string GenerateComplexTypeWriteCode(PropertyInfo property)
    {
        // Handle Guid and other system types with built-in support
        if (property.Type == "System.Guid" || property.Type == "global::System.Guid")
        {
            return $"writer.WriteString(\"{property.JsonName}\", value.{property.Name});";
        }

        if (IsCollectionType(property.TypeSymbol))
        {
            return GenerateCollectionWriteCode(property);
        }

        if (IsDictionaryType(property.TypeSymbol))
        {
            return GenerateDictionaryWriteCode(property);
        }

        // For complex types that we're generating converters for, use the generated converter directly
        string baseTypeName = GetBaseTypeName(property.TypeSymbol);
        string converterName = $"{baseTypeName}JsonConverter";

        return $@"writer.WritePropertyName(""{property.JsonName}"");
            new {converterName}().Write(writer, value.{property.Name}, options);";
    }

    private static string GenerateCollectionWriteCode(PropertyInfo property)
    {
        var elementType = GetCollectionElementType(property.TypeSymbol);
        string elementTypeName = elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        return $@"writer.WritePropertyName(""{property.JsonName}"");
            CollectionHelpers.WriteCollection_{GetSafeTypeName(elementTypeName)}(writer, value.{property.Name}, options);";
    }

    private static string GenerateDictionaryWriteCode(PropertyInfo property)
    {
        var dictTypes = GetDictionaryTypes(property.TypeSymbol);
        if (!dictTypes.HasValue) return "";

        string keyTypeName = dictTypes.Value.keyType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        string valueTypeName = dictTypes.Value.valueType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        return $@"writer.WritePropertyName(""{property.JsonName}"");
            DictionaryHelpers.WriteDictionary_{GetSafeTypeName($"{keyTypeName}_{valueTypeName}")}(writer, value.{property.Name}, options);";
    }

    private static void GenerateSerializerImplementation(StringBuilder sb, List<TypeInfo> types)
    {
        sb.AppendLine("    internal class GeneratedJsonSerializer : IGaldrJsonTypeSerializer");
        sb.AppendLine("    {");
        sb.AppendLine("        public bool CanSerialize(Type type)");
        sb.AppendLine("        {");
        sb.AppendLine("            return type switch");
        sb.AppendLine("            {");
        foreach (TypeInfo type in types)
        {
            sb.AppendLine($"                Type t when t == typeof({type.FullName}) => true,");
        }
        sb.AppendLine("                _ => false");
        sb.AppendLine("            };");
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
        sb.AppendLine("            return type switch");
        sb.AppendLine("            {");

        foreach (TypeInfo type in types)
        {
            sb.AppendLine($"                Type t when t == typeof({type.FullName}) => new {type.Name}JsonConverter().Read(ref reader, typeof({type.FullName}), JsonSerializerOptions.Default),");
        }

        sb.AppendLine("                _ => throw new NotSupportedException($\"Type {type} is not registered for deserialization\")");
        sb.AppendLine("            };");
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
