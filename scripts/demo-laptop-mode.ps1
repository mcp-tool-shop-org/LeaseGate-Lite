param(
    [string]$BaseUrl = "http://localhost:5177"
)

Write-Host "[LeaseGate-Lite Demo] Laptop mode setup"

Invoke-RestMethod -Method Post -Uri "$BaseUrl/preset/apply" -ContentType "application/json" -Body (@{ name = "Quiet" } | ConvertTo-Json)

$config = Invoke-RestMethod -Method Get -Uri "$BaseUrl/config"
$config.InteractiveReserve = [Math]::Max([int]$config.InteractiveReserve, 2)
$config.BackgroundCap = [Math]::Min([int]$config.BackgroundCap, [Math]::Max(1, [int]$config.MaxConcurrency - [int]$config.InteractiveReserve))

Invoke-RestMethod -Method Post -Uri "$BaseUrl/config" -ContentType "application/json" -Body ($config | ConvertTo-Json -Depth 6)
$status = Invoke-RestMethod -Method Get -Uri "$BaseUrl/status"

Write-Host "Heat: $($status.HeatState)"
Write-Host "EffectiveConcurrency: $($status.EffectiveConcurrency)"
Write-Host "LastReason: $($status.LastThrottleReason)"
Write-Host "Done."
