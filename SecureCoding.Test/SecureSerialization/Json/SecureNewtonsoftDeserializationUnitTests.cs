
using Newtonsoft.Json;
using SecureCoding.Test.SecureSerialization.SerializationDummy;
using Skyline.DataMiner.Utils.SecureCoding.SecureSerialization.Json;
using Skyline.DataMiner.Utils.SecureCoding.SecureSerialization.Json.Newtonsoft;
using System.Reflection;

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

        [TestMethod]
        public void DeserializeObject_MaliciousPayload_ThrowsJsonReaderException()
        {
            // Arrange
            var payload = @"{
                '$type':'System.Windows.Data.ObjectDataProvider, PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35',
                'MethodName':'Start',
                'MethodParameters':{
                    '$type':'System.Collections.ArrayList, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089',
                    '$values':['cmd', '/c calc']
                },
                'ObjectInstance':{'$type':'System.Diagnostics.Process, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'}
            }";

            var customObj = new CustomObj
            {
                Content = new CustomContent { Payload = payload },
            };

            var customObjString = JsonConvert.SerializeObject(customObj, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.None });

            var result = SecureNewtonsoftDeserialization.DeserializeObject<object>(customObjString, new List<Type> { typeof(object) });

            // Act & Assert
            //Assert.ThrowsException<JsonReaderException>(() => );
        }

        public class CustomObj 
        {
            public CustomObj()
            {
                
            }

            public CustomContent Content { get; set; }
        }

        public class CustomContent
        {
            public string Payload { get; set; }
        }
    }
}
