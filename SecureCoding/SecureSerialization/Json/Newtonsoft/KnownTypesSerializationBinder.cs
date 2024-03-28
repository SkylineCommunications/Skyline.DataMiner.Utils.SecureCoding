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
        private readonly HashSet<Type> deserializerTypes;

        public KnownTypesSerializationBinder(IEnumerable<Type> knownTypes)
        {
            if (knownTypes is null || !knownTypes.Any())
            {
                throw new ArgumentNullException(nameof(knownTypes));
            }

            deserializerTypes = new HashSet<Type>();

            foreach (Type knownType in knownTypes)
            {
                if (knownType.IsKnownExploitableType())
                {
                    throw new KnownExploitableTypeException($"{knownType.FullName} is a known exploitable type, it is not secure to deserialize this type.");
                }

                deserializerTypes.Add(knownType);
            }
        }

        public Type BindToType(string assemblyName, string typeName)
        {
            var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == assemblyName)
                ?? throw new UnknownTypeException($"{assemblyName} assembly is not loaded.");

            // Try to find the type by its name in the assembly
            Type deserializedType = assembly.GetType(typeName);

            if (!deserializerTypes.Contains(deserializedType))
            {
                throw new UnknownTypeException($"{typeName} is not a known Type.");
            }

            return deserializedType;
        }

        public void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            var matchingSerializedType = deserializerTypes.FirstOrDefault(type => type == serializedType)
                ?? throw new UnknownTypeException($"{serializedType.FullName} is not a known Type.");

            assemblyName = matchingSerializedType.AssemblyQualifiedName;
            typeName = matchingSerializedType.FullName;
        }
    }
}
