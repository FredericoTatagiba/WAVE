@echo off
setlocal
cd /d "%~dp0"
title WAVE - build and run

echo ============================================
echo  WAVE - compilando nova versao (Release)
echo ============================================

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

dotnet build "src\WAVE.App\WAVE.App.csproj" -c Release --nologo
if errorlevel 1 (
  echo.
  echo [ERRO] A compilacao falhou. Veja as mensagens acima.
  pause
  exit /b 1
)

set "EXE=src\WAVE.App\bin\Release\net8.0-windows\WAVE.exe"
if not exist "%EXE%" (
  echo [ERRO] Executavel nao encontrado em %EXE%
  pause
  exit /b 1
)

echo.
echo Build OK. Iniciando WAVE...
start "" "%EXE%"
exit /b 0
