module FsYacc.Core.Tests
open Expecto

open FsLexYacc.FsYacc 

[<EntryPoint>]
let main argv =
    Tests.runTestsInAssembly defaultConfig argv
