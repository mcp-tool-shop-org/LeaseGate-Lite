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
}
