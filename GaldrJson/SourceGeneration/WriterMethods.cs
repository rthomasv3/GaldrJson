namespace GaldrJson.SourceGeneration
{
    /// <summary>
    /// Constants for Utf8JsonWriter method names.
    /// </summary>
    internal static class WriterMethods
    {
        // Value writers (no property name)
        public const string WriteNumberValue = "WriteNumberValue";
        public const string WriteStringValue = "WriteStringValue";
        public const string WriteBooleanValue = "WriteBooleanValue";
        public const string WriteNullValue = "WriteNullValue";
        public const string WriteStartArray = "WriteStartArray";
        public const string WriteEndArray = "WriteEndArray";
        public const string WriteStartObject = "WriteStartObject";
        public const string WriteEndObject = "WriteEndObject";

        // Property writers (with property name)
        public const string WriteNumber = "WriteNumber";
        public const string WriteString = "WriteString";
        public const string WriteBoolean = "WriteBoolean";
        public const string WriteNull = "WriteNull";
        public const string WritePropertyName = "WritePropertyName";

        public const string Flush = "Flush";
    }
}
