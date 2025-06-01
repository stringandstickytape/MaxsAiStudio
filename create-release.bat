@echo off
REM create-release.bat - Creates a 7z archive of the AiStudio4 build output

setlocal enabledelayedexpansion

echo Creating release archive...

REM Check if git is available
git --version >nul 2>&1
if errorlevel 1 (
    echo Error: Git is not available or not in PATH
    exit /b 1
)

REM Check if 7z is available
7z >nul 2>&1
if errorlevel 1 (
    echo Error: 7z is not available or not in PATH
    echo Please install 7-Zip and ensure it's in your PATH
    exit /b 1
)

REM Get the current git SHA and extract first 7 characters
for /f "tokens=*" %%i in ('git rev-parse HEAD') do set FULL_SHA=%%i
set SHORT_SHA=!FULL_SHA:~0,7!

if "!SHORT_SHA!"=="" (
    echo Error: Could not get git SHA
    exit /b 1
)

echo Git SHA: !SHORT_SHA!

REM Define paths
set SOURCE_DIR=AiStudio4\bin\Debug\net9.0-windows
set OUTPUT_FILE=release-!SHORT_SHA!.7z

REM Check if source directory exists
if not exist "%SOURCE_DIR%" (
    echo Error: Source directory does not exist: %SOURCE_DIR%
    echo Please build the project first
    exit /b 1
)

REM Remove existing archive if it exists
if exist "%OUTPUT_FILE%" (
    echo Removing existing archive: %OUTPUT_FILE%
    del "%OUTPUT_FILE%"
)

echo Compressing %SOURCE_DIR% to %OUTPUT_FILE%...
echo Using compression level 3...

REM Create 7z archive with compression level 3
REM -t7z: 7z format
REM -mx=3: compression level 3
REM -mmt=on: multithreading
REM Change to source directory to archive contents without the folder structure
pushd "%SOURCE_DIR%"
7z a -t7z -mx=3 -mmt=on "..\..\..\%OUTPUT_FILE%" *
popd
pause
if errorlevel 1 (
    echo Error: Failed to create archive
	pause
    exit /b 1
)

echo.
echo Success! Created: %OUTPUT_FILE%
echo Archive contains the contents of: %SOURCE_DIR%

REM Show archive info
echo.
echo Archive information:
7z l "%OUTPUT_FILE%"

endlocal