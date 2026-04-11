using System.Text.Json;
using LeaseGateLite.Contracts;

namespace LeaseGateLite.App;

public partial class MainPage : ContentPage
{
	private const string CustomPresetKey = "leasegatelite.custom.preset";
	private const string FirstRunCompletedKey = "leasegatelite.firstRun.completed";
	private readonly DaemonApiClient _daemonApiClient;
	private readonly Dictionary<string, Border> _sectionCards;
	private StatusSnapshot? _latestStatus;
	private LiteConfig _currentConfig = new();
	private LiteConfig _draftConfig = new();
	private bool _ignoreConfigEvents;
	private bool _hasPendingChanges;
	private bool _pauseEvents;
	private bool _ignoreAutostartToggle;
	private bool _ignoreNotificationsToggle;
	private bool _ignoreProfileEvents;
	private int _eventReconnectDelayMs = 500;
	private bool _daemonReachable = true;
	private readonly bool _animationsEnabled = !Preferences.Default.Get("leasegatelite.reduceMotion", false);
	private long _lastEventId;
	private readonly List<EventEntry> _eventBuffer = new();
	private readonly List<SeenClient> _recentProfileApps = new();
	private ProfilesSnapshotResponse? _profiles;
	private string _firstRunGoal = "Balanced";
	private bool _firstRunKeepUiResponsive = true;
	private bool _firstRunStartOnLogin;

	// Centralized daemon call wrapper for error handling
	private async Task SafeDaemonCallAsync(string actionName, Func<Task> action)
	{
		try
		{
			await action();
		}
		catch (HttpRequestException)
		{
			_daemonReachable = false;
			ConfigStatusLabel.Text = $"Cannot {actionName} - daemon not running";
		}
		catch (TaskCanceledException)
		{
			ConfigStatusLabel.Text = $"Timeout while trying to {actionName}";
		}
		catch (System.Text.Json.JsonException ex)
		{
			ConfigStatusLabel.Text = $"JSON error: {ex.Message}";
		}
		catch (Exception ex)
		{
			ConfigStatusLabel.Text = $"{actionName} failed: {ex.Message}";
		}
	}

	private async Task<T?> SafeDaemonCallAsync<T>(string actionName, Func<Task<T>> action, T? fallback = default)
	{
		try
		{
			return await action();
		}
		catch (HttpRequestException)
		{
			_daemonReachable = false;
			ConfigStatusLabel.Text = $"Cannot {actionName} - daemon not running";
			return fallback;
		}
		catch (TaskCanceledException)
		{
			ConfigStatusLabel.Text = $"Timeout while trying to {actionName}";
			return fallback;
		}
		catch (System.Text.Json.JsonException ex)
		{
			ConfigStatusLabel.Text = $"JSON error: {ex.Message}";
			return fallback;
		}
		catch (Exception ex)
		{
			ConfigStatusLabel.Text = $"{actionName} failed: {ex.Message}";
			return fallback;
		}
	}

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
			["Profiles"] = CardProfiles,
			["Diagnostics"] = CardDiagnostics,
			["Audit"] = CardAudit
		};

		Loaded += OnLoaded;
		SizeChanged += OnSizeChanged;
	}

	private async void OnLoaded(object? sender, EventArgs e)
	{
		try
		{
			ModePicker.SelectedIndex = 1;
			OnSizeChanged(sender, e);
			await RefreshAllAsync();
			InitializeFirstRunState();

			Dispatcher.StartTimer(TimeSpan.FromSeconds(2), () =>
			{
				_ = RefreshStatusAndEventsAsync();
				return true;
			});

			_ = RunEventStreamLoopAsync();
		}
		catch (Exception ex)
		{
			_daemonReachable = false;
			ConnectionStateLabel.Text = "Failed to connect to daemon";
			DiagnosticsPathLabel.Text = $"Daemon connection error: {ex.Message}";
			StatusDot.Color = Color.FromArgb("#B0B0B0");
		}
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
		try
		{
			await RefreshStatusAndEventsAsync();
			await RefreshConfigAsync();
			await RefreshAutostartAsync();
			await RefreshNotificationsAsync();
			await RefreshProfilesAsync();
		}
		catch (HttpRequestException)
		{
			_daemonReachable = false;
			ConnectionStateLabel.Text = "Daemon not running";
			DiagnosticsPathLabel.Text = "Start the daemon to enable settings and monitoring.";
			StatusDot.Color = Color.FromArgb("#B0B0B0");
		}
	}

	private async Task RefreshNotificationsAsync()
	{
		try
		{
			var settings = await _daemonApiClient.GetNotificationsSettingsAsync(CancellationToken.None);
			if (settings is null)
			{
				return;
			}

			_ignoreNotificationsToggle = true;
			NotificationsSwitch.IsToggled = settings.Enabled;
			NotificationsStatusLabel.Text = settings.Message;
		}
		catch
		{
			NotificationsStatusLabel.Text = "notifications unavailable";
		}
		finally
		{
			_ignoreNotificationsToggle = false;
		}
	}

	private async Task RefreshProfilesAsync()
	{
		try
		{
			var profiles = await _daemonApiClient.GetProfilesAsync(CancellationToken.None);
			if (profiles is null)
			{
				return;
			}

			_profiles = profiles;
			_recentProfileApps.Clear();
			_recentProfileApps.AddRange(profiles.RecentlySeenApps);

			_ignoreProfileEvents = true;
			RecentAppsPicker.ItemsSource = _recentProfileApps
				.Select(app => string.IsNullOrWhiteSpace(app.ProcessName) ? app.ClientAppId : $"{app.ClientAppId} ({app.ProcessName})")
				.ToList();

			if (_recentProfileApps.Count > 0)
			{
				RecentAppsPicker.SelectedIndex = 0;
				LoadProfileOverride(_recentProfileApps[0].ClientAppId);
			}
			else
			{
				ProfilesStatusLabel.Text = "No client apps observed yet.";
			}
		}
		finally
		{
			_ignoreProfileEvents = false;
			UpdateProfileLabels();
		}
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
			_firstRunStartOnLogin = status.Enabled;
			FirstRunStartOnLoginSwitch.IsToggled = status.Enabled;
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
				_daemonReachable = true;
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
			_daemonReachable = false;
			ConnectionStateLabel.Text = "Disconnected";
			DiagnosticsPathLabel.Text = "Daemon unreachable. Showing last known status.";
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

				_eventReconnectDelayMs = 500;
			}
			catch
			{
				_daemonReachable = false;
				await Task.Delay(_eventReconnectDelayMs);
				_eventReconnectDelayMs = Math.Min(8000, _eventReconnectDelayMs * 2);
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
		catch (HttpRequestException)
		{
			ConfigStatusLabel.Text = "Cannot load config: daemon not running";
		}
		finally
		{
			_ignoreConfigEvents = false;
		}
	}

	private void UpdateStatusUi(StatusSnapshot status)
	{
		ConnectionStateLabel.Text = !_daemonReachable ? "Daemon unreachable" : status.Connected ? "Connected" : "Disconnected";
		EndpointLabel.Text = status.Endpoint;
		VersionUptimeLabel.Text = $"Version: {status.DaemonVersion}    Uptime: {status.Uptime:hh\\:mm\\:ss}";
		HeatStateLabel.Text = status.HeatState.ToString();
		ClampStateLabel.Text = status.AdaptiveClampActive ? "Adaptive clamp active" : "Clamp inactive";
		LiveNumbersLabel.Text = $"Active: {status.ActiveCalls}   Queue: {status.InteractiveQueueDepth}/{status.BackgroundQueueDepth}   Effective concurrency: {status.EffectiveConcurrency}";
		PrimaryActiveLabel.Text = status.ActiveCalls.ToString();
		PrimaryQueuedLabel.Text = (status.InteractiveQueueDepth + status.BackgroundQueueDepth).ToString();
		PrimaryEffectiveLabel.Text = status.EffectiveConcurrency.ToString();
		PressureLabel.Text = $"CPU: {status.CpuPercent}%   Available RAM: {status.AvailableRamPercent}%";
		LastReasonLabel.Text = BuildFriendlyThrottleSentence(status.LastThrottleReason);
		ThrottleReasonsLabel.Text = status.RecentThrottleReasons.Count == 0
			? "No recent throttle reasons."
			: string.Join(Environment.NewLine, status.RecentThrottleReasons
				.TakeLast(3)
				.Reverse()
				.Select(r => $"{r.TimestampUtc:HH:mm:ss} — {BuildFriendlyThrottleSentence(r.Reason)}"));
		EffectiveConcurrencyPreviewLabel.Text = $"Effective concurrency right now: {status.EffectiveConcurrency}";
		UpdateDecisionFeed(status);
		UpdateControlLabels();

		var canApply = _daemonReachable && status.DaemonRunning;
		HeaderApplyButton.IsEnabled = canApply;
		HeaderRevertButton.IsEnabled = canApply;
		if (!canApply)
		{
			ConfigStatusLabel.Text = "Daemon not running. Start daemon to apply or revert settings.";
		}

		var nextColor = status.HeatState switch
		{
			HeatState.Calm => Color.FromArgb("#3CB371"),
			HeatState.Warm => Color.FromArgb("#E8B100"),
			_ => Color.FromArgb("#D9534F")
		};

		if (_animationsEnabled && StatusDot.Color != nextColor)
		{
			_ = AnimateHeatBadgeAsync(nextColor);
		}
		else
		{
			StatusDot.Color = nextColor;
		}
	}

	private async Task AnimateHeatBadgeAsync(Color nextColor)
	{
		await StatusDot.ScaleTo(0.85, 90, Easing.CubicInOut);
		StatusDot.Color = nextColor;
		await StatusDot.ScaleTo(1.0, 120, Easing.CubicOut);
	}

	private void UpdateControlLabels()
	{
		var maxConcurrency = (int)MaxConcurrencySlider.Value;
		var interactiveReserve = (int)InteractiveReserveSlider.Value;
		var backgroundCap = (int)BackgroundCapSlider.Value;
		var softThreshold = (int)SoftThresholdSlider.Value;
		var hardThreshold = (int)HardThresholdSlider.Value;

		MaxConcurrencyLabel.Text = $"Max concurrency (base): {maxConcurrency}   |   Effective now: {_latestStatus?.EffectiveConcurrency ?? 0}";
		InteractiveReserveLabel.Text = $"Interactive reserve: {interactiveReserve} (reserved for active desktop work)";
		BackgroundCapLabel.Text = $"Background cap: {backgroundCap} (limits non-interactive jobs)";
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

		var previewAt85 = PredictConcurrencyAtCpu(maxConcurrency, softThreshold, hardThreshold, 85);
		var nowEffective = _latestStatus?.EffectiveConcurrency ?? maxConcurrency;
		ConcurrencyImpactPreviewLabel.Text = $"Impact preview: Right now you'll run up to {nowEffective} calls at once. If CPU hits 85%, you'll drop to ~{previewAt85}.";

		var previewAt95 = PredictConcurrencyAtCpu(maxConcurrency, softThreshold, hardThreshold, 95);
		AdaptiveImpactPreviewLabel.Text = $"Adaptive preview: At ~85% CPU expect ~{previewAt85} active slots; at ~95% CPU expect ~{previewAt95}.";
	}

	private static int PredictConcurrencyAtCpu(int maxConcurrency, int softThreshold, int hardThreshold, int cpuPercent)
	{
		if (cpuPercent <= softThreshold)
		{
			return Math.Max(1, maxConcurrency);
		}

		if (cpuPercent >= hardThreshold)
		{
			return Math.Max(1, maxConcurrency / 2);
		}

		var ratio = (cpuPercent - softThreshold) / (double)Math.Max(1, hardThreshold - softThreshold);
		var reduced = maxConcurrency - (int)Math.Round((maxConcurrency - 1) * ratio);
		return Math.Max(1, reduced);
	}

	private void UpdateDecisionFeed(StatusSnapshot status)
	{
		var items = status.RecentThrottleReasons
			.TakeLast(3)
			.Reverse()
			.Select(item =>
			{
				var target = GetControlTargetForReason(item.Reason);
				var text = $"{item.TimestampUtc:HH:mm:ss} — {BuildFriendlyThrottleSentence(item.Reason)} Recommended action: {GetRecommendedAction(item.Reason)}";
				return (text, target);
			})
			.ToList();

		ApplyDecisionRow(Decision1Label, Decision1Button, items.ElementAtOrDefault(0));
		ApplyDecisionRow(Decision2Label, Decision2Button, items.ElementAtOrDefault(1));
		ApplyDecisionRow(Decision3Label, Decision3Button, items.ElementAtOrDefault(2));
	}

	private static void ApplyDecisionRow(Label label, Button button, (string text, string target) item)
	{
		if (string.IsNullOrWhiteSpace(item.text))
		{
			label.Text = "—";
			button.IsEnabled = false;
			button.CommandParameter = null;
			return;
		}

		label.Text = item.text;
		button.IsEnabled = true;
		button.CommandParameter = item.target;
	}

	private static string BuildFriendlyThrottleSentence(ThrottleReason reason)
	{
		return reason switch
		{
			ThrottleReason.CpuPressure => "CPU is busy, so I'm running fewer AI calls to keep things smooth.",
			ThrottleReason.MemoryPressure => "Memory is tight, so I'm queueing background work.",
			ThrottleReason.Cooldown => "Pressure stayed high, so I entered cooldown to help your system recover.",
			ThrottleReason.RateLimit => "Rate limits are active to avoid bursts that can cause stutter.",
			ThrottleReason.ManualClamp => "A manual safety limit is active for predictable behavior.",
			_ => "System is steady and no throttle action is currently needed."
		};
	}

	private static string GetControlTargetForReason(ThrottleReason reason)
	{
		return reason switch
		{
			ThrottleReason.CpuPressure => "Adaptive",
			ThrottleReason.MemoryPressure => "Adaptive",
			ThrottleReason.Cooldown => "Concurrency",
			ThrottleReason.RateLimit => "RateLimits",
			ThrottleReason.ManualClamp => "Concurrency",
			_ => "LiveStatus"
		};
	}

	private static string GetRecommendedAction(ThrottleReason reason)
	{
		return reason switch
		{
			ThrottleReason.CpuPressure => "Lower max concurrency or switch to Quiet.",
			ThrottleReason.MemoryPressure => "Lower output cap or increase queue-only behavior.",
			ThrottleReason.Cooldown => "Use a lower hard threshold or stronger cooldown.",
			ThrottleReason.RateLimit => "Increase request/token limits if your system stays cool.",
			ThrottleReason.ManualClamp => "Review manual pause/clamp settings.",
			_ => "No action needed right now."
		};
	}

	private void UpdateProfileLabels()
	{
		ProfileMaxConcurrencyLabel.Text = $"App max concurrency: {(int)ProfileMaxConcurrencySlider.Value}";
		ProfileBackgroundCapLabel.Text = $"App background cap: {(int)ProfileBackgroundCapSlider.Value}";
		ProfileMaxOutputLabel.Text = $"App max output tokens: {(int)ProfileMaxOutputSlider.Value}";
		ProfileMaxPromptLabel.Text = $"App max prompt/context tokens: {(int)ProfileMaxPromptSlider.Value}";
		ProfileRequestsPerMinuteLabel.Text = $"App requests/min: {(int)ProfileRequestsPerMinuteSlider.Value}";
		ProfileTokensPerMinuteLabel.Text = $"App tokens/min: {(int)ProfileTokensPerMinuteSlider.Value}";
	}

	private void LoadProfileOverride(string appId)
	{
		if (_profiles is null)
		{
			return;
		}

		var baseline = CloneConfig(_profiles.DefaultProfile);
		var overrideProfile = _profiles.Overrides.FirstOrDefault(v => string.Equals(v.ClientAppId, appId, StringComparison.OrdinalIgnoreCase));

		ProfileMaxConcurrencySlider.Value = overrideProfile?.MaxConcurrency ?? baseline.MaxConcurrency;
		ProfileBackgroundCapSlider.Value = overrideProfile?.BackgroundCap ?? baseline.BackgroundCap;
		ProfileMaxOutputSlider.Value = overrideProfile?.MaxOutputTokensClamp ?? baseline.MaxOutputTokensClamp;
		ProfileMaxPromptSlider.Value = overrideProfile?.MaxPromptTokensClamp ?? baseline.MaxPromptTokensClamp;
		ProfileRequestsPerMinuteSlider.Value = overrideProfile?.RequestsPerMinute ?? baseline.RequestsPerMinute;
		ProfileTokensPerMinuteSlider.Value = overrideProfile?.TokensPerMinute ?? baseline.TokensPerMinute;
		UpdateProfileLabels();
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
		var changedFieldCount = pending ? CountConfigChanges(_draftConfig, _currentConfig) : 0;
		PendingPill.IsVisible = pending;
		StickyPendingPill.IsVisible = pending;
		PendingPillLabel.Text = pending ? $"Unsaved draft ({changedFieldCount})" : "Unsaved draft";
		StickyPendingPillLabel.Text = pending ? $"Pending changes ({changedFieldCount})" : "Pending changes";
		StickyPendingStatusLabel.Text = pending ? $"{changedFieldCount} field(s) changed. Apply or revert anytime." : "No pending changes.";
	}

	private static int CountConfigChanges(LiteConfig left, LiteConfig right)
	{
		var count = 0;
		if (left.MaxConcurrency != right.MaxConcurrency) count++;
		if (left.InteractiveReserve != right.InteractiveReserve) count++;
		if (left.BackgroundCap != right.BackgroundCap) count++;
		if (left.CooldownBehavior != right.CooldownBehavior) count++;
		if (left.SoftThresholdPercent != right.SoftThresholdPercent) count++;
		if (left.HardThresholdPercent != right.HardThresholdPercent) count++;
		if (left.RecoveryRatePercent != right.RecoveryRatePercent) count++;
		if (left.SmoothingPercent != right.SmoothingPercent) count++;
		if (left.MaxOutputTokensClamp != right.MaxOutputTokensClamp) count++;
		if (left.MaxPromptTokensClamp != right.MaxPromptTokensClamp) count++;
		if (left.OverflowBehavior != right.OverflowBehavior) count++;
		if (left.MaxRetries != right.MaxRetries) count++;
		if (left.RetryBackoffMs != right.RetryBackoffMs) count++;
		if (left.RequestsPerMinute != right.RequestsPerMinute) count++;
		if (left.TokensPerMinute != right.TokensPerMinute) count++;
		if (left.BurstAllowance != right.BurstAllowance) count++;
		return count;
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
		try
		{
			await _daemonApiClient.ServiceCommandAsync(command, CancellationToken.None);
			await RefreshStatusAndEventsAsync();
		}
		catch (HttpRequestException)
		{
			_daemonReachable = false;
			ConnectionStateLabel.Text = "Daemon not running";
			DiagnosticsPathLabel.Text = $"Cannot {command} daemon - service is offline.";
		}
	}

	private async void OnReconnectClicked(object? sender, EventArgs e)
	{
		await SafeDaemonCallAsync("reconnect", RefreshAllAsync);
	}

	private void InitializeFirstRunState()
	{
		var completed = Preferences.Default.Get(FirstRunCompletedKey, false);
		FirstRunBanner.IsVisible = !completed;
		FirstRunCompleteChip.IsVisible = completed;

		if (completed)
		{
			return;
		}

		_firstRunGoal = "Balanced";
		_firstRunKeepUiResponsive = true;
		FirstRunBalancedRadio.IsChecked = true;
		FirstRunResponsiveSwitch.IsToggled = true;
		FirstRunStartOnLoginSwitch.IsToggled = StartOnLoginSwitch.IsToggled;
		FirstRunSuggestionLabel.Text = DetectLaptopLikeDevice()
			? "This device looks laptop-like, so Quiet may keep fan noise and heat lower."
			: "Balanced is the default and usually works well without extra tuning.";
		FirstRunStatusLabel.Text = "";
	}

	private static bool DetectLaptopLikeDevice()
	{
		if (DeviceInfo.Current.Platform == DevicePlatform.WinUI || DeviceInfo.Current.Platform == DevicePlatform.MacCatalyst)
		{
			return Environment.ProcessorCount <= 8;
		}

		return false;
	}

	private void OnFirstRunGoalChanged(object? sender, CheckedChangedEventArgs e)
	{
		if (!e.Value || sender is not RadioButton radioButton)
		{
			return;
		}

		_firstRunGoal = radioButton.Content?.ToString() ?? "Balanced";
	}

	private void OnFirstRunToggleChanged(object? sender, ToggledEventArgs e)
	{
		_firstRunKeepUiResponsive = FirstRunResponsiveSwitch.IsToggled;
		_firstRunStartOnLogin = FirstRunStartOnLoginSwitch.IsToggled;
	}

	private async void OnCompleteFirstRunClicked(object? sender, EventArgs e)
	{
		await SafeDaemonCallAsync("complete first-run setup", async () =>
		{
			await ApplyDaemonPresetAsync(_firstRunGoal);

			if (_firstRunKeepUiResponsive)
			{
				var adjusted = CloneConfig(_currentConfig);
				adjusted.InteractiveReserve = Math.Max(2, adjusted.InteractiveReserve);
				adjusted.BackgroundCap = Math.Max(0, Math.Min(adjusted.BackgroundCap, Math.Max(1, adjusted.MaxConcurrency - adjusted.InteractiveReserve)));
				var applyResult = await _daemonApiClient.ApplyConfigAsync(adjusted, CancellationToken.None);
				if (applyResult is not null && applyResult.Success)
				{
					_currentConfig = CloneConfig(applyResult.AppliedConfig);
					_draftConfig = CloneConfig(applyResult.AppliedConfig);
					ApplyConfigToControls(_currentConfig);
					UpdateControlLabels();
				}
			}

			if (_firstRunStartOnLogin != StartOnLoginSwitch.IsToggled)
			{
				await _daemonApiClient.SetAutostartAsync(_firstRunStartOnLogin, CancellationToken.None);
				await RefreshAutostartAsync();
			}

			Preferences.Default.Set(FirstRunCompletedKey, true);
			FirstRunBanner.IsVisible = false;
			FirstRunCompleteChip.IsVisible = true;
			FirstRunStatusLabel.Text = "Setup complete.";
			ConfigStatusLabel.Text = "First-run setup complete. You can leave defaults as-is.";
		});
	}

	private void OnToggleThrottleReasonsClicked(object? sender, EventArgs e)
	{
		ThrottleReasonsLabel.IsVisible = !ThrottleReasonsLabel.IsVisible;
		WhyThrottledButton.Text = ThrottleReasonsLabel.IsVisible ? "Hide throttle details" : "Why am I throttled?";
	}

	private void OnToggleTechnicalDetailsClicked(object? sender, EventArgs e)
	{
		TechnicalDetailsPanel.IsVisible = !TechnicalDetailsPanel.IsVisible;
		ToggleTechnicalDetailsButton.Text = TechnicalDetailsPanel.IsVisible ? "Hide technical details" : "Show technical details";
	}

	private async void OnNotificationsToggled(object? sender, ToggledEventArgs e)
	{
		if (_ignoreNotificationsToggle)
		{
			return;
		}

		var result = await _daemonApiClient.SetNotificationsSettingsAsync(e.Value, CancellationToken.None);
		NotificationsStatusLabel.Text = result?.Message ?? "notifications update failed";
	}

	private async void OnDecisionJumpClicked(object? sender, EventArgs e)
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
		if (_animationsEnabled)
		{
			await card.FadeTo(0.75, 70, Easing.CubicInOut);
			await card.FadeTo(1.0, 120, Easing.CubicOut);
		}
	}

	private async void OnInlineHelpClicked(object? sender, EventArgs e)
	{
		if (sender is not Button button || button.CommandParameter is not string key)
		{
			return;
		}

		var (title, message) = key switch
		{
			"MaxConcurrency" => ("Max concurrency", "Sets your top parallel AI calls. Lower means steadier performance; higher means more throughput and heat."),
			"InteractiveReserve" => ("Interactive reserve", "Keeps slots free for foreground work so the UI stays responsive while background jobs run."),
			"SoftThreshold" => ("Soft threshold", "Where gentle throttling starts. Lower values smooth earlier; higher values push harder before clamping."),
			"HardThreshold" => ("Hard threshold", "Where strong protection kicks in. If this hits often, reduce concurrency or use Quiet preset."),
			"OutputCap" => ("Output cap", "Caps response size to reduce memory pressure and keep latency predictable."),
			"Presets" => ("Presets", "Quiet favors thermals, Balanced is the safe default, and Performance prioritizes throughput."),
			_ => ("Help", "No contextual help is available for this control.")
		};

		await DisplayAlert(title, message, "Got it");
	}

	private void OnRecentAppChanged(object? sender, EventArgs e)
	{
		if (_ignoreProfileEvents)
		{
			return;
		}

		if (RecentAppsPicker.SelectedIndex < 0 || RecentAppsPicker.SelectedIndex >= _recentProfileApps.Count)
		{
			return;
		}

		LoadProfileOverride(_recentProfileApps[RecentAppsPicker.SelectedIndex].ClientAppId);
		ProfilesStatusLabel.Text = $"Selected app: {_recentProfileApps[RecentAppsPicker.SelectedIndex].ClientAppId}";
	}

	private void OnProfileOverrideChanged(object? sender, EventArgs e)
	{
		if (_ignoreProfileEvents)
		{
			return;
		}

		UpdateProfileLabels();
	}

	private async Task ApplySelectedProfilePresetAsync(string presetName)
	{
		if (RecentAppsPicker.SelectedIndex < 0 || RecentAppsPicker.SelectedIndex >= _recentProfileApps.Count)
		{
			ProfilesStatusLabel.Text = "Select a recent app first.";
			return;
		}

		var selected = _recentProfileApps[RecentAppsPicker.SelectedIndex];
		var result = await _daemonApiClient.SetAppProfileAsync(new SetAppProfileRequest
		{
			ClientAppId = selected.ClientAppId,
			ProcessName = selected.ProcessName,
			Signature = selected.Signature,
			PresetName = presetName
		}, CancellationToken.None);

		ProfilesStatusLabel.Text = result?.Message ?? "Failed to apply app preset.";
		await RefreshProfilesAsync();
	}

	private async void OnProfileQuietClicked(object? sender, EventArgs e)
	{
		await ApplySelectedProfilePresetAsync("Quiet");
	}

	private async void OnProfileBalancedClicked(object? sender, EventArgs e)
	{
		await ApplySelectedProfilePresetAsync("Balanced");
	}

	private async void OnProfilePerformanceClicked(object? sender, EventArgs e)
	{
		await ApplySelectedProfilePresetAsync("Performance");
	}

	private async void OnApplyAppProfileClicked(object? sender, EventArgs e)
	{
		await SafeDaemonCallAsync("apply app profile", async () =>
		{
			if (RecentAppsPicker.SelectedIndex < 0 || RecentAppsPicker.SelectedIndex >= _recentProfileApps.Count)
			{
				ProfilesStatusLabel.Text = "Select a recent app first.";
				return;
			}

			var selected = _recentProfileApps[RecentAppsPicker.SelectedIndex];
			var request = new SetAppProfileRequest
			{
				ClientAppId = selected.ClientAppId,
				ProcessName = selected.ProcessName,
				Signature = selected.Signature,
				MaxConcurrency = (int)Math.Round(ProfileMaxConcurrencySlider.Value),
				BackgroundCap = (int)Math.Round(ProfileBackgroundCapSlider.Value),
				MaxOutputTokensClamp = (int)Math.Round(ProfileMaxOutputSlider.Value),
				MaxPromptTokensClamp = (int)Math.Round(ProfileMaxPromptSlider.Value),
				RequestsPerMinute = (int)Math.Round(ProfileRequestsPerMinuteSlider.Value),
				TokensPerMinute = (int)Math.Round(ProfileTokensPerMinuteSlider.Value)
			};

			var result = await _daemonApiClient.SetAppProfileAsync(request, CancellationToken.None);
			ProfilesStatusLabel.Text = result?.Message ?? "Failed to apply app override.";
			await RefreshProfilesAsync();
		});
	}

	private async void OnStartOnLoginToggled(object? sender, ToggledEventArgs e)
	{
		if (_ignoreAutostartToggle)
		{
			return;
		}

		await SafeDaemonCallAsync("update autostart setting", async () =>
		{
			var result = await _daemonApiClient.SetAutostartAsync(e.Value, CancellationToken.None);
			StartOnLoginStatusLabel.Text = result?.Message ?? "Autostart update failed.";
			await RefreshAutostartAsync();
		});
	}

	private async void OnStartClicked(object? sender, EventArgs e)
	{
		await ExecuteServiceCommandAsync("start");
	}

	private async void OnStopClicked(object? sender, EventArgs e)
	{
		var confirm = await DisplayAlert(
			"Stop daemon?",
			"Stopping the daemon will pause throttling for all apps until you start it again.",
			"Stop daemon",
			"Cancel");

		if (!confirm)
		{
			return;
		}

		await ExecuteServiceCommandAsync("stop");
	}

	private async void OnRestartClicked(object? sender, EventArgs e)
	{
		await ExecuteServiceCommandAsync("restart");
	}

	private async void OnApplyClicked(object? sender, EventArgs e)
	{
		await SafeDaemonCallAsync("apply configuration", async () =>
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
			await ShowApplyToastAsync();
			await RefreshStatusAndEventsAsync();
		});
	}

	private async Task ShowApplyToastAsync()
	{
		ApplyToast.IsVisible = true;
		await Task.Delay(1400);
		ApplyToast.IsVisible = false;
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
		var confirm = await DisplayAlert(
			"Reset draft to defaults?",
			"This replaces your current draft settings. It will not take effect until you click Apply.",
			"Reset draft",
			"Cancel");

		if (!confirm)
		{
			return;
		}

		await SafeDaemonCallAsync("reset to defaults", async () =>
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
		});
	}

	private async void OnExportDiagnosticsClicked(object? sender, EventArgs e)
	{
		await SafeDaemonCallAsync("export diagnostics", async () =>
		{
			var response = await _daemonApiClient.ExportDiagnosticsAsync(IncludePathsSwitch.IsToggled, IncludeVerboseSwitch.IsToggled, CancellationToken.None);
			DiagnosticsPathLabel.Text = response is null ? "Diagnostics export failed." : $"Exported: {response.OutputPath} ({response.BytesWritten} bytes)";
			await RefreshStatusAndEventsAsync();
		});
	}

	private async void OnPreviewDiagnosticsClicked(object? sender, EventArgs e)
	{
		await SafeDaemonCallAsync("preview diagnostics", async () =>
		{
			var preview = await _daemonApiClient.GetDiagnosticsPreviewAsync(IncludePathsSwitch.IsToggled, IncludeVerboseSwitch.IsToggled, CancellationToken.None);
			if (preview is null)
			{
				DiagnosticsPreviewLabel.Text = "Could not load diagnostics preview.";
				return;
			}

			var sections = string.Join(", ", preview.IncludedSections);
			var rules = string.Join(" ", preview.RedactionRules);
			DiagnosticsPreviewLabel.Text = $"Includes: {sections}. {rules} {preview.Summary}";
		});
	}

	private async void OnCopySummaryClicked(object? sender, EventArgs e)
	{
		if (_latestStatus is null)
		{
			return;
		}

		var summary = $"Time={DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss}; Heat={_latestStatus.HeatState}; Active={_latestStatus.ActiveCalls}; Queue={_latestStatus.InteractiveQueueDepth}/{_latestStatus.BackgroundQueueDepth}; EffectiveConcurrency={_latestStatus.EffectiveConcurrency}; CPU={_latestStatus.CpuPercent}%; RAM={_latestStatus.AvailableRamPercent}%; Reason={_latestStatus.LastThrottleReason}; Degraded={_latestStatus.DegradedMode}";
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
		try
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
		catch (HttpRequestException)
		{
			PresetStatusLabel.Text = $"Cannot apply preset - daemon not running";
		}
		catch (System.Text.Json.JsonException ex)
		{
			PresetStatusLabel.Text = $"JSON error: {ex.Message}";
		}
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
		await SafeDaemonCallAsync("run audit harness", async () =>
		{
			var lines = new List<string>();
			void Record(string name, bool pass, string evidence)
			{
				lines.Add($"{(pass ? "✅" : "❌")} {name} -- {evidence}");
			}

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

			var diag = await _daemonApiClient.ExportDiagnosticsAsync(false, false, CancellationToken.None);
			Record("export diagnostics", diag?.Exported == true, diag?.OutputPath ?? "no output path");

			await RefreshStatusAndEventsAsync();
			var hasMarkers = _eventBuffer.Any(e => e.Message.Contains("configuration applied", StringComparison.OrdinalIgnoreCase))
			                 && _eventBuffer.Any(e => e.Message.Contains("adaptive clamp", StringComparison.OrdinalIgnoreCase))
			                 && _eventBuffer.Any(e => e.Message.Contains("diagnostics exported", StringComparison.OrdinalIgnoreCase));
			Record("verify event markers", hasMarkers, hasMarkers ? "config/clamp/diagnostics markers present" : "missing expected markers");

			AuditResultsEditor.Text = string.Join(Environment.NewLine, lines);
		});
	}
}
