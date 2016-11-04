module FsLexYacc.Tests

open NUnit.Framework
open Microsoft.FSharp.Build.Logging

let [<Test>]``FsLex app crash error`` () =
  match "FSLEX: error FSL000: Something gone wrong" with
  | FsLexCrash "Something gone wrong" -> ignore()
  | _ -> failwith "Expected FsLexCrash"


let [<Test>]``FsYacc app crash error`` () =
  match "FSYACC: error FSY000: Something gone wrong" with
  | FsYaccCrash "Something gone wrong" -> ignore()
  | _ -> failwith "Expected FsYaccCrash"


let [<Test>]``Unexpected character`` () =
  match "Lexer.fsl(14,22): error: Unexpected character '''" with
  | FsLexYaccError ("Lexer.fsl", 14, 22, "Unexpected character '''") -> ignore()
  | _ -> failwith "Expected: Unexpected character '''"


let [<Test>]``unterminated string in code`` () =
  match "Lexer.fsl(35,0): error: unterminated string in code" with
  | FsLexYaccError ("Lexer.fsl", 35, 0, "unterminated string in code") -> ignore()
  | _ -> failwith "Expected: unterminated string in code"

let [<Test>]``parse error`` () =
  match "Lexer.fsl(37,15): error: parse error" with
  | FsLexYaccError ("Lexer.fsl", 37, 15, "parse error") -> ignore()
  | _ -> failwith "Expected: parse error"