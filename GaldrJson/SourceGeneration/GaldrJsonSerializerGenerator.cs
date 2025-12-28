using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using GaldrJson.SourceGeneration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using TypeInfo = GaldrJson.SourceGeneration.TypeInfo;
using TypeKind = Microsoft.CodeAnalysis.TypeKind;

/// <summary>
/// Generates source code for high-performance JSON serialization and deserialization of types marked with the
/// GaldrJsonSerializable attribute or used in Galdr command registrations.
/// </summary>
/// <remarks>
/// This incremental source generator scans the user's code for types annotated with
/// [GaldrJsonSerializable] as well as types referenced in AddFunction and AddAction invocations on GaldrBuilder. For
/// each discovered type, it generates strongly-typed System.Text.Json converters and registers them with the GaldrJson
/// serialization infrastructure. The generated serializers support custom property naming policies, collection and
/// dictionary types, and handle object cycles using a reference tracker. Only types explicitly discovered by the
/// generator are supported for serialization; attempting to serialize or deserialize unsupported types will result in a
/// NotSupportedException. This generator is intended for use in projects that integrate with the Galdr framework and
/// require fast, reflection-free JSON serialization.
/// </remarks>
[Generator]
public class GaldrJsonSerializerGenerator : IIncrementalGenerator
{
    /// <summary>
    /// Initializes the incremental source generator by registering code generation logic for types marked with the
    /// GaldrJsonSerializable attribute and for types used in AddFunction and AddAction invocations.
    /// </summary>
    /// <remarks>
    /// This method configures the generator to discover relevant types in the user's code and to
    /// generate serialization code for them. It should be called from the generator's Initialize method.
    /// </remarks>
    /// <param name="context">The context for incremental generator initialization, used to register source outputs and access syntax
    /// information.</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        //System.Diagnostics.Debugger.Launch();

        // Find all types marked with [GaldrJsonSerializable] attribute
        IncrementalValuesProvider<TypeInfo> typesWithAttribute = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: (node, _) => IsTypeWithGaldrJsonSerializableAttribute(node),
                transform: (ctx, _) => GetTypeInfoFromDeclaration(ctx))
            .Where(typeInfo => typeInfo != null);

        // Find all return types from AddFunction and parameter types from AddAction invocations
        IncrementalValuesProvider<TypeInfo> typesFromCommands = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: (node, _) => IsAddFunctionOrActionInvocation(node),
                transform: (ctx, _) => GetAllTypesFromInvocation(ctx))
            .Where(typeInfos => typeInfos.Length > 0)
            .SelectMany((typeInfos, _) => typeInfos)
            .Where(typeInfo => typeInfo != null);

        // Combine and deduplicate
        IncrementalValueProvider<ImmutableArray<TypeInfo>> allTypes = typesWithAttribute
            .Collect()
            .Combine(typesFromCommands.Collect())
            .Select((pair, _) =>
            {
                var (attrTypes, cmdTypes) = pair;
                return attrTypes
                    .Concat(cmdTypes)
                    .GroupBy(t => t.FullName)
                    .Select(g => g.First())
                    .ToImmutableArray();
            });

        // Generate serialization code
        context.RegisterSourceOutput(allTypes, GenerateSerializers);
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

    private static bool IsAddFunctionOrActionInvocation(SyntaxNode node)
    {
        return node is InvocationExpressionSyntax invocation &&
               invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
               (memberAccess.Name.Identifier.Text == "AddFunction" ||
                memberAccess.Name.Identifier.Text == "AddAction");
    }

    private static ImmutableArray<TypeInfo> GetAllTypesFromInvocation(GeneratorSyntaxContext context)
    {
        InvocationExpressionSyntax invocation = (InvocationExpressionSyntax)context.Node;
        SemanticModel semanticModel = context.SemanticModel;
        IMethodSymbol methodSymbol = semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;

        if (methodSymbol == null ||
            (methodSymbol.Name != "AddFunction" && methodSymbol.Name != "AddAction") ||
            methodSymbol.ContainingType.Name != "GaldrBuilder" ||
            methodSymbol.ContainingNamespace.ToDisplayString() != "Galdr.Native")
        {
            return ImmutableArray<TypeInfo>.Empty;
        }

        List<TypeInfo> discoveredTypes = new List<TypeInfo>();

        // For AddFunction: Func<T1, T2, ..., TResult> - last type arg is return type, rest are parameters
        // For AddAction: Action<T1, T2, ...> - all type args are parameters
        if (methodSymbol.TypeArguments.Length > 0)
        {
            if (methodSymbol.Name == "AddFunction")
            {
                // For AddFunction, the return type is the last type argument
                if (methodSymbol.TypeArguments.Length > 0)
                {
                    ITypeSymbol returnType = methodSymbol.TypeArguments.Last();

                    // Skip void and primitive types for return types
                    if (returnType.SpecialType == SpecialType.None &&
                        returnType.TypeKind != TypeKind.Enum &&
                        returnType.Name != "String")
                    {
                        TypeInfo returnTypeInfo = ExtractTypeInfo(returnType);
                        if (returnTypeInfo != null)
                        {
                            discoveredTypes.Add(returnTypeInfo);
                        }
                    }
                }

                // Parameter types are all type arguments except the last one
                for (int i = 0; i < methodSymbol.TypeArguments.Length - 1; i++)
                {
                    ITypeSymbol parameterType = methodSymbol.TypeArguments[i];
                    if (ShouldGenerateSerializerForParameter(parameterType))
                    {
                        TypeInfo parameterTypeInfo = ExtractTypeInfo(parameterType);
                        if (parameterTypeInfo != null)
                        {
                            discoveredTypes.Add(parameterTypeInfo);
                        }
                    }
                }
            }
            else if (methodSymbol.Name == "AddAction")
            {
                // For AddAction, all type arguments are parameters
                for (int i = 0; i < methodSymbol.TypeArguments.Length; i++)
                {
                    ITypeSymbol parameterType = methodSymbol.TypeArguments[i];
                    if (ShouldGenerateSerializerForParameter(parameterType))
                    {
                        TypeInfo parameterTypeInfo = ExtractTypeInfo(parameterType);
                        if (parameterTypeInfo != null)
                        {
                            discoveredTypes.Add(parameterTypeInfo);
                        }
                    }
                }
            }
        }

        return discoveredTypes.ToImmutableArray();
    }

    private static bool ShouldGenerateSerializerForParameter(ITypeSymbol parameterType)
    {
        // Skip interfaces (likely DI services)
        if (parameterType.TypeKind == TypeKind.Interface)
            return false;

        // Skip abstract classes (likely DI services)
        if (parameterType.IsAbstract)
            return false;

        // Skip if has ignore attribute
        if (parameterType.GetAttributes().Any(attr =>
            attr.AttributeClass?.Name == "GaldrJsonIgnore" ||
            attr.AttributeClass?.Name == "GaldrJsonIgnoreAttribute"))
            return false;

        // Skip if no parameterless constructor available
        if (parameterType is INamedTypeSymbol namedType)
        {
            bool hasParameterlessConstructor = namedType.Constructors.Any(c =>
                c.Parameters.Length == 0 &&
                c.DeclaredAccessibility == Accessibility.Public);

            if (!hasParameterlessConstructor)
                return false;
        }

        // Skip obvious framework types
        string fullName = parameterType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        // Skip Microsoft and most System types (but allow basic data types to be handled by ShouldSerializeType)
        if (fullName.StartsWith("Microsoft.") || fullName.StartsWith("global::Microsoft."))
            return false;

        // Skip System types that are clearly services/framework types
        if ((fullName.StartsWith("System.") || fullName.StartsWith("global::System.")) &&
            (fullName.Contains("IServiceProvider") ||
             fullName.Contains("ILogger") ||
             fullName.Contains("IConfiguration") ||
             fullName.Contains("IHosting") ||
             fullName.Contains("IMemoryCache") ||
             fullName.Contains("HttpContext")))
            return false;

        // Use the existing logic for everything else
        return ShouldSerializeType(parameterType);
    }

    private static TypeInfo ExtractTypeInfo(ITypeSymbol typeSymbol)
    {
        TypeInfo typeInfo = new TypeInfo
        {
            FullName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            Name = typeSymbol.Name,
            Namespace = typeSymbol.ContainingNamespace?.ToDisplayString() ?? "Global",
            Properties = new List<PropertyInfo>(),
            TypeSymbol = typeSymbol,
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

        // no override
        return null;
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

        if (IsCollectionType(typeSymbol))
            return false;

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

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name) || char.IsLower(name[0]))
            return name;

        return char.ToLowerInvariant(name[0]) + name.Substring(1);
    }

    private static string ToSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        var builder = new StringBuilder();

        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];

            if (char.IsUpper(c))
            {
                if (i > 0)
                    builder.Append('_');
                builder.Append(char.ToLowerInvariant(c));
            }
            else
            {
                builder.Append(c);
            }
        }

        return builder.ToString();
    }

    private static string ToKebabCase(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        var builder = new StringBuilder();

        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];

            if (char.IsUpper(c))
            {
                if (i > 0)
                    builder.Append('-');
                builder.Append(char.ToLowerInvariant(c));
            }
            else
            {
                builder.Append(c);
            }
        }

        return builder.ToString();
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
        
        GenerateNameHelpers(context);
        
        GenerateUtf8JsonWriterCache(context);
        
        GenerateCollectionHelpers(context, allTypes, metadataCache);
        
        GenerateDictionaryHelpers(context, allTypes, metadataCache);

        foreach (TypeInfo typeInfo in allTypes)
        {
            GenerateTypeConverter(context, typeInfo, metadataCache);
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
            // Generate the serializer implementation
            GenerateSerializerImplementation(builder, allTypes, metadataCache);

            // Generate the module initializer
            GenerateModuleInitializer(builder);
        }

        context.AddSource("GaldrJsonSerializers.g.cs", SourceText.From(builder.ToString(), Encoding.UTF8));
    }

    private static void GenerateNameHelpers(SourceProductionContext context)
    {
        IndentedStringBuilder builder = new IndentedStringBuilder();
        builder.AppendLine("// <auto-generated/>");
        builder.AppendLine("#nullable disable");
        builder.AppendLine("using System;");
        builder.AppendLine("using System.Text.Json;");
        builder.AppendLine();
        
        using (builder.Block("namespace GaldrJson.Generated"))
        {
            using (builder.Block("internal static class NameHelpers"))
            {
                using (builder.Block("public static bool MatchesPropertyName(System.ReadOnlySpan<byte> jsonPropertyName, JsonEncodedText exactName, JsonEncodedText camelName, JsonEncodedText snakeName, JsonEncodedText kebabName, JsonEncodedText? customName, System.Text.Json.JsonSerializerOptions options)"))
                {
                    builder.AppendLine("// Select the correct name variant based on naming policy");
                    builder.AppendLine("System.ReadOnlySpan<byte> expectedName;");
                    builder.AppendLine();

                    using (builder.Block("if (customName != null)"))
                    {
                        builder.AppendLine("expectedName = customName.Value.EncodedUtf8Bytes;");
                    }

                    using (builder.Block("else if (options.PropertyNamingPolicy == null)"))
                    {
                        builder.AppendLine("expectedName = exactName.EncodedUtf8Bytes;");
                    }

                    using (builder.Block("else if (options.PropertyNamingPolicy == System.Text.Json.JsonNamingPolicy.CamelCase)"))
                    {
                        builder.AppendLine("expectedName = camelName.EncodedUtf8Bytes;");
                    }

                    using (builder.Block("else if (options.PropertyNamingPolicy == System.Text.Json.JsonNamingPolicy.SnakeCaseLower)"))
                    {
                        builder.AppendLine("expectedName = snakeName.EncodedUtf8Bytes;");
                    }

                    using (builder.Block("else if (options.PropertyNamingPolicy == System.Text.Json.JsonNamingPolicy.KebabCaseLower)"))
                    {
                        builder.AppendLine("expectedName = kebabName.EncodedUtf8Bytes;");
                    }

                    using (builder.Block("else"))
                    {
                        builder.AppendLine("expectedName = exactName.EncodedUtf8Bytes;");
                    }

                    builder.AppendLine();

                    builder.AppendLine("// Fast path: case-sensitive comparison");
                    using (builder.Block("if (!options.PropertyNameCaseInsensitive)"))
                    {
                        builder.AppendLine("return jsonPropertyName.SequenceEqual(expectedName);");
                    }

                    builder.AppendLine();
                    builder.AppendLine("// Slow path: case-insensitive UTF-8 comparison");
                    builder.AppendLine("return EqualsIgnoreCaseUtf8(jsonPropertyName, expectedName);");
                }

                builder.AppendLine();
                
                using (builder.Block("public static JsonEncodedText GetPropertyName(JsonEncodedText exactName, JsonEncodedText camelName, JsonEncodedText snakeName, JsonEncodedText kebabName, JsonEncodedText? customName, JsonSerializerOptions options)"))
                {
                    using (builder.Block("if (customName != null)"))
                    {
                        builder.AppendLine("return customName.Value;");
                    }
                    builder.AppendLine();
                    
                    using (builder.Block("if (options.PropertyNamingPolicy == null)"))
                    {
                        builder.AppendLine("return exactName;");
                    }

                    builder.AppendLine();
                    using (builder.Block("if (options.PropertyNamingPolicy == System.Text.Json.JsonNamingPolicy.CamelCase)"))
                    {
                        builder.AppendLine("return camelName;");
                    }

                    using (builder.Block("else if (options.PropertyNamingPolicy == System.Text.Json.JsonNamingPolicy.SnakeCaseLower)"))
                    {
                        builder.AppendLine("return snakeName;");
                    }

                    using (builder.Block("else if (options.PropertyNamingPolicy == System.Text.Json.JsonNamingPolicy.KebabCaseLower)"))
                    {
                        builder.AppendLine("return kebabName;");
                    }

                    using (builder.Block("else"))
                    {
                        builder.AppendLine("return exactName;");
                    }
                }
                
                builder.AppendLine();
                
                using (builder.Block("private static bool EqualsIgnoreCaseUtf8(System.ReadOnlySpan<byte> utf8A, System.ReadOnlySpan<byte> utf8B)"))
                {
                    using (builder.Block("if (utf8A.Length != utf8B.Length)"))
                    {
                        builder.AppendLine("return false;");
                    }

                    builder.AppendLine();

                    using (builder.Block("for (int i = 0; i < utf8A.Length; i++)"))
                    {
                        builder.AppendLine("byte a = utf8A[i];");
                        builder.AppendLine("byte b = utf8B[i];");
                        builder.AppendLine();

                        using (builder.Block("if (a != b)"))
                        {
                            builder.AppendLine("// ASCII case-insensitive comparison (A-Z: 65-90, a-z: 97-122)");
                            using (builder.Block("if ((a >= 65 && a <= 90) || (a >= 97 && a <= 122))"))
                            {
                                builder.AppendLine("// Convert both to lowercase and compare");
                                using (builder.Block("if ((a | 0x20) != (b | 0x20))"))
                                {
                                    builder.AppendLine("return false;");
                                }
                            }

                            using (builder.Block("else"))
                            {
                                builder.AppendLine("return false;");
                            }
                        }
                    }

                    builder.AppendLine();
                    builder.AppendLine("return true;");
                }
            }
        }
        
        context.AddSource("NameHelpers.g.cs", SourceText.From(builder.ToString(), Encoding.UTF8));
    }

    private static void GenerateUtf8JsonWriterCache(SourceProductionContext context)
    {
        IndentedStringBuilder builder = new IndentedStringBuilder();
        builder.AppendLine("// <auto-generated/>");
        builder.AppendLine("#nullable disable");
        builder.AppendLine();
        
        using (builder.Block("namespace GaldrJson.Generated"))
        {
            using (builder.Block("internal static class Utf8JsonWriterCache"))
            {
                builder.AppendLine("[System.ThreadStatic]");
                builder.AppendLine("private static CachedWriter t_cachedWriterIndented;");
                builder.AppendLine();
                builder.AppendLine("[System.ThreadStatic]");
                builder.AppendLine("private static CachedWriter t_cachedWriterNotIndented;");
                builder.AppendLine();

                using (builder.Block("private sealed class CachedWriter"))
                {
                    builder.AppendLine("public System.Buffers.ArrayBufferWriter<byte> BufferWriter { get; }");
                    builder.AppendLine("public System.Text.Json.Utf8JsonWriter Writer { get; }");
                    builder.AppendLine();

                    using (builder.Block("public CachedWriter(bool indented)"))
                    {
                        builder.AppendLine(
                            "BufferWriter = new System.Buffers.ArrayBufferWriter<byte>(initialCapacity: 16384);");
                        builder.AppendLine(
                            "Writer = new System.Text.Json.Utf8JsonWriter(BufferWriter, new System.Text.Json.JsonWriterOptions { Indented = indented });");
                    }

                    builder.AppendLine();

                    using (builder.Block("public void Reset()"))
                    {
                        builder.AppendLine("BufferWriter.Clear();");
                        builder.AppendLine("Writer.Reset(BufferWriter);");
                    }
                }

                builder.AppendLine();

                using (builder.Block(
                           "public static System.Text.Json.Utf8JsonWriter RentWriter(bool indented, out System.Buffers.ArrayBufferWriter<byte> bufferWriter)"))
                {
                    builder.AppendLine("CachedWriter cached;");
                    builder.AppendLine();

                    using (builder.Block("if (indented)"))
                    {
                        builder.AppendLine("cached = t_cachedWriterIndented;");
                        using (builder.Block("if (cached == null)"))
                        {
                            builder.AppendLine("cached = new CachedWriter(indented: true);");
                            builder.AppendLine("t_cachedWriterIndented = cached;");
                        }
                    }

                    using (builder.Block("else"))
                    {
                        builder.AppendLine("cached = t_cachedWriterNotIndented;");
                        using (builder.Block("if (cached == null)"))
                        {
                            builder.AppendLine("cached = new CachedWriter(indented: false);");
                            builder.AppendLine("t_cachedWriterNotIndented = cached;");
                        }
                    }

                    builder.AppendLine();
                    builder.AppendLine("cached.Reset();");
                    builder.AppendLine("bufferWriter = cached.BufferWriter;");
                    builder.AppendLine("return cached.Writer;");
                }
            }
        }

        context.AddSource("Utf8JsonWriterCache.g.cs", SourceText.From(builder.ToString(), Encoding.UTF8));
    }

    private static void GenerateCollectionHelpers(SourceProductionContext context, List<TypeInfo> allTypes, TypeMetadataCache metadataCache)
    {
        IndentedStringBuilder builder = new IndentedStringBuilder();
        builder.AppendLine("// <auto-generated/>");
        builder.AppendLine("#nullable disable");
        builder.AppendLine("using System.Collections.Generic;");
        builder.AppendLine("using System.Text.Json;");
        builder.AppendLine("using GaldrJson;");
        builder.AppendLine();

        HashSet<ITypeSymbol> elementTypesUsedInCollections = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);

        // Find all element types used in collections across all types
        foreach (TypeInfo typeInfo in allTypes)
        {
            foreach (PropertyInfo property in typeInfo.Properties)
            {
                if (IsCollectionType(property.TypeSymbol))
                {
                    ITypeSymbol elementType = GetCollectionElementType(property.TypeSymbol);
                    if (elementType != null)
                    {
                        elementTypesUsedInCollections.Add(elementType);
                    }
                }
            }

            elementTypesUsedInCollections.Add(typeInfo.TypeSymbol);
        }

        using (builder.Block("namespace GaldrJson.Generated"))
        {
            using (builder.Block("internal static class CollectionHelpers"))
            {
                // Generate helper methods for each element type
                foreach (ITypeSymbol elementType in elementTypesUsedInCollections)
                {
                    GenerateCollectionHelperMethods(builder, elementType, metadataCache);
                }
            }
        }

        context.AddSource("CollectionHelpers.g.cs", SourceText.From(builder.ToString(), Encoding.UTF8));
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
        using (builder.Block($"public static List<{elementTypeDisplayName}> ReadCollection_{safeTypeName}(ref Utf8JsonReader reader, JsonSerializerOptions options)"))
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
            builder.AppendLine("return list;");
        }

        builder.AppendLine();

        using (builder.Block($"public static {elementTypeDisplayName}[] ReadCollectionArray_{safeTypeName}(ref Utf8JsonReader reader, JsonSerializerOptions options)"))
        {
            builder.AppendLine($"var list = ReadCollection_{safeTypeName}(ref reader, options);");
            builder.AppendLine("return list?.ToArray();");
        }

        builder.AppendLine();

        // Generate Write method
        using (builder.Block($"public static void WriteCollection_{safeTypeName}(Utf8JsonWriter writer, System.Collections.Generic.IEnumerable<{elementTypeDisplayName}> collection, JsonSerializerOptions options, ReferenceTracker Tracker)"))
        {
            using (builder.Block("if (collection == null)"))
            {
                builder.AppendLine("writer.WriteNullValue();");
                builder.AppendLine("return;");
            }

            builder.AppendLine();
            builder.AppendLine("writer.WriteStartArray();");
            builder.AppendLine();

            using (builder.Block($"foreach (var item in collection)"))  // No cast needed!
            {
                // Generate element writing code using CodeEmitter (for array elements, no property name)
                string elementWriteCode = elementEmitter.EmitWrite("writer", "item", null);
                builder.AppendLine(elementWriteCode);
            }

            builder.AppendLine();
            builder.AppendLine("writer.WriteEndArray();");
        }

        builder.AppendLine();

        // Generate Optimized Write method for List<T>
        using (builder.Block($"public static void WriteCollection_{safeTypeName}(Utf8JsonWriter writer, List<{elementTypeDisplayName}> collection, JsonSerializerOptions options, ReferenceTracker Tracker)"))
        {
            builder.AppendLine("if (collection == null) { writer.WriteNullValue(); return; }");
            builder.AppendLine("writer.WriteStartArray();");
            builder.AppendLine();

            using (builder.Block("for (int i = 0; i < collection.Count; i++)"))
            {
                builder.AppendLine("var item = collection[i];");
                string elementWriteCode = elementEmitter.EmitWrite("writer", "item", null);
                builder.AppendLine(elementWriteCode);
            }

            builder.AppendLine();
            builder.AppendLine("writer.WriteEndArray();");
        }

        builder.AppendLine();
    }

    private static void GenerateDictionaryHelpers(SourceProductionContext context, List<TypeInfo> allTypes, TypeMetadataCache metadataCache)
    {
        IndentedStringBuilder builder = new IndentedStringBuilder();
        builder.AppendLine("// <auto-generated/>");
        builder.AppendLine("#nullable disable");
        builder.AppendLine("using System.Collections.Generic;");
        builder.AppendLine("using System.Text.Json;");
        builder.AppendLine("using GaldrJson;");
        builder.AppendLine();
        
        HashSet<(ITypeSymbol keyType, ITypeSymbol valueType)> dictionaryTypesUsed = new HashSet<(ITypeSymbol keyType, ITypeSymbol valueType)>(new DictionaryTypesComparer());

        // Find all dictionary types used
        foreach (TypeInfo typeInfo in allTypes)
        {
            foreach (PropertyInfo property in typeInfo.Properties)
            {
                if (IsDictionaryType(property.TypeSymbol))
                {
                    (ITypeSymbol keyType, ITypeSymbol valueType)? dictTypes = GetDictionaryTypes(property.TypeSymbol);
                    if (dictTypes.HasValue)
                    {
                        dictionaryTypesUsed.Add((dictTypes.Value.keyType, dictTypes.Value.valueType));
                    }
                }
            }
        }

        if (dictionaryTypesUsed.Count > 0)
        {
            using (builder.Block("namespace GaldrJson.Generated"))
            {
                using (builder.Block("internal static class DictionaryHelpers"))
                {
                    foreach ((ITypeSymbol keyType, ITypeSymbol valueType) in dictionaryTypesUsed)
                    {
                        GenerateDictionaryHelperMethods(builder, keyType, valueType, metadataCache);
                    }
                }
            }

            context.AddSource("DictionaryHelpers.g.cs", SourceText.From(builder.ToString(), Encoding.UTF8));
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
        using (builder.Block($"public static void WriteDictionary_{safeTypeName}(Utf8JsonWriter writer, Dictionary<{keyTypeName}, {valueTypeName}> dictionary, JsonSerializerOptions options, ReferenceTracker Tracker)"))
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

    private static void GenerateTypeConverter(SourceProductionContext context, TypeInfo typeInfo, TypeMetadataCache metadataCache)
    {
        IndentedStringBuilder builder = new IndentedStringBuilder();
        builder.AppendLine("// <auto-generated/>");
        builder.AppendLine("#nullable disable");
        builder.AppendLine("using System;");
        builder.AppendLine("using System.Collections.Generic;");
        builder.AppendLine("using System.Text.Json;");
        builder.AppendLine("using System.Text.Json.Serialization;");
        builder.AppendLine("using GaldrJson;");
        builder.AppendLine();
        
        string typeName = typeInfo.FullName;

        using (builder.Block("namespace GaldrJson.Generated"))
        {
            using (builder.Block($"internal sealed class {typeInfo.ConverterName} : JsonConverter<{typeName}>"))
            {
                builder.AppendLine("[ThreadStatic]");
                builder.AppendLine("private static ReferenceTracker _tracker;");
                builder.AppendLine();
                using (builder.Block("internal ReferenceTracker Tracker"))
                {
                    builder.AppendLine("get => _tracker;");
                    builder.AppendLine("set => _tracker = value;");
                }

                builder.AppendLine();

                List<PropertyInfo> writableProps = typeInfo.Properties.Where(p => p.CanWrite).ToList();

                // Generate UTF-8 property name constants for each property

                if (typeInfo.Properties.Count > 0)
                {
                    builder.AppendLine("// UTF-8 encoded property names for fast comparison");

                    foreach (PropertyInfo prop in typeInfo.Properties)
                    {
                        string exactName = prop.Name;
                        string camelName = ToCamelCase(prop.Name);
                        string snakeName = ToSnakeCase(prop.Name);
                        string kebabName = ToKebabCase(prop.Name);

                        builder.AppendLine($"private static readonly global::System.Text.Json.JsonEncodedText Prop_{prop.Name}_Exact = global::System.Text.Json.JsonEncodedText.Encode(\"{exactName}\");");
                        builder.AppendLine($"private static readonly global::System.Text.Json.JsonEncodedText Prop_{prop.Name}_Camel = global::System.Text.Json.JsonEncodedText.Encode(\"{camelName}\");");
                        builder.AppendLine($"private static readonly global::System.Text.Json.JsonEncodedText Prop_{prop.Name}_Snake = global::System.Text.Json.JsonEncodedText.Encode(\"{snakeName}\");");
                        builder.AppendLine($"private static readonly global::System.Text.Json.JsonEncodedText Prop_{prop.Name}_Kebab = global::System.Text.Json.JsonEncodedText.Encode(\"{kebabName}\");");

                        if (!string.IsNullOrEmpty(prop.JsonName))
                        {
                            builder.AppendLine($"private static readonly global::System.Text.Json.JsonEncodedText? Prop_{prop.Name}_Custom = global::System.Text.Json.JsonEncodedText.Encode(\"{prop.JsonName}\");");
                        }
                        else
                        {
                            builder.AppendLine($"private static readonly global::System.Text.Json.JsonEncodedText? Prop_{prop.Name}_Custom = null;");
                        }
                    }

                    builder.AppendLine();
                }

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

                    // Declare temp variables for all writable properties
                    foreach (PropertyInfo prop in writableProps)
                    {
                        var propertyMetadata = metadataCache.GetOrCreate(prop.TypeSymbol);
                        builder.AppendLine(
                            $"{propertyMetadata.FullyQualifiedName} {GetTempVarName(prop.Name)} = default;");
                    }

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
                        builder.AppendLine("System.ReadOnlySpan<byte> propertyName = reader.ValueSpan;");
                        builder.AppendLine("reader.Read();");
                        builder.AppendLine();

                        if (writableProps.Count > 0)
                        {
                            for (int i = 0; i < writableProps.Count; ++i)
                            {
                                var prop = writableProps[i];

                                if (i == 0)
                                {
                                    using (builder.Block($"if (NameHelpers.MatchesPropertyName(propertyName, Prop_{prop.Name}_Exact, Prop_{prop.Name}_Camel, Prop_{prop.Name}_Snake, Prop_{prop.Name}_Kebab, Prop_{prop.Name}_Custom, options))"))
                                    {
                                        GeneratePropertyReadToTempVar(builder, prop, metadataCache);
                                    }
                                }
                                else
                                {
                                    using (builder.Block($"else if (NameHelpers.MatchesPropertyName(propertyName, Prop_{prop.Name}_Exact, Prop_{prop.Name}_Camel, Prop_{prop.Name}_Snake, Prop_{prop.Name}_Kebab, Prop_{prop.Name}_Custom, options))"))
                                    {
                                        GeneratePropertyReadToTempVar(builder, prop, metadataCache);
                                    }
                                }
                            }

                            using (builder.Block("else"))
                            {
                                builder.AppendLine("reader.Skip();");
                            }
                        }
                        else
                        {
                            builder.AppendLine("reader.Skip();");
                        }
                    }

                    builder.AppendLine();

                    // Create object with object initializer
                    using (builder.Block($"return new {typeName}", endWithSemiColon: true))
                    {
                        for (int i = 0; i < writableProps.Count; i++)
                        {
                            var prop = writableProps[i];
                            string comma = i < writableProps.Count - 1 ? "," : "";
                            builder.AppendLine($"{prop.Name} = {GetTempVarName(prop.Name)}{comma}");
                        }
                    }
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
                    using (builder.Block("if (Tracker != null && !Tracker.Push(value))"))
                    {
                        builder.AppendLine($"throw new JsonException(\"A possible object cycle was detected for type '{typeInfo.FullName}'.\");");
                    }

                    builder.AppendLine();
                    builder.AppendLine("writer.WriteStartObject();");
                    builder.AppendLine();

                    foreach (PropertyInfo prop in typeInfo.Properties)
                    {
                        GeneratePropertyWrite(builder, prop, metadataCache);
                    }

                    builder.AppendLine();
                    builder.AppendLine("writer.WriteEndObject();");
                }
            }
        }

        context.AddSource($"{typeInfo.ConverterName}.g.cs", SourceText.From(builder.ToString(), Encoding.UTF8));
    }

    private static string GetTempVarName(string propertyName)
    {
        // Convert PropertyName to propertyName (camelCase) for temp variable
        return char.ToLowerInvariant(propertyName[0]) + propertyName.Substring(1);
    }

    private static void GeneratePropertyReadToTempVar(IndentedStringBuilder builder, PropertyInfo property, TypeMetadataCache metadataCache)
    {
        TypeMetadata propertyMetadata = metadataCache.GetOrCreate(property.TypeSymbol);
        CodeEmitter emitter = CodeEmitter.Create(propertyMetadata);
        string readCode = emitter.EmitRead("reader");

        builder.AppendLine($"{GetTempVarName(property.Name)} = {readCode};");
    }

    private static void GeneratePropertyWrite(IndentedStringBuilder builder, PropertyInfo property, TypeMetadataCache metadataCache)
    {
        TypeMetadata propertyMetadata = metadataCache.GetOrCreate(property.TypeSymbol);
        CodeEmitter emitter = CodeEmitter.Create(propertyMetadata);
        string writeCode = emitter.EmitWrite("writer", $"value.{property.Name}", property);

        builder.AppendLine(writeCode);
    }

    private static void GenerateSerializerImplementation(IndentedStringBuilder builder, List<TypeInfo> types, TypeMetadataCache metadataCache)
    {
        Dictionary<string, TypeMetadata> typeMetadataMap = new Dictionary<string, TypeMetadata>();

        foreach (TypeInfo typeInfo in types)
        {
            if (typeInfo.TypeSymbol != null)
            {
                TypeMetadata metadata = metadataCache.GetOrCreate(typeInfo.TypeSymbol);
                typeMetadataMap[typeInfo.FullName] = metadata;
            }
        }

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
                bool isFirst = true;
                foreach (TypeInfo type in types)
                {
                    string safeName = TypeMetadata.GenerateSafeName(type.FullName);
    
                    string ifKeyword = isFirst ? "if" : "else if";
    
                    using (builder.Block($"{ifKeyword} (type == typeof({type.FullName}))"))
                    {
                        builder.AppendLine($"return {type.FieldName}.Read(ref reader, typeof({type.FullName}), options);");
                    }
    
                    using (builder.Block($"else if (type == typeof({type.FullName}[]))"))
                    {
                        builder.AppendLine($"return CollectionHelpers.ReadCollectionArray_{safeName}(ref reader, options);");
                    }
    
                    using (builder.Block($"else if (type == typeof(global::System.Collections.Generic.List<{type.FullName}>) || type == typeof(global::System.Collections.Generic.IEnumerable<{type.FullName}>) || type == typeof(global::System.Collections.Generic.IList<{type.FullName}>) || type == typeof(global::System.Collections.Generic.ICollection<{type.FullName}>))"))
                    {
                        builder.AppendLine($"return CollectionHelpers.ReadCollection_{safeName}(ref reader, options);");
                    }

                    isFirst = false;
                }

                using (builder.Block("else"))
                {
                    builder.AppendLine("throw new NotSupportedException($\"Type {{type.FullName}} is not registered\");");
                }
            }

            builder.AppendLine();

            using (builder.Block("internal static void WriteWithConverter(Utf8JsonWriter writer, object value, Type type, JsonSerializerOptions options, ReferenceTracker tracker)"))
            {
                bool isFirst = true;
                foreach (TypeInfo type in types)
                {
                    string safeName = TypeMetadata.GenerateSafeName(type.FullName);
                    bool needsTracking = typeMetadataMap.TryGetValue(type.FullName, out TypeMetadata metadata) && metadata.IsComplex;

                    string ifKeyword = isFirst ? "if" : "else if";
    
                    using (builder.Block($"{ifKeyword} (type == typeof({type.FullName}))"))
                    {
                        if (needsTracking)
                        {
                            using (builder.Block("try"))
                            {
                                builder.AppendLine($"{type.FieldName}.Tracker = tracker;");
                                builder.AppendLine($"{type.FieldName}.Write(writer, ({type.FullName})value, options);");
                            }
                            using (builder.Block("finally"))
                            {
                                builder.AppendLine($"{type.FieldName}.Tracker = null;");
                            }
                        }
                        else
                        {
                            builder.AppendLine($"{type.FieldName}.Write(writer, ({type.FullName})value, options);");
                        }
                    }
    
                    using (builder.Block($"else if (type == typeof(global::System.Collections.Generic.List<{type.FullName}>))"))
                    {
                        builder.AppendLine($"CollectionHelpers.WriteCollection_{safeName}(writer, (global::System.Collections.Generic.List<{type.FullName}>)value, options, tracker);");
                    }
    
                    using (builder.Block($"else if (type == typeof(global::System.Collections.Generic.IEnumerable<{type.FullName}>) || type == typeof(global::System.Collections.Generic.IList<{type.FullName}>) || type == typeof(global::System.Collections.Generic.ICollection<{type.FullName}>) || type == typeof({type.FullName}[]))"))
                    {
                        builder.AppendLine($"CollectionHelpers.WriteCollection_{safeName}(writer, (global::System.Collections.Generic.IEnumerable<{type.FullName}>)value, options, tracker);");
                    }

                    isFirst = false;
                }

                using (builder.Block("else"))
                {
                    builder.AppendLine("throw new NotSupportedException($\"Type {{type.FullName}} is not registered\");");
                }
            }

            builder.AppendLine();

            foreach (TypeInfo type in types)
            {
                bool needsTracking = typeMetadataMap.TryGetValue(type.FullName, out TypeMetadata metadata) && metadata.IsComplex;

                using (builder.Block($"internal static void WriteWithConverter_{metadata.SafeName}(Utf8JsonWriter writer, {type.FullName} value,  JsonSerializerOptions options, ReferenceTracker tracker)"))
                {
                    if (needsTracking)
                    {
                        using (builder.Block("try"))
                        {
                            builder.AppendLine($"{type.FieldName}.Tracker = tracker;");
                            builder.AppendLine($"{type.FieldName}.Write(writer, value, options);");
                        }
                        using (builder.Block("finally"))
                        {
                            builder.AppendLine($"{type.FieldName}.Tracker = null;");
                        }
                    }
                    else
                    {
                        builder.AppendLine($"{type.FieldName}.Write(writer, value, options);");
                    }
                }

                builder.AppendLine();
            }

            // CanSerialize method
            using (builder.Block("public bool CanSerialize(Type type)"))
            {
                builder.AppendLine("Type typeToCheck = type;");
                builder.AppendLine();
                
                builder.AppendLine("// Extract element type from arrays");
                using (builder.Block("if (type.IsArray)"))
                {
                    builder.AppendLine("typeToCheck = type.GetElementType();");
                }
                builder.AppendLine("// Extract element type from generic collections");
                using (builder.Block("else if (type.IsGenericType)"))
                {
                    builder.AppendLine("Type genericDef = type.GetGenericTypeDefinition();");
                    using (builder.Block("if (genericDef == typeof(global::System.Collections.Generic.List<>) || genericDef == typeof(global::System.Collections.Generic.IList<>) || genericDef == typeof(global::System.Collections.Generic.ICollection<>) || genericDef == typeof(global::System.Collections.Generic.IEnumerable<>))"))
                    {
                        builder.AppendLine("Type[] args = type.GetGenericArguments();");
                        using (builder.Block("if (args.Length == 1)"))
                        {
                            builder.AppendLine("typeToCheck = args[0];");
                        }
                    }
                }
                
                foreach (TypeInfo type in types)
                {
                    builder.AppendLine($"if (typeToCheck == typeof({type.FullName})) return true;");
                }
                builder.AppendLine("return false;");
            }

            builder.AppendLine();

            // Write Method
            using (builder.Block("public void Write(Utf8JsonWriter writer, object value, Type type, JsonSerializerOptions options, ReferenceTracker tracker)"))
            {
                builder.AppendLine("WriteWithConverter(writer, value, type, options, tracker);");
            }

            builder.AppendLine();

            // Serialize method
            using (builder.Block("public string Serialize(object value, Type type, GaldrJsonOptions options)"))
            {
                builder.AppendLine("JsonNamingPolicy namingPolicy = null;");
                using (builder.Block("if (options.PropertyNamingPolicy == PropertyNamingPolicy.CamelCase)"))
                {
                    builder.AppendLine("namingPolicy = JsonNamingPolicy.CamelCase;");
                }
                using (builder.Block("else if (options.PropertyNamingPolicy == PropertyNamingPolicy.SnakeCase)"))
                {
                    builder.AppendLine("namingPolicy = JsonNamingPolicy.SnakeCaseLower;");
                }
                using (builder.Block("else if (options.PropertyNamingPolicy == PropertyNamingPolicy.KebabCase)"))
                {
                    builder.AppendLine("namingPolicy = JsonNamingPolicy.KebabCaseLower;");
                }

                using (builder.Block("JsonSerializerOptions serializerOptions = new JsonSerializerOptions()", endWithSemiColon: true))
                {
                    builder.AppendLine("PropertyNameCaseInsensitive = options.PropertyNameCaseInsensitive,");
                    builder.AppendLine("PropertyNamingPolicy = namingPolicy");
                }
                builder.AppendLine();

                builder.AppendLine("Utf8JsonWriter writer = Utf8JsonWriterCache.RentWriter(options.WriteIndented, out System.Buffers.ArrayBufferWriter<byte> bufferWriter);");
                builder.AppendLine("ReferenceTracker tracker = options.DetectCycles ? new ReferenceTracker() : null;");
                builder.AppendLine();

                bool isFirst = true;
                foreach (TypeInfo type in types)
                {
                    bool needsTracking = typeMetadataMap.TryGetValue(type.FullName, out TypeMetadata metadata) && metadata.IsComplex;

                    string ifKeyword = isFirst ? "if" : "else if";
                    
                    using (builder.Block($"{ifKeyword} (type == typeof({type.FullName}))"))
                    {
                        if (needsTracking)
                        {
                            using (builder.Block("try"))
                            {
                                builder.AppendLine($"{type.FieldName}.Tracker = tracker;");
                                builder.AppendLine($"{type.FieldName}.Write(writer, ({type.FullName})value, serializerOptions);");
                            }
                            using (builder.Block("finally"))
                            {
                                builder.AppendLine($"{type.FieldName}.Tracker = null;");
                            }
                        }
                        else
                        {
                            builder.AppendLine($"{type.FieldName}.Write(writer, ({type.FullName})value, serializerOptions);");
                        }
                    }
                    
                    using (builder.Block($"else if (type == typeof(global::System.Collections.Generic.List<{type.FullName}>))"))
                    {
                        string safeName = TypeMetadata.GenerateSafeName(type.FullName);
                        builder.AppendLine($"CollectionHelpers.WriteCollection_{safeName}(writer, (global::System.Collections.Generic.List<{type.FullName}>)value, serializerOptions, {(needsTracking ? "tracker" : "null")});");
                    }
                    
                    using (builder.Block($"else if (type == typeof(global::System.Collections.Generic.IEnumerable<{type.FullName}>) || type == typeof(global::System.Collections.Generic.IList<{type.FullName}>) || type == typeof(global::System.Collections.Generic.ICollection<{type.FullName}>) || type == typeof({type.FullName}[]))"))
                    {
                        string safeName = TypeMetadata.GenerateSafeName(type.FullName);
                        if (needsTracking)
                        {
                            builder.AppendLine($"CollectionHelpers.WriteCollection_{safeName}(writer, (global::System.Collections.Generic.IEnumerable<{type.FullName}>)value, serializerOptions, tracker);");
                        }
                        else
                        {
                            builder.AppendLine($"CollectionHelpers.WriteCollection_{safeName}(writer, (global::System.Collections.Generic.IEnumerable<{type.FullName}>)value, serializerOptions, null);");
                        }
                    }

                    isFirst = false;
                }

                using (builder.Block("else"))
                {
                    builder.AppendLine("throw new NotSupportedException($\"Type {type} is not registered for serialization\");");
                }

                builder.AppendLine();
                builder.AppendLine("writer.Flush();");
                builder.AppendLine("return System.Text.Encoding.UTF8.GetString(bufferWriter.WrittenSpan);");
            }

            builder.AppendLine();

            // Read Method
            using (builder.Block("public object Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)"))
            {
                builder.AppendLine("return ReadWithConverter(ref reader, type, options);");
            }

            builder.AppendLine();

            // Deserialize method
            using (builder.Block("public object Deserialize(string json, Type type, GaldrJsonOptions options)"))
            {
                builder.AppendLine("JsonNamingPolicy namingPolicy = null;");
                using (builder.Block("if (options.PropertyNamingPolicy == PropertyNamingPolicy.CamelCase)"))
                {
                    builder.AppendLine("namingPolicy = JsonNamingPolicy.CamelCase;");
                }
                using (builder.Block("else if (options.PropertyNamingPolicy == PropertyNamingPolicy.SnakeCase)"))
                {
                    builder.AppendLine("namingPolicy = JsonNamingPolicy.SnakeCaseLower;");
                }
                using (builder.Block("else if (options.PropertyNamingPolicy == PropertyNamingPolicy.KebabCase)"))
                {
                    builder.AppendLine("namingPolicy = JsonNamingPolicy.KebabCaseLower;");
                }

                using (builder.Block("JsonSerializerOptions serializerOptions = new JsonSerializerOptions()", endWithSemiColon: true))
                {
                    builder.AppendLine("PropertyNameCaseInsensitive = options.PropertyNameCaseInsensitive,");
                    builder.AppendLine("PropertyNamingPolicy = namingPolicy");
                }
                builder.AppendLine();

                builder.AppendLine("byte[] tempArray = null;");
                builder.AppendLine();

                builder.AppendLine("// Allocation strategy: stackalloc for small, ArrayPool for medium, direct alloc for large");
                builder.AppendLine("const int MaxExpansionFactor = 3;");
                builder.AppendLine("const int StackallocThreshold = 512;");
                builder.AppendLine("const int ArrayPoolThreshold = 1048576; // 1MB");
                builder.AppendLine();

                builder.AppendLine("int maxByteCount = json.Length * MaxExpansionFactor;");
                builder.AppendLine();

                builder.AppendLine("// Use ternary to allow stackalloc");
                builder.AppendLine("System.Span<byte> utf8Bytes = maxByteCount <= StackallocThreshold");
                using (builder.Indent())
                {
                    builder.AppendLine("? stackalloc byte[StackallocThreshold]");
                    builder.AppendLine(": maxByteCount <= ArrayPoolThreshold");
                    using (builder.Indent())
                    {
                        builder.AppendLine("? (tempArray = System.Buffers.ArrayPool<byte>.Shared.Rent(maxByteCount))");
                        builder.AppendLine(": (tempArray = new byte[System.Text.Encoding.UTF8.GetByteCount(json)]);");
                    }
                }

                builder.AppendLine();

                using (builder.Block("try"))
                {
                    builder.AppendLine("int actualBytes = System.Text.Encoding.UTF8.GetBytes(json, utf8Bytes);");
                    builder.AppendLine("utf8Bytes = utf8Bytes.Slice(0, actualBytes);");
                    builder.AppendLine();

                    builder.AppendLine("var reader = new Utf8JsonReader(utf8Bytes);");
                    builder.AppendLine("reader.Read(); // Move to first token");
                    builder.AppendLine();

                    bool isFirst = true;
                    foreach (TypeInfo type in types)
                    {
                        string safeName = TypeMetadata.GenerateSafeName(type.FullName);
    
                        string ifKeyword = isFirst ? "if" : "else if";
    
                        using (builder.Block($"{ifKeyword} (type == typeof({type.FullName}))"))
                        {
                            builder.AppendLine($"return {type.FieldName}.Read(ref reader, typeof({type.FullName}), serializerOptions);");
                        }
    
                        using (builder.Block($"else if (type == typeof({type.FullName}[]))"))
                        {
                            builder.AppendLine($"return CollectionHelpers.ReadCollectionArray_{safeName}(ref reader, serializerOptions);");
                        }
    
                        using (builder.Block($"else if (type == typeof(global::System.Collections.Generic.List<{type.FullName}>) || type == typeof(global::System.Collections.Generic.IEnumerable<{type.FullName}>) || type == typeof(global::System.Collections.Generic.IList<{type.FullName}>) || type == typeof(global::System.Collections.Generic.ICollection<{type.FullName}>) || type == typeof({type.FullName}[]))"))
                        {
                            builder.AppendLine($"return CollectionHelpers.ReadCollection_{safeName}(ref reader, serializerOptions);");
                        }

                        isFirst = false;
                    }

                    using (builder.Block("else"))
                    {
                        builder.AppendLine("throw new NotSupportedException($\"Type {type.FullName} is not registered for deserialization\");");
                    }
                }

                using (builder.Block("finally"))
                {
                    using (builder.Block("if (tempArray != null)"))
                    {
                        builder.AppendLine("// Clear before returning to prevent data leakage");
                        builder.AppendLine("System.Array.Clear(tempArray, 0, tempArray.Length);");
                        using (builder.Block("if (maxByteCount <= ArrayPoolThreshold)"))
                        {
                            builder.AppendLine("System.Buffers.ArrayPool<byte>.Shared.Return(tempArray);");
                        }
                    }
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
