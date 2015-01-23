
module Program
open Microsoft.FSharp.Text.Lexing
open JsonParsing

[<EntryPoint>]
let main argv = 
    let parse json = 
        let lexbuf = LexBuffer<char>.FromString json
        let res = Parser.start Lexer.read lexbuf
        res
    let simpleJson = "{\"f\" : 1, \"x\" : 1}"
    let (Some praseResult) = simpleJson |> parse 
    printfn "%s" (JsonValue.print praseResult)


    let simpleJson2 = @"{
              ""title"": ""Cities"",
              ""cities"": [
                { ""name"": ""Chicago"",  ""zips"": [60601,60600] },
                { ""name"": ""New York"", ""zips"": [10001] } 
              ]
            }"
    let (Some praseResult2) = simpleJson2 |> parse 
    printfn "%s" (JsonValue.print praseResult2)


    let complexJson = System.IO.File.ReadAllText "randomComplexTestsJson.json"
    complexJson |> parse |> ignore

    0