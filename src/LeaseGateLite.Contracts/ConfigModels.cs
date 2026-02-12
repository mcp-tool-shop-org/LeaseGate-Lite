namespace LeaseGateLite.Contracts;

public sealed class LiteConfig
{
    public int ConfigVersion { get; set; } = 1;
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

public sealed class ValidationError
{
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public sealed class ConfigApplyResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public LiteConfig AppliedConfig { get; set; } = new();
    public List<ValidationError> Errors { get; set; } = new();
}

public sealed class ServiceCommandResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public sealed class SimulatePressureRequest
{
    public PressureMode Mode { get; set; } = PressureMode.Normal;
}

public sealed class SimulateFloodRequest
{
    public int InteractiveRequests { get; set; } = 20;
    public int BackgroundRequests { get; set; } = 20;
}
