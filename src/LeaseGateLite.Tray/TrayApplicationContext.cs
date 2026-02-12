using System.Diagnostics;
using System.Net.Http.Json;
using System.Windows.Forms;
using LeaseGateLite.Contracts;

namespace LeaseGateLite.Tray;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly HttpClient _httpClient;
    private readonly NotifyIcon _notifyIcon;
    private readonly System.Windows.Forms.Timer _pollTimer;
    private readonly ToolStripMenuItem _pauseBackgroundItem;
    private readonly ToolStripMenuItem _notificationsItem;
    private DateTimeOffset _lastNotificationUtc = DateTimeOffset.MinValue;

    public TrayApplicationContext()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:5177"),
            Timeout = TimeSpan.FromSeconds(4)
        };
        _httpClient.DefaultRequestHeaders.Add("X-Client-AppId", "LeaseGateLite.Tray");
        _httpClient.DefaultRequestHeaders.Add("X-Process-Name", "LeaseGateLite.Tray");

        var menu = new ContextMenuStrip();
        menu.Items.Add("Open Control Panel", null, (_, _) => OpenControlPanel());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Preset: Quiet", null, async (_, _) => await ApplyPresetAsync("Quiet"));
        menu.Items.Add("Preset: Balanced", null, async (_, _) => await ApplyPresetAsync("Balanced"));
        menu.Items.Add("Preset: Performance", null, async (_, _) => await ApplyPresetAsync("Performance"));

        _pauseBackgroundItem = new ToolStripMenuItem("Pause background work")
        {
            CheckOnClick = true
        };
        _pauseBackgroundItem.Click += async (_, _) => await SetPauseBackgroundAsync(_pauseBackgroundItem.Checked);
        menu.Items.Add(_pauseBackgroundItem);

        _notificationsItem = new ToolStripMenuItem("Enable notifications")
        {
            CheckOnClick = true
        };
        _notificationsItem.Click += async (_, _) => await SetNotificationsEnabledAsync(_notificationsItem.Checked);
        menu.Items.Add(_notificationsItem);

        menu.Items.Add("Exit daemon", null, async (_, _) => await ExitDaemonAsync());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit tray", null, (_, _) => ExitThread());

        _notifyIcon = new NotifyIcon
        {
            Visible = true,
            Icon = SystemIcons.Information,
            Text = "LeaseGate Lite",
            ContextMenuStrip = menu
        };

        _pollTimer = new System.Windows.Forms.Timer { Interval = 2000 };
        _pollTimer.Tick += async (_, _) => await RefreshStatusAsync();
        _pollTimer.Start();

        _ = RefreshStatusAsync();
    }

    protected override void ExitThreadCore()
    {
        _pollTimer.Stop();
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _httpClient.Dispose();
        _pollTimer.Dispose();
        base.ExitThreadCore();
    }

    private async Task RefreshStatusAsync()
    {
        try
        {
            var status = await _httpClient.GetFromJsonAsync<StatusSnapshot>("/status");
            if (status is null)
            {
                return;
            }

            _notifyIcon.Icon = status.HeatState switch
            {
                HeatState.Calm => SystemIcons.Information,
                HeatState.Warm => SystemIcons.Warning,
                _ => SystemIcons.Error
            };

            var queueDepth = status.InteractiveQueueDepth + status.BackgroundQueueDepth;
            _notifyIcon.Text = $"LeaseGate Lite: {status.HeatState} | Eff {status.EffectiveConcurrency} | Q {queueDepth}";
            _pauseBackgroundItem.Checked = status.BackgroundPaused;

            var notificationSettings = await _httpClient.GetFromJsonAsync<NotificationsSettingsResponse>("/notifications");
            _notificationsItem.Checked = notificationSettings?.Enabled == true;

            MaybeNotify(status, queueDepth, notificationSettings?.Enabled == true);
        }
        catch
        {
            _notifyIcon.Icon = SystemIcons.Application;
            _notifyIcon.Text = "LeaseGate Lite: daemon unreachable";
        }
    }

    private void MaybeNotify(StatusSnapshot status, int queueDepth, bool enabled)
    {
        if (!enabled)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        if (now - _lastNotificationUtc < TimeSpan.FromMinutes(2))
        {
            return;
        }

        if (status.LastThrottleReason == ThrottleReason.CpuPressure)
        {
            _notifyIcon.ShowBalloonTip(2500, "LeaseGate Lite", "Clamping due to high CPU", ToolTipIcon.Warning);
            _lastNotificationUtc = now;
            return;
        }

        if (queueDepth > 0)
        {
            _notifyIcon.ShowBalloonTip(2500, "LeaseGate Lite", "Queueing requests", ToolTipIcon.Info);
            _lastNotificationUtc = now;
        }
    }

    private async Task ApplyPresetAsync(string preset)
    {
        var response = await _httpClient.PostAsJsonAsync("/preset/apply", new PresetApplyRequest { Name = preset });
        response.EnsureSuccessStatusCode();
    }

    private async Task SetPauseBackgroundAsync(bool paused)
    {
        var response = await _httpClient.PostAsync($"/service/pause-background?paused={paused.ToString().ToLowerInvariant()}", null);
        response.EnsureSuccessStatusCode();
    }

    private async Task SetNotificationsEnabledAsync(bool enabled)
    {
        var response = await _httpClient.PostAsJsonAsync("/notifications", new NotificationsUpdateRequest { Enabled = enabled });
        response.EnsureSuccessStatusCode();
    }

    private async Task ExitDaemonAsync()
    {
        var result = MessageBox.Show("Stopping the daemon will pause throttling for all apps until it is started again. Continue?", "Stop daemon", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (result != DialogResult.Yes)
        {
            return;
        }

        var response = await _httpClient.PostAsync("/service/exit", null);
        response.EnsureSuccessStatusCode();
    }

    private static void OpenControlPanel()
    {
        var baseDir = AppContext.BaseDirectory;
        var candidate = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "LeaseGateLite.App", "bin", "Debug", "net10.0-windows10.0.19041.0", "win-x64", "LeaseGateLite.App.exe"));
        if (File.Exists(candidate))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = candidate,
                UseShellExecute = true
            });
            return;
        }

        MessageBox.Show("Control panel executable not found. Launch LeaseGateLite.App manually.", "LeaseGate Lite", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
