@echo off
dotnet tool restore
dotnet paket restore
dotnet tool install paket --tool-path .paket REM TODO: remove in the future, but we need it for `Paket.pack` for now
dotnet fake run build.fsx %*
