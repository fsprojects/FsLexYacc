#I @"../../packages/FAKE/tools"
#r @"FakeLib.dll"
open Fake
open System
open Fake.FscHelper
open System.IO

[@"..\..\bin\FsLexYacc.Runtime.dll"
 @"..\..\bin\FSharp.Core.dll"]
|> FileHelper.CopyTo @".\"

let run exe args =
    traceImportant <| sprintf "Running '%s' with args '%s'" exe args
    if not <| directExec (fun x-> 
        x.FileName <- exe
        x.Arguments <- args)
    then failwithf "'%s' failed '%s'" exe args
let fsLex  = run @"..\..\bin\fslex.exe"
let fsYacc = run @"..\..\bin\fsyacc.exe"

let fsc output files =
    traceImportant <| sprintf "Building '%s' with from %A" output files
    "lexing.fs"::"parsing.fs"::@"..\..\src\Common\Arg.fs"::"arg.fs"::"tree.ml"::files
    |> Fsc (fun p ->
        { p with References = [@"FsLexYacc.Runtime.dll"
                               @"FSharp.Core\FSharp.Core.dll"]
                 Output = output; Debug = true; FscTarget = Exe})
    let wrongExe = (files |> List.rev |> List.head) |> FileHelper.changeExt ".exe"
    if FileInfo(output).LastWriteTime < FileInfo(wrongExe).LastWriteTime
        then File.Delete(output)
             File.Move(wrongExe, output)
             traceImportant <| sprintf "File '%s' renamed to '%s'" wrongExe output

let test exe (args, baseLineOutput) =
    let messages = ref []
    let appendMessage msg =
        messages := msg :: !messages
    traceImportant <| sprintf "Running '%s' with args '%s'" exe args
    let exitCode =
        ExecProcessWithLambdas
            (fun x-> x.FileName <- exe; x.Arguments <- args)
            (TimeSpan.FromSeconds(5.)) true appendMessage appendMessage
    if exitCode <> 0 then
        failwithf "Process failed with code %d" exitCode
    let output = !messages |> List.rev |> Array.ofList

    if (not <| IO.File.Exists baseLineOutput)
        then failwithf "Baseline file '%s' does not exist" baseLineOutput
    let expectedLines =
        File.ReadAllLines baseLineOutput
    if output.Length <> expectedLines.Length ||
       Seq.map2 (fun a b -> a=b) output expectedLines |> Seq.exists not
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
         traceImportant <| sprintf "OK: Output is equal to base line '%s'" baseLineOutput


// Regression test for FSB 1885
fsLex "repro1885.fsl"


// test1.exe
fsLex "--light-off -o test1lex.fs test1lex.mll"
fsYacc "--light-off --module TestParser -o test1.fs test1.mly"
fsc "test1.exe" ["test1.fsi"; "test1.fs"; "test1lex.fs"; "main.ml"]

let runTest1Tests() =
    [@"--tokens .\test1.input1", "test1.input1.tokens.bsl"
     @".\test1.input1", "test1.input1.bsl"
     @".\test1.input2.variation1  .\test1.input2.variation2", "test1.input2.bsl"]
    |> List.iter (test "test1.exe")

runTest1Tests()


// test2.exe
fsYacc "--light-off --module TestParser -o test2.fs test2.mly"
fsc "test2.exe" ["test2.fsi"; "test2.fs"; "test1lex.fs"; "main.ml"]

let runTest2Tests() =
    [@"--tokens .\test2.input1", "test2.input1.tokens.bsl"
     @"--tokens .\test2.badInput", "test1.badInput.tokens.bsl"
     //@".\test2.input1", "test2.input1.bsl" TODO: Test fails
     //@".\test2.badInput", "test1.badInput.bsl"
     ]
    |> List.iter (test "test2.exe")

runTest2Tests()


// test1-unicode.exe
fsLex "--light-off --unicode -o test1-unicode-lex.fs test1-unicode-lex.mll"
fsYacc "--light-off --module TestParser -o test1-unicode.fs test1-unicode.mly"
fsc "test1-unicode.exe" ["test1-unicode.fsi"; "test1-unicode.fs"; "test1-unicode-lex.fs"; "main-unicode.ml"]

let runTest1UnicodeTests() =
    [@"--tokens .\test1.input1", "test1-unicode.input1.tokens.bsl"
     @".\test1.input1", "test1-unicode.input1.bsl"
     @".\test1.input2.variation1  .\test1.input2.variation2", "test1-unicode.input2.bsl"
     //@"--tokens .\test1-unicode.input3.utf8", "test1-unicode.input3.tokens.bsl"  TODO: Test fails
     ]
    |> List.iter (test "test1-unicode.exe")

runTest1UnicodeTests()


// test1.exe
fsLex "-o test1lex.ml test1lex.mll"
fsYacc "--module TestParser -o test1.ml test1.mly"
fsc "test1.exe" ["test1.mli"; "test1.ml"; "test1lex.ml"; "main.ml"]

runTest1Tests()


// test1compat.exe
fsYacc "--module TestParser -o test1compat.ml --ml-compatibility test1.mly"
fsc "test1compat.exe" ["test1compat.mli"; "test1compat.ml"; "test1lex.ml"; "main.ml"]

[@"--tokens .\test1.input1", "test1compat.input1.tokens.bsl"
 @".\test1.input1", "test1comapt.input1.bsl"
 @".\test1.input2.variation1  .\test1.input2.variation2", "test1compat.input2.bsl"]
|> List.iter (test "test1compat.exe")


// test2.exe
fsYacc "--module TestParser -o test2.ml test2.mly"
fsc "test2.exe" ["test2.mli"; "test2.ml"; "test1lex.ml"; "main.ml"]

runTest2Tests()


// test2compat.exe
fsYacc "--module TestParser -o test2compat.ml --ml-compatibility test2.mly"
fsc "test2compat.exe" ["test2compat.mli"; "test2compat.ml"; "test1lex.ml"; "main.ml"]


// test1-unicode.exe
fsLex "--unicode -o test1-unicode-lex.ml test1-unicode-lex.mll"
fsYacc "--module TestParser -o test1-unicode.ml test1-unicode.mly"
fsc "test1-unicode.exe" ["test1-unicode.mli"; "test1-unicode.ml"; "test1-unicode-lex.ml"; "main-unicode.ml"]

runTest1UnicodeTests()
