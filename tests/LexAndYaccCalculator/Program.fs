open System.IO
open FSharp.Text.Lexing
open Syntax

let testLexerAndParserFromString text expected =
    let lexbuf = LexBuffer<char>.FromString text

    let parse = Parser.start Lexer.tokenstream lexbuf

    printfn "parse: result = %A, expected %A" parse expected

testLexerAndParserFromString "1" (Int 1)
testLexerAndParserFromString "hello" (Var "hello")
testLexerAndParserFromString "1 + 1" (Add((Int 1),(Int 1)))
testLexerAndParserFromString "1 - 1" (Sub((Int 1),(Int 1)))
testLexerAndParserFromString "1 * 1" (Mul((Int 1),(Int 1)))
testLexerAndParserFromString "1 / 1" (Div((Int 1),(Int 1)))
testLexerAndParserFromString "1 + 2 + 3" (Add(Add(Int 1, Int 2), Int 3))
testLexerAndParserFromString "1 + 2 * 3" (Add(Int 1, Mul(Int 2, Int 3)))
testLexerAndParserFromString "1 * 2 + 3" (Add(Mul(Int 1, Int 2), Int 3))
