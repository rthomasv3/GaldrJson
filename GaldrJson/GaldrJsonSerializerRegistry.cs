namespace GaldrJson
{
    /// <summary>
    /// The serialization registry use to register IGaldrJsonTypeSerializers.
    /// </summary>
    public static class GaldrJsonSerializerRegistry
    {
        private static IGaldrJsonTypeSerializer _serializer;

        /// <summary>
        /// Registers a new serializer.
        /// Called by generated code at module initialization
        /// </summary>
        public static void Register(IGaldrJsonTypeSerializer serializer)
        {
            _serializer = serializer;
        }

        /// <summary>
        /// Gets the generated Galdr Json Type Serializer, if registered.
        /// </summary>
        public static IGaldrJsonTypeSerializer Serializer => _serializer;
    }
}