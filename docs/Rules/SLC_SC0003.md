# SLC_SC0003 - Avoid using the JavaScriptSerializer for (de)serialization.

The JavaScriptSerializerDeserializationAnalyzer is a C# diagnostic analyzer that identifies and warns against the use of JavaScriptSerializer for serialization and deserialization tasks. 
It recommends using safer alternatives like **System.Text.Json** or **Newtonsoft.Json**.

````csharp
string json = @"{
  ""name"": ""JavaScriptSerializer"",
  ""email"": ""javascript.serializer@example.com"",
  ""address"": {
    ""state"": ""System"",
    ""city"": ""Web"",
    ""street"": ""System"",
    ""zipcode"": ""Serialization""
  },
  ""tags"": [""developer"", ""javascript"", ""programming""]
}";

JavaScriptSerializer serializer = new JavaScriptSerializer();
object result = serializer.DeserializeObject<object>(json);
````
