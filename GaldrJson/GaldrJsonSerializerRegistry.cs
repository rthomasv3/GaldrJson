using System;
using System.Threading;

namespace GaldrJson
{
    /// <summary>
    /// The serialization registry used to register IGaldrJsonTypeSerializers.
    /// Multiple serializers (one per assembly that contains [GaldrJsonSerializable] types)
    /// can coexist; lookups are dispatched to the first registered serializer that reports
    /// it can handle the requested type.
    /// </summary>
    public static class GaldrJsonSerializerRegistry
    {
        private static readonly object _lock = new object();
        private static IGaldrJsonTypeSerializer[] _serializers = new IGaldrJsonTypeSerializer[0];
        private static readonly CompositeTypeSerializer _composite = new CompositeTypeSerializer();

        /// <summary>
        /// Registers a new serializer. Called by generated code at module initialization.
        /// Null entries are ignored. Adding the same instance twice is a no-op.
        /// </summary>
        public static void Register(IGaldrJsonTypeSerializer serializer)
        {
            if (serializer != null)
            {
                lock (_lock)
                {
                    IGaldrJsonTypeSerializer[] current = _serializers;
                    bool alreadyRegistered = false;

                    for (int i = 0; i < current.Length; i++)
                    {
                        if (ReferenceEquals(current[i], serializer))
                        {
                            alreadyRegistered = true;
                        }
                    }

                    if (!alreadyRegistered)
                    {
                        IGaldrJsonTypeSerializer[] next = new IGaldrJsonTypeSerializer[current.Length + 1];
                        Array.Copy(current, next, current.Length);
                        next[current.Length] = serializer;
                        Volatile.Write(ref _serializers, next);
                    }
                }
            }
        }

        /// <summary>
        /// Gets a composite serializer that delegates each call to the first registered
        /// underlying serializer that claims it can handle the requested type.
        /// </summary>
        public static IGaldrJsonTypeSerializer Serializer => _composite;

        /// <summary>
        /// Returns a snapshot of the currently registered serializers, safe to iterate
        /// concurrently with registration.
        /// </summary>
        internal static IGaldrJsonTypeSerializer[] Snapshot()
        {
            return Volatile.Read(ref _serializers);
        }
    }
}
