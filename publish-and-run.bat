@echo off
setlocal
cd /d "%~dp0"
title WAVE - publish and run

echo ==================================================
echo  WAVE - publicando .exe unico self-contained (x64)
echo ==================================================

where dotnet >nul 2>nul
if errorlevel 1 (
  echo [ERRO] .NET SDK nao encontrado no PATH.
  echo Instale o .NET 8 SDK e tente novamente.
  pause
  exit /b 1
)

echo Encerrando instancias do WAVE em execucao (se houver)...
taskkill /IM WAVE.exe /F >nul 2>nul
timeout /t 1 /nobreak >nul

dotnet publish "src\WAVE.App\WAVE.App.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "publish\win-x64"
if errorlevel 1 (
  echo.
  echo [ERRO] A publicacao falhou. Veja as mensagens acima.
  pause
  exit /b 1
)

set "EXE=publish\win-x64\WAVE.exe"
if not exist "%EXE%" (
  echo [ERRO] Executavel nao encontrado em %EXE%
  pause
  exit /b 1
)

echo.
echo Publish OK. Iniciando WAVE...
start "" "%EXE%"
exit /b 0
