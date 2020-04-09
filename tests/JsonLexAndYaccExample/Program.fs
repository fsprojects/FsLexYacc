
module Program
open FSharp.Text.Lexing
open JsonParsing

[<EntryPoint>]
let main argv =
    let parse json = 
        let lexbuf = LexBuffer<char>.FromString json
        let res = Parser.start Lexer.read lexbuf
        res

    //a few parsing tests with simple and complex json
    let simpleJson = "{\"f\" : 1}"
    let parseResult = simpleJson |> parse 
    printfn "%s" (JsonValue.print parseResult.Value)


    let simpleJson2 = @"{
              ""title"": ""Cities"",
              ""cities"": [
                { ""name"": ""Chicago"",  ""zips"": [60601,60600] },
                { ""name"": ""New York"", ""zips"": [10001] } 
              ]
            }"
    let parseResult2 = simpleJson2 |> parse 
    printfn "%s" (JsonValue.print parseResult2.Value)

    let complexJson = System.IO.File.ReadAllText "randomComplexTestsJson.json"
    complexJson |> parse |> ignore


    //test lexing error 
    try
        let simpleJson = "{\"f\"\n" + "\n" + ";"
        let parseResult = simpleJson |> parse 
        printfn "%s" (JsonValue.print parseResult.Value)
    with 
        | e ->  printfn "Error is expected here: \n %s" (e.Message)

    0