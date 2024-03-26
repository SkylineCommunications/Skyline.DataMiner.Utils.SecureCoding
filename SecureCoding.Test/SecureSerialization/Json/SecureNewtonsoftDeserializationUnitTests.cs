
using Newtonsoft.Json;
using SecureCoding.Test.SecureSerialization.SerializationDummy;
using Skyline.DataMiner.Utils.SecureCoding.SecureSerialization.Json;
using Skyline.DataMiner.Utils.SecureCoding.SecureSerialization.Json.Newtonsoft;

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
            ComplexDummy dummy = new ComplexDummy(new List<IDummy> { new ComplexDummy(new List<IDummy>())});
            string serializedComplexDummy = JsonConvert.SerializeObject(dummy);

            Assert.ThrowsException<JsonSerializationException>(() => SecureNewtonsoftDeserialization.DeserializeObject<ComplexDummy>(serializedComplexDummy));
            Assert.ThrowsException<JsonSerializationException>(() => SecureNewtonsoftDeserialization.DeserializeObject<IDummy>(serializedComplexDummy));
        }

        [TestMethod]
        public void SimpleObjectDeserializationWithSettingsSuccess()
        {

        }

        [TestMethod]
        public void SimpleObjectDeserializationWithSettingsFailure()
        {
            // wrong arguments
            JsonSerializerSettings settings = new JsonSerializerSettings();
            Assert.ThrowsException<ArgumentException>(() => SecureNewtonsoftDeserialization.DeserializeObject<SimpleDummy>(null, settings));
            Assert.ThrowsException<ArgumentException>(() => SecureNewtonsoftDeserialization.DeserializeObject<SimpleDummy>(" ", settings));

            // verify that different serializationsettings are passed and can change the behaviour
            settings = new JsonSerializerSettings();
            settings.MissingMemberHandling = MissingMemberHandling.Error;
            Assert.ThrowsException<JsonSerializationException>(() => SecureNewtonsoftDeserialization.DeserializeObject<SimpleDummy>("{ }", settings));

            settings = new JsonSerializerSettings();
            settings.TypeNameHandling = TypeNameHandling.All;
            Assert.ThrowsException<InsecureSerializationSettingsException>(() => SecureNewtonsoftDeserialization.DeserializeObject<SimpleDummy>("{ }", settings));
        }

        [TestMethod]
        public void ComplexObjectDeserializationSuccess()
        {

        }

        [TestMethod]
        public void ComplexObjectDeserializationFailure()
        {

        }

        [TestMethod]
        public void ComplexObjectDeserializationWithSettingsSuccess()
        {

        }

        [TestMethod]
        public void ComplexObjectDeserializationWithSettingsFailure()
        {

        }
    }
}
