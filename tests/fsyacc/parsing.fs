// (c) Microsoft Corporation 2005-2009.
// Parsing: support fsyacc-generated parsers

[<CompilerMessage("This module is for ML compatibility. Consider using the FSharp.Text.Parsing namespace directly. This message can be disabled using '--nowarn:62' or '#nowarn \"62\"'.", 62, IsHidden=true)>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Microsoft.FSharp.Compatibility.OCaml.Parsing
open FSharp.Text.Lexing
open FSharp.Text.Parsing
open Microsoft.FSharp.Compatibility.OCaml

let err _  = failwith "You must generate your parser using the '--ml-compatibility' option or call 'Parsing.set_parse_state parseState' in each action before using functions from the Parsing module.  This is because the module uses global state which must be set up for use in each parsing action. Review the notes in the 'Microsoft.FSharp.Compatibility.OCaml.Parsing' module if you are using parsers on multiple threads."
let dummyProvider = 
    { new IParseState with 
        member x.InputRange(i) = err();
        member p.InputStartPosition(n) = err();
        member p.InputEndPosition(n) = err();
        member x.ResultRange = err();
        member x.GetInput(i) = err();
        member x.ParserLocalStore = err();
        member x.RaiseError() = err()  
      }

let mutable parse_information = dummyProvider
let set_parse_state (x:IParseState) = parse_information <- x

let enforce_nonnull_pos (r: Range) = 
  match (box r) with 
  | null -> Range.Empty
  | _ -> r

let symbol_start_pos ()   = parse_information.ResultRange |> enforce_nonnull_pos
let symbol_end_pos ()     = parse_information.ResultRange |> enforce_nonnull_pos
let rhs_start_pos (n:int) = parse_information.InputRange(n) |> enforce_nonnull_pos
let rhs_end_pos (n:int)   = parse_information.InputRange(n) |> enforce_nonnull_pos

exception Parse_error  = RecoverableParseError
let parse_error s = parse_information.RaiseError()(failwith s : unit)

let symbol_start () = (symbol_start_pos()).startPos.pos_cnum
let symbol_end () = (symbol_end_pos()).endPos.pos_cnum
let rhs_start n = (rhs_start_pos n).startPos.pos_cnum
let rhs_end n = (rhs_end_pos n).endPos.pos_cnum

