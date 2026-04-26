@echo off
setlocal

set "SCRIPT_DIR=%~dp0"
set "PROJECT=%SCRIPT_DIR%SampleMcpServer.Http\SampleMcpServer.Http.csproj"
set "OUTPUT=%SCRIPT_DIR%artifacts\publish\http\win-x64"

dotnet publish "%PROJECT%" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o "%OUTPUT%"
if errorlevel 1 exit /b %errorlevel%

echo.
echo Published SampleMcpServer.Http to:
echo   %OUTPUT%
