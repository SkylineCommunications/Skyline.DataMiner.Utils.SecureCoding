using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Serialization;
using SecureCoding.SecureSerialization;

[assembly: InternalsVisibleTo("Skyline.DataMiner.Utils.SecureCoding.Analyzers.Tests")]
namespace Skyline.DataMiner.Utils.SecureCoding.SecureSerialization.Json.Newtonsoft
{
    internal class KnownTypesSerializationBinder : ISerializationBinder
    {
        private readonly Dictionary<string, Type> fullTypeNameToType;


        public KnownTypesSerializationBinder(IEnumerable<Type> knownTypes)
        {
            if (knownTypes is null || !knownTypes.Any())
            {
                throw new ArgumentNullException(nameof(knownTypes));
            }

            fullTypeNameToType = new Dictionary<string, Type>();
            foreach (Type knownType in knownTypes)
            {
                if (knownType.IsKnownExploitableType())
                {
                    throw new KnownExploitableTypeException($"{knownType.FullName} is a known exploitable type, it is not secure to deserialize this type.");
                }
                fullTypeNameToType[knownType.FullName] = knownType;
            }
        }

        public Type BindToType(string assemblyName, string typeName)
        {
            if (!fullTypeNameToType.TryGetValue(typeName, out Type deserializedType))
            {
                throw new UnknownTypeException($"{typeName} is not a known Type.");
            }

            return deserializedType;
        }

        public void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            if (!fullTypeNameToType.ContainsKey(serializedType.FullName))
            {
                throw new UnknownTypeException($"{serializedType.FullName} is not a known Type.");
            }

            assemblyName = serializedType.AssemblyQualifiedName;
            typeName = serializedType.FullName;
        }
    }
}
