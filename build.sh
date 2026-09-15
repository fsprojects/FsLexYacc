#!/bin/bash
if test "$OS" = "Windows_NT"
then
  cmd /C build.cmd
else
  dotnet tool restore
  dotnet fsi build.fsx $@
fi