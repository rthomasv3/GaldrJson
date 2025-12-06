using System;

namespace GaldrJson
{
    public class GaldrJsonSerializer : IGaldrJsonSerializer
    {
        public bool TrySerialize(object value, Type actualType, out string json)
        {
            json = null;

            if (GaldrJsonSerializerRegistry.Serializer != null && 
                GaldrJsonSerializerRegistry.Serializer.CanSerialize(actualType))
            {
                json = GaldrJsonSerializerRegistry.Serializer.Serialize(value, actualType);
            }

            return json != null;
        }

        public bool TrySerialize<T>(T value, out string json)
        {
            json = null;

            if (GaldrJsonSerializerRegistry.Serializer != null && 
                GaldrJsonSerializerRegistry.Serializer.CanSerialize(typeof(T)))
            {
                json = GaldrJsonSerializerRegistry.Serializer.Serialize(value, typeof(T));
            }

            return json != null;
        }

        public bool TryDeserialize(string json, Type targetType, out object value)
        {
            if (GaldrJsonSerializerRegistry.Serializer != null && 
                GaldrJsonSerializerRegistry.Serializer.CanSerialize(targetType))
            {
                value = GaldrJsonSerializerRegistry.Serializer.Deserialize(json, targetType);
                return true;
            }

            value = null;
            return false;
        }

        public bool TryDeserialize<T>(string json, out T value)
        {
            if (GaldrJsonSerializerRegistry.Serializer != null && 
                GaldrJsonSerializerRegistry.Serializer.CanSerialize(typeof(T)))
            {
                value = (T)GaldrJsonSerializerRegistry.Serializer.Deserialize(json, typeof(T));
                return true;
            }

            value = default;
            return false;
        }
    }
}
