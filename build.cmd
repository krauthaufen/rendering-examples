@echo off
setlocal enableextensions enabledelayedexpansion
PUSHD %~dp0

dotnet tool restore



if NOT exist paket.lock (
    echo No paket.lock found, running paket install.
    dotnet paket install
)

dotnet paket restore
if errorlevel 1 (
  exit /b %errorlevel%
)

dotnet fake build %* 

