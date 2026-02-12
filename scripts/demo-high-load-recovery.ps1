param(
    [string]$BaseUrl = "http://localhost:5177"
)

Write-Host "[LeaseGate-Lite Demo] High-load scenario and recovery"

Invoke-RestMethod -Method Post -Uri "$BaseUrl/simulate/pressure" -ContentType "application/json" -Body (@{ mode = "Spiky" } | ConvertTo-Json)
Invoke-RestMethod -Method Post -Uri "$BaseUrl/simulate/flood" -ContentType "application/json" -Body (@{
    interactiveRequests = 40
    backgroundRequests = 60
    clientAppId = "demo-high-load"
    processName = "demo-high-load"
} | ConvertTo-Json)

Start-Sleep -Seconds 4
$statusHot = Invoke-RestMethod -Method Get -Uri "$BaseUrl/status"
Write-Host "During load -> Heat: $($statusHot.HeatState), Effective: $($statusHot.EffectiveConcurrency), Reason: $($statusHot.LastThrottleReason)"

Invoke-RestMethod -Method Post -Uri "$BaseUrl/simulate/pressure" -ContentType "application/json" -Body (@{ mode = "Normal" } | ConvertTo-Json)
Start-Sleep -Seconds 4
$statusRecovered = Invoke-RestMethod -Method Get -Uri "$BaseUrl/status"
Write-Host "After recovery -> Heat: $($statusRecovered.HeatState), Effective: $($statusRecovered.EffectiveConcurrency), Reason: $($statusRecovered.LastThrottleReason)"
Write-Host "Done."
