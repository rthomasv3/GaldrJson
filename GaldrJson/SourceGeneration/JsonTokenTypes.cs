namespace GaldrJson.SourceGeneration
{
    /// <summary>
    /// Constants for JSON token types.
    /// </summary>
    internal static class JsonTokenTypes
    {
        public const string StartObject = "JsonTokenType.StartObject";
        public const string EndObject = "JsonTokenType.EndObject";
        public const string StartArray = "JsonTokenType.StartArray";
        public const string EndArray = "JsonTokenType.EndArray";
        public const string PropertyName = "JsonTokenType.PropertyName";
        public const string String = "JsonTokenType.String";
        public const string Number = "JsonTokenType.Number";
        public const string True = "JsonTokenType.True";
        public const string False = "JsonTokenType.False";
        public const string Null = "JsonTokenType.Null";
    }
}
