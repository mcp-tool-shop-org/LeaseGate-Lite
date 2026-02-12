namespace LeaseGateLite.Contracts;

public sealed class EventEntry
{
    public long Id { get; set; }
    public DateTimeOffset TimestampUtc { get; set; }
    public EventCategory Category { get; set; } = EventCategory.Service;
    public string Level { get; set; } = "info";
    public string Message { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
}

public sealed class EventTailResponse
{
    public List<EventEntry> Events { get; set; } = new();
}

public sealed class EventStreamResponse
{
    public long LastEventId { get; set; }
    public List<EventEntry> Events { get; set; } = new();
}

public sealed class DiagnosticsExportResponse
{
    public bool Exported { get; set; }
    public string OutputPath { get; set; } = string.Empty;
    public long BytesWritten { get; set; }
    public string Message { get; set; } = string.Empty;
}

public sealed class DiagnosticsPreviewResponse
{
    public bool IncludePaths { get; set; }
    public bool IncludeVerbose { get; set; }
    public List<string> IncludedSections { get; set; } = new();
    public List<string> RedactionRules { get; set; } = new();
    public string Summary { get; set; } = string.Empty;
}

public sealed class StatusSampleEntry
{
    public DateTimeOffset TimestampUtc { get; set; }
    public HeatState HeatState { get; set; } = HeatState.Calm;
    public int EffectiveConcurrency { get; set; }
    public int ActiveCalls { get; set; }
    public int InteractiveQueueDepth { get; set; }
    public int BackgroundQueueDepth { get; set; }
    public int CpuPercent { get; set; }
    public int AvailableRamPercent { get; set; }
    public ThrottleReason LastThrottleReason { get; set; } = ThrottleReason.None;
}
