FsLexYacc
=======================

FsLex and FsYacc tools, originally part of the "F# PowerPack"

See https://fsprojects.github.io/FsLexYacc.

* FsLexYacc.Runtime - [![NuGet Badge](https://buildstats.info/nuget/FsLexYacc.Runtime)](https://www.nuget.org/packages/FsLexYacc.Runtime)
* FsLexYacc - [![NuGet Badge](https://buildstats.info/nuget/FsLexYacc)](https://www.nuget.org/packages/FsLexYacc)

Build the project
-----------------

[![Build Status](https://github.com/fsprojects/FsLexYacc/workflows/Build%20and%20test/badge.svg?branch=master)](https://github.com/fsprojects/FsLexYacc/actions?query=branch%3Amaster)

* Unix: Run *build.sh*
* Windows: Run *build.cmd*

### Generating docs

This is currently done manually:

    dotnet fsi docs\generate.fsx // on Windows

Site can be tested locally using local dev server

    dotnet serve -d docs/output --path-base /FsLexYacc

### Releasing

    .\build.cmd --target NuGet
    ./build.sh --target NuGet

    set APIKEY=...
    ..\FSharp.TypeProviders.SDK\.nuget\nuget.exe push bin\FsLexYacc.Runtime.9.0.3.nupkg %APIKEY% -Source https://nuget.org
    ..\FSharp.TypeProviders.SDK\.nuget\nuget.exe push bin\FsLexYacc.9.0.3.nupkg %APIKEY% -Source https://nuget.org
    ..\FSharp.TypeProviders.SDK\.nuget\nuget.exe push bin\FsLex.Core.9.0.3.nupkg %APIKEY% -Source https://nuget.org
    ..\FSharp.TypeProviders.SDK\.nuget\nuget.exe push bin\FsYacc.Core.9.0.3.nupkg %APIKEY% -Source https://nuget.org

### Maintainer(s)

- [@kkm000](https://github.com/kkm000)
- [@dsyme](https://github.com/dsyme)

The default maintainer account for projects under "fsprojects" is [@fsprojectsgit](https://github.com/fsprojectsgit) - F# Community Project Incubation Space (repo management)

