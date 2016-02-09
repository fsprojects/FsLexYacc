
REM
REM <FsLex Include="fslexlex.fsl">
REM   <OtherFlags>--unicode --lexlib Internal.Utilities.Text.Lexing</OtherFlags>
REM </FsLex>
REM

..\..\lkg\bin\fslex.exe -o fslexlex.fs --unicode --lexlib Internal.Utilities.Text.Lexing  fslexlex.fsl

IF ERRORLEVEL 1 exit /b %ERRORLEVEL%

REM
REM <FsYacc Include="fslexpars.fsy">
REM   <OtherFlags>--internal --module FsLexYacc.FsLex.Parser --lexlib Internal.Utilities.Text.Lexing --parslib Internal.Utilities.Text.Parsing</OtherFlags>
REM </FsYacc>
REM

..\..\lkg\bin\fsyacc.exe -o fslexpars.fs --internal --module FsLexYacc.FsLex.Parser --lexlib Internal.Utilities.Text.Lexing --parslib Internal.Utilities.Text.Parsing  fslexpars.fsy

IF ERRORLEVEL 1 exit /b %ERRORLEVEL%

