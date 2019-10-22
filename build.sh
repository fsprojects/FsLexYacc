#!/bin/bash
if test "$OS" = "Windows_NT"
then
  cmd /C build.cmd
else
  dotnet tool restore
  dotnet paket restore
  dotnet tool install paket --tool-path .paket # TODO: remove in the future, but we need it for `Paket.pack` for now
  dotnet fake run build.fsx $@
fi