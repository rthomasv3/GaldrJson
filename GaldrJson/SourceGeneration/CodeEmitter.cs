using System;

namespace GaldrJson.SourceGeneration
{
    /// <summary>
    /// Base class for generating read and write code for different type kinds.
    /// This replaces the scattered GetXXXReadCode/WriteCode methods with a unified strategy pattern.
    /// </summary>
    internal abstract class CodeEmitter
    {
        protected readonly TypeMetadata Metadata;

        protected CodeEmitter(TypeMetadata metadata)
        {
            Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        }

        /// <summary>
        /// Generates code to read a value from a Utf8JsonReader.
        /// </summary>
        /// <param name="readerVar">The name of the reader variable (default: "reader").</param>
        /// <returns>C# expression that reads the value.</returns>
        public abstract string EmitRead(string readerVar = "reader");

        /// <summary>
        /// Generates code to write a value to a Utf8JsonWriter.
        /// </summary>
        /// <param name="writerVar">The name of the writer variable.</param>
        /// <param name="valueExpr">Expression representing the value to write.</param>
        /// <param name="property">Property information for writing object properties.</param>
        /// <returns>C# statement(s) that write the value.</returns>
        public abstract string EmitWrite(string writerVar, string valueExpr, PropertyInfo property);

        /// <summary>
        /// Factory method to create the appropriate CodeEmitter for the given type metadata.
        /// </summary>
        public static CodeEmitter Create(TypeMetadata metadata)
        {
            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));

            switch (metadata.Kind)
            {
                case TypeKind.Primitive:
                    return new PrimitiveCodeEmitter(metadata);
                case TypeKind.SystemType:
                    return new SystemTypeCodeEmitter(metadata);
                case TypeKind.Enum:
                    return new EnumCodeEmitter(metadata);
                case TypeKind.Nullable:
                    return new NullableCodeEmitter(metadata);
                case TypeKind.Collection:
                    return new CollectionCodeEmitter(metadata);
                case TypeKind.Dictionary:
                    return new DictionaryCodeEmitter(metadata);
                case TypeKind.ByteArray:
                    return new ByteArrayCodeEmitter(metadata);
                case TypeKind.Complex:
                    return new ComplexTypeCodeEmitter(metadata);
                default:
                    throw new NotSupportedException($"Type kind {metadata.Kind} is not supported.");
            }
        }

        protected string GetPropertyNameExpression(PropertyInfo property)
        {
            if (property == null)
                return null;  // Array element case - no property name

            return $"NameHelpers.GetPropertyNameUtf8(Prop_{property.Name}_Exact, Prop_{property.Name}_Camel, Prop_{property.Name}_Snake, Prop_{property.Name}_Kebab, Prop_{property.Name}_Custom, options)";
        }
    }
}
