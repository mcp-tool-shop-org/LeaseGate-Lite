param(
    [string]$Version = "0.1.0",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$distRoot = Join-Path $root "dist"
$packageName = "LeaseGateLite-v$Version-$Runtime"
$outDir = Join-Path $distRoot $packageName
$appPublish = Join-Path $distRoot "publish-app"
$daemonPublish = Join-Path $distRoot "publish-daemon"
$trayPublish = Join-Path $distRoot "publish-tray"
$zipPath = Join-Path $distRoot "$packageName.zip"
$checksumPath = "$zipPath.sha256"

Remove-Item $appPublish,$daemonPublish,$trayPublish,$outDir -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $distRoot -Force | Out-Null

Write-Host "Publishing app..."
try {
    & dotnet publish (Join-Path $root "src/LeaseGateLite.App/LeaseGateLite.App.csproj") -f net10.0-windows10.0.19041.0 -r $Runtime -c Release --self-contained false -o $appPublish
    if ($LASTEXITCODE -ne 0) { throw "runtime-specific publish failed" }
}
catch {
    Write-Warning "Runtime-specific app publish failed; retrying framework-only publish."
    & dotnet publish (Join-Path $root "src/LeaseGateLite.App/LeaseGateLite.App.csproj") -f net10.0-windows10.0.19041.0 -c Release --self-contained false -o $appPublish
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Framework-only publish failed; falling back to Release build output."
        & dotnet build (Join-Path $root "src/LeaseGateLite.App/LeaseGateLite.App.csproj") -f net10.0-windows10.0.19041.0 -c Release -m:1
        if ($LASTEXITCODE -ne 0) { throw "release build fallback failed" }

        $fallbackPath = Join-Path $root "src/LeaseGateLite.App\bin\Release\net10.0-windows10.0.19041.0\win-x64"
        if (-not (Test-Path $fallbackPath)) { throw "fallback app output not found at $fallbackPath" }
        New-Item -ItemType Directory -Path $appPublish -Force | Out-Null
        Copy-Item "$fallbackPath\*" $appPublish -Recurse -Force
    }
}

if (-not (Test-Path $appPublish)) {
    throw "App publish output was not created at $appPublish"
}

Write-Host "Publishing daemon..."
dotnet publish (Join-Path $root "src/LeaseGateLite.Daemon/LeaseGateLite.Daemon.csproj") -f net10.0 -c Release --self-contained false -o $daemonPublish

Write-Host "Publishing tray companion..."
dotnet publish (Join-Path $root "src/LeaseGateLite.Tray/LeaseGateLite.Tray.csproj") -f net10.0-windows10.0.19041.0 -r $Runtime -c Release --self-contained false -o $trayPublish

New-Item -ItemType Directory -Path $outDir | Out-Null
New-Item -ItemType Directory -Path (Join-Path $outDir "app") -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $outDir "daemon") -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $outDir "tray") -Force | Out-Null
Copy-Item "$appPublish\*" (Join-Path $outDir "app") -Recurse -Force
Copy-Item "$daemonPublish\*" (Join-Path $outDir "daemon") -Recurse -Force
Copy-Item "$trayPublish\*" (Join-Path $outDir "tray") -Recurse -Force

@"
LeaseGate-Lite v$Version

What happens next: Start the control panel once and it will connect to the local daemon automatically.
Balanced mode is already selected by default, so most people can leave everything as-is.
"@ | Set-Content -Path (Join-Path $outDir "WHAT_HAPPENS_NEXT.txt") -Encoding UTF8

@"
Run install-local.ps1 from this folder for one-click local install.
This package is checksummed; verify the .sha256 file before install if desired.
"@ | Set-Content -Path (Join-Path $outDir "INSTALL.txt") -Encoding UTF8

if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
if (Test-Path $checksumPath) { Remove-Item $checksumPath -Force }
Compress-Archive -Path "$outDir\*" -DestinationPath $zipPath -CompressionLevel Optimal

$hash = Get-FileHash -Path $zipPath -Algorithm SHA256
"$($hash.Hash)  $([IO.Path]::GetFileName($zipPath))" | Set-Content -Path $checksumPath -Encoding ASCII

Write-Host "Created package: $zipPath"
Write-Host "Created checksum: $checksumPath"
