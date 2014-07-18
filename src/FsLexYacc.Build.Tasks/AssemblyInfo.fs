namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("FsLexYacc.Build.Tasks")>]
[<assembly: AssemblyProductAttribute("FsLexYacc.Build.Tasks")>]
[<assembly: AssemblyDescriptionAttribute("FsLex/FsYacc lexer/parser generation tools")>]
[<assembly: AssemblyVersionAttribute("6.0.3")>]
[<assembly: AssemblyFileVersionAttribute("6.0.3")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "6.0.3"
