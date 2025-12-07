namespace GaldrJson
{
    /// <summary>
    /// Defines how property names are written in JSON.
    /// </summary>
    public enum PropertyNamingPolicy
    {
        /// <summary>
        /// camelCase (default): "firstName"
        /// </summary>
        CamelCase,

        /// <summary>
        /// snake_case: "first_name"
        /// </summary>
        SnakeCase,

        /// <summary>
        /// kebab-case: "first-name"
        /// </summary>
        KebabCase,

        /// <summary>
        /// Use exact property name as-is from code
        /// </summary>
        Exact
    }
}
