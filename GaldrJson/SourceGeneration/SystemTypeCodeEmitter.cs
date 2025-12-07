using System;

namespace GaldrJson.SourceGeneration
{
    /// <summary>
    /// Emits code for system types with built-in JSON support (Guid, TimeSpan, DateTimeOffset).
    /// </summary>
    internal sealed class SystemTypeCodeEmitter : CodeEmitter
    {
        public SystemTypeCodeEmitter(TypeMetadata metadata) : base(metadata)
        {
            if (!metadata.IsSystemType)
                throw new ArgumentException($"Type {metadata.FullyQualifiedName} is not a system type.", nameof(metadata));
        }

        public override string EmitRead(string readerVar = "reader")
        {
            var typeName = Metadata.FullyQualifiedName;
            var normalizedName = typeName.StartsWith(WellKnownTypes.GlobalPrefix)
                ? typeName.Substring(WellKnownTypes.GlobalPrefix.Length)
                : typeName;

            if (normalizedName.Contains("Guid"))
                return $"{readerVar}.{ReaderMethods.GetGuid}";

            if (normalizedName.Contains("DateTimeOffset"))
                return $"{readerVar}.{ReaderMethods.GetDateTimeOffset}";

            if (normalizedName.Contains("TimeSpan"))
                return $"System.TimeSpan.FromTicks({readerVar}.{ReaderMethods.GetInt64})";

            throw new NotSupportedException($"System type {typeName} is not supported.");
        }

        public override string EmitWrite(string writerVar, string valueExpr, string propertyName = null, string nameOverride = null)
        {
            var typeName = Metadata.FullyQualifiedName;
            var normalizedName = typeName.StartsWith(WellKnownTypes.GlobalPrefix)
                ? typeName.Substring(WellKnownTypes.GlobalPrefix.Length)
                : typeName;

            // Guid and DateTimeOffset serialize as strings
            if (normalizedName.Contains("Guid") || normalizedName.Contains("DateTimeOffset"))
            {
                if (nameOverride != null)
                    return $"{writerVar}.{WriterMethods.WriteString}(\"{nameOverride}\", {valueExpr});";
                else if (propertyName != null)
                    return $"{writerVar}.{WriterMethods.WriteString}(NameHelpers.GetPropertyName(\"{propertyName}\", options), {valueExpr});";
                else
                    return $"{writerVar}.{WriterMethods.WriteStringValue}({valueExpr});";
            }

            // TimeSpan serializes as ticks (number)
            if (normalizedName.Contains("TimeSpan"))
            {
                if (nameOverride != null)
                    return $"{writerVar}.{WriterMethods.WriteNumber}(\"{nameOverride}\", {valueExpr}.Ticks);";
                else if (propertyName != null)
                    return $"{writerVar}.{WriterMethods.WriteNumber}(NameHelpers.GetPropertyName(\"{propertyName}\", options), {valueExpr}.Ticks);";
                else
                    return $"{writerVar}.{WriterMethods.WriteNumberValue}({valueExpr}.Ticks);";
            }

            throw new NotSupportedException($"System type {typeName} is not supported.");
        }
    }
}
