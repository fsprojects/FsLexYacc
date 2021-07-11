module FsLex.Core.Tests

open Expecto
open FsLexYacc.FsLex

let parse file = AST.Compile 

[<EntryPoint>]
let main argv =
    Tests.runTestsInAssembly defaultConfig argv
