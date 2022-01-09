open Tree
open System.IO
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Compatibility.OCaml
open FSharp.Text.Lexing
let tokenize = ref false

let usage = [ "--tokens", Arg.Set tokenize, "tokenize the first file and exit" ]

let mutable inputs = []

Arg.parse usage (fun x -> inputs <- inputs @ [x]) "test... <filename> <filename>\nTests that all inputs give equivalent syntac trees"

let createLexBuffer (a:Expr<'a->_>) (x:FileStream) : 'a =
    (if typeof<'a> = typeof<LexBuffer<char>> then
        x |> StreamReader |> LexBuffer<_>.FromTextReader :> obj
    elif typeof<'a> = typeof<LexBuffer<byte>> then
        x |> BinaryReader |> LexBuffer<_>.FromBinaryReader :> obj
    else
        failwith "Pies")
    :?> _

if inputs = [] then
    Printf.eprintf "at least one input should be given\n";
try 
  let results =
    inputs
    |> List.map
      (fun filename -> 
        use is = File.OpenRead filename
        let lexbuf = createLexBuffer <@ TestLexer.token @> is
        if !tokenize then
          while true do 
            Printf.eprintf "tokenize - getting one token\n"
            let t = TestLexer.token lexbuf
            Printf.eprintf "tokenize - got %s, now at char %d\n" (TestParser.token_to_string t) (lexbuf.StartPos).pos_cnum
            match t with 
            | TestParser.EOF -> exit 0
            | TestParser.IDENT s -> 
                for c in s do
                    Printf.eprintf "   ident char = %d\n" (int c)
            | _ -> ()
        let tree = 
          try
            TestParser.start TestLexer.token lexbuf
          with e -> 
            Printf.eprintf "%s(%d,%d): error: %s\n" filename lexbuf.StartPos.pos_lnum (lexbuf.StartPos.pos_cnum -  lexbuf.StartPos.pos_bol) (match e with Failure s -> s | _ -> e.ToString())
            exit 1
        Printf.eprintf "parsed %s ok\n" filename
        (filename,tree)
      )
  results
  |> List.iter 
    (fun (filename1,tree1) ->
      results
      |> List.iter 
        (fun (filename2,tree2) -> 
          if filename1 > filename2 then
            if tree1 <> tree2 then
                Printf.eprintf "file %s and file %s parsed to different results!\n" filename1 filename2
                let rec ptree os (Node(n,l)) =
                    Printf.fprintf os "(%s %a)" n ptrees l
                and ptrees os l =
                    match l with
                    | [] -> ()
                    | [h] -> ptree os h
                    | h::t -> Printf.fprintf os "%a %a" ptree h ptrees t
                Printf.eprintf "file %s = %a\n" filename1 ptree tree1
                Printf.eprintf "file %s = %a\n" filename2 ptree tree2
                exit 1
        ) 
    )
with e -> 
  Printf.eprintf "Error: %s\n" (match e with Failure s -> s | e -> e.ToString());
  exit 1

