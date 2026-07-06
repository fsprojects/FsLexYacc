
module Program
open FSharp.Text.Lexing
open FSharp.Text.Parsing
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


    // Capacity-invariance check (issue #54): the AssocTable cache initial capacity is a pure
    // allocation-tuning knob and must never change parse results. The generated `Parser.start`
    // reads ParseSettings.AssocTableCacheInitialCapacity, so setting it here drives the runtime
    // interpreter that both the default and the --assoc-cache-capacity code paths funnel into.
    // Parse each input at the historical default and at a range of capacities; results must match.
    let parseWith capacity json =
        ParseSettings.AssocTableCacheInitialCapacity <- capacity
        parse json

    let baselineCapacity = 2000
    let capacitiesUnderTest = [ 0; 1; 64; 100000 ]
    let invarianceInputs = [ simpleJson; simpleJson2; complexJson ]

    let mismatches =
        [ for json in invarianceInputs do
            let baseline = parseWith baselineCapacity json
            for capacity in capacitiesUnderTest do
                if parseWith capacity json <> baseline then
                    yield capacity ]

    ParseSettings.AssocTableCacheInitialCapacity <- baselineCapacity

    if List.isEmpty mismatches then
        printfn "Capacity-invariance OK across capacities %A" (baselineCapacity :: capacitiesUnderTest)
    else
        eprintfn "Capacity-invariance FAILED: parse results differed at capacities %A" (List.distinct mismatches)
        exit 1


    //test lexing error
    try
        let simpleJson = "{\"f\"\n" + "\n" + ";"
        let parseResult = simpleJson |> parse 
        printfn "%s" (JsonValue.print parseResult.Value)
    with 
        | e ->  printfn "Error is expected here: \n %s" (e.Message)

    0