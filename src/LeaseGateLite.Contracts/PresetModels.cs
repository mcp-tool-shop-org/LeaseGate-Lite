namespace LeaseGateLite.Contracts;

public sealed class PresetDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public LiteConfig Config { get; set; } = new();
}

public sealed class PresetApplyRequest
{
    public string Name { get; set; } = string.Empty;
}

public sealed class PresetDiffItem
{
    public string Field { get; set; } = string.Empty;
    public string Before { get; set; } = string.Empty;
    public string After { get; set; } = string.Empty;
}

public sealed class PresetPreviewResponse
{
    public string Name { get; set; } = string.Empty;
    public List<PresetDiffItem> Diffs { get; set; } = new();
}
