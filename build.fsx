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
                              Folder = "./src"
                              Executable = true } ]

Target "Clean" <| fun _ -> cleanBin

Target "AssemblyInfo" <| fun _ -> generateAssemblyInfo solution

Target "Restore" <| fun _ -> restoreNugetPackages solution

Target "Build" <| fun _ ->
    buildSolution solution
    // pack UniGet.exe with dependent modules to packed one
    let ilrepackExe = (getNugetPackage "ILRepack" "2.0.9") @@ "tools" @@ "ILRepack.exe"
    Shell.Exec(ilrepackExe,
               "/wildcards /out:UniGet.packed.exe UniGet.exe *.dll pdb2mdb.exe",
               "./src/bin" @@ solution.Configuration) |> ignore

Target "Nuget" <| fun _ ->
    createNugetPackages solution
    publishNugetPackages solution

Target "CreateNuget" <| fun _ ->
    createNugetPackages solution

Target "PublishNuget" <| fun _ ->
    publishNugetPackages solution

Target "CI" <| fun _ -> ()

Target "Help" <| fun _ -> 
    showUsage solution (fun _ -> None)

"Clean"
  ==> "AssemblyInfo"
  ==> "Restore"
  ==> "Build"

"Build" ==> "Nuget"
"Build" ==> "CreateNuget"

"Nuget" ==> "CI"

RunTargetOrDefault "Help"
