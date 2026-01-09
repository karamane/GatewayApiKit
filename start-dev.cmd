@echo off
title Gateway + React Admin UI
echo.
echo ========================================
echo   Gateway + React Admin UI Starter
echo ========================================
echo.

REM Node.js PATH'e ekle
set PATH=C:\Program Files\nodejs;%PATH%

echo [1/3] Eski process'ler kapatiliyor...
taskkill /F /IM node.exe >nul 2>&1
taskkill /F /IM dotnet.exe >nul 2>&1
timeout /t 2 /nobreak >nul

echo [2/3] Gateway API baslatiliyor (port 53000)...
start "Gateway API" /D "src\Gateway\ApiGatewayKit.Gateway" dotnet run

echo Bekleniyor...
timeout /t 10 /nobreak >nul

echo [3/3] React Admin UI baslatiliyor (port 3000)...
cd gateway-admin-ui
npm run dev
