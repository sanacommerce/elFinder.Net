ECHO OFF

SET ResultDirectory="_Build"
SET TempDirectory=".output"
SET PrivateKey=%1

if "%1"=="" SET PrivateKey="..\Files\scl_private.snk"

if exist %ResultDirectory% rmdir /S /Q %ResultDirectory%

ECHO Building project...
"%programfiles(x86)%\MSBuild\14.0\Bin\msbuild" "elFinder.Net\elFinder.Net.csproj" /t:Rebuild /p:Configuration=Release;OutputPath=..\\%TempDirectory%;DelaySign=false;AssemblyOriginatorKeyFile=..\\%PrivateKey%

ECHO Copying result to %ResultDirectory%...
xcopy %TempDirectory%\elFinder.Net.dll %ResultDirectory%\

rmdir /S /Q %TempDirectory%

