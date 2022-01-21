#r @"paket:
nuget Fake.IO.FileSystem
nuget Fake.DotNet.Fsc
nuget Fake.Core.Trace
nuget Fake.DotNet.Cli //"

#if !FAKE
#load "./.fake/oldfsyacctests.fsx/intellisense.fsx"
#r "netstandard" // Temp fix for https://github.com/fsharp/FAKE/issues/1985
#endif

open System
open System.Runtime.InteropServices
open System.IO

open Fake.DotNet
open Fake.IO
open Fake.Core

let assertFileExists file =
    if File.Exists(file) then
        printfn "%s exists!" file
    else
        failwithf "'%s' doesn't exist" file

let run project args =
    Trace.traceImportant <| sprintf "Running '%s' with args %s" project args

    project
    |> DotNet.build (fun opts ->
        { opts with
            Configuration = DotNet.BuildConfiguration.Release })

    let res = DotNet.exec (fun opts -> { opts with RedirectOutput = true }) "run" ("-p " + project + " " + args)

    if not res.OK then
        failwithf "Process failed with code %d" res.ExitCode

let test proj shouldBeOK (args, baseLineOutput) =
    Trace.traceImportant <| sprintf "Running '%s' with args '%s'" proj args

    let res = DotNet.exec (fun opts -> { opts with RedirectOutput = true }) "run" ("-p " + proj + " " + args)

    if res.OK <> shouldBeOK then
        failwithf "Process failed with code %d on input %s" res.ExitCode args

    let output =
        res.Results
        |> Array.ofList
        |> Array.map (fun cm -> cm.Message)
        |> Array.map (fun line ->
            if line.StartsWith("parsed") then
                let pieces = line.Split(' ')
                let pathPiece = pieces.[1]                
                let idx =
                    let value =
                        if RuntimeInformation.IsOSPlatform(OSPlatform.Windows) then
                            @"\"
                        else
                            "/"
                    pathPiece.LastIndexOf(value)
                let pathPiece =
                    if idx >= 0 then
                        pathPiece.[idx+1 ..]
                    else
                        pathPiece
                pieces.[0] + " " + pathPiece + " " + pieces.[2]
            else
                line)

    if (not <| File.Exists baseLineOutput)
        then failwithf "Baseline file '%s' does not exist" baseLineOutput

    let expectedLines = File.ReadAllLines baseLineOutput

    if output.Length <> expectedLines.Length ||
       Seq.map2 (=) output expectedLines |> Seq.exists not
       then
         printfn "Expected:"
         for line in expectedLines do
            printfn "\t%s" line

         printfn "Output:"
         for line in output do
            printfn "\t%s" line

         File.WriteAllLines(baseLineOutput+".err", output)
         failwithf "Output is not equal to expected base line '%s'" baseLineOutput

let fslexProject = Path.Combine(__SOURCE_DIRECTORY__, "..", "..", "src", "FsLex", "fslex.fsproj")
let fsYaccProject = Path.Combine(__SOURCE_DIRECTORY__ , "..", "..", "src", "FsYacc", "fsyacc.fsproj")

assertFileExists fslexProject
assertFileExists fsYaccProject

let fsLex  = run fslexProject
let fsYacc = run fsYaccProject

let repro1885Fsl = Path.Combine(__SOURCE_DIRECTORY__, "repro1885", "repro1885.fsl")
// Regression test for FSB 1885
fsLex repro1885Fsl

// Test 1
let test1lexFs = Path.Combine(__SOURCE_DIRECTORY__, "Test1", "test1lex.fs")
let test1lexFsl = Path.Combine(__SOURCE_DIRECTORY__, "Test1", "test1lex.fsl")
let test1Fs = Path.Combine(__SOURCE_DIRECTORY__, "Test1", "test1.fs")
let test1Fsy = Path.Combine(__SOURCE_DIRECTORY__, "Test1", "test1.fsy")
let test1Input1 = Path.Combine(__SOURCE_DIRECTORY__, "Test1", "test1.input1")
let test1Input1Bsl = Path.Combine(__SOURCE_DIRECTORY__, "Test1", "test1.input1.bsl")
let test1Input1TokensBsl = Path.Combine(__SOURCE_DIRECTORY__, "Test1", "test1.input1.tokens.bsl")
let test1Input2Variation1 = Path.Combine(__SOURCE_DIRECTORY__, "Test1", "test1.input2.variation1")
let test1Input2Variation2 = Path.Combine(__SOURCE_DIRECTORY__, "Test1", "test1.input2.variation2")
let test1Input2Bsl = Path.Combine(__SOURCE_DIRECTORY__, "Test1", "test1.input2.bsl")
let test1Input3 = Path.Combine(__SOURCE_DIRECTORY__, "Test1", "test1.input3")
let test1Input3Bsl = Path.Combine(__SOURCE_DIRECTORY__, "Test1", "test1.input3.bsl")
let test1Input3TokensBsl = Path.Combine(__SOURCE_DIRECTORY__, "Test1", "test1.input3.tokens.bsl")
let test1Proj = Path.Combine(__SOURCE_DIRECTORY__, "Test1", "test1.fsproj")
let test1Input4 = Path.Combine(__SOURCE_DIRECTORY__, "Test1", "test1.input4")
let test1Input4Bsl = Path.Combine(__SOURCE_DIRECTORY__, "Test1", "test1.input4.bsl")
let test1Input4TokensBsl = Path.Combine(__SOURCE_DIRECTORY__, "Test1", "test1.input4.tokens.bsl")


let runTests' shouldBeOK projFile xs =    
    projFile
    |> DotNet.build (fun opts -> { opts with Configuration = DotNet.BuildConfiguration.Release })

    xs |> List.iter (test ("-p " + projFile) shouldBeOK)
let runTests projFile xs = runTests' true projFile xs   
let runTests2 projFile xs = runTests' false projFile xs   

fsLex ("-o " + test1lexFs + " " + test1lexFsl)
fsYacc ("--module TestParser -o " + test1Fs + " " + test1Fsy)
runTests test1Proj [
     sprintf "--tokens %s" test1Input1, test1Input1TokensBsl
     test1Input1, test1Input1Bsl
     sprintf "%s %s" test1Input2Variation1 test1Input2Variation2, test1Input2Bsl
     sprintf "--tokens %s" test1Input3, test1Input3TokensBsl
     test1Input3, test1Input3Bsl
     ]

// Case insensitive option test
fsLex ("-i -o " + test1lexFs + " " + test1lexFsl)
runTests test1Proj [
    sprintf "--tokens %s" test1Input4, test1Input4TokensBsl
    sprintf "--tokens %s" test1Input3, test1Input4TokensBsl
    sprintf "%s %s" test1Input3 test1Input4, test1Input4Bsl
    ]

// Test 1 unicode
let test1unicodelexFs = Path.Combine(__SOURCE_DIRECTORY__, "unicode", "test1-unicode-lex.fs")
let test1unicodelexFsl = Path.Combine(__SOURCE_DIRECTORY__, "unicode", "test1-unicode-lex.fsl")
let test1unicodeFs = Path.Combine(__SOURCE_DIRECTORY__, "unicode", "test1-unicode.fs")
let test1unicodeFsy = Path.Combine(__SOURCE_DIRECTORY__, "unicode", "test1-unicode.fsy")
let test1unicodeProj = Path.Combine(__SOURCE_DIRECTORY__, "unicode", "test1-unicode.fsproj")
let test1unicodeInput3 = Path.Combine(__SOURCE_DIRECTORY__, "unicode", "test1-unicode.input3.utf8")
let test1unicodeInput3TokensBsl = Path.Combine(__SOURCE_DIRECTORY__, "unicode", "test1-unicode.input3.tokens.bsl")
let test1unicodeWithTitleCaseLetter = Path.Combine(__SOURCE_DIRECTORY__, "unicode", "test1-unicode.WithTitleCaseLetter.utf8")
let test1unicodeWithTitleCaseLetterTokensBsl = Path.Combine(__SOURCE_DIRECTORY__, "unicode", "test1-unicode.WithTitleCaseLetter.tokens.bsl")
let test1unicodeWithTitleCaseLetterTokensErrorBsl = Path.Combine(__SOURCE_DIRECTORY__, "unicode", "test1-unicode.WithTitleCaseLetter.tokens.error.bsl")

fsLex ("--unicode -o " + test1unicodelexFs + " " + test1unicodelexFsl)
fsYacc ("--module TestParser -o " + test1unicodeFs + " " + test1unicodeFsy)

runTests test1unicodeProj [
    sprintf "--tokens %s" test1Input1, test1Input1TokensBsl
    test1Input1, test1Input1Bsl
    sprintf "%s %s" test1Input2Variation1 test1Input2Variation2, test1Input2Bsl
    sprintf "--tokens %s" test1unicodeInput3, test1unicodeInput3TokensBsl
    ]

runTests2 test1unicodeProj [
    sprintf "--tokens %s" test1unicodeWithTitleCaseLetter, test1unicodeWithTitleCaseLetterTokensErrorBsl
]

// Case insensitive option test
fsLex ("--unicode -i -o " + test1unicodelexFs + " " + test1unicodelexFsl)
runTests test1unicodeProj [
    sprintf "--tokens %s" test1Input1, test1Input1TokensBsl
    test1Input1, test1Input1Bsl
    sprintf "%s %s" test1Input2Variation1 test1Input2Variation2, test1Input2Bsl
    sprintf "--tokens %s" test1unicodeInput3, test1unicodeInput3TokensBsl
    sprintf "--tokens %s" test1unicodeWithTitleCaseLetter, test1unicodeWithTitleCaseLetterTokensBsl
    ]

// Test 2
let test2lexFs = Path.Combine(__SOURCE_DIRECTORY__, "Test2", "test2lex.fs")
let test2lexFsl = Path.Combine(__SOURCE_DIRECTORY__, "Test1", "test1lex.fsl")
let test2Fs = Path.Combine(__SOURCE_DIRECTORY__, "Test2", "test2.fs")
let test2Fsy = Path.Combine(__SOURCE_DIRECTORY__, "Test2", "test2.fsy")
let test2Proj = Path.Combine(__SOURCE_DIRECTORY__, "Test2", "test2.fsproj")
let test2Input1 = Path.Combine(__SOURCE_DIRECTORY__, "Test2", "test2.input1")
let test2Input1TokensBsl = Path.Combine(__SOURCE_DIRECTORY__, "Test2", "test2.input1.tokens.bsl")
let test2BadInput = Path.Combine(__SOURCE_DIRECTORY__, "Test2", "test2.badInput")
let test2BadInputTokensBsl = Path.Combine(__SOURCE_DIRECTORY__, "Test2", "test2.badInput.tokens.bsl")

fsLex ("-o " + test2lexFs + " " + test2lexFsl)
fsYacc ("--module TestParser -o " + test2Fs + " " + test2Fsy)

runTests test2Proj [
    sprintf "--tokens %s" test2Input1, test2Input1TokensBsl
    sprintf "--tokens %s" test2BadInput, test2BadInputTokensBsl
    ]
