// --------------------------------------------------------------------------------------
// FAKE build script 
// --------------------------------------------------------------------------------------

#I @"packages/build/FAKE/tools"
#r @"packages/build/FAKE/tools/FakeLib.dll"
#r "System.IO.Compression.FileSystem"
// #r @"packages/System.IO.Compression/lib/net46/System.IO.Compression.dll"
// #r @"packages/System.IO.Compression.ZipFile/lib/net46/System.IO.Compression.ZipFile.dll"

open Fake 
open Fake.Git
open Fake.AssemblyInfoFile
open Fake.ReleaseNotesHelper
open Fake.UserInputHelper
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
    !! "/**/**/obj"
    ++ "/**/**/bin"
    |> CleanDirs
    CleanDirs ["bin"; "temp"]
)

Target "CleanDocs" (fun _ ->
    CleanDirs ["docs/output"]
)

let assertExitCodeZero x = 
    if x = 0 then () else 
    failwithf "Command failed with exit code %i" x

let runCmdIn workDir exe = 
    Printf.ksprintf (fun args -> 
        Shell.Exec(exe, args, workDir) |> assertExitCodeZero)

/// Execute a dotnet cli command
let dotnet workDir = runCmdIn workDir "dotnet"

let dotnetcliVersion = "1.0.0-preview3-004056"
let dotnetCliPath = System.IO.DirectoryInfo "./dotnetsdk"

// --------------------------------------------------------------------------------------
// Build library & test project

Target "Build" (fun _ ->
    let projects =
        !! "src/**/*.fsproj"
        ++ "tests/**/*.fsproj"
        -- "src/**.netcore/*.fsproj"
    projects
    |> MSBuildRelease "" "Rebuild"
    |> ignore
)

let dotnetExePath = if isWindows then "dotnetsdk/dotnet.exe" else "dotnetsdk/dotnet" |> FullName

Target "InstallDotNetCore" (fun _ ->
    if isLinux then () else
    let correctVersionInstalled = 
        try
            if System.IO.FileInfo(System.IO.Path.Combine(dotnetCliPath.FullName,"dotnet.exe")).Exists then
                let processResult = 
                    ExecProcessAndReturnMessages (fun info ->  
                    info.FileName <- dotnetExePath
                    info.WorkingDirectory <- Environment.CurrentDirectory
                    info.Arguments <- "--version") (TimeSpan.FromMinutes 30.)

                processResult.Messages |> separated "" = dotnetcliVersion
                
            else
                false
        with 
        | _ -> false

    if correctVersionInstalled then
        tracefn "dotnetcli %s already installed" dotnetcliVersion
    else
        CleanDir dotnetCliPath.FullName
        let archiveFileName = 
            if isLinux then
                sprintf "dotnet-dev-ubuntu-x64.%s.tar.gz" dotnetcliVersion
            else
                sprintf "dotnet-dev-win-x64.%s.zip" dotnetcliVersion
        let downloadPath = 
                sprintf "https://dotnetcli.blob.core.windows.net/dotnet/Sdk/%s/%s" dotnetcliVersion archiveFileName
        let localPath = System.IO.Path.Combine(dotnetCliPath.FullName, archiveFileName)

        tracefn "Installing '%s' to '%s" downloadPath localPath
        
        use webclient = new Net.WebClient()
        webclient.DownloadFile(downloadPath, localPath)
        
        if isLinux then
            Fake.ArchiveHelper.Tar.Extract (DirectoryInfo localPath) (FileInfo dotnetCliPath.FullName)
        else  
            global.System.IO.Compression.ZipFile.ExtractToDirectory(localPath, dotnetCliPath.FullName)
        
        tracefn "dotnet cli path - %s" dotnetCliPath.FullName
        System.IO.Directory.EnumerateFiles dotnetCliPath.FullName
        |> Seq.iter (fun path -> tracefn " - %s" path)
        System.IO.Directory.EnumerateDirectories dotnetCliPath.FullName
        |> Seq.iter (fun path -> tracefn " - %s%c" path System.IO.Path.DirectorySeparatorChar)

    let oldPath = System.Environment.GetEnvironmentVariable("PATH")
    System.Environment.SetEnvironmentVariable("PATH", sprintf "%s%s%s" dotnetCliPath.FullName (System.IO.Path.PathSeparator.ToString()) oldPath)
)

let netcoreFiles = !! "src/**.netcore/*.fsproj" |> Seq.toList

Target "DotnetRestore" (fun _ ->
    if isLinux then
        netcoreFiles
        |> Seq.iter (fun proj ->
            let dir = Path.GetDirectoryName proj
            dotnet dir "--info"
            dotnet dir "--verbose restore"
        )
    else
        netcoreFiles
        |> Seq.iter (fun proj ->
            DotNetCli.Restore (fun c ->
                { c with
                    Project = proj
                    ToolPath = dotnetExePath 
                }) 
        )
)

Target "DotnetBuild" (fun _ ->
    if isLinux then
        netcoreFiles
        |> Seq.iter (fun proj ->
            let dir = Path.GetDirectoryName proj
            dotnet dir "--verbose build"
        )
    else
    netcoreFiles
        |> Seq.iter (fun proj ->
            DotNetCli.Build (fun c ->
                { c with
                    Project = proj
                    ToolPath = dotnetExePath
                })
        )
    )
let versionSuffix = "alpha-0001"
Target "DotnetPackage" (fun _ ->
    if isLinux then
        netcoreFiles
        |> Seq.iter (fun proj ->
            let dir = Path.GetDirectoryName proj
            dotnet dir "--verbose pack --version-suffix %s" versionSuffix  
        )
    else
        netcoreFiles
        |> Seq.iter (fun proj ->
            DotNetCli.Pack (fun (c:DotNetCli.PackParams) ->
                { c with
                    Project = proj
                    ToolPath = dotnetExePath
                    VersionSuffix = versionSuffix
                    // AdditionalArgs = ["/p:RuntimeIdentifier=win7-x64"]
                })
        )
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
            Framework = "v4.0.30319"
            DisableShadowCopy = true
            TimeOut = TimeSpan.FromMinutes 20.
            OutputFile = "TestResults.xml" })
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
Target "Dotnet" DoNothing

"Clean"
  ==> "InstallDotnetCore"
  ==> "DotnetRestore"
  ==> "DotnetBuild"  
  ==> "Dotnet"
  ==> "All"

"Clean"
  ==> "InstallDotnetCore"
  ==> "DotnetRestore"
  ==> "DotnetPackage"

"Clean"
  ==> "AssemblyInfo" 
  ==> "Build"
//  ==> "RunTests"
  =?> ("RunOldFsYaccTests", not isLinux)
  ==> "All"

"All" 
  ==> "CleanDocs"
  ==> "GenerateDocs"
  ==> "ReleaseDocs"

"All"
  ==> "NuGet"
  ==> "Release"

RunTargetOrDefault "All"
