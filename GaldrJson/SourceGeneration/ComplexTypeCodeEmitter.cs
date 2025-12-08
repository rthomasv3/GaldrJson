using System;

namespace GaldrJson.SourceGeneration
{
    /// <summary>
    /// Emits code for complex user-defined types requiring generated converters.
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
            return $"({Metadata.FullyQualifiedName})GeneratedJsonSerializer.ReadWithConverter(ref {readerVar}, typeof({Metadata.FullyQualifiedName}), options)";
        }

        public override string EmitWrite(string writerVar, string valueExpr, string propertyName = null, string nameOverride = null)
        {
            var baseTypeName = GetBaseTypeName(Metadata.Symbol);
            var converterName = $"{baseTypeName}JsonConverter";

            // If we have a property name, write it first
            if (nameOverride != null)
            {
                return $@"{writerVar}.WritePropertyName(""{nameOverride}"");
            GeneratedJsonSerializer.WriteWithConverter({writerVar}, {valueExpr}, typeof({Metadata.FullyQualifiedName}), options, Tracker);";
            }
            else if (propertyName != null)
            {
                return $@"{writerVar}.WritePropertyName(NameHelpers.GetPropertyName(""{propertyName}"", options));
            GeneratedJsonSerializer.WriteWithConverter({writerVar}, {valueExpr}, typeof({Metadata.FullyQualifiedName}), options, Tracker);";
            }
            else
            {
                return $"GeneratedJsonSerializer.WriteWithConverter({writerVar}, {valueExpr}, typeof({Metadata.FullyQualifiedName}), options, Tracker);";
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
