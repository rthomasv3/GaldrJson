using System;
using Microsoft.CodeAnalysis;

namespace GaldrJson.SourceGeneration
{
    /// <summary>
    /// Emits code for primitive types (int, string, bool, etc.).
    /// </summary>
    internal sealed class PrimitiveCodeEmitter : CodeEmitter
    {
        public PrimitiveCodeEmitter(TypeMetadata metadata) : base(metadata)
        {
            if (!metadata.IsPrimitive)
                throw new ArgumentException($"Type {metadata.FullyQualifiedName} is not a primitive type.", nameof(metadata));
        }

        public override string EmitRead(string readerVar = "reader")
        {
            switch (Metadata.Symbol.SpecialType)
            {
                case SpecialType.System_Int32:
                    return $"{readerVar}.{ReaderMethods.GetInt32}";
                case SpecialType.System_Int64:
                    return $"{readerVar}.{ReaderMethods.GetInt64}";
                case SpecialType.System_Int16:
                    return $"{readerVar}.{ReaderMethods.GetInt16}";
                case SpecialType.System_Byte:
                    return $"{readerVar}.{ReaderMethods.GetByte}";
                case SpecialType.System_SByte:
                    return $"{readerVar}.{ReaderMethods.GetSByte}";
                case SpecialType.System_UInt16:
                    return $"{readerVar}.{ReaderMethods.GetUInt16}";
                case SpecialType.System_UInt32:
                    return $"{readerVar}.{ReaderMethods.GetUInt32}";
                case SpecialType.System_UInt64:
                    return $"{readerVar}.{ReaderMethods.GetUInt64}";
                case SpecialType.System_Single:
                    return $"{readerVar}.{ReaderMethods.GetSingle}";
                case SpecialType.System_Double:
                    return $"{readerVar}.{ReaderMethods.GetDouble}";
                case SpecialType.System_Decimal:
                    return $"{readerVar}.{ReaderMethods.GetDecimal}";
                case SpecialType.System_Boolean:
                    return $"{readerVar}.{ReaderMethods.GetBoolean}";
                case SpecialType.System_String:
                    return $"{readerVar}.{ReaderMethods.GetString}";
                case SpecialType.System_Char:
                    return $"{readerVar}.{ReaderMethods.GetString}?[0] ?? '\\0'";
                case SpecialType.System_DateTime:
                    return $"{readerVar}.{ReaderMethods.GetDateTime}";
                default:
                    throw new NotSupportedException($"Primitive type {Metadata.Symbol.SpecialType} is not supported.");
            }
        }

        public override string EmitWrite(string writerVar, string valueExpr, PropertyInfo property)
        {
            var specialType = Metadata.Symbol.SpecialType;

            string propNameExpr = GetPropertyNameExpression(property);

            // Handle string separately for null checking
            if (specialType == SpecialType.System_String)
            {
                if (propNameExpr != null)
                {
                    return $"{writerVar}.{WriterMethods.WriteString}({propNameExpr}, {valueExpr});";
                }
                else
                {
                    return $@"if ({valueExpr} != null)
                {writerVar}.{WriterMethods.WriteStringValue}({valueExpr});
            else
                {writerVar}.{WriterMethods.WriteNullValue}();";
                }
            }

            // Handle char specially (needs ToString())
            if (specialType == SpecialType.System_Char)
            {
                if (propNameExpr != null)
                    return $"{writerVar}.{WriterMethods.WriteString}({propNameExpr}, {valueExpr}.ToString());";
                else
                    return $"{writerVar}.{WriterMethods.WriteStringValue}({valueExpr}.ToString());";
            }

            // Numeric types
            if (IsNumericType(specialType))
            {
                if (propNameExpr != null)
                    return $"{writerVar}.{WriterMethods.WriteNumber}({propNameExpr}, {valueExpr});";
                else
                    return $"{writerVar}.{WriterMethods.WriteNumberValue}({valueExpr});";
            }

            // Boolean
            if (specialType == SpecialType.System_Boolean)
            {
                if (propNameExpr != null)
                    return $"{writerVar}.{WriterMethods.WriteBoolean}({propNameExpr}, {valueExpr});";
                else
                    return $"{writerVar}.{WriterMethods.WriteBooleanValue}({valueExpr});";
            }

            // DateTime
            if (specialType == SpecialType.System_DateTime)
            {
                if (propNameExpr != null)
                    return $"{writerVar}.{WriterMethods.WriteString}({propNameExpr}, {valueExpr});";
                else
                    return $"{writerVar}.{WriterMethods.WriteStringValue}({valueExpr});";
            }

            throw new NotSupportedException($"Primitive type {specialType} is not supported for writing.");
        }

        private static bool IsNumericType(SpecialType specialType)
        {
            return specialType == SpecialType.System_Byte ||
                   specialType == SpecialType.System_SByte ||
                   specialType == SpecialType.System_Int16 ||
                   specialType == SpecialType.System_UInt16 ||
                   specialType == SpecialType.System_Int32 ||
                   specialType == SpecialType.System_UInt32 ||
                   specialType == SpecialType.System_Int64 ||
                   specialType == SpecialType.System_UInt64 ||
                   specialType == SpecialType.System_Single ||
                   specialType == SpecialType.System_Double ||
                   specialType == SpecialType.System_Decimal;
        }
    }
}
