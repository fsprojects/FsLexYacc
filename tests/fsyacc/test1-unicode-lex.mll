
{
module TestLexer
open TestParser
} 

(* These specifications follow the C# specification *)
let digit = '\Nd'
let letter = '\Lu' | '\Ll' | '\Lm' | '\Lo' | '\Nl'

let ident_start_char = letter | ['_'] 

let connecting_char = '\Pc'
let combining_char = '\Mn' | '\Mc'
let formatting_char = '\Cf' 
let ident_char = letter | digit | connecting_char | combining_char | formatting_char

let ident = ident_start_char ident_char*

let whitespace = 
    '\Zs' 
  | '\u0009' (* horizontal tab *)
  | '\u000B' (* vertical tab *)
  | '\u000C' (* form feed *)
  | '\u000D' (* carriage return *)
  | '\u000A' (* line feed *)
  | '\u0085' (* next line *)
  | '\u2028' (* line separator *)
  | '\u2029' (* paragraph separator *)


rule token = parse
 | "(" { LPAREN }
  (* the "approx equals" symbol, just to test a random specific Unicode character *)
 | '≈'+ { IDENT(new System.String(lexbuf.Lexeme) ) }
 (* | '\U00002248'+ { IDENT(new System.String(lexbuf.Lexeme) ) } *)
 
  (* the "not equals" symbol, just to test a random specific Unicode character *)
 | '≠'+ { IDENT(new System.String(lexbuf.Lexeme) ) } 
(* | '\U00002260'+ { IDENT(new System.String(lexbuf.Lexeme) ) } *)
 | ")" { RPAREN }
 | "*" { STAR }
 | "+" { PLUS }
 | "-" { MINUS }
 | ident { let s = new System.String(lexbuf.Lexeme) in 
           match s with 
           |  "let" -> LET
           | "in" -> IN
           | "end" -> END
           | _ -> IDENT(s) }
 | whitespace  { token lexbuf }
 | eof  { EOF }

