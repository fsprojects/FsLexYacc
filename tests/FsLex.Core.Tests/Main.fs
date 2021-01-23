module FsLex.Core.Tests

open Expecto
open FsLex.Core


[<EntryPoint>]
let main argv =
    Tests.runTestsInAssembly defaultConfig argv
