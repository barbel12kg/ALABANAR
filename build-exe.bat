@echo off
setlocal
title KlikAuto - Build EXE
cd /d "%~dp0"

where dotnet >nul 2>nul
if errorlevel 1 (
  echo .NET 8 SDK belum ditemukan.
  echo Install dari https://dotnet.microsoft.com/download/dotnet/8.0
  pause
  exit /b 1
)

echo Building KlikAuto.exe...
dotnet publish KlikAuto.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
if errorlevel 1 (
  echo BUILD GAGAL. Periksa pesan error di atas.
  pause
  exit /b 1
)

echo.
echo SELESAI. EXE ada di:
echo %~dp0bin\Release\net8.0-windows\win-x64\publish\KlikAuto.exe
pause
