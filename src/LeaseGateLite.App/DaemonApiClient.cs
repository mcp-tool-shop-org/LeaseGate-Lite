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
}