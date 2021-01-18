#r @"paket:
nuget FSharp.Core 4.7.2
nuget Fake.Core.Target
nuget Fake.Core.ReleaseNotes
nuget Fake.IO.FileSystem
nuget Fake.DotNet.Cli
nuget Fake.DotNet.AssemblyInfoFile
nuget Fake.DotNet.Paket
nuget Fake.Tools.Git //"

#if !FAKE
#load "./.fake/build.fsx/intellisense.fsx"
#r "netstandard" // Temp fix for https://github.com/fsharp/FAKE/issues/1985
#endif


open Fake.DotNet
// --------------------------------------------------------------------------------------
// FAKE build script 
// --------------------------------------------------------------------------------------

//#I @"packages/FAKE/tools"
//#r @"packages/FAKE/tools/FakeLib.dll"
open Fake 
open Fake.Core.TargetOperators
open Fake.Core 
open Fake.Tools.Git
open Fake.DotNet
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open System
open System.IO

// --------------------------------------------------------------------------------------
// START TODO: Provide project-specific details below
// --------------------------------------------------------------------------------------

// Information about the project are used
//  - for version and project name in generated AssemblyInfo file
//  - by the generated NuGet package 
//  - to run tests and to publish documentation on GitHub gh-pages
//  - for documentation, you also need to edit info in "docs/tools/generate.fsx"

// The name of the project 
// (used by attributes in AssemblyInfo, name of a NuGet package and directory in 'src')
let projects = [
    "FsLex"
    "FsLex.Core"
    "FsYacc"
    "FsYacc.Core"
]
let runtimeProjects = [ "FsLexYacc.Runtime" ]
let project = "FsLexYacc"
// Short summary of the project
// (used as description in AssemblyInfo and as a short summary for NuGet package)
let summary = "FsLex/FsYacc lexer/parser generation tools"

// File system information 
// (<solutionFile>.sln is built during the building process)
let solutionFile  = "FsLexYacc"
// Pattern specifying assemblies to be tested using NUnit
let testAssemblies = "tests/**/bin/Release/*Tests*.dll"

// Git configuration (used for publishing documentation in gh-pages branch)
// The profile where the project is posted 
let gitHome = "https://github.com/fsprojects"
// The name of the project on GitHub
let gitName = "FsLexYacc"

// --------------------------------------------------------------------------------------
// END TODO: The rest of the file includes standard build steps 
// --------------------------------------------------------------------------------------

// Read additional information from the release notes document
Environment.CurrentDirectory <- __SOURCE_DIRECTORY__
let release = ReleaseNotes.parse (IO.File.ReadAllLines "RELEASE_NOTES.md")

// Generate assembly info files with the right version & up-to-date information
Target.create "AssemblyInfo" (fun _ ->
  for project in runtimeProjects do
      let fileName = "src/" + project + "/AssemblyInfo.fs"
      AssemblyInfoFile.createFSharp fileName
          [ AssemblyInfo.Title project
            AssemblyInfo.Product "FsLexYacc.Runtime"
            AssemblyInfo.Description summary
            AssemblyInfo.Version release.AssemblyVersion
            AssemblyInfo.FileVersion release.AssemblyVersion ]
  for project in projects do 
      let fileName = "src/" + project + "/AssemblyInfo.fs"
      AssemblyInfoFile.createFSharp fileName
          [ AssemblyInfo.Title project
            AssemblyInfo.Product "FsLexYacc"
            AssemblyInfo.Description summary
            AssemblyInfo.Version release.AssemblyVersion
            AssemblyInfo.FileVersion release.AssemblyVersion ] 
)

// --------------------------------------------------------------------------------------
// Clean build results & restore NuGet packages

Target.create "Clean" (fun _ ->
    Shell.cleanDirs ["bin"; "temp"]
)

Target.create "CleanDocs" (fun _ ->
    Shell.cleanDirs ["docs/output"]
)

// --------------------------------------------------------------------------------------
// Build library & test project

Target.create "Build" (fun _ ->
    for framework in ["netcoreapp3.1"] do
        [
            "src/FsLex.Core/fslexlex.fs"
            "src/FsLex.Core/fslexpars.fs"
            "src/FsLex.Core/fslexpars.fsi"
            "src/FsYacc.Core/fsyacclex.fs"
            "src/FsYacc.Core/fsyaccpars.fs"
            "src/FsYacc.Core/fsyaccpars.fsi"
        ] |> File.deleteAll

        for project in ["src/FsLex/fslex.fsproj"; "src/FsYacc/fsyacc.fsproj"] do
            DotNet.publish (fun opts -> { 
                opts with 
                    Common = { opts.Common with CustomParams = Some "/v:n" }
                    Configuration = DotNet.BuildConfiguration.Release
                    Framework = Some framework 
            }) project

    [
        "tests/JsonLexAndYaccExample/Lexer.fs"
        "tests/JsonLexAndYaccExample/Parser.fs"
        "tests/JsonLexAndYaccExample/Parser.fsi"
        "tests/LexAndYaccMiniProject/Lexer.fs"
        "tests/LexAndYaccMiniProject/Parser.fs"
        "tests/LexAndYaccMiniProject/Parser.fsi"
    ] |> File.deleteAll

    for project in [ "src/FsLexYacc.Runtime/FsLexYacc.Runtime.fsproj"
                     "tests/JsonLexAndYaccExample/JsonLexAndYaccExample.fsproj"
                     "tests/LexAndYaccMiniProject/LexAndYaccMiniProject.fsproj" ] do
        DotNet.build (fun opts -> { 
            opts with 
                Common = { opts.Common with CustomParams = Some "/v:n" }
                Configuration = DotNet.BuildConfiguration.Release 
        }) project
)

// --------------------------------------------------------------------------------------
// Run the unit tests using test runner

Target.create "RunOldFsYaccTests" (fun _ ->
    let script = Path.Combine(__SOURCE_DIRECTORY__, "tests", "fsyacc", "OldFsYaccTests.fsx")
    let result = DotNet.exec id "fake" ("run " + script)
    if not result.OK then
        failwith "Old FsLexYacc tests were failed"
)

// --------------------------------------------------------------------------------------
// Build a NuGet package

Target.create "NuGet" (fun _ ->
    DotNet.pack (fun p -> 
        { p with 
            Configuration = DotNet.BuildConfiguration.Release
            MSBuildParams = {
                p.MSBuildParams with
                                Properties = [
                                    "PackageReleaseNotes", String.toLines release.Notes
                                    "PackageVersion", release.NugetVersion
                                ]
            }
            OutputPath = Some "bin"
        }
    ) "FsLexYacc.sln"
)

// --------------------------------------------------------------------------------------
// Generate the documentation

Target.create "GenerateDocs" (fun _ ->
    let result =
        DotNet.exec
            (fun p -> { p with WorkingDirectory = __SOURCE_DIRECTORY__ @@ "docs" })
            "fsi"
            "--define:RELEASE --define:REFERENCE --define:HELP --exec generate.fsx"

    if not result.OK then failwith "error generating docs"
)

// --------------------------------------------------------------------------------------
// Release Scripts

Target.create "ReleaseDocs" (fun _ ->
    let tempDocsDir = "temp/gh-pages"
    Shell.cleanDir tempDocsDir
    Repository.cloneSingleBranch "" (gitHome + "/" + gitName + ".git") "gh-pages" tempDocsDir

    Repository.fullclean tempDocsDir
    Shell.copyRecursive "docs/output" tempDocsDir true |> Trace.tracefn "%A"
    Staging.stageAll tempDocsDir
    Commit.exec tempDocsDir (sprintf "Update generated documentation for version %s" release.NugetVersion)
    Branches.push tempDocsDir
)

Target.create "Release" ignore

// --------------------------------------------------------------------------------------
// Run all targets by default. Invoke 'build <Target>' to override

Target.create "All" ignore

"Clean"
  ==>  "AssemblyInfo"
  ==>  "Build"
  ==> "RunOldFsYaccTests"
  ==> "All"

"All" 
 ==> "CleanDocs"
 ==> "GenerateDocs"
 ==> "ReleaseDocs"

"Build"
  ==> "NuGet"

"All" 
  ==> "Release"

"NuGet" 
  ==> "Release"


Target.runOrDefault "All"
