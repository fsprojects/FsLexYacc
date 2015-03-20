// --------------------------------------------------------------------------------------
// FAKE build script 
// --------------------------------------------------------------------------------------

#I @"packages/FAKE/tools"
#r @"packages/FAKE/tools/FakeLib.dll"
open Fake 
open Fake.Git
open Fake.AssemblyInfoFile
open Fake.ReleaseNotesHelper
open System

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
let projects = [ "FsLex"; "FsYacc"; "FsLexYacc.Build.Tasks" ]
let runtimeProjects = [ "FsLexYacc.Runtime"; "FsLexYacc.Profile7"; "FsLexYacc.Profile259" ]
let project = "FsLexYacc"
// Short summary of the project
// (used as description in AssemblyInfo and as a short summary for NuGet package)
let summary = "FsLex/FsYacc lexer/parser generation tools"

// List of author names (for NuGet package)
let authors = [ "Microsoft Research"; "The F# Software Foundation" ]

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
let release = parseReleaseNotes (IO.File.ReadAllLines "RELEASE_NOTES.md")
let releaseRuntime = parseReleaseNotes (IO.File.ReadAllLines "RELEASE_NOTES_Runtime.md")

// Generate assembly info files with the right version & up-to-date information
Target "AssemblyInfo" (fun _ ->
  for project in runtimeProjects do
      let fileName = "src/" + project + "/AssemblyInfo.fs"
      CreateFSharpAssemblyInfo fileName
          [ Attribute.Title project
            Attribute.Product "FsLexYacc.Runtime"
            Attribute.Description summary
            Attribute.Version releaseRuntime.AssemblyVersion
            Attribute.FileVersion releaseRuntime.AssemblyVersion ]
  for project in projects do 
      let fileName = "src/" + project + "/AssemblyInfo.fs"
      CreateFSharpAssemblyInfo fileName
          [ Attribute.Title project
            Attribute.Product "FsLexYacc"
            Attribute.Description summary
            Attribute.Version release.AssemblyVersion
            Attribute.FileVersion release.AssemblyVersion ] 
)

// --------------------------------------------------------------------------------------
// Clean build results & restore NuGet packages

Target "RestorePackages" RestorePackages

Target "Clean" (fun _ ->
    CleanDirs ["bin"; "temp"]
)

Target "CleanDocs" (fun _ ->
    CleanDirs ["docs/output"]
)

// --------------------------------------------------------------------------------------
// Build library & test project

Target "Build" (fun _ ->
    let projects =
        (if not isLinux then
            !! "src/**/*.fsproj"
         else
            !! "src/**/*.fsproj"
              -- "src/FsLexYacc.Profile259/*.fsproj"
              -- "src/FsLexYacc.Profile7/*.fsproj")
          ++ "tests/FsLexYacc.Build.Tasks.Tests/*.fsproj"

    projects
    |> MSBuildRelease "" "Rebuild"
    |> ignore
)

// --------------------------------------------------------------------------------------
// Run the unit tests using test runner

Target "RunOldFsYaccTests" (fun _ ->
    let result = executeFSIWithArgs @"tests\fsyacc" "OldFsYaccTests.fsx" ["--define:RELEASE"] []
    if not result then
        failwith "Old FsLexYacc tests were failed"
)

Target "RunTests" (fun _ ->
    !! testAssemblies 
    |> NUnit (fun p ->
        { p with
            DisableShadowCopy = true
            TimeOut = TimeSpan.FromMinutes 20.
            OutputFile = "TestResults.xml" })
)

// --------------------------------------------------------------------------------------
// Build a NuGet package

Target "NuGet-FsLexYacc.Runtime" (fun _ ->
    NuGet (fun p ->
        { p with
            Authors = authors
            Summary = summary
            Description = """Runtime for FsLex/FsYacc lexer/parser generation tools"""
            Version = releaseRuntime.NugetVersion
            ReleaseNotes = String.Join(Environment.NewLine, releaseRuntime.Notes)
            Tags = "F# fsharp programming fslex fsyacc parser runtime"
            OutputPath = "bin"
            AccessKey = getBuildParamOrDefault "nugetkey" ""
            Publish = hasBuildParam "nugetkey"
            Dependencies = [] })
        ("nuget/FsLexYacc.Runtime.nuspec")
)

Target "NuGet-FsLexYacc" (fun _ ->
    NuGet (fun p ->
        { p with
            Authors = authors
            Summary = summary
            Description = """FsLex/FsYacc lexer/parser generation tools"""
            Version = release.NugetVersion
            ReleaseNotes = String.Join(Environment.NewLine, release.Notes)
            Tags = "F# fsharp programming fslex fsyacc parser generator"
            OutputPath = "bin"
            AccessKey = getBuildParamOrDefault "nugetkey" ""
            Publish = hasBuildParam "nugetkey"
            Dependencies = ["FsLexYacc.Runtime",releaseRuntime.NugetVersion] })
        ("nuget/FsLexYacc.nuspec")
)

// --------------------------------------------------------------------------------------
// Generate the documentation

Target "GenerateDocs" (fun _ ->
    executeFSIWithArgs "docs/tools" "generate.fsx" ["--define:RELEASE"] [] |> ignore
)

// --------------------------------------------------------------------------------------
// Release Scripts

Target "ReleaseDocs" (fun _ ->
    let tempDocsDir = "temp/gh-pages"
    CleanDir tempDocsDir
    Repository.cloneSingleBranch "" (gitHome + "/" + gitName + ".git") "gh-pages" tempDocsDir

    fullclean tempDocsDir
    CopyRecursive "docs/output" tempDocsDir true |> tracefn "%A"
    StageAll tempDocsDir
    Commit tempDocsDir (sprintf "Update generated documentation for version %s" release.NugetVersion)
    Branches.push tempDocsDir
)

Target "Release" DoNothing

// --------------------------------------------------------------------------------------
// Run all targets by default. Invoke 'build <Target>' to override

Target "All" DoNothing

"Clean"
  ==> "RestorePackages"
  ==> "AssemblyInfo"
  ==> "Build"
  ==> "RunTests"
  =?> ("RunOldFsYaccTests", not isLinux)
  ==> "All"

"All" 
  ==> "CleanDocs"
  ==> "GenerateDocs"
  ==> "ReleaseDocs"

"All"
  ==> "NuGet-FsLexYacc.Runtime"
  ==> "NuGet-FsLexYacc"
  ==> "Release"

RunTargetOrDefault "All"
