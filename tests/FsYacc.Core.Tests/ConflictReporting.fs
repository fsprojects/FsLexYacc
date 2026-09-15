module FsYacc.Core.Tests.ConflictReporting

open System
open System.IO
open Expecto
open FsLexYacc.FsYacc.Driver

// A grammar with one shift/reduce conflict: after `expr PLUS expr`, PLUS can either be
// shifted or the production reduced, and no precedence is declared to settle it.
let private conflictingGrammar =
    "%token <int> NUM\n\
     %token PLUS EOF\n\
     %start start\n\
     %type <int> start\n\
     %%\n\
     start: expr EOF { $1 }\n\
     expr: NUM { $1 } | expr PLUS expr { $1 + $3 }\n"

/// Compiles the grammar with the given logger and returns everything fsyacc wrote to stdout.
let private compileCapturingStdout (logger: Logger) =
    let input = Path.GetTempFileName()
    File.WriteAllText(input, conflictingGrammar)
    let original = Console.Out
    use captured = new StringWriter()

    try
        let spec =
            match readSpecFromFile input None with
            | Ok s -> s
            | Result.Error(e, line, col) -> failwithf "grammar failed to parse (%d,%d): %s" line col e.Message

        Console.SetOut captured
        compileSpec spec logger |> ignore
        captured.ToString()
    finally
        Console.SetOut original
        File.Delete input

let private conflictDetail = "shift/reduce error at state"

// fsyacc's spec compiler carries process-global mutable state and these tests redirect
// Console.Out, so they must not run concurrently with the rest of the suite.
[<Tests>]
let conflictReportingTests =
    testSequenced
    <| testList "fsyacc conflict reporting" [
        test "stdout carries the conflict count, not the detail of each conflict" {
            use logger = new NullLogger() :> Logger
            let stdout = compileCapturingStdout logger
            Expect.stringContains stdout "1 shift/reduce conflicts" "the summary should still be printed"
            Expect.isFalse (stdout.Contains conflictDetail) "per-conflict detail should not be printed to stdout"
        }

        test "the listing file carries the detail of each conflict" {
            let listing = Path.GetTempFileName()

            try
                let stdout =
                    use logger = new FileLogger(listing) :> Logger
                    compileCapturingStdout logger

                Expect.isFalse (stdout.Contains conflictDetail) "per-conflict detail should not be printed to stdout"

                Expect.stringContains
                    (File.ReadAllText listing)
                    conflictDetail
                    "per-conflict detail should be written to the listing file"
            finally
                File.Delete listing
        }
    ]
