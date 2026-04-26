@echo off
setlocal

set "SCRIPT_DIR=%~dp0"
set "PROJECT=%SCRIPT_DIR%SampleMcpServer.Stdio\SampleMcpServer.Stdio.csproj"
set "OUTPUT=%SCRIPT_DIR%artifacts\publish\stdio\win-x64"

dotnet publish "%PROJECT%" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishAot=true -p:InvariantGlobalization=true -p:StripSymbols=true -p:IlcOptimizationPreference=Speed -o "%OUTPUT%"
if errorlevel 1 exit /b %errorlevel%

echo.
echo Published SampleMcpServer.Stdio to:
echo   %OUTPUT%
