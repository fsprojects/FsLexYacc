// --------------------------------------------------------------------------------------
// Builds the documentation from `.fsx` and `.md` files in the 'docs/content' directory
// (the generated documentation is stored in the 'docs/output' directory)
// --------------------------------------------------------------------------------------

#load "../packages/FSharp.Formatting/FSharp.Formatting.fsx"

open System.Collections.Generic
open System.IO
open FSharp.Formatting.Razor

// --------------------------------------------------------------------------------------
// Helpers
// --------------------------------------------------------------------------------------
let subDirectories (dir: string) = Directory.EnumerateDirectories dir

let rec copyRecursive dir1 dir2 =
    Directory.CreateDirectory dir2 |> ignore

    for subdir1 in Directory.EnumerateDirectories dir1 do
        let subdir2 = Path.Combine(dir2, Path.GetFileName subdir1)
        copyRecursive subdir1 subdir2

    for file in Directory.EnumerateFiles dir1 do
        File.Copy(file, file.Replace(dir1, dir2), true)

// --------------------------------------------------------------------------------------
// Settings
// --------------------------------------------------------------------------------------

// Binaries that have XML documentation (in a corresponding generated XML file)
let referenceBinaries =
    [
        __SOURCE_DIRECTORY__
        + "/../src/FsLexYacc.Runtime/bin/Release/netstandard2.0/FsLexYacc.Runtime.dll"
    ]

let githubLink = "http://github.com/fsprojects/FsLexYacc"

// Specify more information about your project
let info =
    [
        "project-name", "FsLexYacc"
        "project-author", "FsLexYacc contributors"
        "project-summary", "Lex and Yacc for F#"
        "project-github", githubLink
        "project-nuget", "https://www.nuget.org/packages/FsLexYacc/"
    ]

// --------------------------------------------------------------------------------------
// For typical project, no changes are needed below
// --------------------------------------------------------------------------------------

// When called from 'build.fsx', use the public project URL as <root>
// otherwise, use the current 'output' directory.
let root = "/FsLexYacc"

Directory.SetCurrentDirectory(__SOURCE_DIRECTORY__)

// Paths with template/source/output locations
let bin =
    __SOURCE_DIRECTORY__ + "/../src/FsLexYacc.Runtime/bin/Release/netstandard2.0"

let output = __SOURCE_DIRECTORY__ + "/output"
let contentIn = __SOURCE_DIRECTORY__ + "/content"
let files = __SOURCE_DIRECTORY__ + "/files"
let templates = __SOURCE_DIRECTORY__ + "/templates"
let formatting = __SOURCE_DIRECTORY__ + "/../packages/FSharp.Formatting/"
let docTemplate = formatting + "/templates/docpage.cshtml"
let referenceOut = output + "/reference"
let contentOut = output + "/content"

// Where to look for *.csproj templates (in this order)
let layoutRootsAll = Dictionary<string, string list>()

layoutRootsAll.Add(
    "en",
    [
        templates
        formatting + "/" + "templates"
        formatting + "/" + "templates/reference"
    ]
)

subDirectories templates
|> Seq.iter (fun name ->
    if name.Length = 2 || name.Length = 3 then
        layoutRootsAll.Add(
            name,
            [
                templates + "/" + name
                formatting + "/" + "templates"
                formatting + "/" + "templates/reference"
            ]
        ))

// Copy static files and CSS + JS from F# Formatting
let copyFiles () =
    copyRecursive files output
    copyRecursive (formatting + "/styles") contentOut

let libDirs = [ bin ]

// Build API reference from XML comments
let buildReference () =
    printfn "building reference docs..."

    if Directory.Exists referenceOut then
        Directory.Delete(referenceOut, true)

    Directory.CreateDirectory referenceOut |> ignore

    RazorMetadataFormat.Generate(
        referenceBinaries,
        output + "/" + "reference",
        layoutRootsAll.["en"],
        parameters = ("root", root) :: info,
        sourceRepo = githubLink + "/" + "tree/master",
        sourceFolder = __SOURCE_DIRECTORY__ + "/" + ".." + "/" + "..",
        publicOnly = true,
        libDirs = libDirs
    )

// Build documentation from `fsx` and `md` files in `docs/content`
let buildDocumentation () =
    printfn "building docs..."
    let subdirs = [ (contentIn, docTemplate) ]

    for dir, template in subdirs do
        let sub = "." // Everything goes into the same output directory here

        let langSpecificPath (lang, path: string) =
            path.Split([| '/'; '\\' |], System.StringSplitOptions.RemoveEmptyEntries)
            |> Array.exists (fun i -> i = lang)

        let layoutRoots =
            let key = layoutRootsAll.Keys |> Seq.tryFind (fun i -> langSpecificPath (i, dir))

            match key with
            | Some lang -> layoutRootsAll.[lang]
            | None -> layoutRootsAll.["en"] // "en" is the default language

        RazorLiterate.ProcessDirectory(
            dir,
            template,
            output + "/" + sub,
            replacements = ("root", root) :: info,
            layoutRoots = layoutRoots,
            generateAnchors = true,
            processRecursive = false,
            includeSource = true
        )

// Generate
copyFiles ()
buildDocumentation ()
buildReference ()
