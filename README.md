# UniGet

[![NuGet Status](http://img.shields.io/nuget/v/UniGet.svg?style=flat)](https://www.nuget.org/packages/UniGet/)
[![Build status](https://ci.appveyor.com/api/projects/status/r6ejl1ykf4pjohcg?svg=true)](https://ci.appveyor.com/project/veblush/uniget)
[![Coverage Status](https://coveralls.io/repos/github/SaladLab/UniGet/badge.svg?branch=master)](https://coveralls.io/github/SaladLab/UniGet?branch=master)

The package manager for Unity3D providing transitive project restore and console
packer.

## Where can I get it?

Visit github [Releases](https://github.com/SaladLab/UniGet/releases)
or you can download it via Nuget.

```
> nuget.exe install UniGet
```

## Overview

Unity3D doens't provide two things that I want to have:

- Transitive package restore:
  Without this, it's really painful to import all packages that my project
  depends on.

- Dedicated package building tool:
  Only UnityEditor can build an UnityPackage. Basically it's ok. But When you
  consider using common CI for building package files, it becomes tricky.
  There must be a console tool for building package for easing this problem.

To meet these requirements, a package manager **UniGet** has been built.

[Package Overview](./docs/Package.md)

## Features

Only essential features like pack and restore are served.

### Create package

UniGet can make an Unity3D [asset package](http://docs.unity3d.com/Manual/AssetPackages.html).
Created package is a regular one but contains a meta file used for resolving dependencies.
Also UniGet doesn't need UnityEditor for packing it so you can add a packing
process to CI like appveyor, travis and so on.

[Pack Manual](./docs/Pack.md)

### Restore packages

UniGet is also used for importing packages which is described in UnityPackage.json.
While restoring, it finds all dependencies of packages and
automatically download all packages and install them, which is a common way of modern
package manager.

[Restore Manual](./docs/Restore.md)
