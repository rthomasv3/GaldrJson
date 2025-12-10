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

            if (isArray)
            {
                return $"CollectionHelpers.ReadCollectionArray_{Metadata.ElementType.SafeName}(ref {readerVar}, options)";
            }
            else
            {
                // List<T>, IList<T>, ICollection<T>, IEnumerable<T> all get List<T>
                return $"CollectionHelpers.ReadCollection_{Metadata.ElementType.SafeName}(ref {readerVar}, options)";
            }
        }

        public override string EmitWrite(string writerVar, string valueExpr, string propertyName = null, string nameOverride = null)
        {
            // If we have a property name, write the property name first
            if (nameOverride != null)
            {
                return $@"{writerVar}.{WriterMethods.WritePropertyName}(""{nameOverride}"");
            CollectionHelpers.WriteCollection_{Metadata.ElementType.SafeName}({writerVar}, {valueExpr}, options, Tracker);";
            }
            else if (propertyName != null)
            {
                return $@"{writerVar}.{WriterMethods.WritePropertyName}(NameHelpers.GetPropertyName(""{propertyName}"", options));
            CollectionHelpers.WriteCollection_{Metadata.ElementType.SafeName}({writerVar}, {valueExpr}, options, Tracker);";
            }
            else
            {
                // For array elements, just write the collection directly
                return $"CollectionHelpers.WriteCollection_{Metadata.ElementType.SafeName}({writerVar}, {valueExpr}, options, Tracker);";
            }
        }
    }
}
