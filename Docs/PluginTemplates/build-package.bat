@echo off
setlocal enabledelayedexpansion
rem ============================================================================
rem  Orion plugin package builder
rem
rem  Usage:
rem    build-package.bat Server   Server-only plugin package (default)
rem    build-package.bat Ui       UI extension plugin package
rem    build-package.bat Server ..\ClientPlugin\MyClientPlugin.Client.csproj
rem                                Server + client (two-half) package: pass the
rem                                client project as the 2nd argument. The client
rem                                DLL must end with ".Client.dll" and its plugin.json
rem                                must list it in the "client" array.
rem
rem  Output: <template root>\out\<name>.Orion
rem
rem  NOTE: subdirectories ARE preserved by the importer (entry.FullName is kept),
rem  so dependencies like libs\ can be packed in subfolders and will be restored.
rem  The importer also guards against Zip Slip (../ or absolute paths are skipped).
rem  See tutorial section 7.2.
rem ============================================================================

set MODE=%1
if "%MODE%"=="" set MODE=Server

set ROOT=%~dp0
set OUTDIR=%ROOT%out
set PUB=%ROOT%_publish

set SERVER_PROJ=
set SERVER_JSON=
set CLIENT_PROJ=
set PKG_NAME=

if /i "%MODE%"=="Server" (
    set SERVER_PROJ=%ROOT%ServerPlugin\MyServerPlugin.csproj
    set SERVER_JSON=%ROOT%ServerPlugin\plugin.json
    set CLIENT_PROJ=
    set PKG_NAME=MyServerPlugin.Orion
)
if /i "%MODE%"=="Ui" (
    set SERVER_PROJ=%ROOT%UiExtensionPlugin\MyUiPlugin.csproj
    set SERVER_JSON=%ROOT%UiExtensionPlugin\plugin.json
    set CLIENT_PROJ=
    set PKG_NAME=MyUiPlugin.Orion
)

rem Optional 2nd argument: client project -> build a two-half (server+client) package.
if not "%~2"=="" set CLIENT_PROJ=%~2

if not defined SERVER_PROJ (
    echo [ERROR] Unknown mode: %MODE%  (Server / Ui)
    exit /b 1
)

echo ============================================================
echo  Mode          : %MODE%
echo  Server project: %SERVER_PROJ%
echo  Client project: %CLIENT_PROJ%
echo  Output package: %OUTDIR%\%PKG_NAME%
echo ============================================================

rem ---------- 1. clean ----------
if exist "%PUB%" rmdir /s /q "%PUB%"
if not exist "%OUTDIR%" mkdir "%OUTDIR%"
if exist "%OUTDIR%\%PKG_NAME%" del /f /q "%OUTDIR%\%PKG_NAME%"

rem ---------- 2. build ----------
echo.
echo [1/4] Building server plugin (net9.0-windows) ...
dotnet publish "%SERVER_PROJ%" -c Release -o "%PUB%"
if errorlevel 1 (
    echo [ERROR] Server project build failed.
    exit /b 1
)

if defined CLIENT_PROJ (
    echo [2/4] Building client plugin (net48) ...
    dotnet publish "%CLIENT_PROJ%" -c Release -o "%PUB%"
    if errorlevel 1 (
        echo [ERROR] Client project build failed.
        exit /b 1
    )
) else (
    echo [2/4] Skipping client project.
)

rem ---------- 3. stage ----------
echo [3/4] Staging publish folder ...
copy /y "%SERVER_JSON%" "%PUB%\plugin.json" >nul

del /q "%PUB%\*.deps.json"       2>nul
del /q "%PUB%\*.pdb"             2>nul
del /q "%PUB%\*.xml"             2>nul
rem Never ship Orion's own assemblies - the host already loaded them.
del /q "%PUB%\Orion.dll"       2>nul
del /q "%PUB%\Orion.Server.dll" 2>nul
del /q "%PUB%\Orion.Client.dll" 2>nul
del /q "%PUB%\Orion.Common.dll" 2>nul

echo.
echo Publish folder contents:
dir /b "%PUB%"
echo.

rem ---------- 4. pack (cd in, pack flat) ----------
echo [4/4] Packing %PKG_NAME% ...
pushd "%PUB%"

set SEVENZIP=
if exist "C:\Program Files\7-Zip\7z.exe" set SEVENZIP=C:\Program Files\7-Zip\7z.exe
if exist "C:\Program Files (x86)\7-Zip\7z.exe" set SEVENZIP=C:\Program Files (x86)\7-Zip\7z.exe

if defined SEVENZIP (
    "%SEVENZIP%" a -tzip -mx=9 "%OUTDIR%\%PKG_NAME%" * -r -x!*.Orion >nul
) else (
    powershell -NoProfile -Command "Compress-Archive -Path '.\*' -DestinationPath '%OUTDIR%\%PKG_NAME%' -CompressionLevel Optimal -Force"
)

popd

if not exist "%OUTDIR%\%PKG_NAME%" (
    echo [ERROR] Packing failed.
    exit /b 1
)

rem ---------- 5. verify ----------
echo.
echo Verifying package contents (nested paths are OK and will be preserved):
powershell -NoProfile -Command ^
  "Add-Type -AssemblyName System.IO.Compression.FileSystem;" ^
  "$z=[System.IO.Compression.ZipFile]::OpenRead('%OUTDIR%\%PKG_NAME%');" ^
  "$z.Entries | ForEach-Object { if ([string]::IsNullOrWhiteSpace($_.Name)) { Write-Host ('  [dir] ' + $_.FullName) } else { Write-Host ('  ' + $_.FullName + '  (' + $_.Length + ' bytes)') } };" ^
  "$z.Dispose()"

echo.
echo ============================================================
echo  Done: %OUTDIR%\%PKG_NAME%
echo  Install: Orion menu -^> Plugin Manager -^> Import
echo           or drag the .Orion file onto the plugin list.
echo ============================================================

endlocal
