// (c) Microsoft Corporation 2005-2009. 

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Microsoft.FSharp.Compatibility.OCaml.Arg
open Microsoft.FSharp.Text

let Clear  x = ArgType.Clear x
let Float  x = ArgType.Float x
let Int    x = ArgType.Int x
let Rest   x = ArgType.Rest x
let Set    x = ArgType.Set x
let String x = ArgType.String x
let Unit   x = ArgType.Unit x

type spec = ArgType
type argspec = (string * spec * string) 
#if FX_NO_COMMAND_LINE_ARGS
#else

exception Bad of string
exception Help of string
let parse_argv cursor argv specs other usageText =
    ArgParser.ParsePartial(cursor, argv, List.map (fun (a,b,c) -> ArgInfo(a,b,c)) specs, other, usageText)

let parse specs other usageText = 
    ArgParser.Parse(List.map (fun (a,b,c) -> ArgInfo(a,b,c)) specs, other, usageText)

let usage specs usageText = 
    ArgParser.Usage(List.map (fun (a,b,c) -> ArgInfo(a,b,c)) specs, usageText)
#endif