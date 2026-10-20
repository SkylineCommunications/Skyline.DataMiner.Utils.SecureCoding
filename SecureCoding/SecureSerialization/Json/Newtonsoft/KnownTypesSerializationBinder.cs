using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SecureCoding.SecureSerialization;

namespace Skyline.DataMiner.Utils.SecureCoding.SecureSerialization.Json.Newtonsoft
{
    /// <summary>
    /// Wraps <see cref="DefaultSerializationBinder"/> but only resolves the trusted known types.
    /// </summary>
    /// <remarks>Allowlisting restricts type resolution; approved types and converters must still be trusted.</remarks>
    internal sealed class KnownTypesSerializationBinder : ISerializationBinder
    {
        // Reuse Json.NET's assembly/generic type resolution and standard serialization names instead of a custom resolver.
        // Obtain its shared binder through JsonSerializer (Instance is internal) to reuse the thread-safe type cache.
        // The default binder does not enforce security; this wrapper checks names before resolution and Type identity afterward.
        private static readonly DefaultSerializationBinder defaultBinder =
            new JsonSerializer().SerializationBinder as DefaultSerializationBinder ?? new DefaultSerializationBinder();

        private readonly HashSet<Type> deserializerTypes;
        private readonly HashSet<string> safeTypeKeys;

        public KnownTypesSerializationBinder(IEnumerable<Type> knownTypes)
        {
            if (knownTypes is null || !knownTypes.Any())
            {
                throw new ArgumentNullException(nameof(knownTypes));
            }

            deserializerTypes = new HashSet<Type>();
            // Security identifiers require exact, culture-independent comparison.
            safeTypeKeys = new HashSet<string>(StringComparer.Ordinal);

            foreach (Type knownType in knownTypes)
            {
                if (knownType.IsKnownExploitableType())
                {
                    throw new KnownExploitableTypeException($"{knownType.FullName} is a known exploitable type, it is not secure to deserialize this type.");
                }

                deserializerTypes.Add(knownType);

                // Build both formats from trusted names only; never normalize incoming $type values.
                string fullName = knownType.AssemblyQualifiedName;
                if (fullName != null)
                {
                    safeTypeKeys.Add(fullName);
                    safeTypeKeys.Add(GetSimpleTypeName(fullName));
                }
            }
        }

        public Type BindToType(string assemblyName, string typeName)
        {
            // Deny by default: reject unapproved names before resolution can trigger assembly loading.
            if (!IsTypeSafe(assemblyName, typeName))
            {
                // Do not echo untrusted names here; Json.NET's outer exceptions still require safe logging.
                throw new UnknownTypeException("The type specified in the json is not a known type.");
            }

            // Reuse Json.NET's resolver, then verify Type identity as defense in depth against name collisions.
            Type deserializedType = defaultBinder.BindToType(assemblyName, typeName);
            if (!deserializerTypes.Contains(deserializedType))
            {
                throw new UnknownTypeException("The type specified in the json is not a known type.");
            }

            return deserializedType;
        }

        public void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            if (!deserializerTypes.Contains(serializedType))
            {
                throw new UnknownTypeException("The type being serialized is not a known type.");
            }

            // Preserve Json.NET's standard naming contract rather than constructing names independently.
            defaultBinder.BindToName(serializedType, out assemblyName, out typeName);
        }

        // Exact, ordinal lookup of the strings Json.NET writes for the known types; no parsing of untrusted input.
        internal bool IsTypeSafe(string assemblyName, string typeName)
        {
            return assemblyName != null && safeTypeKeys.Contains(typeName + ", " + assemblyName);
        }

        private static string GetSimpleTypeName(string fullName)
        {
            var result = new StringBuilder(fullName.Length);
            int separatorCount = 0;
            bool arrayRank = false;

            // Match Json.NET's Simple format: keep the first comma-delimited assembly name
            // in each bracket-delimited segment, including commas used for array ranks.
            // Unlike the regex, this preserves escaped '=' characters in assembly names.
            foreach (char character in fullName)
            {
                if (character == '[' || character == ']')
                {
                    separatorCount = 0;
                    arrayRank = character == '[';
                }
                else if (character == ',')
                {
                    if (!arrayRank && separatorCount < 2)
                    {
                        separatorCount++;
                    }
                }
                else
                {
                    arrayRank = false;
                }

                if (separatorCount < 2)
                {
                    result.Append(character);
                }
            }

            return result.ToString();
        }
    }
}