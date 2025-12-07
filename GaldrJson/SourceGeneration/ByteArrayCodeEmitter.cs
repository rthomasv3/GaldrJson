using System;

namespace GaldrJson.SourceGeneration
{
    internal sealed class ByteArrayCodeEmitter : CodeEmitter
    {
        public ByteArrayCodeEmitter(TypeMetadata metadata) : base(metadata)
        {
            if (metadata.Kind != TypeKind.ByteArray)
                throw new ArgumentException($"Type {metadata.FullyQualifiedName} is not a byte array type.", nameof(metadata));
        }

        public override string EmitRead(string readerVar = "reader")
        {
            // Determine if this is a byte[] or List<byte>/IEnumerable<byte>
            bool isArray = Metadata.Symbol.TypeKind == Microsoft.CodeAnalysis.TypeKind.Array;

            if (isArray)
            {
                return $"{readerVar}.TokenType == {JsonTokenTypes.Null} ? null : System.Convert.FromBase64String({readerVar}.GetString() ?? string.Empty)";
            }
            else
            {
                // For List<byte>, IEnumerable<byte>, etc., convert to List
                return $"{readerVar}.TokenType == {JsonTokenTypes.Null} ? null : new System.Collections.Generic.List<byte>(System.Convert.FromBase64String({readerVar}.GetString() ?? string.Empty))";
            }
        }

        public override string EmitWrite(string writerVar, string valueExpr, string propertyName = null)
        {
            // Need to convert List<byte> to byte[] for ToBase64String
            bool isArray = Metadata.Symbol.TypeKind == Microsoft.CodeAnalysis.TypeKind.Array;
            string base64Expr = isArray ? valueExpr : $"{valueExpr}.ToArray()";

            if (propertyName != null)
            {
                return $@"if ({valueExpr} != null)
                {writerVar}.WriteString(""{propertyName}"", System.Convert.ToBase64String({base64Expr}));
            else
                {writerVar}.WriteNull(""{propertyName}"");";
            }
            else
            {
                return $@"if ({valueExpr} != null)
                {writerVar}.WriteStringValue(System.Convert.ToBase64String({base64Expr}));
            else
                {writerVar}.WriteNullValue();";
            }
        }
    }
}
