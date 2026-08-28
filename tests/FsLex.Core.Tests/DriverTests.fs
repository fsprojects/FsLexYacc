module FsLex.Core.Tests.DriverTests

open System.IO
open FSharp.Text.Lexing
open FsLexYacc.FsLex.Driver
open Expecto

let private writeTopCodeToInterface (header: string) =
    let output = Path.GetTempFileName()
    let outputi = String.concat "" [ output; "i" ]

    try
        using (new Writer(output, outputi)) (writeTopCode (header, Position.Empty))
        File.ReadAllText outputi
    finally
        File.Delete output
        File.Delete outputi

[<Tests>]
let tests =
    testList "Driver" [
        testList "getHeaderDeclarations" [
            test "keeps the module declaration and the opens" {
                let actual =
                    getHeaderDeclarations "module Test.Lexer\n\nopen System\nopen System.Text\n\nlet x = 1"

                Expect.sequenceEqual
                    actual
                    [| "module Test.Lexer"; "open System"; "open System.Text" |]
                    "Module declaration and opens should be kept"
            }

            test "skips a nested module definition" {
                let actual =
                    getHeaderDeclarations "open System\nmodule Ranges =\n    let isInt8BadMax x = 1 <<< 7 = x"

                Expect.sequenceEqual actual [| "open System" |] "A nested module definition should be skipped"
            }

            test "keeps a module abbreviation" {
                let actual = getHeaderDeclarations "module Range = FSharp.Compiler.Text.Range"

                Expect.sequenceEqual
                    actual
                    [| "module Range = FSharp.Compiler.Text.Range" |]
                    "A module abbreviation should be kept"
            }
        ]

        testList "writeTopCode" [
            test "the header in the signature file is newline terminated" {
                let actual = writeTopCodeToInterface "module Test.Lexer\nopen System\nlet x = 1"

                Expect.equal
                    actual
                    (sprintf "module Test.Lexer%sopen System%s" System.Environment.NewLine System.Environment.NewLine)
                    "Every header line should be written on its own line"
            }

            test "a header without declarations writes nothing" {
                let actual = writeTopCodeToInterface "let x = 1"

                Expect.equal actual "" "Nothing should be written when there is no declaration to repeat"
            }
        ]
    ]
