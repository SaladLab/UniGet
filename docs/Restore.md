## UniGet Restore

Following pacakge dependency file,
[UnityPackages.json](https://github.com/SaladLab/TicTacToe/blob/master/src/GameClient/UnityPackages.json) is from [TicTacToe](https://github.com/SaladLab/TicTacToe).

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

```
> UniGet pack UnityPackages.json
```
