# Sagar Island - 1-Click Release & Installer Build Script
$ErrorActionPreference = "Stop"

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "   Sagar Island - Build & Release Pipeline" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan

# 1. Locate dotnet
$dotnet = "C:\Users\Sagar\.dotnet\dotnet.exe"
if (-not (Test-Path $dotnet)) {
    $dotnet = (Get-Command dotnet).Source
}
Write-Host "[1/3] Rendering all vector icon & graphic assets..." -ForegroundColor Yellow
& $dotnet run -- --generate-icons

# 2. Publish Standalone Single-File Windows Executable
Write-Host "[2/3] Publishing self-contained win-x64 executable..." -ForegroundColor Yellow
& $dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o "./publish"

# 3. Compile Inno Setup Windows Installer
Write-Host "[3/3] Compiling Windows Setup Installer (SagarIsland-Setup.exe)..." -ForegroundColor Yellow
$iscc = "C:\Users\Sagar\AppData\Local\Programs\Inno Setup 6\ISCC.exe"
if (-not (Test-Path $iscc)) {
    $iscc = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
}
if (-not (Test-Path $iscc)) {
    $iscc = (Get-Command ISCC.exe -ErrorAction SilentlyContinue).Source
}

if ($iscc -and (Test-Path $iscc)) {
    & $iscc "installer.iss"
    Write-Host "`n✔ Installer created at: dist-installer\SagarIsland-Setup.exe" -ForegroundColor Green
} else {
    Write-Host "`n⚠ Inno Setup compiler (ISCC.exe) not found. Standalone EXE is available at publish\SagarIsland.exe" -ForegroundColor Yellow
}

Write-Host "`n=========================================" -ForegroundColor Cyan
Write-Host "   Release Package Completed Successfully!" -ForegroundColor Green
Write-Host "=========================================" -ForegroundColor Cyan
