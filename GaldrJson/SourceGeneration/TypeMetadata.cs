using System;
using Microsoft.CodeAnalysis;

namespace GaldrJson.SourceGeneration
{
    /// <summary>
    /// Represents metadata about a type for JSON serialization code generation.
    /// This class replaces string-based type matching with a proper type classification system.
    /// </summary>
    internal sealed class TypeMetadata
    {
        private TypeMetadata(
            ITypeSymbol symbol,
            TypeKind kind,
            string fullyQualifiedName,
            string safeName,
            TypeMetadata elementType = null,
            TypeMetadata underlyingType = null,
            (TypeMetadata Key, TypeMetadata Value)? dictionaryTypes = null)
        {
            Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
            Kind = kind;
            FullyQualifiedName = fullyQualifiedName ?? throw new ArgumentNullException(nameof(fullyQualifiedName));
            SafeName = safeName ?? throw new ArgumentNullException(nameof(safeName));
            ElementType = elementType;
            UnderlyingType = underlyingType;
            DictionaryTypes = dictionaryTypes;
        }

        /// <summary>
        /// The Roslyn type symbol this metadata represents.
        /// </summary>
        public ITypeSymbol Symbol { get; }

        /// <summary>
        /// The classification of this type for code generation purposes.
        /// </summary>
        public TypeKind Kind { get; }

        /// <summary>
        /// Fully qualified type name (e.g., "global::System.Int32").
        /// Computed once and cached.
        /// </summary>
        public string FullyQualifiedName { get; }

        /// <summary>
        /// Safe name for use in generated method/class names.
        /// Computed once and cached.
        /// </summary>
        public string SafeName { get; }

        /// <summary>
        /// For collection types, the element type metadata.
        /// </summary>
        public TypeMetadata ElementType { get; }

        /// <summary>
        /// For nullable types, the underlying non-nullable type metadata.
        /// </summary>
        public TypeMetadata UnderlyingType { get; }

        /// <summary>
        /// For dictionary types, the key and value type metadata.
        /// </summary>
        public (TypeMetadata Key, TypeMetadata Value)? DictionaryTypes { get; }

        // Convenience properties
        public bool IsPrimitive => Kind == TypeKind.Primitive;
        public bool IsEnum => Kind == TypeKind.Enum;
        public bool IsSystemType => Kind == TypeKind.SystemType;
        public bool IsNullable => Kind == TypeKind.Nullable;
        public bool IsCollection => Kind == TypeKind.Collection;
        public bool IsDictionary => Kind == TypeKind.Dictionary;
        public bool IsComplex => Kind == TypeKind.Complex;

        /// <summary>
        /// Returns true if this type requires a generated converter class.
        /// </summary>
        public bool RequiresConverter => Kind == TypeKind.Complex;

        /// <summary>
        /// Returns true if this type requires helper methods (for collections/dictionaries).
        /// </summary>
        public bool RequiresHelpers => Kind == TypeKind.Collection || Kind == TypeKind.Dictionary;

        /// <summary>
        /// Creates TypeMetadata for the given type symbol, using the cache to avoid recreating metadata.
        /// </summary>
        public static TypeMetadata Create(ITypeSymbol symbol, TypeMetadataCache cache)
        {
            if (symbol == null)
                throw new ArgumentNullException(nameof(symbol));
            if (cache == null)
                throw new ArgumentNullException(nameof(cache));

            // Check if it's a nullable value type first
            if (IsNullableValueType(symbol, out var underlyingSymbol))
            {
                var underlyingMetadata = cache.GetOrCreate(underlyingSymbol);
                var fullyQualifiedName = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var safeName = GenerateSafeName(fullyQualifiedName);

                return new TypeMetadata(
                    symbol,
                    TypeKind.Nullable,
                    fullyQualifiedName,
                    safeName,
                    underlyingType: underlyingMetadata);
            }

            // Classify the type
            var kind = ClassifyType(symbol);
            var fqn = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var safe = GenerateSafeName(fqn);

            TypeMetadata elementType = null;
            TypeMetadata underlyingType = null;
            (TypeMetadata Key, TypeMetadata Value)? dictionaryTypes = null;

            // For collections, get element type metadata
            if (kind == TypeKind.Collection)
            {
                var elementSymbol = GetCollectionElementType(symbol);
                if (elementSymbol != null)
                {
                    elementType = cache.GetOrCreate(elementSymbol);
                }
            }
            // For dictionaries, get key and value type metadata
            else if (kind == TypeKind.Dictionary)
            {
                var dictSymbols = GetDictionaryTypes(symbol);
                if (dictSymbols.HasValue)
                {
                    var keyMetadata = cache.GetOrCreate(dictSymbols.Value.keyType);
                    var valueMetadata = cache.GetOrCreate(dictSymbols.Value.valueType);
                    dictionaryTypes = (keyMetadata, valueMetadata);
                }
            }

            return new TypeMetadata(
                symbol,
                kind,
                fqn,
                safe,
                elementType,
                underlyingType,
                dictionaryTypes);
        }

        private static TypeKind ClassifyType(ITypeSymbol symbol)
        {
            // Check for primitive types (including DateTime which has SpecialType.System_DateTime)
            if (symbol.SpecialType != SpecialType.None)
            {
                return TypeKind.Primitive;
            }

            // Check for enum
            if (symbol.TypeKind == Microsoft.CodeAnalysis.TypeKind.Enum)
            {
                return TypeKind.Enum;
            }

            if (IsByteCollection(symbol))
            {
                return TypeKind.ByteArray;
            }

            if (IsCollectionType(symbol))
            {
                return TypeKind.Collection;
            }

            if (IsDictionaryType(symbol))
            {
                return TypeKind.Dictionary;
            }

            // Check for system types (Guid, TimeSpan, DateTimeOffset - but NOT DateTime which is primitive)
            var fullName = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var normalizedName = fullName.StartsWith("global::") ? fullName.Substring(8) : fullName;

            if ((normalizedName.StartsWith("System.") || normalizedName.StartsWith("global::System.")) &&
                (normalizedName.Contains("Guid") ||
                 normalizedName.Contains("TimeSpan") ||
                 normalizedName.Contains("DateTimeOffset")))
            {
                return TypeKind.SystemType;
            }

            // Default to complex type
            return TypeKind.Complex;
        }

        private static bool IsNullableValueType(ITypeSymbol symbol, out ITypeSymbol underlyingType)
        {
            underlyingType = null;

            // Check if it's Nullable<T>
            if (symbol is INamedTypeSymbol namedType &&
                namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
            {
                // Get the underlying type (T in Nullable<T>)
                underlyingType = namedType.TypeArguments[0];
                return true;
            }

            return false;
        }

        private static bool IsByteCollection(ITypeSymbol symbol)
        {
            // Check for byte[]
            if (symbol is IArrayTypeSymbol arraySymbol &&
                arraySymbol.ElementType.SpecialType == SpecialType.System_Byte)
            {
                return true;
            }

            // Check for generic byte collections (List<byte>, IEnumerable<byte>, etc.)
            if (symbol is INamedTypeSymbol namedTypeSymbol &&
                namedTypeSymbol.TypeArguments.Length == 1)
            {
                var elementType = namedTypeSymbol.TypeArguments[0];
                if (elementType.SpecialType == SpecialType.System_Byte)
                {
                    var fullName = namedTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    var normalizedName = fullName.StartsWith("global::") ? fullName.Substring(8) : fullName;

                    // Check if it's a collection type
                    if (normalizedName.StartsWith("System.Collections.Generic.List<") ||
                        normalizedName.StartsWith("System.Collections.Generic.IList<") ||
                        normalizedName.StartsWith("System.Collections.Generic.ICollection<") ||
                        normalizedName.StartsWith("System.Collections.Generic.IEnumerable<"))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsCollectionType(ITypeSymbol symbol)
        {
            // Handle arrays
            if (symbol.TypeKind == Microsoft.CodeAnalysis.TypeKind.Array)
                return true;

            // Handle generic collections
            if (symbol is INamedTypeSymbol namedTypeSymbol)
            {
                var fullName = namedTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var normalizedName = fullName.StartsWith("global::") ? fullName.Substring(8) : fullName;

                // Check for common collection types - use StartsWith to avoid matching Dictionary<string, List<int>>
                if (normalizedName.StartsWith("System.Collections.Generic.List<") ||
                    normalizedName.StartsWith("System.Collections.Generic.IList<") ||
                    normalizedName.StartsWith("System.Collections.Generic.ICollection<") ||
                    normalizedName.StartsWith("System.Collections.Generic.IEnumerable<"))
                    return true;
            }

            return false;
        }

        private static bool IsDictionaryType(ITypeSymbol symbol)
        {
            if (symbol is INamedTypeSymbol namedTypeSymbol)
            {
                var fullName = namedTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var normalizedName = fullName.StartsWith("global::") ? fullName.Substring(8) : fullName;

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

        private static string GenerateSafeName(string typeName)
        {
            return typeName
                .Replace("global::", "")
                .Replace(".", "_")
                .Replace("<", "_")
                .Replace(">", "_")
                .Replace("?", "_Nullable")
                .Replace(",", "_")
                .Replace(" ", "");
        }

        public override string ToString()
        {
            return $"{Kind}: {FullyQualifiedName}";
        }
    }
}
