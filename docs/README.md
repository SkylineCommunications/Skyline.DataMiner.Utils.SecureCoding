# Analyzers's rules
|Id|Category|Description|Severity|Is enabled|Code fix|
|--|--------|-----------|:------:|:--------:|:------:|
|[SLC-SC0001](https://github.com/SkylineCommunications/Skyline.DataMiner.Utils.SecureCoding/blob/main/docs/Rules/SLC-SC0001.md)|Usage|File operation usage detected without secure path construction or validation|<span title='Warning'>⚠️</span>|✔️|❌|
|[SLC-SC0002](https://github.com/SkylineCommunications/Skyline.DataMiner.Utils.SecureCoding/blob/main/docs/Rules/SLC-SC0002.md)|Usage|Avoid using 'System.IO.Path.Combine'|<span title='Warning'>⚠️</span>|✔️|✔️|
|[SLC-SC0003](https://github.com/SkylineCommunications/Skyline.DataMiner.Utils.SecureCoding/blob/main/docs/Rules/SLC-SC0003.md)|Usage|Avoid using the JavaScriptSerializer for (de)serialization.|<span title='Warning'>⚠️</span>|✔️|❌|
|[SLC-SC0004](https://github.com/SkylineCommunications/Skyline.DataMiner.Utils.SecureCoding/blob/main/docs/Rules/SLC-SC0004.md)|Usage|Avoid deserializing json strings by using Newtonsoft directly.|<span title='Warning'>⚠️</span>|✔️|✔️|


# .editorconfig - default values

```editorconfig
# SLC-SC0001: File operation usage detected without secure path construction or validation
dotnet_diagnostic.SLC-SC0001.severity = warning

# SLC-SC0002: Avoid using 'System.IO.Path.Combine'
dotnet_diagnostic.SLC-SC0002.severity = warning

# SLC-SC0003: Avoid using the JavaScriptSerializer for (de)serialization.
dotnet_diagnostic.SLC-SC0003.severity = warning

# SLC-SC0004: Avoid deserializing json strings by using Newtonsoft directly.
dotnet_diagnostic.SLC-SC0004.severity = warning
```

# .editorconfig - all rules disabled

```editorconfig
# SLC-SC0001: File operation usage detected without secure path construction or validation
dotnet_diagnostic.SLC-SC0001.severity = none

# SLC-SC0002: Avoid using 'System.IO.Path.Combine'
dotnet_diagnostic.SLC-SC0002.severity = none

# SLC-SC0003: Avoid using the JavaScriptSerializer for (de)serialization.
dotnet_diagnostic.SLC-SC0003.severity = none

# SLC-SC0004: Avoid deserializing json strings by using Newtonsoft directly.
dotnet_diagnostic.SLC-SC0004.severity = none
```
