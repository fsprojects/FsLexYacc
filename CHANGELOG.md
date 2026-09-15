# Changelog

## [Unreleased]

### Changed
* Lower the required `FSharp.Core` from `10.1.400` to `10.0.100`. [#253](https://github.com/fsprojects/FsLexYacc/pull/253)

### Fixed
* `FsLex.Core` and `FsYacc.Core` declare their dependency on `FsLexYacc.Runtime`, which was missing from both packages. [#253](https://github.com/fsprojects/FsLexYacc/pull/253)

## [12.1.2] - 2026-09-15

### Changed
* Packages are pushed to NuGet with trusted publishing, so the release workflow no longer needs a long-lived API key. [#251](https://github.com/fsprojects/FsLexYacc/issues/251)

## [12.1.1] - 2026-09-15

### Changed
* Versions and package release notes come from `CHANGELOG.md` through Ionide.KeepAChangelog.Tasks, and every release gets a matching GitHub release. [#250](https://github.com/fsprojects/FsLexYacc/issues/250)

### Fixed
* `CallFsLex` and `CallFsYacc` regenerate their output when only the `.fsi` is missing, and both tools' outputs are registered for `dotnet clean`. [#231](https://github.com/fsprojects/FsLexYacc/issues/231)

## [12.1.0] - 2026-09-15

### Changed
* fsyacc no longer prints every shift/reduce and reduce/reduce conflict to stdout. It prints the counts and writes the detail of each conflict to the `-v` listing file. [#241](https://github.com/fsprojects/FsLexYacc/issues/241)
* Register `FsLex` and `FsYacc` items as `UpToDateCheckInput` so IDE fast up-to-date checks rebuild after grammar edits. [#242](https://github.com/fsprojects/FsLexYacc/issues/242)

## [12.0.0] - 2026-09-02

### Changed
* Migrate fslex and fsyacc to net10.0. The tools now require a .NET 10 runtime. [#244](https://github.com/fsprojects/FsLexYacc/issues/244)
* Raise the minimum FSharp.Core version to 10.0.0. [#244](https://github.com/fsprojects/FsLexYacc/issues/244)

## [11.4.1] - 2026-08-28

### Fixed
* fslex generating an invalid signature file when the header defines a module. [#240](https://github.com/fsprojects/FsLexYacc/issues/240)

## [11.4.0] - 2026-07-06

### Added
* Fable support in FsLexYacc.Runtime.
* `--assoc-cache-capacity` option for fsyacc to set the generated parser's AssocTable cache capacity from the command line. [#54](https://github.com/fsprojects/FsLexYacc/issues/54)

### Changed
* The AssocTable lookup cache initial capacity is configurable, to avoid pre-allocating 2000 entries per parse. [#54](https://github.com/fsprojects/FsLexYacc/issues/54)

## [11.2.0] - 2023-05-12

### Added
* `--open` option for fslex.
* Signature files for transformed files in fslex.

## [11.1.0] - 2023-05-03

### Added
* `--buffer-type-argument` option for fsyacc.

## [11.0.1] - 2022-01-10

### Fixed
* Resolve FSharp.Core dependency restriction. [#168](https://github.com/fsprojects/FsLexYacc/issues/168)

## [11.0.0] - 2022-01-10

### Changed
* Migration to net6.0. [#166](https://github.com/fsprojects/FsLexYacc/issues/166)
* Reuse produced reductions table. [#141](https://github.com/fsprojects/FsLexYacc/issues/141)

### Fixed
* Activating the case insensitive option crashed the lexer generator. [#141](https://github.com/fsprojects/FsLexYacc/issues/141)

## [11.0.0-beta1] - 2021-07-11

### Changed
* Break out core domain logic and generation into core libraries. [#144](https://github.com/fsprojects/FsLexYacc/issues/144)
* Update FsLexYacc.targets. [#149](https://github.com/fsprojects/FsLexYacc/issues/149)
* Avoid copying a string twice in LexBuffer.FromString. [#150](https://github.com/fsprojects/FsLexYacc/issues/150)

### Fixed
* Misc packaging issues. [#145](https://github.com/fsprojects/FsLexYacc/issues/145)

## [10.2.0] - 2020-11-22

### Added
* Enable running tools under .NET 5.0.

## [10.1.0] - 2020-10-04

### Added
* caseInsensitive option.

### Changed
* Migration to netcoreapp3.1.

## [10.0.0] - 2019-10-24

### Changed
* Migration to netcoreapp3.0 based versions of FsLex and FsYacc.

## [9.1.0] - 2019-10-22

### Changed
* Make async lexing obsolete.
* Restart doc generation (manually).

## [9.0.3] - 2019-04-12

### Changed
* Don't require FSharp.Core for tools package.
* Bootstrap using new package.

## [9.0.2] - 2019-04-12

### Changed
* Bootstrap using new package.

## [9.0.1] - 2019-04-12

### Changed
* Tools now run on .NET Core.

## [8.0.1] - 2019-03-21

### Added
* Support netstandard2.0.

### Changed
* Build with dotnet toolchain.
* Cleanup runtime code.

### Fixed
* Recursion problem.

## [7.0.6] - 2017-06-23

### Added
* Source to build.

## [7.0.5] - 2017-02-01

### Fixed
* An error preventing the use of verbose mode.

## [7.0.4] - 2017-01-22

### Fixed
* Targets file for OSX.

## [7.0.3] - 2016-11-29

### Fixed
* Targets file when space in path.

## [7.0.2] - 2016-11-05

### Changed
* Improve output.

## [7.0.1] - 2016-11-05

### Changed
* Remove `<Open>` and `<Module>` and just have the user pass them in via `<OtherFlags>`.

### Fixed
* Targets file.

## [7.0.0] - 2016-11-05

### Changed
* Use only profile 259, move to Paket, remove LKG.
* Remove the use of a task DLL.

## [6.1.0] - 2015-03-20

### Added
* Adding the package to solution automatically configures targets.
* New example with a walkthrough.

### Changed
* Build system upgraded to MSBuild 4.0.

### Fixed
* Mono/Linux compilation.

## [6.0.4] - 2014-09-15

### Added
* Profiles 7, 259 to runtime.

## [6.0.3] - 2014-06-18

### Changed
* FsLex/FsYacc output redirected to VS Output window.
* FsYacc verbose output added to MSBuild log (and VS Output window).

## [6.0.2] - 2014-06-16

### Added
* Logo.
* FsLexYacc.Runtime published as a separate NuGet package.

## [6.0.0] - 2014-04-18

### Added
* First release of the new packaging of fslex/fsyacc.
