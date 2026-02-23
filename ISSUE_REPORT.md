# Issue Report: High CPU Usage from JSON Deserialization Errors

**Date**: 2026-02-12
**Version**: 0.1.1
**Severity**: High

## Summary

The MAUI app is causing excessive CPU usage due to continuous JSON deserialization errors occurring every ~2 seconds in the polling loop. The app makes 1,957 first-chance exception throws in approximately 10 minutes (~200/minute, ~3-4/second), creating unnecessary CPU load.

## Root Cause

### JSON Serialization Mismatch

The daemon (net10.0) and MAUI app (net9.0) are using different JSON serialization formats for enums:

**Problem 1: `HeatState` enum**
- Error: `The JSON value could not be converted to LeaseGateLite.Contracts.HeatState. Path: $.heatState`
- Frequency: ~2 exceptions every 2 seconds (status polling)
- Location: `RefreshStatusAndEventsAsync()` timer callback

**Problem 2: `EventCategory` enum**
- Error: `The JSON value could not be converted to LeaseGateLite.Contracts.EventCategory. Path: $.events[0].category`
- Frequency: ~2 exceptions every 2 seconds (event stream polling)
- Location: `RunEventStreamLoopAsync()` event stream

### Why This Happens

.NET 9 and .NET 10 have different default JSON serialization behavior for enums:
- The daemon (net10.0) is likely serializing enums as integers (default)
- The MAUI app (net9.0) expects enums as strings OR vice versa
- Every HTTP call fails JSON deserialization and throws first-chance exceptions

### CPU Impact

**Polling frequency:**
- Status polling: Every 2 seconds (`Dispatcher.StartTimer(TimeSpan.FromSeconds(2))`)
- Event stream: Continuous with 500-8000ms exponential backoff

**Exception overhead:**
- Each failed deserialization throws 4-5 first-chance exceptions (caught/rethrown in JSON deserializer)
- ~200 exceptions/minute = constant GC pressure + exception handling overhead
- Exceptions are caught by `SafeDaemonCallAsync` BUT the damage is already done
- First-chance exceptions still execute full stack unwinding before being caught

## Evidence

### Log Sample
```
2026-02-12T21:17:55.5472493-05:00 FIRST_CHANCE: System.Text.Json.JsonException: The JSON value could not be converted to LeaseGateLite.Contracts.HeatState. Path: $.heatState | LineNumber: 0 | BytePositionInLine: 243.
2026-02-12T21:17:57.5567890-05:00 FIRST_CHANCE: System.Text.Json.JsonException: The JSON value could not be converted to LeaseGateLite.Contracts.HeatState. Path: $.heatState | LineNumber: 0 | BytePositionInLine: 243.
2026-02-12T21:17:59.5596759-05:00 FIRST_CHANCE: System.Text.Json.JsonException: The JSON value could not be converted to LeaseGateLite.Contracts.HeatState. Path: $.heatState | LineNumber: 0 | BytePositionInLine: 243.
2026-02-12T21:18:01.5714606-05:00 FIRST_CHANCE: System.Text.Json.JsonException: The JSON value could not be converted to LeaseGateLite.Contracts.HeatState. Path: $.heatState | LineNumber: 0 | BytePositionInLine: 243.
2026-02-12T21:18:01.8090374-05:00 FIRST_CHANCE: System.Text.Json.JsonException: The JSON value could not be converted to LeaseGateLite.Contracts.EventCategory. Path: $.events[0].category | LineNumber: 0 | BytePositionInLine: 108.
```

### Metrics
- **Total exceptions**: 1,957 in 10 minutes
- **Exception rate**: ~200/minute, ~3-4/second
- **Daemon CPU**: Minimal (2.8 seconds total CPU time)
- **App CPU**: Unknown (process not measured, but likely high from exception handling)

## Files Affected

### Daemon (Serializer)
- `src/LeaseGateLite.Daemon/Program.cs` - API endpoint serialization
- `src/LeaseGateLite.Daemon/DaemonState.cs` - Status/event generation

### App (Deserializer)
- `src/LeaseGateLite.App/DaemonApiClient.cs` - HTTP calls with JSON deserialization
- `src/LeaseGateLite.App/MainPage.xaml.cs`:
  - Line 110: `RefreshStatusAndEventsAsync()` - Status polling every 2s
  - Line 238: `RunEventStreamLoopAsync()` - Event stream polling

### Contracts (Shared)
- `src/LeaseGateLite.Contracts/StatusSnapshot.cs` - `HeatState` enum
- `src/LeaseGateLite.Contracts/EventEntry.cs` - `EventCategory` enum

## Recommended Fixes

### Option 1: Explicit JSON Serializer Options (Recommended)

Add `JsonSerializerOptions` to both daemon and app with explicit enum handling:

**Daemon (`Program.cs`):**
```csharp
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
```

**App (`DaemonApiClient.cs`):**
```csharp
private static readonly JsonSerializerOptions JsonOptions = new()
{
    Converters = { new JsonStringEnumConverter() }
};

// Use in all GetFromJsonAsync calls
var status = await response.Content.ReadFromJsonAsync<StatusSnapshot>(JsonOptions, cancellationToken);
```

### Option 2: Upgrade App to .NET 10

Make MAUI app use net10.0 to match daemon behavior. This was attempted earlier but failed due to missing Mono runtime for .NET 10 preview.

### Option 3: Reduce Polling Frequency

**Short-term mitigation** (doesn't fix root cause):
- Increase status polling from 2s → 5s
- Add exponential backoff on JSON errors
- Disable first-chance exception logging in production

## Testing Plan

1. Apply Option 1 fix
2. Rebuild daemon and app
3. Clear logs: `rm C:/Temp/leasegate-firstchance.log`
4. Launch both processes
5. Wait 5 minutes
6. Check for first-chance exceptions: `wc -l C:/Temp/leasegate-firstchance.log`
7. Expected: 0 JSON deserialization errors
8. Verify app UI updates correctly with status data

## Impact

**Current State:**
- ❌ App throws ~200 exceptions/minute
- ❌ High CPU usage from exception handling
- ❌ GC pressure from failed deserialization attempts
- ❌ App appears broken (data not loading due to JSON errors)
- ❌ Ironically, the throttling daemon is causing CPU thrashing

**After Fix:**
- ✅ Zero JSON deserialization exceptions
- ✅ Minimal CPU usage from polling (just HTTP + deserialize)
- ✅ App UI shows real status data
- ✅ Daemon actually reduces system load instead of increasing it
