namespace GaldrJson.SourceGeneration
{
    /// <summary>
    /// Constants for common code patterns.
    /// </summary>
    internal static class CodePatterns
    {
        public const string NullCheck = "if ({0} == null)";
        public const string HasValueCheck = "if ({0}.HasValue)";
        public const string TokenTypeCheck = "if (reader.TokenType == {0})";
        public const string ThrowJsonException = "throw new JsonException(\"{0}\")";
    }
}
