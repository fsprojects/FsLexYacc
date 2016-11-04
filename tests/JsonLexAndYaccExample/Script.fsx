#r "bin\\debug\\FsLexYacc.Runtime.dll"
#r "bin\\debug\\JsonLexAndYaccExample.exe"

open Microsoft.FSharp.Text.Lexing
open JsonParsing
open System.IO

let parse json = 
    let lexbuf = LexBuffer<char>.FromString json
    let res = Parser.start Lexer.read lexbuf
    res

//a few parsing tests with simple and complex json
let simpleJson = "{\"f\" : 1, \"x\" : 1}"
let (Some parseResult) = simpleJson |> parse 
printfn "%s" (JsonValue.print parseResult)


let simpleJson2 = @"{
          ""title"": ""Cities"",
          ""cities"": [
            { ""name"": ""Chicago"",  ""zips"": [60601,60600] },
            { ""name"": ""New York"", ""zips"": [10001] } 
          ]
        }"
let (Some parseResult2) = simpleJson2 |> parse 
printfn "%s" (JsonValue.print parseResult2)


let complexJson = File.ReadAllText (Path.Combine(__SOURCE_DIRECTORY__,"randomComplexTestsJson.json"))
complexJson |> parse |> ignore


//test lexing error 
try
    let simpleJson = "{\"f\" ;"
    let (Some parseResult) = simpleJson |> parse 
    printfn "%s" (JsonValue.print parseResult)
with 
    | e ->  printfn "Error is expected here: \n %s" (e.Message)