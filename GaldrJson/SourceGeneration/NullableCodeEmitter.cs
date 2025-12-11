using System;

namespace GaldrJson.SourceGeneration
{
    /// <summary>
    /// Emits code for nullable value types (int?, MyEnum?, etc.).
    /// Wraps the underlying type's emitter with null-checking logic.
    /// </summary>
    internal sealed class NullableCodeEmitter : CodeEmitter
    {
        private readonly CodeEmitter _underlyingEmitter;

        public NullableCodeEmitter(TypeMetadata metadata) : base(metadata)
        {
            if (!metadata.IsNullable)
                throw new ArgumentException($"Type {metadata.FullyQualifiedName} is not a nullable type.", nameof(metadata));

            if (metadata.UnderlyingType == null)
                throw new ArgumentException($"Nullable type {metadata.FullyQualifiedName} has no underlying type.", nameof(metadata));

            // Create an emitter for the underlying type
            _underlyingEmitter = CodeEmitter.Create(metadata.UnderlyingType);
        }

        public override string EmitRead(string readerVar = "reader")
        {
            // Generate: reader.TokenType == JsonTokenType.Null ? null : (int?)reader.GetInt32()
            var underlyingRead = _underlyingEmitter.EmitRead(readerVar);
            return $"{readerVar}.TokenType == {JsonTokenTypes.Null} ? null : ({Metadata.FullyQualifiedName}){underlyingRead}";
        }

        public override string EmitWrite(string writerVar, string valueExpr, PropertyInfo property)
        {
            // Generate null-checking code with proper indentation
            var underlyingWrite = _underlyingEmitter.EmitWrite(writerVar, $"{valueExpr}.Value", property);

            if (property != null)
            {
                string propNameExpr = GetPropertyNameExpression(property);

                // For properties, write null value when HasValue is false
                return $@"if ({valueExpr}.HasValue)
            {{
                {underlyingWrite}
            }}
            else
            {{
                {writerVar}.{WriterMethods.WriteNull}({propNameExpr});
            }}";
            }
            else
            {
                // For array elements, write null value
                return $@"if ({valueExpr}.HasValue)
            {{
                {underlyingWrite}
            }}
            else
            {{
                {writerVar}.{WriterMethods.WriteNullValue}();
            }}";
            }
        }
    }
}
