@echo off
chcp 65001 >nul
title A compilar o ChestCounter
cd /d "%~dp0"

where dotnet >nul 2>nul
if errorlevel 1 (
  echo O .NET 10 SDK não está instalado: https://dotnet.microsoft.com/download/dotnet/10.0
  pause
  exit /b 1
)

echo A compilar (1 a 2 minutos na primeira vez)...
dotnet publish src\ContadorBaus\ContadorBaus.csproj -c Release -r win-x64 --self-contained true ^
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true ^
  -p:EnableCompressionInSingleFile=true -p:DebugType=none -o dist
if errorlevel 1 (
  echo A compilação falhou. Vê a mensagem acima.
  pause
  exit /b 1
)
echo.
echo Pronto: %~dp0dist\ChestCounter.exe
explorer dist
