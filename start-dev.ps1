# Gateway Admin UI - Geliştirme Ortamı Başlatıcı
# Kullanım: .\start-dev.ps1

Write-Host "🚀 Gateway + React Admin UI başlatılıyor..." -ForegroundColor Cyan

# Eski process'leri temizle
Write-Host "📦 Eski process'ler temizleniyor..." -ForegroundColor Yellow
Get-Process -Name "node" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
taskkill /F /IM dotnet.exe 2>$null | Out-Null
Start-Sleep -Seconds 2

# Gateway'i arka planda başlat
Write-Host "⚡ Gateway API başlatılıyor (port 53000)..." -ForegroundColor Green
$gatewayPath = Join-Path $PSScriptRoot "src\Gateway\ApiGatewayKit.Gateway"
Start-Process -NoNewWindow -FilePath "dotnet" -ArgumentList "run" -WorkingDirectory $gatewayPath

# Gateway'in başlamasını bekle
Start-Sleep -Seconds 8

# React UI'ı başlat
Write-Host "🎨 React Admin UI başlatılıyor (port 3000)..." -ForegroundColor Green
$uiPath = Join-Path $PSScriptRoot "gateway-admin-ui"
Set-Location $uiPath

# npm.cmd ile başlat (PowerShell execution policy sorunu için)
& "C:\Program Files\nodejs\npm.cmd" run dev

