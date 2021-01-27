module FsLex.Core.Tests

open Expecto
open FsLex.Core

let parse file = 
    FsLexYacc.FsLex.AST.Compile 


[<EntryPoint>]
let main argv =
    Tests.runTestsInAssembly defaultConfig argv
