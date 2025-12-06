using System;

namespace GaldrJson.SourceGeneration
{
    /// <summary>
    /// Emits code for enum types (serialized as integers).
    /// </summary>
    internal sealed class EnumCodeEmitter : CodeEmitter
    {
        public EnumCodeEmitter(TypeMetadata metadata) : base(metadata)
        {
            if (!metadata.IsEnum)
                throw new ArgumentException($"Type {metadata.FullyQualifiedName} is not an enum.", nameof(metadata));
        }

        public override string EmitRead(string readerVar = "reader")
        {
            return $"({Metadata.FullyQualifiedName}){readerVar}.{ReaderMethods.GetInt32}";
        }

        public override string EmitWrite(string writerVar, string valueExpr, string propertyName = null)
        {
            if (propertyName != null)
                return $"{writerVar}.{WriterMethods.WriteNumber}(\"{propertyName}\", (int){valueExpr});";
            else
                return $"{writerVar}.{WriterMethods.WriteNumberValue}((int){valueExpr});";
        }
    }
}
