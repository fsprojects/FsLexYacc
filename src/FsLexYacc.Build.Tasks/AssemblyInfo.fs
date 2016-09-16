﻿namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("FsLexYacc.Build.Tasks")>]
[<assembly: AssemblyProductAttribute("FsLexYacc")>]
[<assembly: AssemblyDescriptionAttribute("FsLex/FsYacc lexer/parser generation tools")>]
[<assembly: AssemblyVersionAttribute("6.1.0")>]
[<assembly: AssemblyFileVersionAttribute("6.1.0")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "6.1.0"
    let [<Literal>] InformationalVersion = "6.1.0"
