using System;

namespace GaldrJson.SourceGeneration
{
    /// <summary>
    /// Emits code for complex user-defined types requiring generated converters.
    /// TODO: Phase 2 - Implement proper converter instantiation (with caching from Phase 3).
    /// </summary>
    internal sealed class ComplexTypeCodeEmitter : CodeEmitter
    {
        public ComplexTypeCodeEmitter(TypeMetadata metadata) : base(metadata)
        {
            if (!metadata.IsComplex)
                throw new ArgumentException($"Type {metadata.FullyQualifiedName} is not a complex type.", nameof(metadata));
        }

        public override string EmitRead(string readerVar = "reader")
        {
            // Get the base type name for the converter
            var baseTypeName = GetBaseTypeName(Metadata.Symbol);
            var converterName = $"{baseTypeName}JsonConverter";

            // For now, create new instance (Phase 3 will add caching)
            // Generate: new PersonJsonConverter().Read(ref reader, typeof(Person), options)
            return $"new {converterName}().Read(ref {readerVar}, typeof({Metadata.FullyQualifiedName}), options)";
        }

        public override string EmitWrite(string writerVar, string valueExpr, string propertyName = null)
        {
            var baseTypeName = GetBaseTypeName(Metadata.Symbol);
            var converterName = $"{baseTypeName}JsonConverter";

            // If we have a property name, write it first
            if (propertyName != null)
            {
                return $@"{writerVar}.{WriterMethods.WritePropertyName}(""{propertyName}"");
            new {converterName}().Write({writerVar}, {valueExpr}, options);";
            }
            else
            {
                return $"new {converterName}().Write({writerVar}, {valueExpr}, options);";
            }
        }

        private static string GetBaseTypeName(Microsoft.CodeAnalysis.ITypeSymbol typeSymbol)
        {
            // Handle nullable types - get the underlying type name
            if (typeSymbol.NullableAnnotation == Microsoft.CodeAnalysis.NullableAnnotation.Annotated &&
                typeSymbol is Microsoft.CodeAnalysis.INamedTypeSymbol namedType &&
                namedType.TypeArguments.Length == 1)
            {
                return namedType.TypeArguments[0].Name;
            }

            return typeSymbol.Name;
        }
    }
}
