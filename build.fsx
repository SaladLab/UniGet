#I @"packages/FAKE/tools"
#I @"packages/FAKE.BuildLib/lib/net451"
#r "FakeLib.dll"
#r "BuildLib.dll"

open Fake
open BuildLib

let solution = 
    initSolution
        "./StubSolution.sln" "Release" 
        [ { emptyProject with Name = "StubProject" 
                              Folder = "./core/StubProject" } ]

Target "Clean" <| fun _ -> cleanBin

Target "AssemblyInfo" <| fun _ -> generateAssemblyInfo solution

Target "Restore" <| fun _ -> restoreNugetPackages solution

Target "Build" <| fun _ -> buildSolution solution

Target "Test" <| fun _ -> testSolution solution

Target "Cover" <| fun _ -> coverSolution solution
    
Target "Coverity" <| fun _ -> coveritySolution solution "SaladLab/StubSolution"

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
  ==> "Test"

"Build" ==> "Nuget"
"Build" ==> "CreateNuget"
"Build" ==> "Cover"
"Restore" ==> "Coverity"

"Test" ==> "CI"
"Cover" ==> "CI"
"Nuget" ==> "CI"

RunTargetOrDefault "Help"
