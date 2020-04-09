#r @"paket:
nuget Fake.IO.FileSystem
nuget Fake.DotNet.Fsc
nuget Fake.Core.Trace
nuget Fake.DotNet.Cli //"

#if !FAKE
#load "./.fake/build.fsx/intellisense.fsx"
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

let test proj (args, baseLineOutput) =
    Trace.traceImportant <| sprintf "Running '%s' with args '%s'" proj args

    let res = DotNet.exec (fun opts -> { opts with RedirectOutput = true }) "run" ("-p " + proj + " " + args)

    if not res.OK then
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
                        pathPiece.[idx..]
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

let repro1885Fsl = Path.Combine(__SOURCE_DIRECTORY__, "repro1885.fsl")
// Regression test for FSB 1885
fsLex repro1885Fsl

// Test 1
let test1lexFs = Path.Combine(__SOURCE_DIRECTORY__, "Test1", "test1lex.fs")
let test1lexMll = Path.Combine(__SOURCE_DIRECTORY__, "Test1", "test1lex.mll")
let test1Fs = Path.Combine(__SOURCE_DIRECTORY__, "Test1", "test1.fs")
let test1Mly = Path.Combine(__SOURCE_DIRECTORY__, "Test1", "test1.mly")
let test1Input1 = Path.Combine(__SOURCE_DIRECTORY__, "Test1", "test1.input1")
let test1Input1Bsl = Path.Combine(__SOURCE_DIRECTORY__, "Test1", "test1.input1.bsl")
let test1Input1TokensBsl = Path.Combine(__SOURCE_DIRECTORY__, "Test1", "test1.input1.tokens.bsl")
let test1Input2Variation1 = Path.Combine(__SOURCE_DIRECTORY__, "Test1", "test1.input2.variation1")
let test1Input2Variation2 = Path.Combine(__SOURCE_DIRECTORY__, "Test1", "test1.input2.variation2")
let test1Input2Bsl = Path.Combine(__SOURCE_DIRECTORY__, "Test1", "test1.input2.bsl")
let test1Proj = Path.Combine(__SOURCE_DIRECTORY__, "Test1", "test1.fsproj")

fsLex ("--light-off -o " + test1lexFs + " " + test1lexMll)
fsYacc ("--light-off --module TestParser -o " + test1Fs + " " + test1Mly)

let runTest1Tests () =    
    test1Proj
    |> DotNet.build (fun opts -> { opts with Configuration = DotNet.BuildConfiguration.Release })

    [sprintf "--tokens %s" test1Input1, test1Input1TokensBsl
     test1Input1, test1Input1Bsl
     sprintf "%s %s" test1Input2Variation1 test1Input2Variation2, test1Input2Bsl]
    |> List.iter (test ("-p " + test1Proj))

runTest1Tests ()

// All other tests have missing files. It's not clear if they ever existed.

// Test 2 - Thre is no 'test2.fs'/'test2.fsi' ...
//fsYacc "--light-off --module TestParser -o test2.fs test2.mly"
//fsc "test2.exe" ["test2.fsi"; "test2.fs"; "test1lex.fs"; "main.ml"]

// let runTest2Tests() =
//     ["--tokens ./test2.input1", "test2.input1.tokens.bsl"
//      "--tokens ./test2.badInput", "test1.badInput.tokens.bsl"
//      //"./test2.input1", "test2.input1.bsl" TODO: Test fails
//      //"./test2.badInput", "test1.badInput.bsl"
//      ]
//     |> List.iter (test "test2.exe")

// runTest2Tests()


// // test1.exe
// fsLex "-o test1lex.ml test1lex.mll"
// fsYacc "--module TestParser -o test1.ml test1.mly"
// fsc "test1.exe" ["test1.mli"; "test1.ml"; "test1lex.ml"; "main.ml"]

// runTest1Tests()


// // test1compat.exe
// fsYacc "--module TestParser -o test1compat.ml --ml-compatibility test1.mly"
// fsc "test1compat.exe" ["test1compat.mli"; "test1compat.ml"; "test1lex.ml"; "main.ml"]

// ["--tokens ./test1.input1", "test1compat.input1.tokens.bsl"
//  "./test1.input1", "test1comapt.input1.bsl"
//  "./test1.input2.variation1  ./test1.input2.variation2", "test1compat.input2.bsl"]
// |> List.iter (test "test1compat.exe")


// // test2.exe
// fsYacc "--module TestParser -o test2.ml test2.mly"
// fsc "test2.exe" ["test2.mli"; "test2.ml"; "test1lex.ml"; "main.ml"]

// runTest2Tests()


// // test2compat.exe
// fsYacc "--module TestParser -o test2compat.ml --ml-compatibility test2.mly"
// fsc "test2compat.exe" ["test2compat.mli"; "test2compat.ml"; "test1lex.ml"; "main.ml"]


// // test1-unicode.exe
// fsLex "--unicode -o test1-unicode-lex.ml test1-unicode-lex.mll"
// fsYacc "--module TestParser -o test1-unicode.ml test1-unicode.mly"
// fsc "test1-unicode.exe" ["test1-unicode.mli"; "test1-unicode.ml"; "test1-unicode-lex.ml"; "main-unicode.ml"]

// runTest1UnicodeTests()
