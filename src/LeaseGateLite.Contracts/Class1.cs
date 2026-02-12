namespace LeaseGateLite.Contracts;

public enum LeaseGateLiteMode
{
	Embedded,
	Daemon
}

public enum HeatState
{
	Calm,
	Warm,
	Spicy
}

public enum CooldownBehavior
{
	Off,
	Mild,
	Aggressive
}

public enum OverflowBehavior
{
	TrimOldest,
	Deny,
	QueueOnly
}

public sealed class LiteConfig
{
	public int MaxConcurrency { get; set; } = 8;
	public int InteractiveReserve { get; set; } = 2;
	public int BackgroundCap { get; set; } = 6;
	public CooldownBehavior CooldownBehavior { get; set; } = CooldownBehavior.Mild;
	public int SoftThresholdPercent { get; set; } = 70;
	public int HardThresholdPercent { get; set; } = 90;
	public int RecoveryRatePercent { get; set; } = 20;
	public int SmoothingPercent { get; set; } = 40;
	public int MaxOutputTokensClamp { get; set; } = 1024;
	public int MaxPromptTokensClamp { get; set; } = 4096;
	public OverflowBehavior OverflowBehavior { get; set; } = OverflowBehavior.TrimOldest;
	public int MaxRetries { get; set; } = 2;
	public int RetryBackoffMs { get; set; } = 500;
	public int RequestsPerMinute { get; set; } = 120;
	public int TokensPerMinute { get; set; } = 120_000;
	public int BurstAllowance { get; set; } = 12;
}

public sealed class StatusSnapshot
{
	public bool Connected { get; set; }
	public bool DaemonRunning { get; set; }
	public string DaemonVersion { get; set; } = "0.1.0";
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
	public string LastThrottleReason { get; set; } = "none";
	public bool AdaptiveClampActive { get; set; }
}

public sealed class ServiceCommandResponse
{
	public bool Success { get; set; }
	public string Message { get; set; } = string.Empty;
}

public sealed class EventEntry
{
	public DateTimeOffset TimestampUtc { get; set; }
	public string Level { get; set; } = "info";
	public string Message { get; set; } = string.Empty;
}

public sealed class EventTailResponse
{
	public List<EventEntry> Events { get; set; } = new();
}

public sealed class DiagnosticsExportResponse
{
	public bool Exported { get; set; }
	public string OutputPath { get; set; } = string.Empty;
	public long BytesWritten { get; set; }
	public string Message { get; set; } = string.Empty;
}
