## UniGet Pack

Following package definition file, [TypeAlias.unitypackage.json](https://github.com/SaladLab/TypeAlias/blob/master/core/UnityPackage/TypeAlias.unitypackage.json)
is from [TypeAlias](https://github.com/SaladLab/TypeAlias).

```json
{
  "id": "TypeAlias",
  "version": "1.1.2",
  "authors": [ "Esun Kim" ],
  "owners": [ "Esun Kim" ],
  "description": "Library that makes the unique alias of types in .NET.",
  "dependencies": {
    "NetLegacySupport": {
      "version": ">=1.1.0",
      "source": "github:SaladLab/NetLegacySupport"
    }
  },
  "files": [
    "../TypeAlias.Net35/bin/Release/TypeAlias.dll",
    "$dependencies$"
  ]
}
```

```
> UniGet pack TypeAlias.unitypackage.json
```

```json
"protobuf-net": {
  "version": "2.0.0.668",
  "source": "nuget:net20"
}
```

```json
{
  "source": "Assets/JsonNetSample*",
  "target": "$homebase$/JsonNetSample/",
  "extra": true
}
```
