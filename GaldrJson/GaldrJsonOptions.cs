namespace GaldrJson
{
    /// <summary>
    /// Options for controlling JSON serialization behavior.
    /// </summary>
    public sealed class GaldrJsonOptions
    {
        /// <summary>
        /// Gets the default options (camelCase, not indented, case-insensitive).
        /// </summary>
        public static GaldrJsonOptions Default { get; } = new GaldrJsonOptions();

        /// <summary>
        /// Property naming policy.
        /// </summary>
        public PropertyNamingPolicy PropertyNamingPolicy { get; set; } = PropertyNamingPolicy.Exact;

        /// <summary>
        /// Whether to write indented JSON (pretty-print).
        /// </summary>
        public bool WriteIndented { get; set; } =
#if DEBUG
            true;
#else
            false;
#endif

        /// <summary>
        /// Whether property name matching during deserialization is case-insensitive.
        /// </summary>
        public bool PropertyNameCaseInsensitive { get; set; } = true;

        /// <summary>
        /// Determines if a <see cref="ReferenceTracker"/> is used to detect cycles when serializing JSON.
        /// <remarks>
        /// Enabling will cause an exception to be thrown when a cycle is detected. This does add a small
        /// performance penalty.
        /// </remarks>
        /// </summary>
        public bool DetectCycles { get; set; }
    }
}
