@echo off
echo Publishing Linux x64
dotnet publish --configuration Release -p:Platform=x64 -p:OS=Unix -r linux-x64
echo Publishing Windows x64
dotnet publish --configuration Release -p:Platform=x64 -p:OS=Windows_NT -r win-x64
echo Publishing Windows x32
dotnet publish --configuration Release -p:Platform=x86 -p:OS=Windows_NT -r win-x86