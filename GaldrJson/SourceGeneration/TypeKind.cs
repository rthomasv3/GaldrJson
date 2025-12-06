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
        /// Collection types (List&lt;T&gt;, T[], IEnumerable&lt;T&gt;).
        /// </summary>
        Collection,

        /// <summary>
        /// Dictionary types (Dictionary&lt;K,V&gt;, IDictionary&lt;K,V&gt;).
        /// </summary>
        Dictionary,

        /// <summary>
        /// Complex user-defined types requiring generated converters.
        /// </summary>
        Complex
    }
}
