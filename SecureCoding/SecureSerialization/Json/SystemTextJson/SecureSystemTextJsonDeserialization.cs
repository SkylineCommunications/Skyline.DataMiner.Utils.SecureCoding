using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Skyline.DataMiner.Utils.SecureCoding.SecureSerialization.Json.SystemTextJson

{
    public static class SecureSystemTextJsonDeserialization
    {

        public static T DeserializeObject<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(json);
        }

        public static T DeserializeObject<T>(string json, JsonSerializerOptions options)
        {
            //TODO: overwrite options to make them secure
            return JsonSerializer.Deserialize<T>(json, options);
        }

        public static T DeserializeObject<T>(string json, List<Type> knownTypes)
        {
            //TODO: write converter that only deserializes the knowntypes
            return JsonSerializer.Deserialize<T>(json);
        }

        public static T DeserializeObject<T>(string json, List<Type> knownTypes, JsonSerializerOptions options)
        {
            //TODO: write converter that only deserializes the knowntypes and overwrite the options
            return JsonSerializer.Deserialize<T>(json, options);
        }
    }

    internal class KnonwTypesResolver : IJsonTypeInfoResolver
    {
        public JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}
