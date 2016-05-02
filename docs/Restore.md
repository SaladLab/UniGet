## UniGet Restore

Following package dependency file,
[UnityPackages.json](https://github.com/SaladLab/TicTacToe/blob/master/src/GameClient/UnityPackages.json) is from [TicTacToe](https://github.com/SaladLab/TicTacToe).
This package files should be placed at the top-most directory of Unity project.

```json
{
  "dependencies": {
    "AkkaInterfacedSlimSocket": {
      "version": ">=0.2.1",
      "source": "github:SaladLab/Akka.Interfaced.SlimSocket"
    },
    "TrackableData": {
      "version": ">=1.1.0",
      "source": "github:SaladLab/TrackableData"
    },   
    "UiManager": {
      "version": ">=1.0.0",
      "source": "github:SaladLab/Unity3D.UiManager"
    }
  }
}
```

Following command will download all libraries listed on a package file.

```
> UniGet restore UnityPackages.json
```
