// (c) Microsoft Corporation 2005-2009.

module FsLexYacc.FsLex.Driver

open FsLexYacc.FsLex
open FsLexYacc.FsLex.AST
open FsLexYacc.FsLex.Parser
open Printf
open FSharp.Text
open FSharp.Text.Lexing
open System
open System.Collections.Generic
open System.IO

//------------------------------------------------------------------
// This code is duplicated from Microsoft.FSharp.Compiler.UnicodeLexing

/// Standard utility to create a Unicode LexBuffer
///
/// One small annoyance is that LexBuffers and not IDisposable. This means
/// we can't just return the LexBuffer object, since the file it wraps wouldn't
/// get closed when we're finished with the LexBuffer. Hence we return the stream,
/// the reader and the LexBuffer. The caller should dispose the first two when done.
let UnicodeFileAsLexbuf (filename,codePage : int option) : FileStream * StreamReader * LexBuffer<char> =
    // Use the .NET functionality to auto-detect the unicode encoding
    // It also presents the bytes read to the lexer in UTF8 decoded form
    let stream  = new FileStream(filename,FileMode.Open,FileAccess.Read,FileShare.Read)
    let reader =
        match codePage with
        | None -> new  StreamReader(stream,true)
        | Some n -> new  StreamReader(stream,System.Text.Encoding.GetEncoding(n))
    let lexbuf = LexBuffer.FromFunction(reader.Read)
    lexbuf.EndPos <- Position.FirstLine(filename)
    stream, reader, lexbuf

//------------------------------------------------------------------
// This is the program proper

type Domain = Unicode | ASCII
type GeneratorState =
    { inputFileName: string
      outputFileName: string
      inputCodePage: System.Text.Encoding
      generatedModuleName: string option
      disableLightMode: bool option
      generateInternalModule: bool
      lexerLibraryName: string
      domain : Domain }

let mutable input = None
let mutable out = None
let mutable inputCodePage = None
let mutable light = None
let mutable modname = None
let mutable internal_module = false
let mutable lexlib = "FSharp.Text.Lexing"
let mutable unicode = false

let usage =
  [ ArgInfo ("-o", ArgType.String (fun s -> out <- Some s), "Name the output file.")
    ArgInfo ("--module", ArgType.String (fun s -> modname <- Some s), "Define the F# module name to host the generated parser.");
    ArgInfo ("--internal", ArgType.Unit (fun () -> internal_module <- true), "Generate an internal module");
    ArgInfo ("--codepage", ArgType.Int (fun i -> inputCodePage <- Some i), "Assume input lexer specification file is encoded with the given codepage.")
    ArgInfo ("--light", ArgType.Unit (fun () ->  light <- Some true), "(ignored)")
    ArgInfo ("--light-off", ArgType.Unit (fun () ->  light <- Some false), "Add #light \"off\" to the top of the generated file")
    ArgInfo ("--lexlib", ArgType.String (fun s ->  lexlib <- s), "Specify the namespace for the implementation of the lexer table interpreter (default FSharp.Text.Lexing)")
    ArgInfo ("--unicode", ArgType.Unit (fun () -> unicode <- true), "Produce a lexer for use with 16-bit unicode characters.")
    ArgInfo ("-i", ArgType.Unit (fun () -> caseInsensitive <- true), "Produce a case-insensitive lexer.")
  ]

let _ = ArgParser.Parse(usage, (fun x -> match input with Some _ -> failwith "more than one input given" | None -> input <- Some x), "fslex <filename>")

let outputInt (os: TextWriter) (n:int) = os.Write(string n)

let sentinel = 255 * 256 + 255

let readSpecFromFile fileName codePage =
  let stream,reader,lexbuf = UnicodeFileAsLexbuf(fileName, codePage)
  use stream = stream
  use reader = reader
  try
      let spec = Parser.spec FsLexYacc.FsLex.Lexer.token lexbuf
      Ok spec
  with e ->
      (e, lexbuf.StartPos.Line, lexbuf.StartPos.Column)
      |> Error

type PerRuleData = list<DfaNode * seq<Code>>
type DfaNodes = list<DfaNode>

type Writer(fileName) =
    let os = System.IO.File.CreateText fileName
    let mutable lineCount = 0
    let incr () =
        lineCount <- lineCount + 1

    member x.writeLine fmt =
        Printf.kfprintf (fun () -> incr(); os.WriteLine()) os fmt

    member x.write fmt =
        Printf.fprintf os fmt

    member x.writeCode (code, pos: Position) =
        if pos <> Position.Empty  // If bottom code is unspecified, then position is empty.
        then
            x.writeLine "# %d \"%s\"" pos.Line pos.FileName
            x.writeLine "%s" code
            let numLines = code.Replace("\r","").Split([| '\n' |]).Length
            lineCount  <- lineCount + numLines
            x.writeLine "# %d \"%s\"" lineCount fileName

    member x.LineCount = lineCount

    member x.WriteUint16 (n: int) =
        os.Write n;
        os.Write "us;"

    interface IDisposable with
        member x.Dispose() = os.Dispose()

let writeLightMode lightModeDisabled (fileName: string) (writer: Writer) =
    if lightModeDisabled = Some false || (lightModeDisabled = None && (Path.HasExtension(fileName) && Path.GetExtension(fileName) = ".ml"))
    then
        writer.write "#light \"off\""

let writeModuleExpression genModuleName isInternal (writer: Writer) =
    match genModuleName with
    | None -> ()
    | Some s ->
        let internal_tag = if isInternal then "internal " else ""
        writer.writeLine "module %s%s" internal_tag s

let writeTopCode (code) (writer: Writer) = writer.writeCode code

let writeUnicodeTranslationArray dfaNodes domain (writer: Writer) =
    writer.writeLine "let trans : uint16[] array = "
    writer.writeLine "    [| "
    match domain with
    | Unicode ->
        let specificUnicodeChars = GetSpecificUnicodeChars()
        // This emits a (numLowUnicodeChars+NumUnicodeCategories+(2*#specificUnicodeChars)+1) * #states array of encoded UInt16 values

        // Each row for the Unicode table has format
        //      128 entries for ASCII characters
        //      A variable number of 2*UInt16 entries for SpecificUnicodeChars
        //      30 entries, one for each UnicodeCategory
        //      1 entry for EOF
        //
        // Each entry is an encoded UInt16 value indicating the next state to transition to for this input.
        //
        // For the SpecificUnicodeChars the entries are char/next-state pairs.
        for state in dfaNodes do
            writer.writeLine "    (* State %d *)" state.Id
            writer.write  "     [| "
            let trans =
                let dict = new Dictionary<_,_>()
                state.Transitions |> List.iter dict.Add
                dict
            let emit n =
                if trans.ContainsKey(n) then
                  writer.WriteUint16 trans.[n].Id
                else
                  writer.WriteUint16 sentinel
            for i = 0 to numLowUnicodeChars-1 do
                let c = char i
                emit (EncodeChar c)
            for c in specificUnicodeChars do
                writer.WriteUint16 (int c)
                emit (EncodeChar c)
            for i = 0 to NumUnicodeCategories-1 do
                emit (EncodeUnicodeCategoryIndex i)
            emit Eof
            writer.writeLine  "|];"
        done

    | ASCII ->
        // Each row for the ASCII table has format
        //      256 entries for ASCII characters
        //      1 entry for EOF
        //
        // Each entry is an encoded UInt16 value indicating the next state to transition to for this input.

        // This emits a (256+1) * #states array of encoded UInt16 values
        for state in dfaNodes do
            writer.writeLine "   (* State %d *)" state.Id
            writer.write " [|"
            let trans =
                let dict = new Dictionary<_,_>()
                state.Transitions |> List.iter dict.Add
                dict
            let emit n =
                if trans.ContainsKey(n) then
                  writer.WriteUint16 trans.[n].Id
                else
                  writer.WriteUint16 sentinel
            for i = 0 to 255 do
                let c = char i
                emit (EncodeChar c)
            emit Eof
            writer.writeLine "|];"
        done

    writer.writeLine "    |] "

let writeUnicodeActionsArray dfaNodes (writer: Writer) =
    writer.write "let actions : uint16[] = [|"
    for state in dfaNodes do
        if state.Accepted.Length > 0 then
          writer.WriteUint16 (snd state.Accepted.Head)
        else
          writer.WriteUint16 sentinel
    done
    writer.writeLine  "|]"

let writeUnicodeTables lexerLibraryName domain dfaNodes (writer: Writer) =
    writeUnicodeTranslationArray dfaNodes domain writer
    writeUnicodeActionsArray dfaNodes writer
    writer.writeLine  "let _fslex_tables = %s.%sTables.Create(trans,actions)" lexerLibraryName (match domain with | Unicode -> "Unicode" | ASCII -> "ASCII")

let writeRules (rules: Rule list) (perRuleData: PerRuleData) outputFileName (writer: Writer) =
    writer.writeLine  "let rec _fslex_dummy () = _fslex_dummy() "

    // These actions push the additional start state and come first, because they are then typically inlined into later
    // rules. This means more tailcalls are taken as direct branches, increasing efficiency and
    // improving stack usage on platforms that do not take tailcalls.
    for ((startNode, actions),(ident,args,_)) in List.zip perRuleData rules do
        writer.writeLine "// Rule %s" ident
        writer.writeLine "and %s %s lexbuf =" ident (String.Join(" ", Array.ofList args))
        writer.writeLine "  match _fslex_tables.Interpret(%d,lexbuf) with" startNode.Id
        actions |> Seq.iteri (fun i (code:string, pos) ->
            writer.writeLine "  | %d -> ( " i
            writer.writeLine "# %d \"%s\"" pos.Line pos.FileName
            let lines = code.Split([| '\r'; '\n' |], StringSplitOptions.RemoveEmptyEntries)
            for line in lines do
                writer.writeLine "               %s" line
            writer.writeLine "# %d \"%s\"" writer.LineCount outputFileName
            writer.writeLine "          )")
        writer.writeLine "  | _ -> failwith \"%s\"" ident

    writer.writeLine ""

let writeBottomCode code (writer: Writer) = writer.writeCode code

let writeFooter outputFileName (writer: Writer) = writer.writeLine "# 3000000 \"%s\"" outputFileName

let writeSpecToFile (state: GeneratorState) (spec: Spec) (perRuleData: PerRuleData) (dfaNodes: DfaNodes) =
    use writer = new Writer(state.outputFileName)
    writeLightMode state.disableLightMode state.outputFileName writer
    writeModuleExpression state.generatedModuleName state.generateInternalModule writer
    writeTopCode spec.TopCode writer
    writeUnicodeTables state.lexerLibraryName state.domain dfaNodes writer
    writeRules spec.Rules perRuleData state.outputFileName writer
    writeBottomCode spec.BottomCode writer
    writeFooter state.outputFileName writer
    ()

let compileSpec (spec: Spec) =
    let perRuleData, dfaNodes = AST.Compile spec
    let dfaNodes = dfaNodes |> List.sortBy (fun n -> n.Id)
    perRuleData, dfaNodes

let main() =
  try
    let filename = (match input with Some x -> x | None -> failwith "no input given")
    let spec =
        match readSpecFromFile filename inputCodePage with
        | Ok spec -> spec
        | Error (e, line, column) ->
            eprintf "%s(%d,%d): error: %s" filename line column
              (match e with
               | Failure s -> s
               | _ -> e.Message)
            exit 1

    printfn "compiling to dfas (can take a while...)"
    let perRuleData, dfaNodes = compileSpec spec
    printfn "%d states" dfaNodes.Length

    printfn "writing output"

    let output =
        match out with
        | Some x -> x
        | _ -> Path.ChangeExtension(filename, ".fs")

    let state : GeneratorState =
        { inputFileName = filename
          outputFileName = output
          inputCodePage = inputCodePage |> Option.map System.Text.Encoding.GetEncoding |> Option.defaultValue System.Text.Encoding.UTF8
          generatedModuleName = modname
          disableLightMode = light
          generateInternalModule = internal_module
          lexerLibraryName = lexlib
          domain = if unicode then Unicode else ASCII }
    writeSpecToFile state spec perRuleData dfaNodes

  with e ->
    eprintf "FSLEX: error FSL000: %s" (match e with Failure s -> s | e -> e.ToString())
    exit 1


let result = main()
