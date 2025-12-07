using System;

namespace GaldrJson.SourceGeneration
{
    /// <summary>
    /// Emits code for collection types (List, T[], IEnumerable).
    /// </summary>
    internal sealed class CollectionCodeEmitter : CodeEmitter
    {
        public CollectionCodeEmitter(TypeMetadata metadata) : base(metadata)
        {
            if (!metadata.IsCollection)
                throw new ArgumentException($"Type {metadata.FullyQualifiedName} is not a collection type.", nameof(metadata));

            if (metadata.ElementType == null)
                throw new ArgumentException($"Collection type {metadata.FullyQualifiedName} has no element type.", nameof(metadata));
        }

        public override string EmitRead(string readerVar = "reader")
        {
            // Determine if this is an array or list
            bool isArray = Metadata.Symbol.TypeKind == Microsoft.CodeAnalysis.TypeKind.Array;

            // Generate: (List<T>)CollectionHelpers.ReadCollection_SafeName(ref reader, options, isArray)
            return $"({Metadata.FullyQualifiedName})CollectionHelpers.ReadCollection_{Metadata.ElementType.SafeName}(ref {readerVar}, options, {isArray.ToString().ToLower()})";
        }

        public override string EmitWrite(string writerVar, string valueExpr, string propertyName = null)
        {
            // If we have a property name, write the property name first
            if (propertyName != null)
            {
                return $@"{writerVar}.{WriterMethods.WritePropertyName}(""{propertyName}"");
            CollectionHelpers.WriteCollection_{Metadata.ElementType.SafeName}({writerVar}, {valueExpr}, options);";
            }
            else
            {
                // For array elements, just write the collection directly
                return $"CollectionHelpers.WriteCollection_{Metadata.ElementType.SafeName}({writerVar}, {valueExpr}, options);";
            }
        }
    }
}
