@echo off
setlocal enabledelayedexpansion

color 0A
title Orion Project Cleaner

echo ===================================================
echo   Orion Project Cleaner v2.0
echo ===================================================
echo.
echo This script will remove all build artifacts:
echo   - bin, obj, x64, Debug, Release folders
echo   - .vs folder (Visual Studio cache)
echo   - *.user, *.suo, *.log, *.pdb temp files
echo.

set "ROOT=%~dp0"
cd /d "%ROOT%" || (
    echo [ERROR] Cannot change to directory: %ROOT%
    pause
    exit /b 1
)

echo [INFO] Working directory: %CD%
echo.

set "DIRS_TO_REMOVE=bin obj x64 Debug Release"
set /a DEL_COUNT=0

:: Remove root build folders
for %%d in (%DIRS_TO_REMOVE%) do (
    if exist "%%d" (
        echo [DELETE] Root: %%d
        rd /s /q "%%d" 2>nul
        if exist "%%d" (
            echo [WARN] Cannot delete: %%d (maybe in use)
        ) else (
            set /a DEL_COUNT+=1
        )
    )
)

:: Remove .vs folder
if exist ".vs" (
    echo [DELETE] .vs Visual Studio cache
    rd /s /q ".vs" 2>nul
    if exist ".vs" (
        echo [WARN] Cannot delete .vs (maybe in use)
    ) else (
        set /a DEL_COUNT+=1
    )
)

:: Recursively find build folders
echo.
echo [SCAN] Searching for build folders in subdirectories...
for /f "delims=" %%d in ('dir /b /s /ad bin obj x64 Debug Release 2^>nul') do (
    if exist "%%d" (
        echo [DELETE] %%d
        rd /s /q "%%d" 2>nul
        if not exist "%%d" set /a DEL_COUNT+=1
    )
)

:: Delete temp files
echo.
echo [CLEAN] Removing temporary files...
del /s /f /q *.user *.suo *.log *.tlog *.lastbuildstate *.pdb *.ilk *.exp 2>nul >nul

:: Delete stray .lib build outputs
echo [CLEAN] Removing stray .lib files...
del /s /f /q *.lib 2>nul >nul

:: Remove empty directories
echo [CLEAN] Removing empty directories...
for /f "delims=" %%d in ('dir /b /s /ad 2^>nul ^| sort /r') do (
    rd "%%d" 2>nul
)

echo.
echo ===================================================
echo   Cleanup Complete!
echo   Total folders deleted: %DEL_COUNT%
echo   Source code and project files preserved.
echo ===================================================
echo.
echo Press any key to exit...
pause >nul
exit /b 0