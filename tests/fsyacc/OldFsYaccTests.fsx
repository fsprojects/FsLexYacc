#!/usr/bin/env -S dotnet fsi

// The original fsyacc/fslex test suite: generate a lexer and parser from the checked-in
// specifications, build the project that consumes them, and compare its output against a
// baseline file. Run on its own with `dotnet fsi tests/fsyacc/OldFsYaccTests.fsx`.

open System
open System.Diagnostics
open System.IO
open System.Runtime.InteropServices

let assertFileExists file =
    if File.Exists(file) then
        printfn "%s exists!" file
    else
        failwithf "'%s' doesn't exist" file

/// Start a process with an explicit argument list, so a path containing a space survives.
let private start (redirect: bool) (arguments: string list) =
    let startInfo =
        ProcessStartInfo(
            "dotnet",
            UseShellExecute = false,
            RedirectStandardOutput = redirect,
            RedirectStandardError = redirect
        )

    for argument in arguments do
        startInfo.ArgumentList.Add argument

    Process.Start startInfo

let dotnet (arguments: string list) =
    use proc = start false arguments
    proc.WaitForExit()

    if proc.ExitCode <> 0 then
        failwithf "Failed to run \"dotnet %s\"" (String.concat " " arguments)

/// Run and capture, returning the exit code together with everything written to stderr.
let private dotnetCaptured (arguments: string list) =
    use proc = start true arguments
    let error = proc.StandardError.ReadToEnd()
    proc.StandardOutput.ReadToEnd() |> ignore
    proc.WaitForExit()
    proc.ExitCode, error

let run project (arguments: string list) =
    printfn "Running '%s' with args %s" project (String.concat " " arguments)
    dotnet [ "build"; project; "-c"; "Release" ]
    dotnet [ "run"; "--project"; project; yield! arguments ]

let test proj shouldBeOK (arguments: string list, baseLineOutput) =
    printfn "Running '%s' with args '%s'" proj (String.concat " " arguments)

    let exitCode, error = dotnetCaptured [ "run"; "--project"; proj; yield! arguments ]

    if (exitCode = 0) <> shouldBeOK then
        failwithf "Process failed with code %d on input %s" exitCode (String.concat " " arguments)

    let output =
        // For some reason, the output is captured in the stderr
        error.Split('\n', StringSplitOptions.RemoveEmptyEntries)
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
                        pathPiece.[idx + 1 ..]
                    else
                        pathPiece

                pieces.[0] + " " + pathPiece + " " + pieces.[2]
            else
                line)

    if (not <| File.Exists baseLineOutput) then
        failwithf "Baseline file '%s' does not exist" baseLineOutput

    let expectedLines = File.ReadAllLines baseLineOutput

    if
        output.Length <> expectedLines.Length
        || Seq.map2 (=) output expectedLines |> Seq.exists not
    then
        printfn "Expected:"

        for line in expectedLines do
            printfn "\t%s" line

        printfn "Output:"

        for line in output do
            printfn "\t%s" line

        File.WriteAllLines(baseLineOutput + ".err", output)
        failwithf "Output is not equal to expected base line '%s'" baseLineOutput

let fslexProject =
    Path.Combine(__SOURCE_DIRECTORY__, "..", "..", "src", "FsLex", "fslex.fsproj")

let fsYaccProject =
    Path.Combine(__SOURCE_DIRECTORY__, "..", "..", "src", "FsYacc", "fsyacc.fsproj")

assertFileExists fslexProject
assertFileExists fsYaccProject

let fsLex = run fslexProject
let fsYacc = run fsYaccProject

let repro1885Fsl =
    Path.Combine(__SOURCE_DIRECTORY__, "repro1885", "repro1885.fsl")
// Regression test for FSB 1885
fsLex [ repro1885Fsl ]

// Test 1
let test1lexFs = Path.Combine(__SOURCE_DIRECTORY__, "Test1", "test1lex.fs")
let test1lexFsl = Path.Combine(__SOURCE_DIRECTORY__, "Test1", "test1lex.fsl")
let test1Fs = Path.Combine(__SOURCE_DIRECTORY__, "Test1", "test1.fs")
let test1Fsy = Path.Combine(__SOURCE_DIRECTORY__, "Test1", "test1.fsy")
let test1Input1 = Path.Combine(__SOURCE_DIRECTORY__, "Test1", "test1.input1")

let test1Input1Bsl =
    Path.Combine(__SOURCE_DIRECTORY__, "Test1", "test1.input1.bsl")

let test1Input1TokensBsl =
    Path.Combine(__SOURCE_DIRECTORY__, "Test1", "test1.input1.tokens.bsl")

let test1Input2Variation1 =
    Path.Combine(__SOURCE_DIRECTORY__, "Test1", "test1.input2.variation1")

let test1Input2Variation2 =
    Path.Combine(__SOURCE_DIRECTORY__, "Test1", "test1.input2.variation2")

let test1Input2Bsl =
    Path.Combine(__SOURCE_DIRECTORY__, "Test1", "test1.input2.bsl")

let test1Input3 = Path.Combine(__SOURCE_DIRECTORY__, "Test1", "test1.input3")

let test1Input3Bsl =
    Path.Combine(__SOURCE_DIRECTORY__, "Test1", "test1.input3.bsl")

let test1Input3TokensBsl =
    Path.Combine(__SOURCE_DIRECTORY__, "Test1", "test1.input3.tokens.bsl")

let test1Proj = Path.Combine(__SOURCE_DIRECTORY__, "Test1", "test1.fsproj")
let test1Input4 = Path.Combine(__SOURCE_DIRECTORY__, "Test1", "test1.input4")

let test1Input4Bsl =
    Path.Combine(__SOURCE_DIRECTORY__, "Test1", "test1.input4.bsl")

let test1Input4TokensBsl =
    Path.Combine(__SOURCE_DIRECTORY__, "Test1", "test1.input4.tokens.bsl")

let runTests' shouldBeOK projFile xs =
    dotnet [ "build"; projFile; "-c"; "Release" ]
    xs |> List.iter (test projFile shouldBeOK)

let runTests projFile xs = runTests' true projFile xs

fsLex [ "-o"; test1lexFs; test1lexFsl ]
fsYacc [ "--module"; "TestParser"; "-o"; test1Fs; test1Fsy ]

runTests
    test1Proj
    [
        [ "--tokens"; test1Input1 ], test1Input1TokensBsl
        [ test1Input1 ], test1Input1Bsl
        [ test1Input1 ], test1Input1Bsl
        [ test1Input2Variation1; test1Input2Variation2 ], test1Input2Bsl
        [ "--tokens"; test1Input3 ], test1Input3TokensBsl
        [ test1Input3 ], test1Input3Bsl
    ]

// Case insensitive option test
fsLex [ "-i"; "-o"; test1lexFs; test1lexFsl ]

runTests
    test1Proj
    [
        [ "--tokens"; test1Input4 ], test1Input4TokensBsl
        [ "--tokens"; test1Input3 ], test1Input4TokensBsl
        [ test1Input3; test1Input4 ], test1Input4Bsl
    ]

// Test 1 unicode
let test1unicodelexFs =
    Path.Combine(__SOURCE_DIRECTORY__, "unicode", "test1-unicode-lex.fs")

let test1unicodelexFsl =
    Path.Combine(__SOURCE_DIRECTORY__, "unicode", "test1-unicode-lex.fsl")

let test1unicodeFs =
    Path.Combine(__SOURCE_DIRECTORY__, "unicode", "test1-unicode.fs")

let test1unicodeFsy =
    Path.Combine(__SOURCE_DIRECTORY__, "unicode", "test1-unicode.fsy")

let test1unicodeProj =
    Path.Combine(__SOURCE_DIRECTORY__, "unicode", "test1-unicode.fsproj")

let test1unicodeInput3 =
    Path.Combine(__SOURCE_DIRECTORY__, "unicode", "test1-unicode.input3.utf8")

let test1unicodeInput3TokensBsl =
    Path.Combine(__SOURCE_DIRECTORY__, "unicode", "test1-unicode.input3.tokens.bsl")

let test1unicodeWithTitleCaseLetter =
    Path.Combine(__SOURCE_DIRECTORY__, "unicode", "test1-unicode.WithTitleCaseLetter.utf8")

let test1unicodeWithTitleCaseLetterTokensBsl =
    Path.Combine(__SOURCE_DIRECTORY__, "unicode", "test1-unicode.WithTitleCaseLetter.tokens.bsl")

let test1unicodeWithTitleCaseLetterTokensErrorBsl =
    Path.Combine(__SOURCE_DIRECTORY__, "unicode", "test1-unicode.WithTitleCaseLetter.tokens.error.bsl")

fsLex [ "--unicode"; "-o"; test1unicodelexFs; test1unicodelexFsl ]
fsYacc [ "--module"; "TestParser"; "-o"; test1unicodeFs; test1unicodeFsy ]

runTests
    test1unicodeProj
    [
        [ "--tokens"; test1Input1 ], test1Input1TokensBsl
        [ test1Input1 ], test1Input1Bsl
        [ test1Input2Variation1; test1Input2Variation2 ], test1Input2Bsl
        [ "--tokens"; test1unicodeInput3 ], test1unicodeInput3TokensBsl
    ]

runTests'
    false
    test1unicodeProj
    [
        [ "--tokens"; test1unicodeWithTitleCaseLetter ], test1unicodeWithTitleCaseLetterTokensErrorBsl
    ]

// Case insensitive option test
fsLex [ "--unicode"; "-i"; "-o"; test1unicodelexFs; test1unicodelexFsl ]

runTests
    test1unicodeProj
    [
        [ "--tokens"; test1Input1 ], test1Input1TokensBsl
        [ test1Input1 ], test1Input1Bsl
        [ test1Input2Variation1; test1Input2Variation2 ], test1Input2Bsl
        [ "--tokens"; test1unicodeInput3 ], test1unicodeInput3TokensBsl
        [ "--tokens"; test1unicodeWithTitleCaseLetter ], test1unicodeWithTitleCaseLetterTokensBsl
    ]

// Test 2
let test2lexFs = Path.Combine(__SOURCE_DIRECTORY__, "Test2", "test2lex.fs")
let test2lexFsl = Path.Combine(__SOURCE_DIRECTORY__, "Test1", "test1lex.fsl")
let test2Fs = Path.Combine(__SOURCE_DIRECTORY__, "Test2", "test2.fs")
let test2Fsy = Path.Combine(__SOURCE_DIRECTORY__, "Test2", "test2.fsy")
let test2Proj = Path.Combine(__SOURCE_DIRECTORY__, "Test2", "test2.fsproj")
let test2Input1 = Path.Combine(__SOURCE_DIRECTORY__, "Test2", "test2.input1")

let test2Input1TokensBsl =
    Path.Combine(__SOURCE_DIRECTORY__, "Test2", "test2.input1.tokens.bsl")

let test2BadInput = Path.Combine(__SOURCE_DIRECTORY__, "Test2", "test2.badInput")

let test2BadInputTokensBsl =
    Path.Combine(__SOURCE_DIRECTORY__, "Test2", "test2.badInput.tokens.bsl")

fsLex [ "-o"; test2lexFs; test2lexFsl ]
fsYacc [ "--module"; "TestParser"; "-o"; test2Fs; test2Fsy ]

runTests
    test2Proj
    [
        [ "--tokens"; test2Input1 ], test2Input1TokensBsl
        [ "--tokens"; test2BadInput ], test2BadInputTokensBsl
    ]

// #141 TODO
let repro141Fsl =
    Path.Combine(__SOURCE_DIRECTORY__, "repro_#141", "Lexer_fail_option_i.fsl")

let repro141Fs =
    Path.Combine(__SOURCE_DIRECTORY__, "repro_#141", "Lexer_fail_option_i.fs")

fsLex [ "-i"; "-o"; repro141Fs; repro141Fsl ]
fsLex [ "--unicode"; "-i"; "-o"; repro141Fs; repro141Fsl ]
