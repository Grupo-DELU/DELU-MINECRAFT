#!/bin/sh
if [ -z "$1" ]
  then
    build_type="Release"
else
    build_type="$1"
fi
echo Building Linux x64 $build_type
dotnet build --configuration $build_type -p:Platform=x64 -p:OS=Unix -r linux-x64
echo Building Windows x64 $build_type
dotnet build --configuration $build_type -p:Platform=x64 -p:OS=Windows_NT -r win-x64
echo Building Windows x32 $build_type
dotnet build --configuration $build_type -p:Platform=x86 -p:OS=Windows_NT -r win-x86