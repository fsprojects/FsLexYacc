#!/usr/bin/env -S dotnet fsi --

#r "nuget: Fun.Build, 1.2.0"
#r "nuget: Ionide.KeepAChangelog, 0.2.0"

open System
open System.IO
open Fun.Build
open Ionide.KeepAChangelog
open Ionide.KeepAChangelog.Domain
open SemVersion

let (</>) (a: string) (b: string) = Path.Combine(a, b)

let root = __SOURCE_DIRECTORY__

// --------------------------------------------------------------------------------------
// Release
// --------------------------------------------------------------------------------------

/// Whether this run was asked not to publish anything: `-p Release --dry-run`.
let isDryRun = fsi.CommandLineArgs |> Array.contains "--dry-run"

type Release =
    {
        /// As written in CHANGELOG.md, prerelease suffix included.
        Version: string
        IsPrerelease: bool
        /// The entry's sections as markdown: the package release notes and the GitHub release body.
        Notes: string
    }

/// The newest released entry of CHANGELOG.md.
///
/// The projects read the same file through Ionide.KeepAChangelog.Tasks, which sets their Version
/// and PackageReleaseNotes. This copy is for the FsLexYacc meta-package, which paket packs from
/// a template, and for the GitHub release. An `[Unreleased]` section on top is skipped, so
/// merging work under it changes nothing until it moves under a versioned heading.
let release: Release =
    match Parser.parseChangeLog (FileInfo(root </> "CHANGELOG.md")) with
    | Error error -> failwith $"CHANGELOG.md could not be parsed: %A{error}"
    | Ok changelog ->
        match changelog.Releases with
        | [] -> failwith "CHANGELOG.md has no release entry."
        | (version: SemanticVersion, _date, data) :: _ ->
            let notes =
                match data with
                | None -> failwith $"The %O{version} entry of CHANGELOG.md has no sections."
                | Some data ->
                    [
                        "Added", data.Added
                        "Changed", data.Changed
                        "Deprecated", data.Deprecated
                        "Removed", data.Removed
                        "Fixed", data.Fixed
                        "Security", data.Security
                        yield! Map.toList data.Custom
                    ]
                    |> List.choose (fun (header: string, body: string) ->
                        if String.IsNullOrWhiteSpace body then
                            None
                        else
                            Some $"### %s{header}\n%s{body.Trim()}")
                    |> String.concat "\n\n"

            {
                Version = string<SemanticVersion> version
                IsPrerelease = not (String.IsNullOrEmpty version.Prerelease)
                Notes = notes
            }

// --------------------------------------------------------------------------------------
// Helpers
// --------------------------------------------------------------------------------------

/// Start a process with an explicit argument list rather than a command line.
///
/// The meta-package step passes the release notes through as a single argument, and those
/// contain newlines. Handing them to a shell to be re-split would mangle them, so the arguments
/// are never joined into a string in the first place.
let exec (fileName: string) (arguments: string list) =
    async {
        let startInfo =
            Diagnostics.ProcessStartInfo(fileName, UseShellExecute = false, WorkingDirectory = root)

        for argument in arguments do
            startInfo.ArgumentList.Add argument

        use proc = Diagnostics.Process.Start startInfo
        do! proc.WaitForExitAsync() |> Async.AwaitTask
        return proc.ExitCode
    }

let cleanDirs (dirs: string list) =
    async {
        for dir in dirs do
            if Directory.Exists dir then
                Directory.Delete(dir, true)

        return 0
    }

let deleteFiles (files: string list) =
    async {
        for file in files do
            let path = root </> file

            if File.Exists path then
                File.Delete path

        return 0
    }

// --------------------------------------------------------------------------------------
// Build
// --------------------------------------------------------------------------------------

let targetFramework = "net10.0"

/// Sources fslex and fsyacc regenerate. They are deleted first so that a stale copy can never
/// be what gets compiled into the tools that are about to regenerate it.
let generatedSources =
    [
        "src/FsLex.Core/fslexlex.fs"
        "src/FsLex.Core/fslexpars.fs"
        "src/FsLex.Core/fslexpars.fsi"
        "src/FsYacc.Core/fsyacclex.fs"
        "src/FsYacc.Core/fsyaccpars.fs"
        "src/FsYacc.Core/fsyaccpars.fsi"
    ]

let generatedTestSources =
    [
        "tests/JsonLexAndYaccExample/Lexer.fs"
        "tests/JsonLexAndYaccExample/Parser.fs"
        "tests/JsonLexAndYaccExample/Parser.fsi"
        "tests/LexAndYaccMiniProject/Lexer.fs"
        "tests/LexAndYaccMiniProject/Parser.fs"
        "tests/LexAndYaccMiniProject/Parser.fsi"
    ]

let buildTools =
    async {
        let! _ = deleteFiles generatedSources

        let mutable exitCode = 0

        for project in [ "src/FsLex/fslex.fsproj"; "src/FsYacc/fsyacc.fsproj" ] do
            if exitCode = 0 then
                let! code =
                    exec "dotnet" [ "publish"; project; "-c"; "Release"; "/v:n"; "-f"; targetFramework ]

                exitCode <- code

        return exitCode
    }

/// The runtime, plus the two sample projects that exercise the freshly published tools.
let buildLibraries =
    async {
        let! _ = deleteFiles generatedTestSources

        let mutable exitCode = 0

        for project in
            [
                "src/FsLexYacc.Runtime/FsLexYacc.Runtime.fsproj"
                "tests/JsonLexAndYaccExample/JsonLexAndYaccExample.fsproj"
                "tests/LexAndYaccMiniProject/LexAndYaccMiniProject.fsproj"
            ] do
            if exitCode = 0 then
                let! code = exec "dotnet" [ "build"; project; "-c"; "Release"; "/v:n" ]
                exitCode <- code

        return exitCode
    }

// --------------------------------------------------------------------------------------
// Packaging
// --------------------------------------------------------------------------------------

let pack =
    async {
        // The per-project packages. Their version and release notes come from CHANGELOG.md
        // through Ionide.KeepAChangelog.Tasks, see src/Directory.Build.props.
        let! projectPackages =
            exec "dotnet" [ "pack"; "FsLexYacc.slnx"; "-c"; "Release"; "-o"; "bin" ]

        if projectPackages <> 0 then
            return projectPackages
        else
            // The FsLexYacc meta-package, which is assembled by paket from a template rather
            // than from a project, because it ships the published tools instead of a library.
            return!
                exec
                    "dotnet"
                    [
                        "paket"
                        "pack"
                        "--template"
                        "nuget/FsLexYacc.template"
                        "--version"
                        release.Version
                        "--release-notes"
                        release.Notes
                        "bin"
                    ]
    }

/// Push the packages to NuGet, then create the matching GitHub release.
///
/// Every push to master runs this, so it is gated on the GitHub release: a version that has one
/// is done, and the run changes nothing. The NuGet push skips a version that is already there,
/// so a release that was pushed by hand still gets its GitHub release.
let publish (ctx: Internal.StageContext) =
    async {
        let tag = $"v%s{release.Version}"

        match! ctx.RunCommandCaptureOutput $"gh release view %s{tag} --json tagName" with
        | Ok _ ->
            printfn $"Release %s{tag} already exists on GitHub, nothing to do."
            return 0
        | Error _ ->

            let packages = Directory.GetFiles(root </> "bin", $"*.%s{release.Version}.nupkg")

            if Array.isEmpty packages then
                failwith $"No packages for %s{release.Version} in bin. Did the NuGet stage run?"

            let nugetLinks =
                packages
                |> Array.map (fun package ->
                    let id = Path.GetFileName(package).Replace($".%s{release.Version}.nupkg", "")

                    $"* [%s{id}](https://www.nuget.org/packages/%s{id}/%s{release.Version})")
                |> String.concat "\n"

            let notes = $"%s{release.Notes}\n\n### NuGet\n%s{nugetLinks}\n"

            if isDryRun then
                printfn $"[dry-run] Would push %d{packages.Length} packages and create release %s{tag}:"
                printfn "---\n%s\n---" notes
                return 0
            else

                let key = Environment.GetEnvironmentVariable "NUGET_KEY"

                for package in packages do
                    match!
                        ctx.RunSensitiveCommand
                            $"dotnet nuget push \"{package}\" --api-key {key} --source https://api.nuget.org/v3/index.json --skip-duplicate"
                    with
                    | Ok() -> ()
                    | Error _ -> failwith $"Pushing %s{Path.GetFileName package} failed."

                let notesFile = Path.GetTempFileName()
                File.WriteAllText(notesFile, notes)
                let files = packages |> Array.map (sprintf "\"%s\"") |> String.concat " "
                let prerelease = if release.IsPrerelease then "--prerelease" else ""

                let! result =
                    ctx.RunCommand $"gh release create %s{tag} %s{files} --title %s{tag} --notes-file \"%s{notesFile}\" %s{prerelease}"

                File.Delete notesFile

                match result with
                | Ok() -> return 0
                | Error _ -> return failwith $"Creating the GitHub release %s{tag} failed."
    }

// --------------------------------------------------------------------------------------
// Analyzers
// --------------------------------------------------------------------------------------

/// Every project in the solution. Reading the solution rather than globbing keeps the fixtures
/// under tests/fsyacc out, which are inputs to OldFsYaccTests.fsx rather than code of their own.
let projectsToAnalyze: string list =
    File.ReadAllLines(root </> "FsLexYacc.slnx")
    |> Array.choose (fun line ->
        let m = Text.RegularExpressions.Regex.Match(line, "<Project Path=\"([^\"]+)\"")
        if m.Success then Some m.Groups.[1].Value else None)
    |> Array.toList

/// The scripts the analyzers run over: the only F# in this repository no project compiles.
let scriptsToAnalyze: string list =
    [ "build.fsx"; "tests/fsyacc/OldFsYaccTests.fsx" ]

/// Restored by paket into the Analyzers group, see paket.dependencies.
let analyzerPaths: string list =
    [ "Ionide.Analyzers"; "G-Research.FSharp.Analyzers" ]
    |> List.map (fun package ->
        root
        </> "packages"
        </> "analyzers"
        </> package
        </> "analyzers"
        </> "dotnet"
        </> "fs")

let analysisReport = root </> "analysis.sarif"

/// One run over every project and script, so a single SARIF covers the repository.
///
/// The tool only exits non-zero for error-severity findings, so a run full of warnings still
/// passes; the findings are read from the report, or from the Code Scanning tab in CI.
let analyze =
    async {
        let! _ = deleteFiles [ "analysis.sarif" ]

        return!
            exec
                "dotnet"
                [
                    "fsharp-analyzers"
                    for path in analyzerPaths do
                        "--analyzers-path"
                        path
                    for project in projectsToAnalyze do
                        "--project"
                        root </> project
                    for script in scriptsToAnalyze do
                        "--script"
                        root </> script
                    // Not ours to fix: what fslex and fsyacc generate, the AssemblyInfo the SDK
                    // generates, the test SDK entry point, and the scripts NuGet writes per
                    // `#r "nuget: ..."`.
                    "--exclude-files"
                    // Globs, because the tool matches these against absolute paths.
                    for generated in generatedSources @ generatedTestSources do
                        "**/" + Path.GetFileName generated
                    "**/*.AssemblyInfo.fs"
                    "**/Microsoft.NET.Test.Sdk.Program.fs"
                    "**/.packagemanagement/**"
                    "--configuration"
                    "Release"
                    // With a trailing separator, or the tool reads the last segment as a file name
                    // and reports every path as "FsLexYacc/...", which GitHub cannot link.
                    "--code-root"
                    root + Path.DirectorySeparatorChar.ToString()
                    "--report"
                    analysisReport
                ]
    }

// --------------------------------------------------------------------------------------
// Pipelines
// --------------------------------------------------------------------------------------

/// Local tools first, because paket is one of them, then the paket restore that writes the
/// Paket.Restore.targets every project imports.
let restore =
    stage "Restore" {
        run "dotnet tool restore"
        run "dotnet paket restore"
    }

pipeline "Build" {
    workingDir root
    restore
    stage "Clean" { run (cleanDirs [ "bin"; "temp" ]) }
    stage "CheckFormat" { run "dotnet fantomas check ." }
    stage "BuildTools" { run buildTools }
    stage "BuildLibraries" { run buildLibraries }
    stage "UnitTests" { run "dotnet test ." }
    stage "OldFsYaccTests" { run "dotnet fsi tests/fsyacc/OldFsYaccTests.fsx" }
    runIfOnlySpecified false
}

pipeline "Release" {
    workingDir root
    restore
    stage "Clean" { run (cleanDirs [ "bin"; "temp" ]) }
    stage "CheckFormat" { run "dotnet fantomas check ." }
    stage "BuildTools" { run buildTools }
    stage "BuildLibraries" { run buildLibraries }
    stage "UnitTests" { run "dotnet test ." }
    stage "OldFsYaccTests" { run "dotnet fsi tests/fsyacc/OldFsYaccTests.fsx" }
    stage "NuGet" { run pack }
    stage "Publish" { run publish }
    runIfOnlySpecified true
}

pipeline "Docs" {
    workingDir root
    restore
    stage "CleanDocs" { run (cleanDirs [ "output"; ".fsdocs" ]) }
    stage "BuildTools" { run buildTools }
    stage "BuildLibraries" { run buildLibraries }
    stage "GenerateDocs" { run "dotnet fsdocs build --eval" }
    runIfOnlySpecified true
}

// The generated sources have to exist before a project can be type checked, so the tools and
// libraries are built first, the same way the Build pipeline does.
pipeline "Analyze" {
    workingDir root
    restore
    stage "BuildTools" { run buildTools }
    stage "BuildLibraries" { run buildLibraries }
    stage "Analyze" { run analyze }
    runIfOnlySpecified true
}

tryPrintPipelineCommandHelp ()
