// --------------------------------------------------------------------------------------
// Builds the documentation from `.fsx` and `.md` files in the 'docsrc' directory
// (the generated documentation is stored in the 'docs' directory)
// --------------------------------------------------------------------------------------

// Binaries that have XML documentation (in a corresponding generated XML file)
let referenceBinaries = [ "FsLexYacc.Runtime.dll" ]
// Web site location for the generated documentation
let website = "/FsLexYacc"

let githubLink = "http://github.com/fsprojects/FsLexYacc"

// Specify more information about your project
let info =
  [ "project-name", "FsLexYacc"
    "project-author", "Your Name"
    "project-summary", "A short summary of your project"
    "project-github", githubLink
    "project-nuget", "https://www.nuget.org/packages/FsLexYacc/" ]

// --------------------------------------------------------------------------------------
// For typical project, no changes are needed below
// --------------------------------------------------------------------------------------

#load "../../packages/FSharp.Formatting/FSharp.Formatting.fsx"
//#I "../../packages/FAKE/tools/"
//#r "FakeLib.dll"
//open Fake
open System.IO
//open Fake.FileHelper
open FSharp.Literate
open FSharp.MetadataFormat

// When called from 'build.fsx', use the public project URL as <root>
// otherwise, use the current 'output' directory.
#if RELEASE
let root = website
#else
let root = "file://" + (__SOURCE_DIRECTORY__ + "/../docs")
#endif

// Paths with template/source/docs locations
let bin        = __SOURCE_DIRECTORY__ + "/../../src/FsLexYacc.Runtime/bin/Release/net46"
let content    = __SOURCE_DIRECTORY__ + "/../content"
let output     = __SOURCE_DIRECTORY__ + "/../../docs"
let files      = __SOURCE_DIRECTORY__ + "/../files"
let templates  = __SOURCE_DIRECTORY__ + "/templates"
let formatting = __SOURCE_DIRECTORY__ + "/../../packages/FSharp.Formatting/"
let docTemplate = formatting + "/templates/docpage.cshtml"
let reference = output + "/reference"

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
       File.Copy(file, file.Replace(dir1, dir2))

// Copy static files and CSS + JS from F# Formatting
let copyFiles () =
  copyRecursive (formatting + "/styles") (output + "/content")

// Build API reference from XML comments
let buildReference () =
  Directory.Delete reference
  Directory.CreateDirectory reference |> ignore
  for lib in referenceBinaries do
    MetadataFormat.Generate
      ( bin + "/" + lib, output + "/reference", layoutRoots,
        parameters = ("root", root)::info,
        sourceRepo = githubLink + "/tree/master",
        sourceFolder = __SOURCE_DIRECTORY__ + "/.." + "/..",
        publicOnly = true )

// Build documentation from `fsx` and `md` files in `docs/content`
let buildDocumentation () =
  let subdirs = Directory.EnumerateDirectories(content, "*", SearchOption.AllDirectories)
  for dir in Seq.append [content] subdirs do
    let sub = if dir.Length > content.Length then dir.Substring(content.Length + 1) else "."
    Literate.ProcessDirectory
      ( dir, docTemplate, output + "/" + sub, replacements = ("root", root)::info,
        layoutRoots = layoutRoots )

// Generate
copyFiles()
buildDocumentation()
buildReference()