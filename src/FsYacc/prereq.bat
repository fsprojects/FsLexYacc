
REM
REM <FsLex Include="fsyacclex.fsl">
REM   <OtherFlags>--unicode --lexlib Internal.Utilities.Text.Lexing</OtherFlags>
REM </FsLex>
REM

..\..\lkg\bin\fslex.exe -o fsyacclex.fs --unicode --lexlib Internal.Utilities.Text.Lexing  fsyacclex.fsl

IF ERRORLEVEL 1 exit /b %ERRORLEVEL%

REM 
REM <FsYacc Include="fsyaccpars.fsy">
REM   <OtherFlags>--internal --module FsLexYacc.FsYacc.Parser --lexlib Internal.Utilities.Text.Lexing --parslib Internal.Utilities.Text.Parsing</OtherFlags>
REM </FsYacc>
REM

..\..\lkg\bin\fsyacc.exe -o fsyaccpars.fs --internal --module FsLexYacc.FsYacc.Parser --lexlib Internal.Utilities.Text.Lexing --parslib Internal.Utilities.Text.Parsing  fsyaccpars.fsy

IF ERRORLEVEL 1 exit /b %ERRORLEVEL%

REM compiler command line
REM
REM fsc.exe -o:obj\Release\fsyacc.exe -g --debug:pdbonly --noframework --define:INTERNALIZED_FSLEXYACC_RUNTIME --doc:..\..\bin\fsyacc.xml --optimize+ -r:"C:\Program Files (x86)\Reference Assemblies\Microsoft\FSharp\.NETFramework\v4.0\4.3.0.0\FSharp.Core.dll" -r:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0\mscorlib.dll" -r:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0\System.Core.dll" -r:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0\System.dll" --target:exe --warnaserror:76 --fullpaths --flaterrors --highentropyva- "C:\Users\E.SADA\AppData\Local\Temp\.NETFramework,Version=v4.0.AssemblyAttributes.fs" AssemblyInfo.fs ..\FsLexYacc.Runtime\Lexing.fsi ..\FsLexYacc.Runtime\Lexing.fs ..\FsLexYacc.Runtime\Parsing.fsi ..\FsLexYacc.Runtime\Parsing.fs ..\Common\Arg.fsi ..\Common\Arg.fs fsyaccast.fs fsyaccpars.fs fsyacclex.fs fsyacc.fs 

