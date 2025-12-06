namespace GaldrJson.SourceGeneration
{
    /// <summary>
    /// Constants for well-known type names to eliminate magic strings throughout the code.
    /// </summary>
    internal static class WellKnownTypes
    {
        // Prefixes
        public const string GlobalPrefix = "global::";
        public const string SystemPrefix = "System.";

        // Primitive types (full names)
        public const string Int32 = "System.Int32";
        public const string Int64 = "System.Int64";
        public const string Int16 = "System.Int16";
        public const string Byte = "System.Byte";
        public const string SByte = "System.SByte";
        public const string UInt16 = "System.UInt16";
        public const string UInt32 = "System.UInt32";
        public const string UInt64 = "System.UInt64";
        public const string Single = "System.Single";
        public const string Double = "System.Double";
        public const string Decimal = "System.Decimal";
        public const string Boolean = "System.Boolean";
        public const string String = "System.String";
        public const string Char = "System.Char";
        public const string DateTime = "System.DateTime";
        public const string DateTimeOffset = "System.DateTimeOffset";
        public const string TimeSpan = "System.TimeSpan";
        public const string Guid = "System.Guid";

        // Collection types
        public const string ListPrefix = "System.Collections.Generic.List<";
        public const string IListPrefix = "System.Collections.Generic.IList<";
        public const string ICollectionPrefix = "System.Collections.Generic.ICollection<";
        public const string IEnumerablePrefix = "System.Collections.Generic.IEnumerable<";

        // Dictionary types
        public const string DictionaryPrefix = "System.Collections.Generic.Dictionary<";
        public const string IDictionaryPrefix = "System.Collections.Generic.IDictionary<";
        public const string IReadOnlyDictionaryPrefix = "System.Collections.Generic.IReadOnlyDictionary<";

        // Nullable
        public const string NullablePrefix = "System.Nullable<";
    }
}
