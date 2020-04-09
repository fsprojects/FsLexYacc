
{
module TestLexer
open TestParser
open Microsoft.FSharp.Compatibility.OCaml
} 

let letter = ['A'-'Z'] | ['a'-'z']
let digit = ['0'-'9']
let ident_start_char = 
    letter | ['_'] 
let ident_char = ( ident_start_char| digit | ['\''] )
let ident = ident_start_char ident_char*
let whitespace = [' ' '\t' '\n' '\r']


rule token = parse
 | "(" { LPAREN }
 | ")" { RPAREN }
 | "*" { STAR }
 | "+" { PLUS }
 | "-" { MINUS }
 | "let" { LET }
 | "in" { IN }
 | "end" { END }
 | ident { let s = Lexing.lexeme lexbuf in 
           match s with 
           |  "let" -> LET
           | "in" -> IN
           | "end" -> END
           | _ -> IDENT(s) }
 | whitespace  { token lexbuf }
 | eof  { EOF }

