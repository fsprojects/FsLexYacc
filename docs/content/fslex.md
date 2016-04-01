Overview
========

The `fslex.exe` tool is a lexer generator for byte and Unicode character input.

Getting Started
---------------

Install the `FsLexYacc` nuget package.

MSBuild support
---------------

The nuget package includes MSBuild support for `FsLex` and `FsYacc`. You must add a `FsLexYacc.targets` reference
to your project file manually like this (adjust the nuget package number if needed):

    <Import Project="..\packages\FsLexYacc.6.0.1\bin\FsLexYacc.targets" />

You must also add `FsLex` and `FsYacc` entries like this:

    <FsYacc Include="..\LexAndYaccMiniProject\Parser.fsy">
      <OtherFlags>--module Parser</OtherFlags>
    </FsYacc>
    <FsLex Include="..\LexAndYaccMiniProject\Lexer.fsl">
      <OtherFlags>--unicode</OtherFlags>
    </FsLex>

Lexer syntax
------------

Define your lexer in the Lexer.fsl file.

    { header }
    let ident = regexp ...
    rule entrypoint [arg1... argn] =
      parse regexp { action }
          | ...
          | regexp { action }
    and entrypoint [arg1… argn] =
      parse ...
    and ...
    { trailer }

Comments are delimited by (* and *) and line comments // are also supported, as in F#. 

The rule and parse keywords are required.

The header and trailer sections are arbitrary F# code, which will write to the beginning and end of the output file (Lexer.fs). 
Either or both can be omitted. Headers typically include values and functions used in the rule body actions.

Following the header and before the rules are named regular expressions for use in the rules.

    let ident = regexp …

Following this declaration, the identifier ident can be used as shorthand for regexp.

Entry points
------------

Entry points are valid F# identifiers. Similarly, the arguments 

    arg1... argn 
	
must be valid identifiers. 
Each entry point becomes a function that takes n+1 arguments, the implicit last argument being of type LexBuffer<'a>.
Characters are read from the LexBuffer<'a> argument and matched against the regular expressions provided in the rule, until a prefix of the input matches one of the rules. 
The Lexer then evaluates the action and returns it as the result of the function. Rule entry points can be entered recursively.

If several regular expressions match a prefix of the input the regular expression that matches the longest prefix of the input is selected. 
In case of tie, the regular expression that occurs earlier in the rule is selected.

Rule regular expressions
------------------------

    ' regular-char | escape-sequence '

A character constant, with the same syntax as F# character constants. Match the denoted character.

    _

(underscore) Match any character.

    eof

Match the end of the lexer input.

Note: Fslex will not correctly handle regular expressions that contain eof followed by something else.

    "string"

A string constant, with the same syntax as F# string constants. Match the corresponding sequence of characters.

    [ character-set ]

Match any single character belonging to the given character set. Valid character sets are: single character constants ' c '; ranges of characters ' c1 ' - ' c2 ' (all characters between c1 and c2, inclusive); and the union of two or more character sets, denoted by concatenation.

    [ ^ character-set ]

Match any single character not belonging to the given character set.

    regexp1 # regexp2

(difference of character sets) Regular expressions regexp1 and regexp2 must be character sets defined with [… ] (or a a single character expression or underscore _). Match the difference of the two specified character sets.

    regexp *

(repetition) Match the concatenation of zero or more strings that match regexp.

    regexp +

(strict repetition) Match the concatenation of one or more strings that match regexp.

    regexp ?

(option) Match the empty string, or a string matching regexp.

    regexp1 | regexp2

(alternative) Match any string that matches regexp1 or regexp2

    regexp1 regexp2

(concatenation) Match the concatenation of two strings, the first matching regexp1, the second matching regexp2.

    ( regexp )

Match the same strings as regexp.

    ident

Reference the regular expression bound to ident by an earlier let ident =  regexp definition.

Concerning the precedences of operators, # has the highest precedence, followed by *, + and ?, then concatenation, then | (alternation).

Rule actions
------------

The actions are arbitrary F# expressions. Additionally, `lexbuf` is bound to the current lexer buffer.

Some typical uses for `lexbuf`, in conjunction with the operations on lexer buffers provided by the Microsoft.FSharp.Text.Lexing standard library module, are listed below.

    lexeme lexbuf

Return the matched string.

    lexbuf.LexemeChar n

Return the nth character in the matched string. The first character corresponds to n = 0.

    lexbuf.StartPos

Return the data on the absolute position in the input text of the beginning of the matched string (i.e. the offset of the first character of the matched string) in an object of type Position. The first character read from the input text has offset 0.

    lexbuf.EndPos

Return the data on absolute position in the input text of the end of the matched string (i.e. the offset of the first character after the matched string) in an object of type Position. The first character read from the input text has offset 0.

    entrypoint [exp1… expn] lexbuf

(Where entrypoint is the name of another entry point in the same lexer definition.) Recursively call the lexer on the given entry point. Notice that lexbuf is the last argument. Useful for lexing nested comments, for example.

The Position type
-----------------

	type Position = 
    { /// The file name for the position
      pos_fname: string
      /// The line number for the position
      pos_lnum: int
      /// The absolute offset of the beginning of the line
      pos_bol: int
      /// The absolute offset of the column for the position
      pos_cnum: int }
	 /// The file name associated with the input stream.
     member FileName : string
     /// The line number in the input stream, assuming fresh positions have been updated 
     /// using AsNewLinePos() and by modifying the EndPos property of the LexBuffer.
     member Line : int
     /// The character number in the input stream
     member AbsoluteOffset : int
     /// Return absolute offset of the start of the line marked by the position
     member StartOfLineAbsoluteOffset : int
     /// Return the column number marked by the position, i.e. the difference between the AbsoluteOffset and the StartOfLineAbsoluteOffset
     member Column : int
     // Given a position just beyond the end of a line, return a position at the start of the next line
     member NextLine : Position     
     /// Given a position at the start of a token of length n, return a position just beyond the end of the token
     member EndOfToken: n:int -> Position
     /// Gives a position shifted by specified number of characters
     member ShiftColumnBy: by:int -> Position

Sample input
------------

This is taken from the `Parsing` sample previously in the F# distribution. See below for information on `newline` and line counting.

    let digit = ['0'-'9']
    let whitespace = [' ' '\t' ]
    let newline = ('\n' | '\r' '\n')
    
    
    rule token = parse
    | whitespace     { token lexbuf }
    | newline        { newline lexbuf; token lexbuf }
    | "while"        { WHILE }
    | "begin"        { BEGIN }
    | "end"          { END }
    | "do"           { DO }
    | "if"           { IF }
    | "then"         { THEN }
    | "else"         { ELSE }
    | "print"        { PRINT }
    | "decr"         { DECR }
    | "("            { LPAREN }
    | ")"            { RPAREN }
    | ";"            { SEMI }
    | ":="           { ASSIGN }
    | ['a'-'z']+     { ID(lexeme lexbuf) }
    | ['-']?digit+   { INT (Int32.Parse(lexeme lexbuf)) }
    | ['-']?digit+('.'digit+)?(['e''E']digit+)?   { FLOAT (Double.Parse(lexeme lexbuf)) }
    | eof            { EOF }



More than one lexer state is permitted - use

    rule state1 =
     | "this"    { state2 lexbuf }
     | ...
    and state2 =
     | "that"    { state1 lexbuf }
     | ...


States can be passed arguments:

    rule state1 arg1 arg2 = ...
     | "this"    { state2 (arg1+1) (arg2+2) lexbuf }
     | ...
    and state2 arg1 arg2 = ...
     | ...



**Using a lexer**

If in the first example above the constructors `INT` etc generate values of type `tok` then the above generates a lexer with a function

    val token : LexBuffer<byte> -> tok

Once you have a lexbuffer you can call the above to generate new tokens. Typically you use some methods from `Microsoft.FSharp.Text.Lexing` 
to create lex buffers, either a `LexBuffer<byte>` for ASCII lexing, or `LexBuffer<char>` for Unicode lexing.

Some ways of creating lex buffers are by using:

    LexBuffer<_>.FromChars  
    LexBuffer<_>.FromFunction
    LexBuffer<_>.FromStream
    LexBuffer<_>.FromTextReader
    LexBuffer<_>.FromArray

Within lexing actions the variable `lexbuf` is in scope and you may use properties on the `LexBuffer` type such as:

    lexbuf.Lexeme  // get the lexeme as an array of characters or bytes
    LexBuffer.LexemeString lexbuf // get the lexeme as a string, for Unicode lexing

Lexing positions give locations in source files (the relevant type is `Microsoft.FSharp.Text.Lexing.Position`).

Generated lexers are nearly always used in conjunction with parsers generated by `FsYacc` (also documented on this site). See the Parsed Language starter template.

 Command line options

    fslex <filename>
        -o <string>: Name the output file.

        --codepage <int>: Assume input lexer specification file is encoded with the given codepage.

        --light: (ignored)

        --light-off: Add #light "off" to the top of the generated file

        --lexlib <string>: Specify the namespace for the implementation of the lexer table interperter (default Microsoft.FSharp.Text.Lexing)

        --unicode: Produce a lexer for use with 16-bit unicode characters.

        --help: display this list of options

        -help: display this list of options

Positions and line counting in lexers

Within a lexer lines can in theory be counted simply by incrementing a global variable or a passed line number count:

    rule token line = ...
     | "\n" | '\r' '\n'    { token (line+1) }
     | ...

However for character positions this is tedious, as it means every action becomes polluted with character counting, as you have to manually attach line numbers to tokens. Also, for error reporting writing service it is useful to have position information associated held as part of the state in the lexbuffer itself.

Thus F# follows the `OCamlLex` model where the lexer and parser state carry `position` values that record information for the current match (`lex`) and the `l.h.s`/`r.h.s` of the grammar productions (`yacc`).

The information carried for each position is:

 * a filename
 * a current 'absolute' character number
 * a placeholder for a user-tracked beginning-of-line marker
 * a placeholder for a user-tracked line number count.

Passing state through lexers
---------------------------

It is sometimes under-appreciated that you can pass arguments around between lexer states. For example, in one example we used imperative state to track a line number.

    let current_line = ref 0
    let current_char = ref 0
    let set_next_line lexbuf = ..
    
    ...
    rule main = parse
      | ...
      | "//" [^ '\n']* '\n' {
           set_next_line lexbuf; main lexbuf
        }


This sort of imperative code is better replaced by passing arguments:

    rule main line char = parse
      | ...
      | "//" [^ '\n']* '\n' {
           main (line+1) 0 lexbuf
        }

A good example is that when lexing a comment you want to pass through the start-of-comment position so that you can give a good error message if no end-of-comment is found. Or likewise you may want to pass through the number of nested of comments.
