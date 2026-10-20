namespace SecureCoding.Test.SecureSerialization.Json
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;

    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json.Serialization;
    using Skyline.DataMiner.Utils.SecureCoding.SecureSerialization.Json;
    using Skyline.DataMiner.Utils.SecureCoding.SecureSerialization.Json.Newtonsoft;

    [TestClass]
    public class KnownTypesSerializationBinderTests
    {
        public class NestedDummy
        {
            public string Name { get; set; }
        }

        [TestMethod]
        [DataRow(TypeNameAssemblyFormatHandling.Simple)]
        [DataRow(TypeNameAssemblyFormatHandling.Full)]
        public void BindToType_AcceptsCanonicalNames(TypeNameAssemblyFormatHandling format)
        {
            var values = new object[]
            {
                new NestedDummy { Name = "nested" },
                new NestedDummy[0],
                new NestedDummy[0, 0],
                new NestedDummy[0][],
                new Dictionary<string, List<NestedDummy[,]>>(),
            };

            foreach (object value in values)
            {
                Type type = value.GetType();
                var binder = new KnownTypesSerializationBinder(new[] { type });
                var settings = new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.All,
                    TypeNameAssemblyFormatHandling = format,
                };
                string json = JsonConvert.SerializeObject(value, settings);
                SplitTypeName((string)JObject.Parse(json)["$type"], out string assemblyName, out string typeName);
                Assert.IsTrue(binder.IsTypeSafe(assemblyName, typeName));
                Assert.AreSame(type, binder.BindToType(assemblyName, typeName));

                var defaultBinder = new DefaultSerializationBinder();
                defaultBinder.BindToName(type, out string expectedAssembly, out string expectedType);
                binder.BindToName(type, out string actualAssembly, out string actualType);
                Assert.AreEqual(expectedAssembly, actualAssembly);
                Assert.AreEqual(expectedType, actualType);
                settings.SerializationBinder = binder;
                Assert.AreEqual(json, JsonConvert.SerializeObject(value, settings));
                Assert.AreEqual(type, JsonConvert.DeserializeObject<object>(json, settings).GetType());
            }
        }

        [TestMethod]
        [DataRow("BinderRegression=Types")]
        [DataRow("BinderRegression")]
        public void IsTypeSafe_MatchesJsonNetForEmittedAssemblyNames(string simpleName)
        {
            var assemblyName = new AssemblyName { Name = simpleName + Guid.NewGuid().ToString("N") };
#if NETFRAMEWORK
            var assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
#else
            var assembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
#endif
            var module = assembly.DefineDynamicModule("Regression");
            Type emittedType = module.DefineType("Regression.KnownType", TypeAttributes.Public).CreateTypeInfo().AsType();
            var types = new[]
            {
                emittedType,
                emittedType.MakeArrayType(),
                typeof(List<>).MakeGenericType(emittedType),
                typeof(Dictionary<,>).MakeGenericType(typeof(string), typeof(List<>).MakeGenericType(emittedType.MakeArrayType(2))),
            };
            var binder = new KnownTypesSerializationBinder(types);
            foreach (Type type in types)
            {
                object value = type.IsArray ? Array.CreateInstance(type.GetElementType(), new int[type.GetArrayRank()]) : Activator.CreateInstance(type);
                foreach (var format in new[] { TypeNameAssemblyFormatHandling.Simple, TypeNameAssemblyFormatHandling.Full })
                {
                    string json = JsonConvert.SerializeObject(value, new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.All,
                        TypeNameAssemblyFormatHandling = format,
                    });
                    SplitTypeName((string)JObject.Parse(json)["$type"], out string writtenAssembly, out string writtenType);
                    Assert.IsTrue(binder.IsTypeSafe(writtenAssembly, writtenType), $"{format}: {writtenType}, {writtenAssembly}");
                    Assert.Throws<UnknownTypeException>(() => binder.BindToType(writtenAssembly + ".Unknown", writtenType));
                    Assert.Throws<UnknownTypeException>(() => binder.BindToType(writtenAssembly, writtenType + ".Unknown"));
                }
            }
        }

        [TestMethod]
        public void BindToType_RejectsNamesBeforeAssemblyResolution()
        {
            var binder = new KnownTypesSerializationBinder(new[] { typeof(NestedDummy), typeof(List<NestedDummy>) });
            string marker = "Nonexistent" + Guid.NewGuid().ToString("N");
            bool resolved = false;
            ResolveEventHandler handler = (sender, args) =>
            {
                if (args.Name.Contains(marker)) resolved = true;
                return null;
            };
            AppDomain.CurrentDomain.AssemblyResolve += handler;
            try
            {
                var exception = Assert.Throws<UnknownTypeException>(() => binder.BindToType(marker, "Unknown.Type"));
                Assert.IsFalse(exception.Message.Contains(marker));
                Assert.Throws<UnknownTypeException>(() => binder.BindToType(typeof(List<>).Assembly.GetName().Name, $"System.Collections.Generic.List`1[[Unknown.Type, {marker}]]"));
                Assert.Throws<UnknownTypeException>(() => binder.BindToType(null, typeof(NestedDummy).FullName));
                Assert.Throws<UnknownTypeException>(() => binder.BindToType(typeof(NestedDummy).Assembly.GetName().Name, typeof(NestedDummy).FullName.ToUpperInvariant()));
                Assert.Throws<UnknownTypeException>(() => binder.BindToName(typeof(string), out _, out _));
                Assert.IsFalse(resolved);
            }
            finally
            {
                AppDomain.CurrentDomain.AssemblyResolve -= handler;
            }
        }

        private static void SplitTypeName(string name, out string assemblyName, out string typeName)
        {
            int depth = 0;
            for (int i = 0; i < name.Length; i++)
            {
                if (name[i] == '[') depth++;
                else if (name[i] == ']') depth--;
                else if (name[i] == ',' && depth == 0)
                {
                    typeName = name.Substring(0, i).Trim();
                    assemblyName = name.Substring(i + 1).Trim();
                    return;
                }
            }

            throw new InvalidOperationException("Expected an assembly-qualified type name.");
        }
    }
}
