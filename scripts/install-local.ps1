param(
    [string]$Version = "0.1.0",
    [string]$Runtime = "win-x64",
    [switch]$EnableAutostart
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$distRoot = Join-Path $root "dist"
$packageName = "LeaseGateLite-v$Version-$Runtime"
$zipPath = Join-Path $distRoot "$packageName.zip"
$installDir = Join-Path $env:LOCALAPPDATA "LeaseGateLite"
$tempExtract = Join-Path $env:TEMP ("LeaseGateLite-install-" + [guid]::NewGuid().ToString("N"))

if (-not (Test-Path $zipPath)) {
    throw "Package not found at $zipPath. Run scripts/package-v0.1.0.ps1 first."
}

New-Item -ItemType Directory -Path $tempExtract | Out-Null
Expand-Archive -Path $zipPath -DestinationPath $tempExtract -Force

if (Test-Path $installDir) {
    Remove-Item $installDir -Recurse -Force
}
New-Item -ItemType Directory -Path $installDir | Out-Null
Copy-Item "$tempExtract\*" $installDir -Recurse -Force

$daemonExe = Join-Path $installDir "daemon\LeaseGateLite.Daemon.exe"
$appExe = Join-Path $installDir "app\LeaseGateLite.App.exe"
$trayExe = Join-Path $installDir "tray\LeaseGateLite.Tray.exe"

if (Test-Path $daemonExe) {
    Start-Process -FilePath $daemonExe -ArgumentList "--run" -WindowStyle Hidden | Out-Null
}

if ($EnableAutostart -and (Test-Path $daemonExe)) {
    Start-Process -FilePath $daemonExe -ArgumentList "--install-autostart" -WindowStyle Hidden -Wait | Out-Null
}

if (Test-Path $trayExe) {
    Start-Process -FilePath $trayExe | Out-Null
}

if (Test-Path $appExe) {
    Start-Process -FilePath $appExe | Out-Null
}

Remove-Item $tempExtract -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "Install complete. LeaseGate-Lite is now running with Balanced defaults."
Write-Host "Open the Control Panel to adjust anything, or leave it as-is for smooth day-to-day use."
