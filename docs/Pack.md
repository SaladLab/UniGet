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

Following command will build a uniget-package.

```
> UniGet pack TypeAlias.unitypackage.json
```

### Basic information

Basic information of project. Id will be used package-id and should be unique.

```json
{
  "id": "TypeAlias",
  "version": "1.1.2",
  "authors": [ "Esun Kim" ],
  "owners": [ "Esun Kim" ],
  "description": "Library that makes the unique alias of types in .NET."
}
```

With package-id and version, output package filename is decided as `{id}.{version}.unitypackage`.
For the previous example, `TypeAlias.1.1.2.unitypackage` will be a filename.

### Dependencies

`dependencies` is a section for listing dependent libraries. Two kinds of source
can be used.

For github: source looks like `github:{owner}/{project}'

```json
"TrackableData": {
  "version": ">=1.1.0",
  "source": "github:SaladLab/TrackableData"
}
```

For NuGet: source look like 'nuget:{tfm}'

```json
"protobuf-net": {
  "version": "2.0.0.668",
  "source": "nuget:net20"
}
```

{tfm} is target framework moniker and usually set as net20 or net35.

### Files

`files` is a section for listing files which will be contained in a package.
It's an array of files.

#### Simple file

Following `TypeAlias.dll` will be packaged under `Assets/UnityPackage/{project-id}`
which is a home directory of uniget-package.

```json
"../TypeAlias.Net35/bin/Release/TypeAlias.dll"
```

You can specify same item with a verbose format.

```json
{ "source": "TypeAlias.Net35/bin/Release/TypeAlias.dll", "target": "$home$/" }
```

Wildcard can be used.

```json
"../TypeAlias.Net35/bin/Release/*.dll"
```

#### Extra files

`extra` field is used for setting extra file. If file is extra, it will be excluded for
restoring a package under UniGet.

```json
{ "source": "Assets/JsonNetSample*", "target": "$homebase$/JsonNetSample/", "extra": true }
```

#### Merged dependencies

Following item will merge all dependent libraries into this uniget-package.

```json
"$dependencies$"
```

This makes a package self-contained and an user will gets good out-of-experience.
Merged files are not used when UniGet restore it.

#### Path variables

For terse representation of project, following variables are provided.

`$home$` will be `Assets/UnityPackage/{project-id}`
`$homebase$` will be `Assets/UnityPackage`

#### MDB Conversion

When packaging a DLL, if there is a pdb file for it, UniGet tries to convert
a pdb file to mdb file and includes it.
It's a handy feature to remove an another process for it.

#### Auto meta file

Unity3D assigns an UUID for all files and stores it to \*.meta file.
Most of Meta files of source and DLL files just have simple UUID.
For these file, auto meta file is quite usuful to keep source small and clean.

Basically UniGet generates an UUID for files that doesn't have meta file and
create a common meta file. Also to guarantee UUID same for every build, it always
generates same UUID from same path of file.

For example, `Assets/TypeAlias/TypeAlias.dll` will get
`Assets/TypeAlias/TypeAlias.dll.meta` that looks like:

```yaml
fileFormatVersion: 2
guid: 86174a0992215b488a1990fbd4f85082
MonoAssemblyImporter:
  serializedVersion: 1
  iconMap: {}
  executionOrder: {}
  userData:
```

Value of guid won't change if file name keeps same.

### Local repository

Local repository can be used for fetching packages from a specified directory.

```
>uniget restore --local ./locals
```

On looking up packages, UniGet tries to find a package on ./locals at first.

### Upload a package on github

Just upload output package file without modifying filename.
UniGet determines version of package from the filename of a package.
