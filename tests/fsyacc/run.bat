@if "%_echo%"=="" echo off

setlocal 
dir build.ok > NUL ) || (
  @echo 'build.ok' not found.
  goto :ERROR
)

if ERRORLEVEL 1 goto Error


for /f  %%i in ("%FSDIFF%") do (
 dir %%i% > NUL
 if ERRORLEVEL 1 goto Error
)

.\test1.exe --tokens .\test1.input1 2> test1.input1.tokens.err
if ERRORLEVEL 1 goto Error

.\test1.exe .\test1.input1 2> test1.input1.err
if ERRORLEVEL 1 goto Error

.\test1.exe .\test1.input2.variation1  .\test1.input2.variation2 2> test1.input2.err
if ERRORLEVEL 1 goto Error


.\test1-unicode.exe --tokens .\test1.input1 2> test1-unicode.input1.tokens.err
if ERRORLEVEL 1 goto Error

.\test1-unicode.exe .\test1.input1 2> test1-unicode.input1.err
if ERRORLEVEL 1 goto Error

.\test1-unicode.exe .\test1.input2.variation1  .\test1.input2.variation2 2> test1-unicode.input2.err
if ERRORLEVEL 1 goto Error

.\test1-unicode.exe --tokens .\test1-unicode.input3.utf8 2> test1-unicode.input3.tokens.err
if ERRORLEVEL 1 goto Error



.\test1compat.exe --tokens .\test1.input1 2> test1compat.input1.tokens.err
if ERRORLEVEL 1 goto Error

.\test1compat.exe .\test1.input1 2> test1comapt.input1.err
if ERRORLEVEL 1 goto Error

.\test1compat.exe .\test1.input2.variation1  .\test1.input2.variation2 2> test1compat.input2.err
if ERRORLEVEL 1 goto Error


.\test2.exe --tokens .\test2.input1 2> test2.input1.tokens.err
if ERRORLEVEL 1 goto Error

.\test2.exe --tokens .\test2.badInput 2> test1.badInput.tokens.err
if ERRORLEVEL 1 goto Error

.\test2.exe .\test2.input1 2> test2.input1.err
if ERRORLEVEL 1 goto Error

.\test2.exe .\test2.badInput 2> test1.badInput.err
if ERRORLEVEL 1 goto Error


for /d %%f IN (test1.input1.tokens test1.input1 test1.input2 test1-unicode.input1.tokens test1-unicode.input1 test1-unicode.input2 test1-unicode.input3.tokens test1compat.input1.tokens test1comapt.input1 test1compat.input2 test2.input1.tokens test1.badInput.tokens test2.input1 test1.badInput) do (
  echo ***** FSDIFF=%FSDIFF%, f = %%f
  %FSDIFF% %%f.err %%f.bsl > %%f.diff
  for /f %%c IN (%%f.diff) do (
    echo ***** %%f.err %%f.bsl differed: a bug or baseline may neeed updating

    IF DEFINED WINDIFF (start %windiff% %%f.bsl  %%f.err)
    goto SETERROR 
  )
  echo Good, output %%f.err matched %%f.bsl
)

:Ok
echo Ran fsharp %~f0 ok.
endlocal
exit /b 0

:Skip
echo Skipped %~f0
endlocal
exit /b 0


:Error
endlocal
exit /b %ERRORLEVEL%


:SETERROR
set NonexistentErrorLevel 2> nul
goto Error
