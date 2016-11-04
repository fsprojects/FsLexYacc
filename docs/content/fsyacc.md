Overview
========

`fsyacc.exe` is a `LALR` parser generator. It follows a similar specification to the `OCamlYacc` parser generator (especially when used with the `ml compatibility` switch)

Getting Started
---------------

Install the `FsLexYacc` nuget package.

Sample input
------------

Parser generators typically produce numbers represented by values in an F# Union Type. For example:

    type Expr = 
     | Val of string 
     | Int of int
     | Float of float
     | Decr of Sxpr
    
    
    type Stmt = 
     | Assign of string * Sxpr
     | While of Expr * Stmt
     | Seq of Stmt list
     | IfThen of Expr * Stmt
     | IfThenElse of Expr * Stmt * Stmt
     | Print of Expr
    
    
    type Prog = Prog of Stmt list

Given that, a typical parser specification is as follows:

    %{
    open Ast
    %}
    
    %start start
    %token <string> ID
    %token <System.Int32> INT
    %token <System.Double> FLOAT
    %token DECR LPAREN RPAREN WHILE DO END BEGIN IF THEN ELSE PRINT SEMI ASSIGN EOF
    %type < Ast.Prog > start
    
    
    %%
    
    
    start: Prog {  $1 }
    
    
    Prog: StmtList { Prog(List.rev($1)) }
    
    
    Expr: ID { Val($1) }
        | INT {  Int($1)  }
        | FLOAT {  Float($1)  }
        | DECR LPAREN Expr RPAREN {  Decr($3)  }
    
    
    Stmt: ID ASSIGN Expr { Assign($1,$3) }
        | WHILE Expr DO Stmt { While($2,$4) }
        | BEGIN StmtList END { Seq(List.rev($2)) }
        | IF Expr THEN Stmt { IfThen($2,$4) }
        | IF Expr THEN Stmt ELSE Stmt { IfThenElse($2,$4,$6) }
        | PRINT Expr { Print($2) }
    
    
    StmtList: Stmt { [$1] }
           | StmtList SEMI Stmt { $3 :: $1  }

The above generates a datatype for tokens and a function for each `start` production. Parsers are typically combined with a lexer generated using `FsLex`.

MSBuild support
---------------

The nuget package includes MSBuild support for `FsLex` and `FsYacc`. You must add a `FsLexYacc.targets` reference
to your project file manually like this (adjust the nuget package number if needed):

    <Import Project="..\packages\FsLexYacc.6.0.3\bin\FsLexYacc.targets" />

You must also add `FsLex` andd `FsYacc` entries like this:

    <FsYacc Include="..\LexAndYaccMiniProject\Parser.fsy">
      <OtherFlags>--module Parser</OtherFlags>
    </FsYacc>
    <FsLex Include="..\LexAndYaccMiniProject\Lexer.fsl">
      <OtherFlags>--unicode</OtherFlags>
    </FsLex>
    
If you want to see `verbose` output from `FsYacc` you need to add `-v` in `OtherFlags` section like this:

    <FsYacc Include="..\LexAndYaccMiniProject\Parser.fsy">
      <OtherFlags>--module Parser -v</OtherFlags>
    </FsYacc>

Command line options
--------------------

    fsyacc <filename> fsyacc <filename>

        -o <string>: Name the output file.

        -v: Produce a listing file.

        --module <string>: Define the F# module name to host the generated parser.

        --internal: Generate an internal module

        --open <string>: Add the given module to the list of those to open in both the generated signature and implementation.

        --light: (ignored)

        --light-off: Add #light "off" to the top of the generated file

        --ml-compatibility: Support the use of the global state from the 'Parsing' module in FSharp.PowerPack.dll.

        --tokens: Simply tokenize the specification file itself.

        --lexlib <string>: Specify the namespace for the implementation of the parser table interperter (default Microsoft.FSharp.Text.Parsing)

        --parslib <string>: Specify the namespace for the implementation of the parser table interperter (default Microsoft.FSharp.Text.Parsing)

        --codepage <int>: Assume input lexer specification file is encoded with the given codepage.

        --help: display this list of options

        -help: display this list of options

Managing and using position markers
-----------------------------------

Each action in an fsyacc parser has access to a parseState value through which you can access position information.

    type IParseState =
        abstract InputStartPosition: int -> Position
        abstract InputEndPosition: int -> Position
        abstract InputRange: int -> Position * Position
        abstract ParserLocalStore: IDictionary<string,obj>
        abstract ResultRange  : Position * Position
        abstract RaiseError<'b> : unit -> 'b

`Input` relate to the indexes of the items on the right hand side of the current production, the `Result` relates to the entire range covered by the production. You shouldn't use `GetData` directly - these is called automatically by `$1`, `$2` etc. You can call `RaiseError` if you like.

You must set the initial position when you create the lexbuf:

    let setInitialPos (lexbuf:lexbuf) filename =
         lexbuf.EndPos <- { pos_bol = 0;
                            pos_fname=filename;
                            pos_cnum=0;
                            pos_lnum=1 }


You must also update the position recorded in the lex buffer each time you process what you consider to be a new line:

    let newline (lexbuf:lexbuf) =
        lexbuf.EndPos <- lexbuf.EndPos.AsNewLinePos()


Likewise if your language includes the ability to mark source code locations, see custom essay (e.g. the `#line` directive in OCaml and F#) then you must similarly adjust the `lexbuf.EndPos` according to the information you grok from your input.

Notes on OCaml Compatibility
----------------------------

`OCamlYacc` accepts the following:

    %type < context -> context > toplevel

For `FsYacc` you just add parentheses:

    %type < (context -> context)) > toplevel
