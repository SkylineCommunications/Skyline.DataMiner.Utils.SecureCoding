using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Serialization;

[assembly: InternalsVisibleTo("Skyline.DataMiner.Utils.SecureCoding.Analyzers.Tests")]
namespace Skyline.DataMiner.Utils.SecureCoding.SecureSerialization.Json
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
                if (IsExploitableType(knownType))
                {
                    throw new KnownExploitableTypeException($"{knownType.AssemblyQualifiedName}.{knownType.FullName} is a known exploitable type, it is not secure to deserialize this type.");
                }
                fullTypeNameToType[$"{knownType.AssemblyQualifiedName}.{knownType.Name}"] = knownType;
            }
        }

        public Type BindToType(string assemblyName, string typeName)
        {
            string fullName = $"{assemblyName}.{typeName}";
            if (!fullTypeNameToType.TryGetValue(fullName, out Type deserializedType))
            {
                throw new UnknownTypeException($"{fullName} is not a known Type.");
            }
            return deserializedType;
        }

        public void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            assemblyName = serializedType.AssemblyQualifiedName;
            typeName = serializedType.FullName;
        }

        private static bool IsExploitableType(Type type)
        {
            return knownExploitableTypes.Exists(t => type.FullName.Contains(t));
        }

        private static readonly List<string> knownExploitableTypes = new List<string>
        {
            "ActivitySurrogateDisableTypeCheck",
            "TypeConfuseDelegateGenerator",
            "ActivitySurrogateSelector",
            "ActivitySurrogateSelectorFromFile",
            "AxHostState",
            "ClaimsIdentity",
            "ClaimsPrincipal",
            "System.Data.DataSet",
            "DataSetOldBehaviour",
            "DataSetOldBehaviourFromFile",
            "DataSetTypeSpoof",
            "GenericPrincipal",
            "ObjectDataProvider",
            "ObjRef",
            "PSObject",
            "ResourceSet",
            "RolePrincipal",
            "SessionSecurityToken",
            "SessionViewStateHistoryItem",
            "TextFormattingRunProperties",
            "ToolboxItemContainer",
            "TypeConfuseDelegate",
            "TypeConfuseDelegateMono",
            "WindowsClaimsIdentity",
            "WindowsIdentity",
            "WindowsPrincipal",
            "XamlAssemblyLoadFromFile"
        };
    }
}
