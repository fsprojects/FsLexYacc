#r @"paket:
nuget Fake.IO.FileSystem
nuget Fake.DotNet.Fsc
nuget Fake.Core.Trace //"

#if !FAKE
#load "./.fake/build.fsx/intellisense.fsx"
#r "netstandard" // Temp fix for https://github.com/fsharp/FAKE/issues/1985
#endif

open System.IO

open Fake.DotNet
open Fake.IO
open Fake.Core

let assertExists file =
    if File.Exists(file) then
        printfn "%s exists!" file
    else
        failwithf "'%s' doesn't exist" file

let fsLexYaccRuntimeReference = Path.Combine(__SOURCE_DIRECTORY__ , "..", "..", "src","FsLexYacc.Runtime", "bin", "Release", "netstandard2.0", "FsLexYacc.Runtime.dll")
let fsharpCoreDLL = Path.Combine(__SOURCE_DIRECTORY__ , "..", "..", "src", "FsLex", "bin","Release","netcoreapp3.1","FSharp.Core.dll")
let fsLexExe = Path.Combine(__SOURCE_DIRECTORY__, "..", "..", "src", "FsLex", "bin", "Release", "netcoreapp3.1", "fslex.exe")
let fsYaccExe = Path.Combine(__SOURCE_DIRECTORY__ , "..", "..", "src", "FsYacc", "bin", "Release", "netcoreapp3.1", "fsyacc.exe")

assertExists fsLexYaccRuntimeReference
assertExists fsharpCoreDLL
assertExists fsLexExe
assertExists fsYaccExe

let run exe (args: Arguments) =
    Trace.traceImportant <| sprintf "Running '%s' with args '%s'" exe (args.ToLinuxShellCommandLine)

    let rc = RawCommand(exe, args)
    printfn "%A" rc.Arguments.ToWindowsCommandLine

    rc
    |> CreateProcess.fromCommand
    |> CreateProcess.ensureExitCode
    |> Proc.run

let fsLex  = run fsLexExe
let fsYacc = run fsYaccExe

let fsc output files =
    Trace.traceImportant <| sprintf "Building '%s' with from %A" output files

    "lexing.fs"::"parsing.fs"::"../../src/Common/Arg.fs"::"arg.fs"::"tree.ml"::files
    |> Fsc.compile
        [ Fsc.Out output
          Fsc.Target Fsc.Exe
          Fsc.Debug false
          Fsc.References [fsLexYaccRuntimeReference; "System.Runtime"; "System.IO"; fsharpCoreDLL] ]

    let wrongExe = (files |> List.rev |> List.head) |> Path.changeExtension ".exe"
    if FileInfo(output).LastWriteTime < FileInfo(wrongExe).LastWriteTime
        then File.Delete(output)
             File.Move(wrongExe, output)
             Trace.traceImportant <| sprintf "File '%s' renamed to '%s'" wrongExe output

let test exe (args, baseLineOutput) =
    let mutable messages = []

    let appendMessage msg =
        messages <- msg :: messages

    Trace.traceImportant <| sprintf "Running '%s' with args '%s'" exe args

    let exitCode =
        let res =
            RawCommand(exe, Arguments.OfArgs [args])
            |> CreateProcess.fromCommand
            |> Proc.run
        res.ExitCode

    if exitCode <> 0 then
        failwithf "Process failed with code %d" exitCode

    let output = messages |> List.rev |> Array.ofList

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
    else
        Trace.traceImportant <| sprintf "OK: Output is equal to base line '%s'" baseLineOutput


let repro1885Fsl = Path.Combine(__SOURCE_DIRECTORY__, "repro1885.fsl")
// Regression test for FSB 1885
fsLex (Arguments.Empty |> Arguments.appendRaw repro1885Fsl)

// test1.exe
let test1lexFs = Path.Combine(__SOURCE_DIRECTORY__, "test1lex.fs")
let test1lexMll = Path.Combine(__SOURCE_DIRECTORY__, "test1lex.mll")
let test1Fs = Path.Combine(__SOURCE_DIRECTORY__, "test1.fs")
let test1Mly = Path.Combine(__SOURCE_DIRECTORY__, "test1.mly")
let test1Fsi = Path.Combine(__SOURCE_DIRECTORY__, "test1.fsi")
let mainMl = Path.Combine(__SOURCE_DIRECTORY__, "main.ml")
let test1Exe = Path.Combine(__SOURCE_DIRECTORY__, "test1.exe")
let test1Input1 = Path.Combine(__SOURCE_DIRECTORY__, "test1.input1")
let test1Input1TokensBsl = Path.Combine(__SOURCE_DIRECTORY__, "test1.input1.tokens.bsl")
let test1Input2Variation1 = Path.Combine(__SOURCE_DIRECTORY__, "test1.input2.variation1")
let test1Input2Variation2 = Path.Combine(__SOURCE_DIRECTORY__, "test1.input2.variation2")
let test1Input2Bsl = Path.Combine(__SOURCE_DIRECTORY__, "test1.input2.bsl")


let lexArgs =
    Arguments.Empty
    |> Arguments.appendRaw "--light-off"
    |> Arguments.appendRaw "-o"
    |> Arguments.appendRaw test1lexFs
    |> Arguments.appendRaw test1lexMll

let yaccArgs =
    Arguments.Empty
    |> Arguments.appendRaw "--light-off"
    |> Arguments.appendRaw "--module"
    |> Arguments.appendRaw "TestParser"
    |> Arguments.appendRaw "-o"
    |> Arguments.appendRaw test1Fs
    |> Arguments.appendRaw test1Mly

fsLex lexArgs
fsYacc yaccArgs
fsc test1Exe [test1Fsi; test1Fs; test1lexFs; mainMl]

let runTest1Tests () =
    [sprintf "--tokens %s" test1Input1, test1Input1TokensBsl
     test1Input1, test1Input1TokensBsl
     sprintf "%s %s" test1Input2Variation1 test1Input2Variation2, test1Input2Bsl]
    |> List.iter (test test1Exe)

runTest1Tests ()

// test2.exe
// fsYacc "--light-off --module TestParser -o test2.fs test2.mly"
// fsc "test2.exe" ["test2.fsi"; "test2.fs"; "test1lex.fs"; "main.ml"]

// let runTest2Tests() =
//     ["--tokens ./test2.input1", "test2.input1.tokens.bsl"
//      "--tokens ./test2.badInput", "test1.badInput.tokens.bsl"
//      //"./test2.input1", "test2.input1.bsl" TODO: Test fails
//      //"./test2.badInput", "test1.badInput.bsl"
//      ]
//     |> List.iter (test "test2.exe")

// runTest2Tests()


// // test1-unicode.exe
// fsLex "--light-off --unicode -o test1-unicode-lex.fs test1-unicode-lex.mll"
// fsYacc "--light-off --module TestParser -o test1-unicode.fs test1-unicode.mly"
// fsc "test1-unicode.exe" ["test1-unicode.fsi"; "test1-unicode.fs"; "test1-unicode-lex.fs"; "main-unicode.ml"]

// let runTest1UnicodeTests() =
//     ["--tokens ./test1.input1", "test1-unicode.input1.tokens.bsl"
//      "./test1.input1", "test1-unicode.input1.bsl"
//      "./test1.input2.variation1  ./test1.input2.variation2", "test1-unicode.input2.bsl"
//      //"--tokens ./test1-unicode.input3.utf8", "test1-unicode.input3.tokens.bsl"  TODO: Test fails
//      ]
//     |> List.iter (test "test1-unicode.exe")

// runTest1UnicodeTests()


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
