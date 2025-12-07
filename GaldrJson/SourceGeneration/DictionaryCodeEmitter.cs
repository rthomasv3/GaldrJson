using System;

namespace GaldrJson.SourceGeneration
{
    /// <summary>
    /// Emits code for dictionary types (Dictionary, IDictionary).
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

        public override string EmitWrite(string writerVar, string valueExpr, string propertyName = null, string nameOverride = null)
        {
            var (keyMetadata, valueMetadata) = Metadata.DictionaryTypes.Value;
            var helperName = $"{keyMetadata.SafeName}_{valueMetadata.SafeName}";

            // If we have a property name, write the property name first
            if (nameOverride != null)
            {
                return $@"{writerVar}.{WriterMethods.WritePropertyName}(""{nameOverride}"");
            DictionaryHelpers.WriteDictionary_{helperName}({writerVar}, {valueExpr}, options);";
            }
            else if (propertyName != null)
            {
                return $@"{writerVar}.{WriterMethods.WritePropertyName}(NameHelpers.GetPropertyName(""{propertyName}"", options));
            DictionaryHelpers.WriteDictionary_{helperName}({writerVar}, {valueExpr}, options);";
            }
            else
            {
                return $"DictionaryHelpers.WriteDictionary_{helperName}({writerVar}, {valueExpr}, options);";
            }
        }
    }
}
