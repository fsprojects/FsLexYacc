%{
//module TestParser

%} 

%type <Tree.tree> start
%token MINUS STAR LPAREN RPAREN PLUS EOF LET IN END UNICODE1 UNICODE2
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
|  UNICODE1 {  Tree.Node("UNICODE1",[])} 
|  UNICODE2 {  Tree.Node("UNICODE2",[])} 



