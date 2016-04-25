#I @"packages/FAKE/tools"
#I @"packages/FAKE.BuildLib/lib/net451"
#r "FakeLib.dll"
#r "BuildLib.dll"

open Fake
open BuildLib

let solution = 
    initSolution
        "./UniGet.sln" "Release" 
        [ { emptyProject with Name = "UniGet"
                              Folder = "./src/UniGet"
                              Executable = true } ]

Target "Clean" <| fun _ -> cleanBin

Target "AssemblyInfo" <| fun _ -> generateAssemblyInfo solution

Target "Restore" <| fun _ -> restoreNugetPackages solution

Target "Build" <| fun _ ->
    buildSolution solution
    // pack UniGet.exe with dependent modules to packed one
    let ilrepackExe = (getNugetPackage "ILRepack" "2.0.9") @@ "tools" @@ "ILRepack.exe"
    Shell.Exec(ilrepackExe,
               "/wildcards /out:UniGet.packed.exe UniGet.exe *.dll pdb2mdb.exe nuget.exe",
               "./src/UniGet/bin" @@ solution.Configuration) |> ignore

Target "Test" <| fun _ -> testSolution solution

Target "Cover" <| fun _ ->
    coverSolutionWithParams 
        (fun p -> { p with Filter = "+[UniGet*]* -[*.Tests]*" })
        solution

Target "PackNuget" <| fun _ -> createNugetPackages solution

Target "Pack" <| fun _ -> ()

Target "PublishNuget" <| fun _ -> publishNugetPackages solution

Target "Publish" <| fun _ -> ()

Target "CI" <| fun _ -> ()

Target "Help" <| fun _ -> 
    showUsage solution (fun _ -> None)

"Clean"
  ==> "AssemblyInfo"
  ==> "Restore"
  ==> "Build"
  ==> "Test"

"Build" ==> "Cover"
  
let isPublishOnly = getBuildParam "publishonly"

"Build" ==> "PackNuget" =?> ("PublishNuget", isPublishOnly = "")
"PackNuget" ==> "Pack"
"PublishNuget" ==> "Publish"

"Test" ==> "CI"
"Cover" ==> "CI"
"Publish" ==> "CI"

RunTargetOrDefault "Help"
