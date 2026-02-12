param(
    [string]$BaseUrl = "http://localhost:5177",
    [string]$ReportPath = "RELEASE_GATE_REPORT.md"
)

$ErrorActionPreference = "Stop"

$results = New-Object System.Collections.Generic.List[object]

function Add-Result {
    param([string]$Name, [bool]$Pass, [string]$Detail)
    $results.Add([pscustomobject]@{ Name = $Name; Pass = $Pass; Detail = $Detail })
}

function Try-Step {
    param([string]$Name, [scriptblock]$Step)
    try {
        $detail = & $Step
        Add-Result -Name $Name -Pass $true -Detail ($detail | Out-String).Trim()
    }
    catch {
        Add-Result -Name $Name -Pass $false -Detail $_.Exception.Message
    }
}

Try-Step "endpoints reachable" {
    $status = Invoke-RestMethod -Method Get -Uri "$BaseUrl/status"
    if ($null -eq $status) { throw "status response missing" }
    "status heat=$($status.heatState) effective=$($status.effectiveConcurrency)"
}

Try-Step "presets apply" {
    foreach ($preset in "Quiet", "Balanced", "Performance") {
        $resp = Invoke-RestMethod -Method Post -Uri "$BaseUrl/preset/apply" -ContentType "application/json" -Body (@{ name = $preset } | ConvertTo-Json)
        if (-not $resp.success) { throw "preset failed: $preset" }
    }
    "quiet/balanced/performance applied"
}

Try-Step "autostart toggles" {
    $initial = Invoke-RestMethod -Method Get -Uri "$BaseUrl/autostart/status"
    $initialEnabled = [bool]$initial.enabled

    Invoke-RestMethod -Method Post -Uri "$BaseUrl/autostart" -ContentType "application/json" -Body (@{ enabled = $true } | ConvertTo-Json) | Out-Null
    $enabled = Invoke-RestMethod -Method Get -Uri "$BaseUrl/autostart/status"
    if (-not $enabled.enabled) { throw "enable did not stick" }

    Invoke-RestMethod -Method Post -Uri "$BaseUrl/autostart" -ContentType "application/json" -Body (@{ enabled = $false } | ConvertTo-Json) | Out-Null
    $disabled = Invoke-RestMethod -Method Get -Uri "$BaseUrl/autostart/status"
    if ($disabled.enabled) { throw "disable did not stick" }

    Invoke-RestMethod -Method Post -Uri "$BaseUrl/autostart" -ContentType "application/json" -Body (@{ enabled = $initialEnabled } | ConvertTo-Json) | Out-Null
    "autostart enable/disable roundtrip succeeded"
}

Try-Step "diagnostics export works" {
    $diag = Invoke-RestMethod -Method Post -Uri "$BaseUrl/diagnostics/export?includePaths=false&includeVerbose=false"
    if (-not $diag.exported) { throw "export failed" }
    "export path=$($diag.outputPath) bytes=$($diag.bytesWritten)"
}

Try-Step "event tailing works" {
    $tail = Invoke-RestMethod -Method Get -Uri "$BaseUrl/events/tail?n=20"
    if ($null -eq $tail.events) { throw "tail missing events list" }

    $stream = Invoke-RestMethod -Method Get -Uri "$BaseUrl/events/stream?sinceId=0&timeoutMs=500"
    if ($null -eq $stream.lastEventId) { throw "stream payload missing lastEventId" }
    "tailCount=$($tail.events.Count) streamLast=$($stream.lastEventId)"
}

$passed = ($results | Where-Object { $_.Pass }).Count
$total = $results.Count
$failed = $total - $passed

$lines = @()
$lines += "# Release Gate Report"
$lines += ""
$lines += "Generated: $(Get-Date -Format s)"
$lines += "Target: $BaseUrl"
$lines += ""
$lines += "Summary: $passed / $total passed"
$lines += ""

foreach ($item in $results) {
    $icon = if ($item.Pass) { "✅" } else { "❌" }
    $lines += "- $icon $($item.Name) - $($item.Detail)"
}

$lines += ""
$lines += if ($failed -eq 0) { "Release gate status: PASS" } else { "Release gate status: FAIL" }

Set-Content -Path $ReportPath -Value $lines -Encoding UTF8
Write-Host "Wrote $ReportPath"
Write-Host "Summary: $passed/$total passed"

if ($failed -gt 0) {
    exit 1
}
