#light

open Tree
open System.IO
open Microsoft.FSharp.Compatibility.OCaml
open Microsoft.FSharp.Text

let tokenize = ref false

let usage =
  [ "--tokens", Arg.Set tokenize, "tokenize the first file and exit" ]

let inputs = ref []

let _ = Arg.parse usage (fun x -> inputs := !inputs @ [x]) "test... <filename> <filename>\nTests that all inputs give equivalent syntac trees"

open Microsoft.FSharp.Text.Lexing
open Microsoft.FSharp.Compatibility.OCaml.Lexing

type UnicodeLexbuf =  LexBuffer<char>
  
/// Create a Unicode LexBuffer
///
/// F# 1.9.3.6 doesn't have convenient support for creating Unicode Lexbuffers.
/// This function creates such a buffer and instruments it with the standard support
/// to update the start/end positions based on character count.
///
/// One small annoyance is that LexBuffers and not IDisposable. This means 
/// we can't just return the LexBuffer object, since the file it wraps wouldn't
/// get closed when we're finished with the LexBuffer. Hence we return the stream,
/// the reader and the LexBuffer. The caller should dispose the first two when done.
let UnicodeFileAsCharLexbuf (filename,codePage : int option) =
    // Use the .NET functionality to auto-detect the unicode encoding
    // It also uses Lexing.from_text_reader to present the bytes read to the lexer in UTF8 decoded form
    let stream  = new FileStream(filename,FileMode.Open,FileAccess.Read,FileShare.Read) in
    let reader = 
        match codePage with 
        | None -> new  StreamReader(stream,true)
        | Some n -> new  StreamReader(stream,System.Text.Encoding.GetEncoding(n)) in
    let dummyPos = {Lexing.pos_fname=""; Lexing.pos_lnum= 0; Lexing.pos_bol= 0; Lexing.pos_cnum=0 } in
    let lexbuf = LexBuffer.FromCharFunction((fun buf n -> reader.Read(buf,0,n))) in
    stream, reader, lexbuf


let main() = 
  if !inputs = [] then
    Printf.eprintf "at least one input should be given\n";
  try 
    let results = 
      !inputs |> List.map (fun filename -> 
          let dummyPos = {Lexing.pos_fname=""; Lexing.pos_lnum= 0; Lexing.pos_bol= 0; Lexing.pos_cnum=0 } in
          let stream,reader,lexbuf = UnicodeFileAsCharLexbuf(filename,None) in 
          use stream = stream
          use reader = reader
          if !tokenize then begin 
            while true do 
              Printf.eprintf "tokenize - getting one token\n";
              let t = TestLexer.token lexbuf in 
              Printf.eprintf "tokenize - got %s, now at char %d\n" (TestParser.token_to_string t) lexbuf.StartPos.AbsoluteOffset;
              match t with 
              | TestParser.EOF -> exit 0;
              | TestParser.IDENT s -> 
                  for c in s do
                      Printf.eprintf "   ident char = %d\n" (int c)
                  done;
              | _ -> ()
                      
            done;
          end;
          let tree = 
            try TestParser.start TestLexer.token lexbuf 
            with e -> 
              Printf.eprintf "%s(%d,%d): error: %s\n" filename lexbuf.StartPos.Line lexbuf.StartPos.Column (match e with Failure s -> s | _ -> e.ToString());
              exit 1 in 
          Printf.eprintf "parsed %s ok\n" filename;
          (filename,tree)) in 
    for (filename1,tree1) in results do
        for (filename2,tree2) in results do
            if filename1 > filename2 then 
              if tree1 <> tree2 then 
                  Printf.eprintf "file %s and file %s parsed to different results!\n" filename1 filename2;
                  let rec ptree os (Node(n,l)) = Printf.fprintf os "(%s %a)" n ptrees l
                  and ptrees os l = match l with [] -> () | [h] -> ptree os h | h::t -> Printf.fprintf os "%a %a" ptree h ptrees t in 
                  Printf.eprintf "file %s = %a\n" filename1 ptree tree1;
                  Printf.eprintf "file %s = %a\n" filename2 ptree tree2;
                  exit 1
  with e -> 
    Printf.eprintf "Error: %s\n" (match e with Failure s -> s | e -> e.ToString());
    exit 1
   

let _ = main ()

