# SLC_SC0004 - Avoid deserializing json strings by using Newtonsoft directly.

The NewtonsoftDeserializationAnalyzer is a C# diagnostic analyzer that detects and warns against direct usage of **Newtonsoft.Json** for deserialization tasks. 
It encourages developers to use a more secure alternative, **SecureNewtonsoftDeserialization**.

````csharp
string json = @"{
  ""name"": ""NewtonSoftSerializer"",
  ""email"": ""newtonsoft.serialize@example.com"",
  ""address"": {
    ""state"": ""NewtonSoft"",
    ""city"": ""Json"",
  },
  ""tags"": [""developer"", ""newtonsoft"", ""programming""]
}";

// Non-compliant
object result = JsonConvert.DeserializeObject(json);

// Should be
object result = SecureNewtonsoftDeserialization.DeserializeObject<object>(json);
````
