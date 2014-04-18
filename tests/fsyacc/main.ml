open Tree
open Microsoft.FSharp.Compatibility.OCaml
let tokenize = ref false

let usage =
  [ "--tokens", Arg.Set tokenize, "tokenize the first file and exit" ]

let inputs = ref []

let _ = Arg.parse usage (fun x -> inputs := !inputs @ [x]) "test... <filename> <filename>\nTests that all inputs give equivalent syntac trees"

let main() = 
  if !inputs = [] then
    Printf.eprintf "at least one input should be given\n";
  try 
    let results = 
      List.map
        (fun filename -> 
          use is = System.IO.File.OpenText filename in
          let lexbuf = Lexing.from_channel is in 
          if !tokenize then begin 
            while true do 
              Printf.eprintf "tokenize - getting one token\n";
              let t = TestLexer.token lexbuf in 
              Printf.eprintf "tokenize - got %s, now at char %d\n" (TestParser.token_to_string t) (Lexing.lexeme_start_p lexbuf).pos_cnum;
              if t = TestParser.EOF then exit 0;
            done;
          end;
          let tree = 
            try TestParser.start TestLexer.token lexbuf 
            with e -> 
              Printf.eprintf "%s(%d,%d): error: %s\n" filename (Lexing.lexeme_start_p lexbuf).pos_lnum ((Lexing.lexeme_start_p lexbuf).pos_cnum -  (Lexing.lexeme_start_p lexbuf).pos_bol) (match e with Failure s -> s | _ -> e.ToString());
              exit 1 in 
          Printf.eprintf "parsed %s ok\n" filename;
          (filename,tree))
        !inputs in 
    List.iter 
      (fun (filename1,tree1) -> 
        List.iter 
          (fun (filename2,tree2) -> 
            if filename1 > filename2 then 
              if tree1 <> tree2 then 
                begin 
                  Printf.eprintf "file %s and file %s parsed to different results!\n" filename1 filename2;
                  let rec ptree os (Node(n,l)) = Printf.fprintf os "(%s %a)" n ptrees l
                  and ptrees os l = match l with [] -> () | [h] -> ptree os h | h::t -> Printf.fprintf os "%a %a" ptree h ptrees t in 
                  Printf.eprintf "file %s = %a\n" filename1 ptree tree1;
                  Printf.eprintf "file %s = %a\n" filename2 ptree tree2;
                  exit 1;
                end)
        results)
      results;
  with e -> 
    Printf.eprintf "Error: %s\n" (match e with Failure s -> s | e -> e.ToString());
    exit 1
   

let _ = main ()

