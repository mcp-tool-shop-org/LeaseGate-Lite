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
			["Diagnostics"] = CardDiagnostics
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

			var eventTail = await _daemonApiClient.GetEventsAsync(200, CancellationToken.None);
			if (eventTail is not null)
			{
				EventsEditor.Text = string.Join(Environment.NewLine, eventTail.Events.Select(e => $"{e.TimestampUtc:HH:mm:ss} [{e.Level}] {e.Message}"));
			}
		}
		catch
		{
			ConnectionStateLabel.Text = "Disconnected";
			StatusDot.Color = Color.FromArgb("#B0B0B0");
		}
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

	private void ApplyQuietPreset()
	{
		MaxConcurrencySlider.Value = 4;
		InteractiveReserveSlider.Value = 2;
		BackgroundCapSlider.Value = 2;
		SoftThresholdSlider.Value = 60;
		HardThresholdSlider.Value = 80;
		RecoveryRateSlider.Value = 15;
		SmoothingSlider.Value = 70;
		MaxOutputTokensSlider.Value = 512;
		MaxPromptTokensSlider.Value = 2048;
		RequestsPerMinuteSlider.Value = 80;
		TokensPerMinuteSlider.Value = 80_000;
		BurstAllowanceSlider.Value = 8;
		CooldownBehaviorPicker.SelectedItem = CooldownBehavior.Aggressive.ToString();
		OverflowBehaviorPicker.SelectedItem = OverflowBehavior.QueueOnly.ToString();
	}

	private void ApplyBalancedPreset()
	{
		MaxConcurrencySlider.Value = 8;
		InteractiveReserveSlider.Value = 2;
		BackgroundCapSlider.Value = 6;
		SoftThresholdSlider.Value = 70;
		HardThresholdSlider.Value = 90;
		RecoveryRateSlider.Value = 20;
		SmoothingSlider.Value = 40;
		MaxOutputTokensSlider.Value = 1024;
		MaxPromptTokensSlider.Value = 4096;
		RequestsPerMinuteSlider.Value = 120;
		TokensPerMinuteSlider.Value = 120_000;
		BurstAllowanceSlider.Value = 12;
		CooldownBehaviorPicker.SelectedItem = CooldownBehavior.Mild.ToString();
		OverflowBehaviorPicker.SelectedItem = OverflowBehavior.TrimOldest.ToString();
	}

	private void ApplyPerformancePreset()
	{
		MaxConcurrencySlider.Value = 14;
		InteractiveReserveSlider.Value = 2;
		BackgroundCapSlider.Value = 12;
		SoftThresholdSlider.Value = 78;
		HardThresholdSlider.Value = 95;
		RecoveryRateSlider.Value = 35;
		SmoothingSlider.Value = 25;
		MaxOutputTokensSlider.Value = 2048;
		MaxPromptTokensSlider.Value = 8192;
		RequestsPerMinuteSlider.Value = 240;
		TokensPerMinuteSlider.Value = 240_000;
		BurstAllowanceSlider.Value = 20;
		CooldownBehaviorPicker.SelectedItem = CooldownBehavior.Off.ToString();
		OverflowBehaviorPicker.SelectedItem = OverflowBehavior.TrimOldest.ToString();
	}

	private void OnQuietPresetClicked(object? sender, EventArgs e)
	{
		ApplyQuietPreset();
		SetPending(true);
		PresetStatusLabel.Text = "Quiet preset loaded. Click Apply to commit.";
	}

	private void OnBalancedPresetClicked(object? sender, EventArgs e)
	{
		ApplyBalancedPreset();
		SetPending(true);
		PresetStatusLabel.Text = "Balanced preset loaded. Click Apply to commit.";
	}

	private void OnPerformancePresetClicked(object? sender, EventArgs e)
	{
		ApplyPerformancePreset();
		SetPending(true);
		PresetStatusLabel.Text = "Performance preset loaded. Click Apply to commit.";
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
}
