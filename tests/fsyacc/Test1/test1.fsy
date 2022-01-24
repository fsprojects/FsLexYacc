%{
//module TestParser
//Bug1885: is about skipping // comments in the header and code sections, rather than lexing as tokens
//Bug1885: REPRO: Convert a string such as "\"c:\\windows\\\"" into "c:\windows\" 
%} 

%type <Tree.tree> start
%token MINUS STAR LPAREN RPAREN PLUS EOF LET IN END
%token <string> IDENT
%start start

%right MINUS
%left PLUS
%left STAR
%%      

start: expr EOF { $1 }

decl: IDENT expr { Tree.Node("decl",[$2]) }

expr:  expr MINUS expr { Tree.Node("-",[$1;$3]) } 
|  expr PLUS expr { Tree.Node("+",[$1;$3]) } 
|  expr STAR expr { Tree.Node("*",[$1;$3]) } 
|  LPAREN expr RPAREN { $2 } 
|  IDENT { Tree.Node($1,[]) } 
|  LET decl IN expr END { $4 } 



