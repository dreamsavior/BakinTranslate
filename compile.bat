@echo off
setlocal EnableExtensions

set "SCRIPT_DIR=%~dp0"
cd /d "%SCRIPT_DIR%"

set "SOLUTION=BakinTranslate.sln"
set "CONFIGURATION=%~1"
if not defined CONFIGURATION set "CONFIGURATION=Release"

if /I not "%CONFIGURATION%"=="Debug" if /I not "%CONFIGURATION%"=="Release" (
    echo Usage: %~nx0 [Debug^|Release]
    exit /b 1
)

set "PLATFORM=Any CPU"
set "TOOLS_DIR=%SCRIPT_DIR%.tools"
set "NUGET_EXE=%TOOLS_DIR%\nuget.exe"

call :find_msbuild
if errorlevel 1 exit /b 1

call :ensure_nuget
if errorlevel 1 exit /b 1

echo ==^> Restoring NuGet packages...
"%NUGET_EXE%" restore "%SOLUTION%" -NonInteractive
if errorlevel 1 (
    echo NuGet restore failed.
    exit /b 1
)

echo ==^> Building %SOLUTION% (%CONFIGURATION%^|%PLATFORM%)...
"%MSBUILD_EXE%" "%SOLUTION%" /m /nr:false /t:Build "/p:Configuration=%CONFIGURATION%;Platform=%PLATFORM%" /verbosity:minimal
if errorlevel 1 (
    echo Build failed.
    exit /b 1
)

echo ==^> Build succeeded.
echo Output folders:
echo   bakinplayer\bin\%CONFIGURATION%
echo   BakinTranslate.CLI\bin\%CONFIGURATION%
exit /b 0

:find_msbuild
set "MSBUILD_EXE="
set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"

if exist "%VSWHERE%" (
    for /f "usebackq delims=" %%I in (`"%VSWHERE%" -latest -products * -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe`) do (
        if not defined MSBUILD_EXE set "MSBUILD_EXE=%%I"
    )
)

if not defined MSBUILD_EXE (
    for /f "delims=" %%I in ('where msbuild.exe 2^>nul') do (
        if not defined MSBUILD_EXE set "MSBUILD_EXE=%%I"
    )
)

if not defined MSBUILD_EXE (
    echo MSBuild was not found.
    echo Install Visual Studio 2022 or Build Tools with MSBuild support.
    exit /b 1
)

echo Using MSBuild: %MSBUILD_EXE%
exit /b 0

:ensure_nuget
if exist "%NUGET_EXE%" exit /b 0

for /f "delims=" %%I in ('where nuget.exe 2^>nul') do (
    set "NUGET_EXE=%%I"
    exit /b 0
)

if not exist "%TOOLS_DIR%" mkdir "%TOOLS_DIR%"

echo ==^> Downloading nuget.exe...
powershell -NoProfile -ExecutionPolicy Bypass -Command "Invoke-WebRequest -Uri 'https://dist.nuget.org/win-x86-commandline/latest/nuget.exe' -OutFile '%NUGET_EXE%'"
if errorlevel 1 (
    echo Failed to download nuget.exe.
    exit /b 1
)

exit /b 0
