using Newtonsoft.Json;
using SecureCoding.Test.SecureSerialization.SerializationDummy;
using Skyline.DataMiner.Utils.SecureCoding.SecureSerialization.Json;
using Skyline.DataMiner.Utils.SecureCoding.SecureSerialization.Json.Newtonsoft;
using Skyline.DataMiner.Utils.Security.SecureIO;

namespace Skyline.DataMiner.Utils.SecureCoding.Tests.SecureSerialization.Json
{
    [TestClass]
    public class SecureNewtonsoftDeserializationUnitTests
    {
        [TestMethod]
        public void SimpleObjectDeserializationSuccess()
        {
            SimpleDummy dummy = new SimpleDummy("simpleObject");
            string serializedDummy = JsonConvert.SerializeObject(dummy);
            SimpleDummy deserializedDummy = SecureNewtonsoftDeserialization.DeserializeObject<SimpleDummy>(serializedDummy);
            Assert.AreEqual(dummy, deserializedDummy);

            SimpleDummy defaultSimpleDummy = new SimpleDummy(null);
            Assert.AreEqual(defaultSimpleDummy, SecureNewtonsoftDeserialization.DeserializeObject<SimpleDummy>("{ }"));
        }

        [TestMethod]
        public void SimpleObjectDeserializationFailure()
        {
            // wrong arguments
            Assert.ThrowsException<ArgumentException>(() => SecureNewtonsoftDeserialization.DeserializeObject<SimpleDummy>(null));
            Assert.ThrowsException<ArgumentException>(() => SecureNewtonsoftDeserialization.DeserializeObject<SimpleDummy>(" "));

            // wrong json string
            Assert.ThrowsException<JsonSerializationException>(() => SecureNewtonsoftDeserialization.DeserializeObject<SimpleDummy>("{"));
            Assert.ThrowsException<JsonReaderException>(() => SecureNewtonsoftDeserialization.DeserializeObject<SimpleDummy>("{ \" }"));
            Assert.ThrowsException<JsonReaderException>(() => SecureNewtonsoftDeserialization.DeserializeObject<SimpleDummy>("{ : }"));


            // too complex objects
            ComplexDummy dummy = new ComplexDummy(new List<IDummy> { new ComplexDummy(new List<IDummy>()) });
            string serializedComplexDummy = JsonConvert.SerializeObject(dummy);

            Assert.ThrowsException<JsonSerializationException>(() => SecureNewtonsoftDeserialization.DeserializeObject<ComplexDummy>(serializedComplexDummy));
            Assert.ThrowsException<JsonSerializationException>(() => SecureNewtonsoftDeserialization.DeserializeObject<IDummy>(serializedComplexDummy));
        }

        [TestMethod]
        public void SimpleObjectDeserializationWithKnownTypesSuccess()
        {
            var simpleDummy = new SimpleDummy("sucess");

            var knownTypes = new List<Type> { typeof(SimpleDummy) };

            var serializedSimpleDummy = JsonConvert.SerializeObject(simpleDummy, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });

            var result = SecureNewtonsoftDeserialization.DeserializeObject<SimpleDummy>(serializedSimpleDummy, knownTypes);

            Assert.AreEqual(simpleDummy, result);
        }

        [TestMethod]
        public void SimpleObjectDeserializationWithSettingsSuccess()
        {
            var serializerSettings = new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Error
            };

            Assert.ThrowsException<JsonSerializationException>(() => SecureNewtonsoftDeserialization.DeserializeObject<SimpleDummy>("{ \"NonExistant\":null }", serializerSettings));
        }

        [TestMethod]
        public void SimpleObjectDeserializationWithSettingsFailure()
        {
            // wrong arguments
            JsonSerializerSettings settings = new JsonSerializerSettings();
            Assert.ThrowsException<ArgumentException>(() => SecureNewtonsoftDeserialization.DeserializeObject<SimpleDummy>(null, settings));
            Assert.ThrowsException<ArgumentException>(() => SecureNewtonsoftDeserialization.DeserializeObject<SimpleDummy>(" ", settings));

            settings = new JsonSerializerSettings();
            settings.TypeNameHandling = TypeNameHandling.All;
            Assert.ThrowsException<InsecureSerializationSettingsException>(() => SecureNewtonsoftDeserialization.DeserializeObject<SimpleDummy>("{ }", settings));
        }

        [TestMethod]
        public void ComplexObjectDeserializationSuccess()
        {
            var dummiesList = new List<IDummy> { new ComplexDummy(new List<IDummy>()), new SimpleDummy("simple") };
            var complexDummyWithList = new ComplexDummy(dummiesList);

            var dummiesDictionary = new Dictionary<ComplexDummy, IDummy> { { new ComplexDummy(new List<IDummy>()), new SimpleDummy("simple") } };
            var complexDummyWithDictionary = new ComplexDummy(dummiesDictionary);

            var knownTypes = new List<Type> { typeof(SimpleDummy), typeof(ComplexDummy), typeof(List<IDummy>), typeof(Dictionary<ComplexDummy, IDummy>) };

            var serializedComplexDummyList = JsonConvert.SerializeObject(complexDummyWithList, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });
            var serializedComplexDummyDictionary = JsonConvert.SerializeObject(complexDummyWithDictionary, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });

            var resultComplexDummyList = SecureNewtonsoftDeserialization.DeserializeObject<ComplexDummy>(serializedComplexDummyList, knownTypes);
            var resultComplexDummyDictionary = SecureNewtonsoftDeserialization.DeserializeObject<ComplexDummy>(serializedComplexDummyDictionary, knownTypes);

            Assert.AreEqual(complexDummyWithList, resultComplexDummyList);
            Assert.AreEqual(complexDummyWithDictionary, resultComplexDummyDictionary);
        }

        [TestMethod]
        public void ComplexObjectDeserializationFailure()
        {
            var complexDummy = new ComplexDummy(new List<IDummy> { new SimpleDummy("simpleDummy") });

            var knownTypes = new List<Type> { typeof(ComplexDummy), typeof(List<IDummy>) };

            var serializedComplexDummy = JsonConvert.SerializeObject(complexDummy, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });

            Assert.ThrowsException<JsonSerializationException>(() => SecureNewtonsoftDeserialization.DeserializeObject<ComplexDummy>(serializedComplexDummy, knownTypes));
            Assert.ThrowsException<ArgumentException>(() => SecureNewtonsoftDeserialization.DeserializeObject<ComplexDummy>(string.Empty, knownTypes));
        }

        [TestMethod]
        public void KnownTypesExceptions()
        {
            // Arrange
            var payload = @"{
                '$type': 'System.Security.Principal.WindowsIdentity, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089',
                'System.Security.ClaimsIdentity.actor': 'AAEAAAD/////AQAAAAAAAAAMAgAAAF5NaWNyb3NvZnQuUG93ZXJTaGVsbC5FZGl0b3IsIFZlcnNpb249My4wLjAuMCwgQ3VsdHVyZT1uZXV0cmFsLCBQdWJsaWNLZXlUb2tlbj0zMWJmMzg1NmFkMzY0ZTM1BQEAAABCTWljcm9zb2Z0LlZpc3VhbFN0dWRpby5UZXh0LkZvcm1hdHRpbmcuVGV4dEZvcm1hdHRpbmdSdW5Qcm9wZXJ0aWVzAQAAAA9Gb3JlZ3JvdW5kQnJ1c2gBAgAAAAYDAAAAtwU8P3htbCB2ZXJzaW9uPSIxLjAiIGVuY29kaW5nPSJ1dGYtMTYiPz4NCjxPYmplY3REYXRhUHJvdmlkZXIgTWV0aG9kTmFtZT0iU3RhcnQiIElzSW5pdGlhbExvYWRFbmFibGVkPSJGYWxzZSIgeG1sbnM9Imh0dHA6Ly9zY2hlbWFzLm1pY3Jvc29mdC5jb20vd2luZngvMjAwNi94YW1sL3ByZXNlbnRhdGlvbiIgeG1sbnM6c2Q9ImNsci1uYW1lc3BhY2U6U3lzdGVtLkRpYWdub3N0aWNzO2Fzc2VtYmx5PVN5c3RlbSIgeG1sbnM6eD0iaHR0cDovL3NjaGVtYXMubWljcm9zb2Z0LmNvbS93aW5meC8yMDA2L3hhbWwiPg0KICA8T2JqZWN0RGF0YVByb3ZpZGVyLk9iamVjdEluc3RhbmNlPg0KICAgIDxzZDpQcm9jZXNzPg0KICAgICAgPHNkOlByb2Nlc3MuU3RhcnRJbmZvPg0KICAgICAgICA8c2Q6UHJvY2Vzc1N0YXJ0SW5mbyBBcmd1bWVudHM9Ii9jIGNhbGMuZXhlIiBTdGFuZGFyZEVycm9yRW5jb2Rpbmc9Int4Ok51bGx9IiBTdGFuZGFyZE91dHB1dEVuY29kaW5nPSJ7eDpOdWxsfSIgVXNlck5hbWU9IiIgUGFzc3dvcmQ9Int4Ok51bGx9IiBEb21haW49IiIgTG9hZFVzZXJQcm9maWxlPSJGYWxzZSIgRmlsZU5hbWU9ImNtZCIgLz4NCiAgICAgIDwvc2Q6UHJvY2Vzcy5TdGFydEluZm8+DQogICAgPC9zZDpQcm9jZXNzPg0KICA8L09iamVjdERhdGFQcm92aWRlci5PYmplY3RJbnN0YW5jZT4NCjwvT2JqZWN0RGF0YVByb3ZpZGVyPgs='
            }";

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() => SecureNewtonsoftDeserialization.DeserializeObject<object>(payload, new List<Type>()));
            Assert.ThrowsException<ArgumentException>(() => SecureNewtonsoftDeserialization.DeserializeObject<object>(payload, default(List<Type>)));
            Assert.ThrowsException<JsonSerializationException>(() => SecureNewtonsoftDeserialization.DeserializeObject<object>(payload, new List<Type> { typeof(SecurePath) }));
            Assert.ThrowsException<KnownExploitableTypeException>(() => SecureNewtonsoftDeserialization.DeserializeObject<object>(payload, new List<Type> { typeof(System.Security.Principal.WindowsIdentity) }));
        }
    }
}
