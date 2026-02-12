param(
    [string]$BaseUrl = "http://localhost:5177"
)

Write-Host "[Demo 60s] LeaseGate-Lite v0.1.0"

Write-Host "Step 1/5: Apply Balanced preset"
Invoke-RestMethod -Method Post -Uri "$BaseUrl/preset/apply" -ContentType "application/json" -Body (@{ name = "Balanced" } | ConvertTo-Json) | Out-Null

Write-Host "Step 2/5: Trigger short high-load burst"
Invoke-RestMethod -Method Post -Uri "$BaseUrl/simulate/pressure" -ContentType "application/json" -Body (@{ mode = "Spiky" } | ConvertTo-Json) | Out-Null
Invoke-RestMethod -Method Post -Uri "$BaseUrl/simulate/flood" -ContentType "application/json" -Body (@{
    interactiveRequests = 24
    backgroundRequests = 36
    clientAppId = "demo.60s"
    processName = "demo.60s"
} | ConvertTo-Json) | Out-Null
Start-Sleep -Seconds 3

Write-Host "Step 3/5: Show recovery"
Invoke-RestMethod -Method Post -Uri "$BaseUrl/simulate/pressure" -ContentType "application/json" -Body (@{ mode = "Normal" } | ConvertTo-Json) | Out-Null
Start-Sleep -Seconds 3
$status = Invoke-RestMethod -Method Get -Uri "$BaseUrl/status"
Write-Host "Status -> Heat=$($status.heatState), Effective=$($status.effectiveConcurrency), Reason=$($status.lastThrottleReason)"

Write-Host "Step 4/5: Export diagnostics (privacy-first)"
$diag = Invoke-RestMethod -Method Post -Uri "$BaseUrl/diagnostics/export?includePaths=false&includeVerbose=false"
Write-Host "Diagnostics -> $($diag.outputPath)"

Write-Host "Step 5/5: Show last events"
$tail = Invoke-RestMethod -Method Get -Uri "$BaseUrl/events/tail?n=5"
$tail.events | ForEach-Object { Write-Host ("{0} [{1}] {2}" -f $_.timestampUtc, $_.category, $_.message) }

Write-Host "Demo complete."
