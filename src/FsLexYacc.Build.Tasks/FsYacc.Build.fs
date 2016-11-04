// (c) Microsoft Corporation 2005-2009.

namespace Microsoft.FSharp.Build

open System
open Microsoft.Build.Framework
open Microsoft.Build.Utilities

(**************************************
MSBuild task for fsyacc
**************************************)

type FsYacc() = 
    inherit ToolTask()

    let mutable inputFile  : string = null
    let mutable outputFile : string = null
    let mutable verboseFile: string = null
    
    let mutable codePage   : string = null
    let mutable otherFlags : string = null
    let mutable mlCompat   = false
    
    let mutable _open  : string = null
    let mutable _module  : string = null

    let mutable toolPath = System.IO.Path.GetDirectoryName(typeof<FsYacc>.Assembly.Location)
    let mutable toolExe = "fsyacc.exe"

    // [<Required>]
    member this.InputFile
        with get ()  = inputFile
        and  set (x) = inputFile <- x
    
    [<Microsoft.Build.Framework.Output>]
    member this.OutputFile
        with get ()  = outputFile
        and  set (x) = outputFile <- x

    member this.VerboseFile
        with get ()  = verboseFile
        and  set (x) = verboseFile <- x
    
    member this.OtherFlags
        with get() = otherFlags
        and set(s) = otherFlags <- s

    // --codepage <int>: Assume input lexer specification file is encoded with the given codepage.
    member this.CodePage
        with get ()  = codePage
        and  set (x) = codePage <- x
    
    // --ml-compatibility: Support the use of the global state from the 'Parsing' module in MLLib.
    member this.MLCompatibility
        with get ()  = mlCompat
        and  set (x) = mlCompat <- x
        
    // --open
    member this.Open
        with get ()  = _open
        and  set (x) = _open <- x       

   // --module
    member this.Module
        with get ()  = _module
        and  set (x) = _module <- x             

    // For targeting other versions of fslex.exe, such as "\LKG\" or "\Prototype\"
    member this.ToolPath
        with get ()  = toolPath
        and  set (s) = toolPath <- s
        
    // ToolTask methods
    override this.ToolName = "fsyacc.exe"
    
    override this.GenerateFullPathToTool() = 
        System.IO.Path.Combine(toolPath, this.ToolExe)
        
    override this.GenerateCommandLineCommands() =
    
        let builder = new CommandLineBuilder()
        
        // CodePage
        builder.AppendSwitchIfNotNull("--codepage ", codePage)
        
        // ML Compatibility
        if mlCompat then builder.AppendSwitch("--ml-compatibility")

        // Open
        builder.AppendSwitchIfNotNull("--open ", _open)

        // Module
        builder.AppendSwitchIfNotNull("--module ", _module)

        // OutputFile
        builder.AppendSwitchIfNotNull("-o ", outputFile)

        // OtherFlags - must be second-to-last
        builder.AppendSwitchUnquotedIfNotNull("", otherFlags)

        builder.AppendSwitchIfNotNull(" ", inputFile)
        
        let args = builder.ToString()

        // when doing simple unit tests using API, no BuildEnginer/Logger is attached
        if this.BuildEngine <> null then
            let eventArgs = { new CustomBuildEventArgs(message=args,helpKeyword="",senderName="") with member x.Equals(y) = false }
            this.BuildEngine.LogCustomEvent(eventArgs)
        
        args
    
    // Expose this to internal components (for unit testing)
    member internal this.InternalGenerateCommandLineCommands() =
        this.GenerateCommandLineCommands()

    // Log errors and warnings
    override this.LogEventsFromTextOutput(singleLine, _) =
        if not <| Logging.logFsLexYaccOutput singleLine this.Log
        then base.LogEventsFromTextOutput(singleLine, MessageImportance.High)

    override this.Execute () =
        let result =  base.Execute()
        if IO.File.Exists verboseFile then
            let message = sprintf "Log from verbose output file '%s':" verboseFile
            this.Log.LogMessageFromText("------------------------", MessageImportance.High) |> ignore
            this.Log.LogMessageFromText(message, MessageImportance.High) |> ignore
            for lineOfText in IO.File.ReadAllLines verboseFile do
                this.Log.LogMessageFromText(lineOfText, MessageImportance.High) |> ignore
            this.DeleteTempFile(verboseFile)
        result
