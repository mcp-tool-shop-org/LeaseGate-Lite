using System.Net.Http.Json;
using LeaseGateLite.Contracts;

namespace LeaseGateLite.App;

public sealed class DaemonApiClient
{
    private readonly HttpClient _httpClient;

    public DaemonApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<StatusSnapshot?> GetStatusAsync(CancellationToken cancellationToken)
    {
        return await _httpClient.GetFromJsonAsync<StatusSnapshot>("/status", cancellationToken);
    }

    public async Task<LiteConfig?> GetConfigAsync(CancellationToken cancellationToken)
    {
        return await _httpClient.GetFromJsonAsync<LiteConfig>("/config", cancellationToken);
    }

    public async Task<LiteConfig?> GetDefaultConfigAsync(CancellationToken cancellationToken)
    {
        return await _httpClient.GetFromJsonAsync<LiteConfig>("/config/defaults", cancellationToken);
    }

    public async Task<ConfigApplyResponse?> ApplyConfigAsync(LiteConfig config, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync("/config", config, cancellationToken);
        return await response.Content.ReadFromJsonAsync<ConfigApplyResponse>(cancellationToken);
    }

    public async Task<ConfigApplyResponse?> ResetConfigAsync(bool apply, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsync($"/config/reset?apply={apply.ToString().ToLowerInvariant()}", null, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ConfigApplyResponse>(cancellationToken);
    }

    public async Task<ServiceCommandResponse?> ServiceCommandAsync(string command, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsync($"/service/{command}", null, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ServiceCommandResponse>(cancellationToken);
    }

    public async Task<AutostartStatusResponse?> GetAutostartStatusAsync(CancellationToken cancellationToken)
    {
        return await _httpClient.GetFromJsonAsync<AutostartStatusResponse>("/autostart/status", cancellationToken);
    }

    public async Task<ServiceCommandResponse?> SetAutostartAsync(bool enabled, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync("/autostart", new AutostartUpdateRequest { Enabled = enabled }, cancellationToken);
        return await response.Content.ReadFromJsonAsync<ServiceCommandResponse>(cancellationToken);
    }

    public async Task<DiagnosticsExportResponse?> ExportDiagnosticsAsync(CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsync("/diagnostics/export", null, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DiagnosticsExportResponse>(cancellationToken);
    }

    public async Task<EventTailResponse?> GetEventsAsync(int n, CancellationToken cancellationToken)
    {
        return await _httpClient.GetFromJsonAsync<EventTailResponse>($"/events/tail?n={n}", cancellationToken);
    }

    public async Task<EventStreamResponse?> GetEventsStreamAsync(long sinceId, int timeoutMs, CancellationToken cancellationToken)
    {
        return await _httpClient.GetFromJsonAsync<EventStreamResponse>($"/events/stream?sinceId={sinceId}&timeoutMs={timeoutMs}", cancellationToken);
    }

    public async Task<List<PresetDefinition>?> GetPresetsAsync(CancellationToken cancellationToken)
    {
        return await _httpClient.GetFromJsonAsync<List<PresetDefinition>>("/presets", cancellationToken);
    }

    public async Task<ProfilesSnapshotResponse?> GetProfilesAsync(CancellationToken cancellationToken)
    {
        return await _httpClient.GetFromJsonAsync<ProfilesSnapshotResponse>("/profiles", cancellationToken);
    }

    public async Task<ServiceCommandResponse?> SetAppProfileAsync(SetAppProfileRequest request, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync("/profiles/apply", request, cancellationToken);
        return await response.Content.ReadFromJsonAsync<ServiceCommandResponse>(cancellationToken);
    }

    public async Task<PresetPreviewResponse?> PreviewPresetAsync(string name, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync("/preset/preview", new PresetApplyRequest { Name = name }, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PresetPreviewResponse>(cancellationToken);
    }

    public async Task<ConfigApplyResponse?> ApplyPresetAsync(string name, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync("/preset/apply", new PresetApplyRequest { Name = name }, cancellationToken);
        return await response.Content.ReadFromJsonAsync<ConfigApplyResponse>(cancellationToken);
    }

    public async Task<ServiceCommandResponse?> SetPressureModeAsync(PressureMode mode, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync("/simulate/pressure", new SimulatePressureRequest { Mode = mode }, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ServiceCommandResponse>(cancellationToken);
    }

    public async Task<ServiceCommandResponse?> SimulateFloodAsync(int interactiveRequests, int backgroundRequests, CancellationToken cancellationToken, string clientAppId = "", string processName = "", string signature = "")
    {
        var response = await _httpClient.PostAsJsonAsync("/simulate/flood", new SimulateFloodRequest
        {
            InteractiveRequests = interactiveRequests,
            BackgroundRequests = backgroundRequests,
            ClientAppId = clientAppId,
            ProcessName = processName,
            Signature = signature
        }, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ServiceCommandResponse>(cancellationToken);
    }
}