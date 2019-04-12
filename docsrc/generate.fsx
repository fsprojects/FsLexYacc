// --------------------------------------------------------------------------------------
// Builds the documentation from `.fsx` and `.md` files in the 'docsrc' directory
// (the generated documentation is stored in the 'docs' directory)
// --------------------------------------------------------------------------------------

// Binaries that have XML documentation (in a corresponding generated XML file)
let referenceBinaries = [  __SOURCE_DIRECTORY__ + "/../src/FsLexYacc.Runtime/bin/Release/net46/FsLexYacc.Runtime.dll" ]

// Web site location for the generated documentation
let website = "/FsLexYacc"

let githubLink = "http://github.com/fsprojects/FsLexYacc"

// Specify more information about your project
let info =
  [ "project-name", "FsLexYacc"
    "project-author", "FsLexYacc contributors"
    "project-summary", "Lex and Yacc for F#"
    "project-github", githubLink
    "project-nuget", "https://www.nuget.org/packages/FsLexYacc/" ]

// --------------------------------------------------------------------------------------
// For typical project, no changes are needed below
// --------------------------------------------------------------------------------------

#load "../packages/FSharp.Formatting/FSharp.Formatting.fsx"
//#I "../../packages/FAKE/tools/"
//#r "FakeLib.dll"
//open Fake
open System.IO
//open Fake.FileHelper
open FSharp.Literate
open FSharp.MetadataFormat

// Paths with template/source/docs locations
let output     = __SOURCE_DIRECTORY__ + "/../docs"
let contentIn  = __SOURCE_DIRECTORY__ + "/content"
let files      = __SOURCE_DIRECTORY__ + "/files"
let templates  = __SOURCE_DIRECTORY__ + "/templates"
let formatting = __SOURCE_DIRECTORY__ + "/../packages/FSharp.Formatting/"
let docTemplate = formatting + "/templates/docpage.cshtml"
let referenceOut = output + "/reference"
let contentOut = output + "/content"

// When called from 'build.fsx', use the public project URL as <root>
// otherwise, use the current 'output' directory.
#if RELEASE
let root = website
#else
let root = "file://" + output
#endif

// Where to look for *.csproj templates (in this order)
let layoutRoots =
  [ templates; formatting + "/templates"
    formatting + "/templates/reference" ]

let rec copyRecursive dir1 dir2 = 
  Directory.CreateDirectory dir2 |> ignore
  for subdir1 in Directory.EnumerateDirectories dir1 do
       let subdir2 = Path.Combine(dir2, Path.GetDirectoryName subdir1)
       copyRecursive subdir1 subdir2
  for file in Directory.EnumerateFiles dir1 do
       File.Copy(file, file.Replace(dir1, dir2), true)

// Copy static files and CSS + JS from F# Formatting
let copyFiles () =
  copyRecursive (formatting + "/styles") contentOut

// Build documentation from `fsx` and `md` files in `docsrc/content` to `docs`
let buildDocumentation () =
  let subdirs = Directory.EnumerateDirectories(contentIn, "*", SearchOption.AllDirectories)
  for dir in Seq.append [contentIn] subdirs do
    let sub = if dir.Length > contentIn.Length then dir.Substring(contentIn.Length + 1) else "."
    Literate.ProcessDirectory
      ( dir, docTemplate, output + "/" + sub, replacements = ("root", root)::info,
        layoutRoots = layoutRoots )

// Build API reference from XML comments
let buildReference () =
  if Directory.Exists referenceOut then Directory.Delete(referenceOut, true)
  Directory.CreateDirectory referenceOut |> ignore
  for lib in referenceBinaries do
    MetadataFormat.Generate
      ( lib, output + "/reference", layoutRoots,
        parameters = ("root", root)::info,
        sourceRepo = githubLink + "/tree/master",
        sourceFolder = __SOURCE_DIRECTORY__ + "/.." + "/..",
        publicOnly = true )

// Generate
copyFiles()
buildDocumentation()
buildReference()