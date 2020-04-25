@echo off
if "%~1"=="" goto :NO_ARGUMENT
set build_type=%1
goto :BUILD
:NO_ARGUMENT
set build_type="Release"
:BUILD
echo Building Linux x64 %build_type%
dotnet build --configuration %build_type% -p:Platform=x64 -p:OS=Unix -r linux-x64
echo Building Windows x64 %build_type%
dotnet build --configuration %build_type% -p:Platform=x64 -p:OS=Windows_NT -r win-x64
echo Building Windows x32 %build_type%
dotnet build --configuration %build_type% -p:Platform=x86 -p:OS=Windows_NT -r win-x86