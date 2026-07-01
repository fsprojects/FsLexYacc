module FsYacc.Core.Tests.AssocCacheCapacity

open System.IO
open Expecto
open FsLexYacc.FsYacc.Driver

// Generates a parser from a minimal grammar and returns the generated source text,
// so we can assert on the `engine` line that invokes the runtime interpreter.
let private generateParser (capacity: int option) =
    let grammar =
        "%token <int> NUM\n\
         %token EOF\n\
         %start start\n\
         %type <int> start\n\
         %%\n\
         start: NUM EOF { $1 }\n"
    let input = Path.GetTempFileName()
    let output = Path.GetTempFileName()
    File.WriteAllText(input, grammar)
    try
        let spec =
            match readSpecFromFile input None with
            | Ok s -> s
            | Result.Error(e, line, col) -> failwithf "grammar failed to parse (%d,%d): %s" line col e.Message
        use logger = new NullLogger() :> Logger
        let compiled = compileSpec spec logger
        let state =
            { GeneratorState.Default with
                input = input
                output = Some output
                modname = Some "TestParser"
                parslib = "FSharp.Text.Parsing"
                lexlib = "FSharp.Text.Lexing"
                assocCacheCapacity = capacity }
        writeSpecToFile state spec compiled
        File.ReadAllText output
    finally
        File.Delete input
        if File.Exists output then File.Delete output

// fsyacc's spec compiler carries process-global mutable state, so the two generation
// runs must not execute concurrently with each other or with the rest of the suite.
[<Tests>]
let assocCacheCapacityTests =
    testSequenced
    <| testList "fsyacc --assoc-cache-capacity" [
        test "explicit capacity emits the 4-arg Interpret overload" {
            let generated = generateParser (Some 0)
            Expect.stringContains
                generated
                "tables.Interpret(lexer, lexbuf, startState, 0)"
                "generated engine should pass the configured capacity to Interpret"
        }

        test "no capacity emits the default 3-arg Interpret" {
            let generated = generateParser None
            Expect.stringContains
                generated
                "tables.Interpret(lexer, lexbuf, startState)"
                "generated engine should use the parameterless Interpret overload"
            Expect.isFalse
                (generated.Contains "tables.Interpret(lexer, lexbuf, startState,")
                "generated engine should not include a capacity argument when the flag is absent"
        }
    ]
