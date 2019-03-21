open Fake.DotNet
// --------------------------------------------------------------------------------------
// FAKE build script 
// --------------------------------------------------------------------------------------

#I @"packages/FAKE/tools"
#r @"packages/FAKE/tools/FakeLib.dll"
open Fake 
open Fake.Core 
open Fake.Git
open Fake.AssemblyInfoFile
open Fake.ReleaseNotesHelper
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
let projects = [ "FsLex"; "FsYacc"; ]
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

let desiredSdkVersion = (DotNet.getSDKVersionFromGlobalJson ())
let mutable sdkPath = None
let getSdkPath() = (defaultArg sdkPath "dotnet")
let installed =
  try
    DotNet.getVersion id <> null
  with _ -> false

printfn "Desired .NET SDK version = %s" desiredSdkVersion
printfn "DotNetCli.isInstalled() = %b" installed

let getPathForSdkVersion (sdkVersion) =
  DotNet.install (fun v -> { v with Version = DotNet.Version sdkVersion }) (DotNet.Options.Create ())
  |> fun o -> o.DotNetCliPath

if installed then
    let installedSdkVersion = DotNet.getVersion id
    printfn "The installed default .NET SDK version reported by FAKE's 'DotNetCli.getVersion()' is %s" installedSdkVersion
    if installedSdkVersion <> desiredSdkVersion then
        match Environment.environVar "CI" with
        | null ->
            if installedSdkVersion > desiredSdkVersion then
                printfn "*** You have .NET SDK version '%s' installed, assuming it is compatible with version '%s'" installedSdkVersion desiredSdkVersion
            else
                printfn "*** You have .NET SDK version '%s' installed, we expect at least version '%s'" installedSdkVersion desiredSdkVersion
        | _ ->
            printfn "*** The .NET SDK version '%s' will be installed (despite the fact that version '%s' is already installed) because we want precisely that version in CI" desiredSdkVersion installedSdkVersion
            sdkPath <- Some (getPathForSdkVersion desiredSdkVersion)
    else
        sdkPath <- Some (getPathForSdkVersion installedSdkVersion)
else
    printfn "*** The .NET SDK version '%s' will be installed (no other version was found by FAKE helpers)" desiredSdkVersion
    sdkPath <- Some (getPathForSdkVersion desiredSdkVersion)

// --------------------------------------------------------------------------------------
// END TODO: The rest of the file includes standard build steps 
// --------------------------------------------------------------------------------------

// Read additional information from the release notes document
Environment.CurrentDirectory <- __SOURCE_DIRECTORY__
let release = parseReleaseNotes (IO.File.ReadAllLines "RELEASE_NOTES.md")

// Generate assembly info files with the right version & up-to-date information
Target "AssemblyInfo" (fun _ ->
  for project in runtimeProjects do
      let fileName = "src/" + project + "/AssemblyInfo.fs"
      CreateFSharpAssemblyInfo fileName
          [ Attribute.Title project
            Attribute.Product "FsLexYacc.Runtime"
            Attribute.Description summary
            Attribute.Version release.AssemblyVersion
            Attribute.FileVersion release.AssemblyVersion ]
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
        (!! "src/**/*.fsproj").And("tests/**/*.fsproj")

    projects |> Seq.iter (fun proj ->
      DotNet.build (fun opts -> { opts with Common = { opts.Common with DotNetCliPath = getSdkPath ()
                                                                        CustomParams = Some "/v:n /p:SourceLinkCreate=true" }
                                            Configuration = DotNet.BuildConfiguration.Release }) proj)
)

// --------------------------------------------------------------------------------------
// Run the unit tests using test runner

Target "RunOldFsYaccTests" (fun _ ->
    let result = executeFSIWithArgs @"tests\fsyacc" "OldFsYaccTests.fsx" ["--define:RELEASE"] []
    if not result then
        failwith "Old FsLexYacc tests were failed"
)

// --------------------------------------------------------------------------------------
// Build a NuGet package

Target "NuGet" (fun _ ->
    Paket.Pack (fun p -> 
        { p with 
            TemplateFile = "nuget/FsLexYacc.Runtime.template"
            Version = release.NugetVersion
            OutputPath = "bin"
            ReleaseNotes = toLines release.Notes })
    Paket.Pack (fun p -> 
        { p with 
            TemplateFile = "nuget/FsLexYacc.template"
            Version = release.NugetVersion
            OutputPath = "bin"
            ReleaseNotes = toLines release.Notes })

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
  ==> "AssemblyInfo"
  ==> "Build"
//  =?> ("RunOldFsYaccTests", isWindows)
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


RunTargetOrDefault "All"
