%{
//module TestParser

/// Stephan Tolksdorf reported a bug where quotation characters in headers and semantic
/// actions caused the parser generator to fail with an "unterminated string" comment.
let testQuotationCharInHeader1 = '"'
let testQuotationCharInHeader2 = '\"'

open Microsoft.FSharp.Compatibility.OCaml

%} 

%type <Tree.tree> start
%token MINUS STAR LPAREN RPAREN PLUS EOF LET IN END
%token <string> IDENT
%start start

%right MINUS
%left PLUS
%left STAR
%%	

start: decls EOF { System.Console.WriteLine("#decls = {0}.", List.length $1);  Tree.Node("decls",$1) }

decls: decls decl { $2 :: $1 } | decl { [$1] }


decl: IDENT expr { 
    /// Stephan Tolksdorf reported a bug where quotation characters in headers and semantic
    /// actions caused the parser generator to fail with an "unterminated string" comment.
    let testQuotationCharInHeader1 = '"' in
    let testQuotationCharInHeader2 = '\"' in
    Tree.Node("decl",[$2]) }

expr:  expr MINUS expr { Tree.Node("-",[$1;$3]) } 
|  expr PLUS expr { Tree.Node("+",[$1;$3]) } 
|  expr STAR expr { Tree.Node("*",[$1;$3]) } 
|  LPAREN expr RPAREN { $2 } 
|  LET decl IN expr END {  $4 } 
|  LET error IN expr END {  System.Console.Error.WriteLine("invisible error recovery successful."); $4 } 
|  LPAREN expr error { System.Console.Error.WriteLine("Missing paren: visible recovery successful."); $2 } 
|  RPAREN RPAREN RPAREN { System.Console.Error.WriteLine("Three parens is a bit rich - why not use Lisp if you like that sort of thing. Raising explicit parse error, which we will recover from."); 
                          raise Microsoft.FSharp.Text.Parsing.RecoverableParseError } 
|  IDENT { Tree.Node($1,[]) } 



