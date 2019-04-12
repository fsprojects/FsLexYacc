FsLexYacc
=======================

FsLex and FsYacc tools, originally part of the "F# PowerPack"

See https://fsprojects.github.io/FsLexYacc.

Build the project
-----------------

* Unix: Run *build.sh*  [![Travis build status](https://travis-ci.org/fsprojects/FsLexYacc.svg)](https://travis-ci.org/fsprojects/FsLexYacc)
* Windows: Run *build.cmd* [![AppVeyor build status](https://ci.appveyor.com/api/projects/status/061nqkynrysnyiv7)](https://ci.appveyor.com/project/fsgit/fslexyacc)

* [![NuGet Badge](https://buildstats.info/nuget/FsLexYacc.Runtime)](https://www.nuget.org/packages/FsLexYacc.Runtime) - `FsLexYacc.Runtime`
* [![NuGet Badge](https://buildstats.info/nuget/FsLexYacc)](https://www.nuget.org/packages/FsLexYacc) - `FsLexYacc`


### Generating docs

This is currently done manually:

    fsi docsrc\generate.fsx
    
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

