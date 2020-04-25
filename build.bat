@echo off
if "%~1"=="" goto :NO_ARGUMENT
set build_type="%1"
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

Rem This magic comes from: https://superuser.com/a/1413170
for /f "delims=" %%a in ('powershell.exe -command "& {write-host $([Environment]::GetFolderPath('MyDocuments'))}"') do Set "$documents_path=%%a"
Rem Echo Value received from Powershell : %$documents_path%\MCEdit\Filters
Rem For older Machines Maybe reg query "HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders" /v Personal
echo Copying Files from ".\Python" to "%$documents_path%\MCEdit\Filters"
xcopy /y /s /Q /i ".\Python" "%$documents_path%\MCEdit\Filters"