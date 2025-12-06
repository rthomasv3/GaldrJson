using System;

namespace GaldrJson.SourceGeneration
{
    /// <summary>
    /// Emits code for dictionary types (Dictionary&lt;K,V&gt;, IDictionary&lt;K,V&gt;).
    /// TODO: Phase 2 - Implement full dictionary handling with helper methods.
    /// </summary>
    internal sealed class DictionaryCodeEmitter : CodeEmitter
    {
        public DictionaryCodeEmitter(TypeMetadata metadata) : base(metadata)
        {
            if (!metadata.IsDictionary)
                throw new ArgumentException($"Type {metadata.FullyQualifiedName} is not a dictionary type.", nameof(metadata));

            if (!metadata.DictionaryTypes.HasValue)
                throw new ArgumentException($"Dictionary type {metadata.FullyQualifiedName} has no key/value types.", nameof(metadata));
        }

        public override string EmitRead(string readerVar = "reader")
        {
            var (keyMetadata, valueMetadata) = Metadata.DictionaryTypes.Value;
            var helperName = $"{keyMetadata.SafeName}_{valueMetadata.SafeName}";

            // Generate: DictionaryHelpers.ReadDictionary_SafeName(ref reader, options)
            return $"DictionaryHelpers.ReadDictionary_{helperName}(ref {readerVar}, options)";
        }

        public override string EmitWrite(string writerVar, string valueExpr, string propertyName = null)
        {
            var (keyMetadata, valueMetadata) = Metadata.DictionaryTypes.Value;
            var helperName = $"{keyMetadata.SafeName}_{valueMetadata.SafeName}";

            // If we have a property name, write the property name first
            if (propertyName != null)
            {
                return $@"{writerVar}.{WriterMethods.WritePropertyName}(""{propertyName}"");
            DictionaryHelpers.WriteDictionary_{helperName}({writerVar}, {valueExpr}, options);";
            }
            else
            {
                return $"DictionaryHelpers.WriteDictionary_{helperName}({writerVar}, {valueExpr}, options);";
            }
        }
    }
}
