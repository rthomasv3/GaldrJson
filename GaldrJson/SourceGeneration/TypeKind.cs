namespace GaldrJson.SourceGeneration
{
    /// <summary>
    /// Classification of types for code generation purposes.
    /// </summary>
    internal enum TypeKind
    {
        /// <summary>
        /// Primitive types (int, string, bool, etc.) - use reader.GetXXX() directly.
        /// </summary>
        Primitive,

        /// <summary>
        /// System types with built-in JSON support (Guid, DateTime, TimeSpan, DateTimeOffset).
        /// </summary>
        SystemType,

        /// <summary>
        /// Enum types - serialize as integers.
        /// </summary>
        Enum,

        /// <summary>
        /// Nullable value types (int?, MyEnum?, etc.).
        /// </summary>
        Nullable,

        /// <summary>
        /// Collection types (List, T[], IEnumerable).
        /// </summary>
        Collection,

        /// <summary>
        /// Dictionary types (Dictionary, IDictionary).
        /// </summary>
        Dictionary,

        /// <summary>
        /// An array of bytes (byte[])
        /// </summary>
        ByteArray,

        /// <summary>
        /// Complex user-defined types requiring generated converters.
        /// </summary>
        Complex,
    }
}
