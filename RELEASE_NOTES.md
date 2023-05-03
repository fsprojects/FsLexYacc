#### 11.1.0 - 3 May, 2022
* Add `--buffer-type-argument` option for fsyacc.

#### 11.0.1 - 10 January, 2022
* Resolve FSharp.Core dependency restriction #168

#### 11.0.0 - 10 January, 2022
* Migration to net6.0 #166
* Fix Activating case insensitive option crash the lexer generator #141
* Reuse produced reductions table #141

#### 11.0.0-beta1 - 11 July, 2021
* Break out core domain logic and generation into core libraries #144
* Update FsLexYacc.targets #149
* Avoid copying a string twice in LexBuffer.FromString. #150
* Fix misc packaging issues #145

#### 10.2.0 - 22 November, 2020
* Enable running tools under .net 5.0

#### 10.1.0 - 04 October, 2020
* Add caseInsensitive option
* Migration to netcoreapp3.1

#### 10.0.0 - 24 October, 2019
* Migration to netcoreapp3.0 based versions of FxLex and FsYacc

#### 9.1.0 - 22 October, 2019
* Make async lexing obsolete
* Restart doc generation (manually)

#### 9.0.3 - 12 April, 2019
* Don't require FSharp.Core for tools package
* Bootstrap using new package

#### 9.0.2 - 12 April, 2019
* Bootstrap using new package

#### 9.0.1 - 12 April, 2019
* Tools now run on .NET Core

#### 8.0.1 - 21 March, 2019
* Fix recursion problem 
* Support netstandard2.0
* Build with dotnet toolchain
* Cleanup runtime code

#### 7.0.6 - 23 June, 2017
* Add source to build

#### 7.0.5 - February 1, 2017
* Fix an error preventing the use of verbose mode

#### 7.0.4 - January 22, 2017
* Fix targets file for OSX

#### 7.0.3 - November 29, 2016
* Fix targets file when space in path

#### 7.0.2 - November 5, 2016
* Improve output

#### 7.0.1 - November 5, 2016
* Fix targets file
* Remove <Open> and <Module> and just have the user pass them in via <OtherFlags>

#### 7.0.0 - November 5, 2016
* Use only profile 259, move to Paket, remove LKG
* Remove the use of a task DLL

#### 6.1.0 - March 20, 2015
* Adding the package to solution automatically configures targets
* Build system upgraded to MSBuild 4.0
* Fixed Mono/Linux compilation
* New example with a walkthrough

#### 6.0.4 - September 15, 2014
* Add profiles 7, 259 to runtime

#### 6.0.3 - June 18 2014
* FsLex/FsYacc output redirected to VS Output window
* FsYacc verbose output added to MSBuild log (and VS Output window)

#### 6.0.2 - June 16 2014
* Logo was added
* FsLexYacc.Runtime published as a separate NuGet package

#### 6.0.0 - April 18 2014
* First release of the new packaging of fslex/fsyacc
