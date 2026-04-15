using System;
using System.Text.Json;

namespace GaldrJson
{
    /// <summary>
    /// Dispatches each call to the first registered <see cref="IGaldrJsonTypeSerializer"/>
    /// that reports it can handle the requested type. Returned by
    /// <see cref="GaldrJsonSerializerRegistry.Serializer"/>.
    /// </summary>
    internal sealed class CompositeTypeSerializer : IGaldrJsonTypeSerializer
    {
        public bool CanSerialize(Type type)
        {
            IGaldrJsonTypeSerializer[] snapshot = GaldrJsonSerializerRegistry.Snapshot();
            bool canSerialize = false;

            for (int i = 0; i < snapshot.Length; i++)
            {
                if (!canSerialize && snapshot[i].CanSerialize(type))
                {
                    canSerialize = true;
                }
            }

            return canSerialize;
        }

        public string Serialize(object value, Type type, GaldrJsonOptions options)
        {
            IGaldrJsonTypeSerializer match = FindSerializer(type);
            string result;

            if (match == null)
            {
                throw new NotSupportedException(BuildNotSupportedMessage(type));
            }
            else
            {
                result = match.Serialize(value, type, options);
            }

            return result;
        }

        public object Deserialize(string json, Type type, GaldrJsonOptions options)
        {
            IGaldrJsonTypeSerializer match = FindSerializer(type);
            object result;

            if (match == null)
            {
                throw new NotSupportedException(BuildNotSupportedMessage(type));
            }
            else
            {
                result = match.Deserialize(json, type, options);
            }

            return result;
        }

        public void Write(Utf8JsonWriter writer, object value, Type type, JsonSerializerOptions options, ReferenceTracker tracker)
        {
            IGaldrJsonTypeSerializer match = FindSerializer(type);

            if (match == null)
            {
                throw new NotSupportedException(BuildNotSupportedMessage(type));
            }
            else
            {
                match.Write(writer, value, type, options, tracker);
            }
        }

        public object Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
        {
            IGaldrJsonTypeSerializer match = FindSerializer(type);
            object result;

            if (match == null)
            {
                throw new NotSupportedException(BuildNotSupportedMessage(type));
            }
            else
            {
                result = match.Read(ref reader, type, options);
            }

            return result;
        }

        public void SerializeTo(Utf8JsonWriter writer, object value, Type type, GaldrJsonOptions options)
        {
            IGaldrJsonTypeSerializer match = FindSerializer(type);

            if (match == null)
            {
                throw new NotSupportedException(BuildNotSupportedMessage(type));
            }
            else
            {
                match.SerializeTo(writer, value, type, options);
            }
        }

        private static IGaldrJsonTypeSerializer FindSerializer(Type type)
        {
            IGaldrJsonTypeSerializer[] snapshot = GaldrJsonSerializerRegistry.Snapshot();
            IGaldrJsonTypeSerializer match = null;

            for (int i = 0; i < snapshot.Length; i++)
            {
                if (match == null && snapshot[i].CanSerialize(type))
                {
                    match = snapshot[i];
                }
            }

            return match;
        }

        private static string BuildNotSupportedMessage(Type type)
        {
            return $"No registered GaldrJson serializer can handle type {type.FullName}. Add [GaldrJsonSerializable] to the type.";
        }
    }
}
