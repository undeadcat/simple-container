@echo off
set dotNetBasePath=%windir%\Microsoft.NET\Framework
if exist %dotNetBasePath%64 set dotNetBasePath=%dotNetBasePath%64
for /R %dotNetBasePath% %%i in (*msbuild.exe) do set msbuild=%%i

set target=_Src\SimpleContainer.sln

%msbuild% /t:Rebuild /v:m /p:Configuration=Release /p:Platform=FullFramework %target% || exit /b 1
%msbuild% /t:Rebuild /v:m /p:Configuration=Release /p:Platform=Portable %target% || exit /b 1
