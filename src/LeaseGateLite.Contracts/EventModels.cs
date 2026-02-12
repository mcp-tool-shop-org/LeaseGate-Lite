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
