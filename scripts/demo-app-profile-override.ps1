param(
    [string]$BaseUrl = "http://localhost:5177"
)

Write-Host "[LeaseGate-Lite Demo] Per-app profile override"

Invoke-RestMethod -Method Post -Uri "$BaseUrl/profiles/apply" -ContentType "application/json" -Body (@{
    clientAppId = "demo.imagegen"
    processName = "imagegen"
    presetName = "Performance"
    maxConcurrency = 12
    backgroundCap = 10
    maxOutputTokensClamp = 1536
    maxPromptTokensClamp = 6144
    requestsPerMinute = 220
    tokensPerMinute = 220000
} | ConvertTo-Json)

Invoke-RestMethod -Method Post -Uri "$BaseUrl/simulate/flood" -ContentType "application/json" -Body (@{
    interactiveRequests = 12
    backgroundRequests = 24
    clientAppId = "demo.imagegen"
    processName = "imagegen"
} | ConvertTo-Json)

Start-Sleep -Seconds 3
$profiles = Invoke-RestMethod -Method Get -Uri "$BaseUrl/profiles"
$status = Invoke-RestMethod -Method Get -Uri "$BaseUrl/status"

$override = $profiles.overrides | Where-Object { $_.clientAppId -eq "demo.imagegen" }
Write-Host "Override applied: $($null -ne $override)"
Write-Host "Status -> Heat: $($status.HeatState), Effective: $($status.EffectiveConcurrency), Queued: $($status.InteractiveQueueDepth + $status.BackgroundQueueDepth)"
Write-Host "Done."
