using System.Text.Json;
using LeaseGateLite.Contracts;

namespace LeaseGateLite.App;

public partial class MainPage : ContentPage
{
	private const string CustomPresetKey = "leasegatelite.custom.preset";
	private readonly DaemonApiClient _daemonApiClient;
	private readonly Dictionary<string, Border> _sectionCards;
	private StatusSnapshot? _latestStatus;
	private LiteConfig _currentConfig = new();
	private LiteConfig _draftConfig = new();
	private bool _ignoreConfigEvents;
	private bool _hasPendingChanges;
	private bool _pauseEvents;
	private bool _ignoreAutostartToggle;
	private long _lastEventId;
	private readonly List<EventEntry> _eventBuffer = new();

	public MainPage(DaemonApiClient daemonApiClient)
	{
		InitializeComponent();
		_daemonApiClient = daemonApiClient;
		_sectionCards = new Dictionary<string, Border>(StringComparer.OrdinalIgnoreCase)
		{
			["Service"] = CardService,
			["LiveStatus"] = CardLiveStatus,
			["Concurrency"] = CardConcurrency,
			["Adaptive"] = CardAdaptive,
			["Shaping"] = CardShaping,
			["RateLimits"] = CardRateLimits,
			["Presets"] = CardPresets,
			["Diagnostics"] = CardDiagnostics,
			["Audit"] = CardAudit
		};

		Loaded += OnLoaded;
		SizeChanged += OnSizeChanged;
	}

	private async void OnLoaded(object? sender, EventArgs e)
	{
		ModePicker.SelectedIndex = 1;
		OnSizeChanged(sender, e);
		await RefreshAllAsync();

		Dispatcher.StartTimer(TimeSpan.FromSeconds(2), () =>
		{
			_ = RefreshStatusAndEventsAsync();
			return true;
		});

		_ = RunEventStreamLoopAsync();
	}

	private void OnSizeChanged(object? sender, EventArgs e)
	{
		BodyGrid.ColumnDefinitions.Clear();
		if (Width < 1080)
		{
			BodyGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
			BodyGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
		}
		else
		{
			BodyGrid.ColumnDefinitions.Add(new ColumnDefinition(320));
			BodyGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
		}
	}

	private async Task RefreshAllAsync()
	{
		await RefreshStatusAndEventsAsync();
		await RefreshConfigAsync();
		await RefreshAutostartAsync();
	}

	private async Task RefreshAutostartAsync()
	{
		try
		{
			var status = await _daemonApiClient.GetAutostartStatusAsync(CancellationToken.None);
			if (status is null)
			{
				return;
			}

			_ignoreAutostartToggle = true;
			StartOnLoginSwitch.IsEnabled = status.Supported;
			StartOnLoginSwitch.IsToggled = status.Enabled;
			StartOnLoginStatusLabel.Text = status.Message;
		}
		catch
		{
			StartOnLoginStatusLabel.Text = "Autostart unavailable.";
		}
		finally
		{
			_ignoreAutostartToggle = false;
		}
	}

	private async Task RefreshStatusAndEventsAsync()
	{
		try
		{
			var status = await _daemonApiClient.GetStatusAsync(CancellationToken.None);
			if (status is not null)
			{
				_latestStatus = status;
				UpdateStatusUi(status);
			}

			if (_eventBuffer.Count == 0)
			{
				var eventTail = await _daemonApiClient.GetEventsAsync(200, CancellationToken.None);
				if (eventTail is not null)
				{
					_eventBuffer.Clear();
					_eventBuffer.AddRange(eventTail.Events);
					_lastEventId = _eventBuffer.LastOrDefault()?.Id ?? 0;
					RenderEventBuffer();
				}
			}
		}
		catch
		{
			ConnectionStateLabel.Text = "Disconnected";
			StatusDot.Color = Color.FromArgb("#B0B0B0");
		}
	}

	private async Task RunEventStreamLoopAsync()
	{
		while (true)
		{
			try
			{
				if (_pauseEvents)
				{
					await Task.Delay(500);
					continue;
				}

				var stream = await _daemonApiClient.GetEventsStreamAsync(_lastEventId, 2500, CancellationToken.None);
				if (stream is not null && stream.Events.Count > 0)
				{
					MainThread.BeginInvokeOnMainThread(() =>
					{
						foreach (var entry in stream.Events)
						{
							_eventBuffer.Add(entry);
						}

						if (_eventBuffer.Count > 1200)
						{
							_eventBuffer.RemoveRange(0, _eventBuffer.Count - 1200);
						}

						_lastEventId = Math.Max(_lastEventId, stream.LastEventId);
						RenderEventBuffer();
					});
				}
			}
			catch
			{
				await Task.Delay(1000);
			}
		}
	}

	private void RenderEventBuffer()
	{
		var includeLease = FilterLeaseSwitch.IsToggled;
		var includePressure = FilterPressureSwitch.IsToggled;
		var includeConfig = FilterConfigSwitch.IsToggled;

		var visible = _eventBuffer.Where(e =>
		{
			return e.Category switch
			{
				EventCategory.Lease => includeLease,
				EventCategory.Pressure => includePressure,
				EventCategory.Config => includeConfig,
				_ => true
			};
		}).TakeLast(200).ToList();

		EventsEditor.Text = string.Join(Environment.NewLine,
			visible.Select(e => $"{e.TimestampUtc:HH:mm:ss} [{e.Category}] [{e.Level}] {e.Message} {e.Detail}"));
	}

	private async Task RefreshConfigAsync()
	{
		try
		{
			var config = await _daemonApiClient.GetConfigAsync(CancellationToken.None);
			if (config is null)
			{
				return;
			}

			_currentConfig = CloneConfig(config);
			_draftConfig = CloneConfig(config);
			ApplyConfigToControls(_draftConfig);
			UpdateControlLabels();
			SetPending(false);
			ConfigStatusLabel.Text = "Config synced with daemon.";
		}
		finally
		{
			_ignoreConfigEvents = false;
		}
	}

	private void UpdateStatusUi(StatusSnapshot status)
	{
		ConnectionStateLabel.Text = status.Connected ? "Connected" : "Disconnected";
		EndpointLabel.Text = status.Endpoint;
		VersionUptimeLabel.Text = $"Version: {status.DaemonVersion}    Uptime: {status.Uptime:hh\\:mm\\:ss}";
		HeatStateLabel.Text = status.HeatState.ToString();
		ClampStateLabel.Text = status.AdaptiveClampActive ? "Adaptive clamp active" : "Clamp inactive";
		LiveNumbersLabel.Text = $"Active: {status.ActiveCalls}   Queue: {status.InteractiveQueueDepth}/{status.BackgroundQueueDepth}   Effective concurrency: {status.EffectiveConcurrency}";
		PressureLabel.Text = $"CPU: {status.CpuPercent}%   Available RAM: {status.AvailableRamPercent}%";
		LastReasonLabel.Text = $"Last throttle reason: {status.LastThrottleReason}";
		EffectiveConcurrencyPreviewLabel.Text = $"Effective concurrency right now: {status.EffectiveConcurrency}";

		StatusDot.Color = status.HeatState switch
		{
			HeatState.Calm => Color.FromArgb("#3CB371"),
			HeatState.Warm => Color.FromArgb("#E8B100"),
			_ => Color.FromArgb("#D9534F")
		};
	}

	private void UpdateControlLabels()
	{
		MaxConcurrencyLabel.Text = $"Max concurrency (base): {(int)MaxConcurrencySlider.Value}";
		InteractiveReserveLabel.Text = $"Interactive reserve: {(int)InteractiveReserveSlider.Value}";
		BackgroundCapLabel.Text = $"Background cap: {(int)BackgroundCapSlider.Value}";
		SoftThresholdLabel.Text = $"Soft threshold: {(int)SoftThresholdSlider.Value}%";
		HardThresholdLabel.Text = $"Hard threshold: {(int)HardThresholdSlider.Value}%";
		RecoveryRateLabel.Text = $"Recovery rate: {(int)RecoveryRateSlider.Value}%";
		SmoothingLabel.Text = $"Smoothing: {(int)SmoothingSlider.Value}%";
		MaxOutputTokensLabel.Text = $"Max output tokens clamp: {(int)MaxOutputTokensSlider.Value}";
		MaxPromptTokensLabel.Text = $"Max prompt/context tokens clamp: {(int)MaxPromptTokensSlider.Value}";
		MaxRetriesLabel.Text = $"Max retries: {(int)MaxRetriesSlider.Value}";
		RetryBackoffLabel.Text = $"Retry backoff: {(int)RetryBackoffSlider.Value} ms";
		RequestsPerMinuteLabel.Text = $"Requests/min: {(int)RequestsPerMinuteSlider.Value}";
		TokensPerMinuteLabel.Text = $"Tokens/min: {(int)TokensPerMinuteSlider.Value}";
		BurstAllowanceLabel.Text = $"Burst allowance: {(int)BurstAllowanceSlider.Value}";
	}

	private LiteConfig BuildConfigFromControls()
	{
		return new LiteConfig
		{
			ConfigVersion = 1,
			MaxConcurrency = (int)Math.Round(MaxConcurrencySlider.Value),
			InteractiveReserve = (int)Math.Round(InteractiveReserveSlider.Value),
			BackgroundCap = (int)Math.Round(BackgroundCapSlider.Value),
			CooldownBehavior = Enum.TryParse<CooldownBehavior>(CooldownBehaviorPicker.SelectedItem?.ToString(), true, out var cooldown) ? cooldown : CooldownBehavior.Mild,
			SoftThresholdPercent = (int)Math.Round(SoftThresholdSlider.Value),
			HardThresholdPercent = (int)Math.Round(HardThresholdSlider.Value),
			RecoveryRatePercent = (int)Math.Round(RecoveryRateSlider.Value),
			SmoothingPercent = (int)Math.Round(SmoothingSlider.Value),
			MaxOutputTokensClamp = (int)Math.Round(MaxOutputTokensSlider.Value),
			MaxPromptTokensClamp = (int)Math.Round(MaxPromptTokensSlider.Value),
			OverflowBehavior = Enum.TryParse<OverflowBehavior>(OverflowBehaviorPicker.SelectedItem?.ToString(), true, out var overflowBehavior) ? overflowBehavior : OverflowBehavior.TrimOldest,
			MaxRetries = (int)Math.Round(MaxRetriesSlider.Value),
			RetryBackoffMs = (int)Math.Round(RetryBackoffSlider.Value),
			RequestsPerMinute = (int)Math.Round(RequestsPerMinuteSlider.Value),
			TokensPerMinute = (int)Math.Round(TokensPerMinuteSlider.Value),
			BurstAllowance = (int)Math.Round(BurstAllowanceSlider.Value)
		};
	}

	private void SetPending(bool pending)
	{
		_hasPendingChanges = pending;
		PendingPill.IsVisible = pending;
	}

	private void ApplyConfigToControls(LiteConfig config)
	{
		_ignoreConfigEvents = true;
		MaxConcurrencySlider.Value = config.MaxConcurrency;
		InteractiveReserveSlider.Value = config.InteractiveReserve;
		BackgroundCapSlider.Value = config.BackgroundCap;
		CooldownBehaviorPicker.SelectedItem = config.CooldownBehavior.ToString();
		SoftThresholdSlider.Value = config.SoftThresholdPercent;
		HardThresholdSlider.Value = config.HardThresholdPercent;
		RecoveryRateSlider.Value = config.RecoveryRatePercent;
		SmoothingSlider.Value = config.SmoothingPercent;
		MaxOutputTokensSlider.Value = config.MaxOutputTokensClamp;
		MaxPromptTokensSlider.Value = config.MaxPromptTokensClamp;
		OverflowBehaviorPicker.SelectedItem = config.OverflowBehavior.ToString();
		MaxRetriesSlider.Value = config.MaxRetries;
		RetryBackoffSlider.Value = config.RetryBackoffMs;
		RequestsPerMinuteSlider.Value = config.RequestsPerMinute;
		TokensPerMinuteSlider.Value = config.TokensPerMinute;
		BurstAllowanceSlider.Value = config.BurstAllowance;
		_ignoreConfigEvents = false;
	}

	private static LiteConfig CloneConfig(LiteConfig config)
	{
		return new LiteConfig
		{
			ConfigVersion = config.ConfigVersion,
			MaxConcurrency = config.MaxConcurrency,
			InteractiveReserve = config.InteractiveReserve,
			BackgroundCap = config.BackgroundCap,
			CooldownBehavior = config.CooldownBehavior,
			SoftThresholdPercent = config.SoftThresholdPercent,
			HardThresholdPercent = config.HardThresholdPercent,
			RecoveryRatePercent = config.RecoveryRatePercent,
			SmoothingPercent = config.SmoothingPercent,
			MaxOutputTokensClamp = config.MaxOutputTokensClamp,
			MaxPromptTokensClamp = config.MaxPromptTokensClamp,
			OverflowBehavior = config.OverflowBehavior,
			MaxRetries = config.MaxRetries,
			RetryBackoffMs = config.RetryBackoffMs,
			RequestsPerMinute = config.RequestsPerMinute,
			TokensPerMinute = config.TokensPerMinute,
			BurstAllowance = config.BurstAllowance
		};
	}

	private async Task ExecuteServiceCommandAsync(string command)
	{
		await _daemonApiClient.ServiceCommandAsync(command, CancellationToken.None);
		await RefreshStatusAndEventsAsync();
	}

	private async void OnReconnectClicked(object? sender, EventArgs e)
	{
		await RefreshAllAsync();
	}

	private async void OnStartOnLoginToggled(object? sender, ToggledEventArgs e)
	{
		if (_ignoreAutostartToggle)
		{
			return;
		}

		var result = await _daemonApiClient.SetAutostartAsync(e.Value, CancellationToken.None);
		StartOnLoginStatusLabel.Text = result?.Message ?? "Autostart update failed.";
		await RefreshAutostartAsync();
	}

	private async void OnStartClicked(object? sender, EventArgs e)
	{
		await ExecuteServiceCommandAsync("start");
	}

	private async void OnStopClicked(object? sender, EventArgs e)
	{
		await ExecuteServiceCommandAsync("stop");
	}

	private async void OnRestartClicked(object? sender, EventArgs e)
	{
		await ExecuteServiceCommandAsync("restart");
	}

	private async void OnApplyClicked(object? sender, EventArgs e)
	{
		_draftConfig = BuildConfigFromControls();
		var response = await _daemonApiClient.ApplyConfigAsync(_draftConfig, CancellationToken.None);
		if (response is null)
		{
			ConfigStatusLabel.Text = "Apply failed: no response";
			return;
		}

		if (!response.Success)
		{
			var first = response.Errors.FirstOrDefault();
			ConfigStatusLabel.Text = first is null ? "Apply failed." : $"Apply failed: {first.Field} - {first.Message}";
			return;
		}

		_currentConfig = CloneConfig(response.AppliedConfig);
		_draftConfig = CloneConfig(response.AppliedConfig);
		ApplyConfigToControls(_currentConfig);
		SetPending(false);
		ConfigStatusLabel.Text = "Config applied atomically.";
		await RefreshStatusAndEventsAsync();
	}

	private void OnRevertClicked(object? sender, EventArgs e)
	{
		_draftConfig = CloneConfig(_currentConfig);
		ApplyConfigToControls(_draftConfig);
		UpdateControlLabels();
		SetPending(false);
		ConfigStatusLabel.Text = "Draft reverted to last confirmed config.";
	}

	private async void OnOpenConfigClicked(object? sender, EventArgs e)
	{
		if (string.IsNullOrWhiteSpace(_latestStatus?.ConfigFilePath))
		{
			return;
		}

		await Launcher.Default.OpenAsync(new OpenFileRequest
		{
			File = new ReadOnlyFile(_latestStatus.ConfigFilePath)
		});
	}

	private async void OnResetDefaultsClicked(object? sender, EventArgs e)
	{
		var response = await _daemonApiClient.ResetConfigAsync(false, CancellationToken.None);
		if (response is null)
		{
			ConfigStatusLabel.Text = "Defaults request failed.";
			return;
		}

		_draftConfig = CloneConfig(response.AppliedConfig);
		ApplyConfigToControls(_draftConfig);
		UpdateControlLabels();
		SetPending(true);
		ConfigStatusLabel.Text = "Defaults loaded into draft. Click Apply to commit.";
	}

	private async void OnExportDiagnosticsClicked(object? sender, EventArgs e)
	{
		var response = await _daemonApiClient.ExportDiagnosticsAsync(CancellationToken.None);
		DiagnosticsPathLabel.Text = response is null ? "Diagnostics export failed." : $"Exported: {response.OutputPath} ({response.BytesWritten} bytes)";
		await RefreshStatusAndEventsAsync();
	}

	private async void OnCopySummaryClicked(object? sender, EventArgs e)
	{
		if (_latestStatus is null)
		{
			return;
		}

		var summary = $"Heat={_latestStatus.HeatState}; Active={_latestStatus.ActiveCalls}; Queue={_latestStatus.InteractiveQueueDepth}/{_latestStatus.BackgroundQueueDepth}; EffectiveConcurrency={_latestStatus.EffectiveConcurrency}; CPU={_latestStatus.CpuPercent}%; RAM={_latestStatus.AvailableRamPercent}%; Reason={_latestStatus.LastThrottleReason}";
		await Clipboard.Default.SetTextAsync(summary);
		DiagnosticsPathLabel.Text = "Status summary copied to clipboard.";
	}

	private void OnPauseEventsClicked(object? sender, EventArgs e)
	{
		_pauseEvents = !_pauseEvents;
		PauseEventsButton.Text = _pauseEvents ? "Resume" : "Pause";
		DiagnosticsPathLabel.Text = _pauseEvents ? "Live event tail paused." : "Live event tail resumed.";
	}

	private async void OnCopyLast50Clicked(object? sender, EventArgs e)
	{
		var includeLease = FilterLeaseSwitch.IsToggled;
		var includePressure = FilterPressureSwitch.IsToggled;
		var includeConfig = FilterConfigSwitch.IsToggled;

		var text = string.Join(Environment.NewLine,
			_eventBuffer.Where(e =>
			{
				return e.Category switch
				{
					EventCategory.Lease => includeLease,
					EventCategory.Pressure => includePressure,
					EventCategory.Config => includeConfig,
					_ => true
				};
			}).TakeLast(50).Select(e => $"{e.TimestampUtc:HH:mm:ss} [{e.Category}] [{e.Level}] {e.Message} {e.Detail}"));

		await Clipboard.Default.SetTextAsync(text);
		DiagnosticsPathLabel.Text = "Copied last 50 events.";
	}

	private void OnEventFilterToggled(object? sender, ToggledEventArgs e)
	{
		RenderEventBuffer();
	}

	private async void OnChecklistJumpClicked(object? sender, EventArgs e)
	{
		if (sender is not Button button || button.CommandParameter is not string key)
		{
			return;
		}

		if (!_sectionCards.TryGetValue(key, out var card))
		{
			return;
		}

		await ControlScroll.ScrollToAsync(card, ScrollToPosition.Start, true);
		var original = card.BackgroundColor;
		card.BackgroundColor = Color.FromArgb("#E8F0FE");
		await Task.Delay(550);
		card.BackgroundColor = original;
	}

	private void OnConfigChanged(object? sender, EventArgs e)
	{
		if (_ignoreConfigEvents)
		{
			return;
		}

		_draftConfig = BuildConfigFromControls();
		UpdateControlLabels();
		SetPending(!ConfigsEqual(_draftConfig, _currentConfig));
		if (_hasPendingChanges)
		{
			ConfigStatusLabel.Text = "Draft changed. Apply to commit or Revert to discard.";
		}
	}

	private static bool ConfigsEqual(LiteConfig left, LiteConfig right)
	{
		return left.ConfigVersion == right.ConfigVersion
		       && left.MaxConcurrency == right.MaxConcurrency
		       && left.InteractiveReserve == right.InteractiveReserve
		       && left.BackgroundCap == right.BackgroundCap
		       && left.CooldownBehavior == right.CooldownBehavior
		       && left.SoftThresholdPercent == right.SoftThresholdPercent
		       && left.HardThresholdPercent == right.HardThresholdPercent
		       && left.RecoveryRatePercent == right.RecoveryRatePercent
		       && left.SmoothingPercent == right.SmoothingPercent
		       && left.MaxOutputTokensClamp == right.MaxOutputTokensClamp
		       && left.MaxPromptTokensClamp == right.MaxPromptTokensClamp
		       && left.OverflowBehavior == right.OverflowBehavior
		       && left.MaxRetries == right.MaxRetries
		       && left.RetryBackoffMs == right.RetryBackoffMs
		       && left.RequestsPerMinute == right.RequestsPerMinute
		       && left.TokensPerMinute == right.TokensPerMinute
		       && left.BurstAllowance == right.BurstAllowance;
	}

	private async Task ApplyDaemonPresetAsync(string presetName)
	{
		var preview = await _daemonApiClient.PreviewPresetAsync(presetName, CancellationToken.None);
		if (preview is not null)
		{
			PresetDiffLabel.Text = preview.Diffs.Count == 0
				? "No config changes."
				: string.Join(Environment.NewLine, preview.Diffs.Select(d => $"{d.Field}: {d.Before} → {d.After}"));
		}

		var response = await _daemonApiClient.ApplyPresetAsync(presetName, CancellationToken.None);
		if (response is null || !response.Success)
		{
			PresetStatusLabel.Text = $"Preset apply failed: {presetName}";
			return;
		}

		_currentConfig = CloneConfig(response.AppliedConfig);
		_draftConfig = CloneConfig(response.AppliedConfig);
		ApplyConfigToControls(_currentConfig);
		UpdateControlLabels();
		SetPending(false);
		PresetStatusLabel.Text = $"Applied preset: {presetName}";
		await RefreshStatusAndEventsAsync();
	}

	private async void OnQuietPresetClicked(object? sender, EventArgs e)
	{
		await ApplyDaemonPresetAsync("Quiet");
	}

	private async void OnBalancedPresetClicked(object? sender, EventArgs e)
	{
		await ApplyDaemonPresetAsync("Balanced");
	}

	private async void OnPerformancePresetClicked(object? sender, EventArgs e)
	{
		await ApplyDaemonPresetAsync("Performance");
	}

	private void OnSaveCustomPresetClicked(object? sender, EventArgs e)
	{
		var json = JsonSerializer.Serialize(BuildConfigFromControls());
		Preferences.Default.Set(CustomPresetKey, json);
		PresetStatusLabel.Text = "Custom preset saved.";
	}

	private void OnLoadCustomPresetClicked(object? sender, EventArgs e)
	{
		var json = Preferences.Default.Get(CustomPresetKey, string.Empty);
		if (string.IsNullOrWhiteSpace(json))
		{
			PresetStatusLabel.Text = "No custom preset found.";
			return;
		}

		var config = JsonSerializer.Deserialize<LiteConfig>(json);
		if (config is null)
		{
			PresetStatusLabel.Text = "Custom preset is invalid.";
			return;
		}

		_ignoreConfigEvents = true;
		MaxConcurrencySlider.Value = config.MaxConcurrency;
		InteractiveReserveSlider.Value = config.InteractiveReserve;
		BackgroundCapSlider.Value = config.BackgroundCap;
		CooldownBehaviorPicker.SelectedItem = config.CooldownBehavior.ToString();
		SoftThresholdSlider.Value = config.SoftThresholdPercent;
		HardThresholdSlider.Value = config.HardThresholdPercent;
		RecoveryRateSlider.Value = config.RecoveryRatePercent;
		SmoothingSlider.Value = config.SmoothingPercent;
		MaxOutputTokensSlider.Value = config.MaxOutputTokensClamp;
		MaxPromptTokensSlider.Value = config.MaxPromptTokensClamp;
		OverflowBehaviorPicker.SelectedItem = config.OverflowBehavior.ToString();
		MaxRetriesSlider.Value = config.MaxRetries;
		RetryBackoffSlider.Value = config.RetryBackoffMs;
		RequestsPerMinuteSlider.Value = config.RequestsPerMinute;
		TokensPerMinuteSlider.Value = config.TokensPerMinute;
		BurstAllowanceSlider.Value = config.BurstAllowance;
		_ignoreConfigEvents = false;

		UpdateControlLabels();
		SetPending(true);
		PresetStatusLabel.Text = "Custom preset loaded. Click Apply to commit.";
	}

	private async void OnRunAuditHarnessClicked(object? sender, EventArgs e)
	{
		var lines = new List<string>();
		void Record(string name, bool pass, string evidence)
		{
			lines.Add($"{(pass ? "✅" : "❌")} {name} -- {evidence}");
		}

		try
		{
			await RefreshAllAsync();
			Record("connect/reconnect", _latestStatus?.Connected == true, _latestStatus?.Connected == true ? "status connected" : "status disconnected");

			await _daemonApiClient.ServiceCommandAsync("restart", CancellationToken.None);
			await RefreshStatusAndEventsAsync();
			Record("start/stop/restart", _latestStatus?.DaemonRunning == true, _latestStatus?.DaemonRunning == true ? "daemon running after restart" : "daemon not running");

			var original = await _daemonApiClient.GetConfigAsync(CancellationToken.None) ?? new LiteConfig();
			var mutated = CloneConfig(original);
			mutated.MaxConcurrency = Math.Clamp(original.MaxConcurrency == 1 ? 2 : original.MaxConcurrency - 1, 1, 32);
			var applied = await _daemonApiClient.ApplyConfigAsync(mutated, CancellationToken.None);
			var reverted = await _daemonApiClient.ApplyConfigAsync(original, CancellationToken.None);
			Record("apply config + revert", (applied?.Success ?? false) && (reverted?.Success ?? false), "apply and revert roundtrip succeeded");

			var floodConfig = CloneConfig(original);
			floodConfig.MaxConcurrency = 1;
			floodConfig.InteractiveReserve = 1;
			floodConfig.BackgroundCap = 0;
			await _daemonApiClient.ApplyConfigAsync(floodConfig, CancellationToken.None);
			await _daemonApiClient.SimulateFloodAsync(25, 25, CancellationToken.None);
			await Task.Delay(1500);
			await RefreshStatusAndEventsAsync();
			var queued = (_latestStatus?.InteractiveQueueDepth ?? 0) + (_latestStatus?.BackgroundQueueDepth ?? 0) > 0;
			Record("trigger queueing", queued, queued ? "queue depth > 0" : "queue depth did not increase");

			await _daemonApiClient.SetPressureModeAsync(PressureMode.Spiky, CancellationToken.None);
			await Task.Delay(2200);
			await RefreshStatusAndEventsAsync();
			var clamp = _latestStatus?.AdaptiveClampActive == true;
			Record("trigger adaptive clamp", clamp, clamp ? "adaptive clamp active" : "clamp not active");

			await _daemonApiClient.SetPressureModeAsync(PressureMode.Normal, CancellationToken.None);
			await _daemonApiClient.ApplyConfigAsync(original, CancellationToken.None);

			var diag = await _daemonApiClient.ExportDiagnosticsAsync(CancellationToken.None);
			Record("export diagnostics", diag?.Exported == true, diag?.OutputPath ?? "no output path");

			await RefreshStatusAndEventsAsync();
			var hasMarkers = _eventBuffer.Any(e => e.Message.Contains("configuration applied", StringComparison.OrdinalIgnoreCase))
			                 && _eventBuffer.Any(e => e.Message.Contains("adaptive clamp", StringComparison.OrdinalIgnoreCase))
			                 && _eventBuffer.Any(e => e.Message.Contains("diagnostics exported", StringComparison.OrdinalIgnoreCase));
			Record("verify event markers", hasMarkers, hasMarkers ? "config/clamp/diagnostics markers present" : "missing expected markers");
		}
		catch (Exception ex)
		{
			Record("audit harness execution", false, ex.Message);
		}

		AuditResultsEditor.Text = string.Join(Environment.NewLine, lines);
	}
}
