@echo off
setlocal

set ROOT=%~dp0..\..
set PROJECT=%ROOT%\Pass-all\Pass-all.csproj
set PUBLISH_DIR=%ROOT%\build\publish-windows

echo Publier Pass-all pour Windows...
dotnet publish "%PROJECT%" ^
    -c Release ^
    -r win-x64 ^
    --self-contained true ^
    -p:PublishSingleFile=false ^
    -p:PublishReadyToRun=true ^
    -o "%PUBLISH_DIR%"

if %ERRORLEVEL% neq 0 (
    echo ERREUR: dotnet publish a echoue
    exit /b 1
)

echo.
echo Publication reussie dans : %PUBLISH_DIR%
echo.

REM Chercher Inno Setup
set INNO=""
if exist "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" set INNO="C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
if exist "C:\Program Files\Inno Setup 6\ISCC.exe"       set INNO="C:\Program Files\Inno Setup 6\ISCC.exe"

if %INNO%=="" (
    echo Inno Setup non trouve. Installez-le depuis https://jrsoftware.org/isinfo.php
    echo Puis compilez manuellement : Pass-all.iss
    exit /b 0
)

echo Compilation de l'installeur...
%INNO% "%~dp0Pass-all.iss"

if %ERRORLEVEL% neq 0 (
    echo ERREUR: Inno Setup a echoue
    exit /b 1
)

echo.
echo Installeur cree dans : %ROOT%\build\
