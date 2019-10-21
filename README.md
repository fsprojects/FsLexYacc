FsLexYacc
=======================

FsLex and FsYacc tools, originally part of the "F# PowerPack"

See https://fsprojects.github.io/FsLexYacc.

Build the project
-----------------

[![Build Status](https://sergeytihon.visualstudio.com/sergeytihon/_apis/build/status/fsprojects.FsLexYacc?branchName=master)](https://sergeytihon.visualstudio.com/sergeytihon/_build/latest?definitionId=4&branchName=master)

* Unix: Run *build.sh*
* Windows: Run *build.cmd*

* [![NuGet Badge](https://buildstats.info/nuget/FsLexYacc.Runtime)](https://www.nuget.org/packages/FsLexYacc.Runtime) - FsLexYacc.Runtime
* [![NuGet Badge](https://buildstats.info/nuget/FsLexYacc)](https://www.nuget.org/packages/FsLexYacc) - FsLexYacc


### Generating docs

This is currently done manually:

    fsi docs\generate.fsx

Site can be tested locally using local dev server

    dotnet serve -d docs/output --path-base /FsLexYacc

### Releasing

    .\build.cmd --target NuGet
    ./build.sh --target NuGet

    set APIKEY=...
    ..\FSharp.TypeProviders.SDK\.nuget\nuget.exe push bin\FsLexYacc.Runtime.9.0.3.nupkg %APIKEY% -Source https://nuget.org
    ..\FSharp.TypeProviders.SDK\.nuget\nuget.exe push bin\FsLexYacc.9.0.3.nupkg %APIKEY% -Source https://nuget.org

### Maintainer(s)

- [@kkm000](https://github.com/kkm000)
- [@dsyme](https://github.com/dsyme)

The default maintainer account for projects under "fsprojects" is [@fsprojectsgit](https://github.com/fsprojectsgit) - F# Community Project Incubation Space (repo management)

