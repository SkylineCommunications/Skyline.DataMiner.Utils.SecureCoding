# Analyzers's rules
|Id|Category|Description|Severity|Is enabled|Code fix|
|--|--------|-----------|:------:|:--------:|:------:|
|[SLC_SC0001](https://github.com/SkylineCommunications/Skyline.DataMiner.Utils.SecureCoding/blob/main/docs/Rules/SLC_SC0001.md)|Usage|File operation usage detected without secure path construction or validation|<span title='Warning'>⚠️</span>|✔️|❌|
|[SLC_SC0002](https://github.com/SkylineCommunications/Skyline.DataMiner.Utils.SecureCoding/blob/main/docs/Rules/SLC_SC0002.md)|Usage|Avoid using 'System.IO.Path.Combine'|<span title='Warning'>⚠️</span>|✔️|✔️|
|[SLC_SC0003](https://github.com/SkylineCommunications/Skyline.DataMiner.Utils.SecureCoding/blob/main/docs/Rules/SLC_SC0003.md)|Usage|Avoid using the JavaScriptSerializer for (de)serialization.|<span title='Warning'>⚠️</span>|✔️|❌|
|[SLC_SC0004](https://github.com/SkylineCommunications/Skyline.DataMiner.Utils.SecureCoding/blob/main/docs/Rules/SLC_SC0004.md)|Usage|Avoid deserializing json strings by using Newtonsoft directly.|<span title='Warning'>⚠️</span>|✔️|✔️|
|[SLC_SC0005](https://github.com/SkylineCommunications/Skyline.DataMiner.Utils.SecureCoding/blob/main/docs/Rules/SLC_SC0005.md)|Usage|Certificate callbacks should not always evaluate to true|<span title='Warning'>⚠️</span>|✔️|❌|
|[SLC_SC0006](https://github.com/SkylineCommunications/Skyline.DataMiner.Utils.SecureCoding/blob/main/docs/Rules/SLC_SC0006.md)|Usage|Ensure secure loading of Assemblies|<span title='Warning'>⚠️</span>|✔️|✔️|
|[SLC_SC0007](https://github.com/SkylineCommunications/Skyline.DataMiner.Utils.SecureCoding/blob/main/docs/Rules/SLC_SC0007.md)|Usage|Avoid insecure cryptographic algorithms|<span title='Warning'>⚠️</span>|✔️|❌|


# .editorconfig - default values

```editorconfig
# SLC_SC0001: File operation usage detected without secure path construction or validation
dotnet_diagnostic.SLC_SC0001.severity = warning

# SLC_SC0002: Avoid using 'System.IO.Path.Combine'
dotnet_diagnostic.SLC_SC0002.severity = warning

# SLC_SC0003: Avoid using the JavaScriptSerializer for (de)serialization.
dotnet_diagnostic.SLC_SC0003.severity = warning

# SLC_SC0004: Avoid deserializing json strings by using Newtonsoft directly.
dotnet_diagnostic.SLC_SC0004.severity = warning

# SLC_SC0005: Certificate callbacks should not always evaluate to true
dotnet_diagnostic.SLC_SC0005.severity = warning

# SLC_SC0006: Ensure secure loading of Assemblies
dotnet_diagnostic.SLC_SC0006.severity = warning

# SLC_SC0007: Avoid insecure cryptographic algorithms
dotnet_diagnostic.SLC_SC0007.severity = warning
```

# .editorconfig - all rules disabled

```editorconfig
# SLC_SC0001: File operation usage detected without secure path construction or validation
dotnet_diagnostic.SLC_SC0001.severity = none

# SLC_SC0002: Avoid using 'System.IO.Path.Combine'
dotnet_diagnostic.SLC_SC0002.severity = none

# SLC_SC0003: Avoid using the JavaScriptSerializer for (de)serialization.
dotnet_diagnostic.SLC_SC0003.severity = none

# SLC_SC0004: Avoid deserializing json strings by using Newtonsoft directly.
dotnet_diagnostic.SLC_SC0004.severity = none

# SLC_SC0005: Certificate callbacks should not always evaluate to true
dotnet_diagnostic.SLC_SC0005.severity = none

# SLC_SC0006: Ensure secure loading of Assemblies
dotnet_diagnostic.SLC_SC0006.severity = none

# SLC_SC0007: Avoid insecure cryptographic algorithms
dotnet_diagnostic.SLC_SC0007.severity = none
```
