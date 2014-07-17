module Microsoft.FSharp.Build.Logging

open System
open System.IO
open System.Text.RegularExpressions
open Microsoft.Build.Utilities

// Error formats used in FsLex and FsYacc

let (|FsLexYaccError|_|) =
  // eprintf "%s(%d,%d): error: %s"
  let fsLexYaccErrorPattern = Regex(@"^(?<ORIGIN>.*)\((?<LINE>\d+),(?<COLUMN>\d+)\)\: error\: (?<MESSAGE>.*)$", RegexOptions.Compiled)
  (fun s ->
    let x = fsLexYaccErrorPattern.Match(s)
    match x.Success with
    | true ->
        let origin  = x.Groups.["ORIGIN"].Value.Trim()
        let line    = x.Groups.["LINE"].Value.Trim() |> Int32.Parse
        let column  = x.Groups.["COLUMN"].Value.Trim() |> Int32.Parse
        let message = x.Groups.["MESSAGE"].Value.Trim()
        Some(origin, line, column, message)
    | _ -> None)

let (|FsLexCrash|_|) =
  // eprintf "FSLEX: error FSL000: %s"
  let fsLexCrashPattern  = Regex(@"^FSLEX: error FSL000: (?<MESSAGE>.*)$", RegexOptions.Compiled)
  (fun s ->
    let x = fsLexCrashPattern.Match(s)
    match x.Success with
    | true -> Some(x.Groups.["MESSAGE"].Value.Trim())
    | _ -> None)

let (|FsYaccCrash|_|) =
  // eprintf "FSYACC: error FSY000: %s"
  let fsYaccCrashPattern = Regex(@"^FSYACC: error FSY000: (?<MESSAGE>.*)$", RegexOptions.Compiled)
  (fun s ->
    let x = fsYaccCrashPattern.Match(s)
    match x.Success with
    | true -> Some(x.Groups.["MESSAGE"].Value.Trim())
    | _ -> None)

let logFsLexYaccOutput s (log:TaskLoggingHelper) =
    match s with
    | FsLexYaccError (origin, line, column, message) ->
        let filePath =
            try FileInfo(origin).FullName
            with |_-> origin
        log.LogError(null, null, null, filePath, line, column, line, column, message, [||]); true
    | FsLexCrash message ->
        log.LogError("FSLEX", "FSL000", null, null, 0, 0, 0, 0, message, [||]); true
    | FsYaccCrash message ->
        log.LogError("FSYACC", "FSY000", null, null, 0, 0, 0, 0, message, [||]); true
    | _ -> false