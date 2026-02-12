namespace LeaseGateLite.Contracts;

public sealed class StatusSnapshot
{
    public bool Connected { get; set; }
    public bool DaemonRunning { get; set; }
    public string DaemonVersion { get; set; } = "0.2.0-lite";
    public TimeSpan Uptime { get; set; }
    public string Endpoint { get; set; } = "http://localhost:5177";
    public string ConfigFilePath { get; set; } = string.Empty;
    public HeatState HeatState { get; set; } = HeatState.Calm;
    public int ActiveCalls { get; set; }
    public int InteractiveQueueDepth { get; set; }
    public int BackgroundQueueDepth { get; set; }
    public int EffectiveConcurrency { get; set; }
    public int CpuPercent { get; set; }
    public int AvailableRamPercent { get; set; }
    public ThrottleReason LastThrottleReason { get; set; } = ThrottleReason.None;
    public bool AdaptiveClampActive { get; set; }
    public PressureMode PressureMode { get; set; } = PressureMode.Normal;
    public bool DegradedMode { get; set; }
    public string DegradedReason { get; set; } = string.Empty;
    public bool BackgroundPaused { get; set; }
    public List<ThrottleReasonEntry> RecentThrottleReasons { get; set; } = new();
}

public sealed class ThrottleReasonEntry
{
    public DateTimeOffset TimestampUtc { get; set; }
    public ThrottleReason Reason { get; set; } = ThrottleReason.None;
    public string Detail { get; set; } = string.Empty;
}

public sealed class AutostartStatusResponse
{
    public bool Supported { get; set; }
    public bool Enabled { get; set; }
    public string Mechanism { get; set; } = "registry-run";
    public string Command { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public sealed class AutostartUpdateRequest
{
    public bool Enabled { get; set; }
}
