namespace LeaseGateLite.Contracts;

public sealed class SeenClient
{
    public string ClientAppId { get; set; } = string.Empty;
    public string ProcessName { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
    public DateTimeOffset LastSeenUtc { get; set; }
}

public sealed class AppProfileOverride
{
    public string ClientAppId { get; set; } = string.Empty;
    public string PresetName { get; set; } = string.Empty;
    public int? MaxConcurrency { get; set; }
    public int? BackgroundCap { get; set; }
    public int? MaxOutputTokensClamp { get; set; }
    public int? MaxPromptTokensClamp { get; set; }
    public int? RequestsPerMinute { get; set; }
    public int? TokensPerMinute { get; set; }
}

public sealed class ProfilesSnapshotResponse
{
    public LiteConfig DefaultProfile { get; set; } = new();
    public List<SeenClient> RecentlySeenApps { get; set; } = new();
    public List<AppProfileOverride> Overrides { get; set; } = new();
}

public sealed class SetAppProfileRequest
{
    public string ClientAppId { get; set; } = string.Empty;
    public string ProcessName { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
    public string PresetName { get; set; } = string.Empty;
    public int? MaxConcurrency { get; set; }
    public int? BackgroundCap { get; set; }
    public int? MaxOutputTokensClamp { get; set; }
    public int? MaxPromptTokensClamp { get; set; }
    public int? RequestsPerMinute { get; set; }
    public int? TokensPerMinute { get; set; }
}
