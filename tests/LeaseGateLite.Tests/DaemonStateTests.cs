using LeaseGateLite.Contracts;
using LeaseGateLite.Daemon;
using Xunit;

namespace LeaseGateLite.Tests;

public sealed class DaemonStateTests
{
    private static DaemonState CreateState()
    {
        var state = new DaemonState();
        state.ResetConfig(apply: true);
        state.SetBackgroundPause(false);
        state.SetNotificationsEnabled(false);
        return state;
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(99, false)]
    public void ConfigVersionValidation_IsEnforced(int value, bool expectedSuccess)
    {
        var state = CreateState();
        var config = state.GetConfig();
        config.ConfigVersion = value;

        var result = state.ApplyConfig(config);

        Assert.Equal(expectedSuccess, result.Success);
        if (!expectedSuccess)
        {
            Assert.Contains(result.Errors, error => error.Field == nameof(LiteConfig.ConfigVersion));
        }
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(8, true)]
    [InlineData(32, true)]
    [InlineData(33, false)]
    public void MaxConcurrencyValidation_IsEnforced(int value, bool expectedSuccess)
    {
        var state = CreateState();
        var config = state.GetConfig();
        config.MaxConcurrency = value;
        if (value >= 1)
        {
            config.InteractiveReserve = Math.Min(config.InteractiveReserve, value);
            config.BackgroundCap = Math.Min(config.BackgroundCap, value);
        }

        var result = state.ApplyConfig(config);

        Assert.Equal(expectedSuccess, result.Success);
        if (!expectedSuccess)
        {
            Assert.Contains(result.Errors, error => error.Field == nameof(LiteConfig.MaxConcurrency));
        }
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(2, true)]
    [InlineData(16, true)]
    [InlineData(17, false)]
    public void InteractiveReserveValidation_IsEnforced(int value, bool expectedSuccess)
    {
        var state = CreateState();
        var config = state.GetConfig();
        config.MaxConcurrency = 32;
        config.InteractiveReserve = value;

        var result = state.ApplyConfig(config);

        Assert.Equal(expectedSuccess, result.Success);
        if (!expectedSuccess)
        {
            Assert.Contains(result.Errors, error => error.Field == nameof(LiteConfig.InteractiveReserve));
        }
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(6, true)]
    [InlineData(32, true)]
    [InlineData(33, false)]
    public void BackgroundCapValidation_IsEnforced(int value, bool expectedSuccess)
    {
        var state = CreateState();
        var config = state.GetConfig();
        config.MaxConcurrency = 32;
        config.BackgroundCap = value;

        var result = state.ApplyConfig(config);

        Assert.Equal(expectedSuccess, result.Success);
        if (!expectedSuccess)
        {
            Assert.Contains(result.Errors, error => error.Field == nameof(LiteConfig.BackgroundCap));
        }
    }

    [Theory]
    [InlineData(39, false)]
    [InlineData(40, true)]
    [InlineData(70, true)]
    [InlineData(95, true)]
    [InlineData(96, false)]
    public void SoftThresholdValidation_IsEnforced(int value, bool expectedSuccess)
    {
        var state = CreateState();
        var config = state.GetConfig();
        config.HardThresholdPercent = 99;
        config.SoftThresholdPercent = value;

        var result = state.ApplyConfig(config);

        Assert.Equal(expectedSuccess, result.Success);
        if (!expectedSuccess)
        {
            Assert.Contains(result.Errors, error => error.Field == nameof(LiteConfig.SoftThresholdPercent));
        }
    }

    [Theory]
    [InlineData(49, false)]
    [InlineData(50, true)]
    [InlineData(90, true)]
    [InlineData(99, true)]
    [InlineData(100, false)]
    public void HardThresholdValidation_IsEnforced(int value, bool expectedSuccess)
    {
        var state = CreateState();
        var config = state.GetConfig();
        config.SoftThresholdPercent = 40;
        config.HardThresholdPercent = value;

        var result = state.ApplyConfig(config);

        Assert.Equal(expectedSuccess, result.Success);
        if (!expectedSuccess)
        {
            Assert.Contains(result.Errors, error => error.Field == nameof(LiteConfig.HardThresholdPercent));
        }
    }

    [Theory]
    [InlineData(4, false)]
    [InlineData(5, true)]
    [InlineData(20, true)]
    [InlineData(100, true)]
    [InlineData(101, false)]
    public void RecoveryRateValidation_IsEnforced(int value, bool expectedSuccess)
    {
        var state = CreateState();
        var config = state.GetConfig();
        config.RecoveryRatePercent = value;

        var result = state.ApplyConfig(config);

        Assert.Equal(expectedSuccess, result.Success);
        if (!expectedSuccess)
        {
            Assert.Contains(result.Errors, error => error.Field == nameof(LiteConfig.RecoveryRatePercent));
        }
    }

    [Theory]
    [InlineData(4, false)]
    [InlineData(5, true)]
    [InlineData(40, true)]
    [InlineData(95, true)]
    [InlineData(96, false)]
    public void SmoothingPercentValidation_IsEnforced(int value, bool expectedSuccess)
    {
        var state = CreateState();
        var config = state.GetConfig();
        config.SmoothingPercent = value;

        var result = state.ApplyConfig(config);

        Assert.Equal(expectedSuccess, result.Success);
        if (!expectedSuccess)
        {
            Assert.Contains(result.Errors, error => error.Field == nameof(LiteConfig.SmoothingPercent));
        }
    }

    [Theory]
    [InlineData(63, false)]
    [InlineData(64, true)]
    [InlineData(1024, true)]
    [InlineData(4096, true)]
    [InlineData(4097, false)]
    public void MaxOutputTokensValidation_IsEnforced(int value, bool expectedSuccess)
    {
        var state = CreateState();
        var config = state.GetConfig();
        config.MaxOutputTokensClamp = value;

        var result = state.ApplyConfig(config);

        Assert.Equal(expectedSuccess, result.Success);
        if (!expectedSuccess)
        {
            Assert.Contains(result.Errors, error => error.Field == nameof(LiteConfig.MaxOutputTokensClamp));
        }
    }

    [Theory]
    [InlineData(255, false)]
    [InlineData(256, true)]
    [InlineData(4096, true)]
    [InlineData(32000, true)]
    [InlineData(32001, false)]
    public void MaxPromptTokensValidation_IsEnforced(int value, bool expectedSuccess)
    {
        var state = CreateState();
        var config = state.GetConfig();
        config.MaxPromptTokensClamp = value;

        var result = state.ApplyConfig(config);

        Assert.Equal(expectedSuccess, result.Success);
        if (!expectedSuccess)
        {
            Assert.Contains(result.Errors, error => error.Field == nameof(LiteConfig.MaxPromptTokensClamp));
        }
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(2, true)]
    [InlineData(8, true)]
    [InlineData(9, false)]
    public void MaxRetriesValidation_IsEnforced(int value, bool expectedSuccess)
    {
        var state = CreateState();
        var config = state.GetConfig();
        config.MaxRetries = value;

        var result = state.ApplyConfig(config);

        Assert.Equal(expectedSuccess, result.Success);
        if (!expectedSuccess)
        {
            Assert.Contains(result.Errors, error => error.Field == nameof(LiteConfig.MaxRetries));
        }
    }

    [Theory]
    [InlineData(99, false)]
    [InlineData(100, true)]
    [InlineData(500, true)]
    [InlineData(5000, true)]
    [InlineData(5001, false)]
    public void RetryBackoffValidation_IsEnforced(int value, bool expectedSuccess)
    {
        var state = CreateState();
        var config = state.GetConfig();
        config.RetryBackoffMs = value;

        var result = state.ApplyConfig(config);

        Assert.Equal(expectedSuccess, result.Success);
        if (!expectedSuccess)
        {
            Assert.Contains(result.Errors, error => error.Field == nameof(LiteConfig.RetryBackoffMs));
        }
    }

    [Theory]
    [InlineData(9, false)]
    [InlineData(10, true)]
    [InlineData(120, true)]
    [InlineData(1000, true)]
    [InlineData(1001, false)]
    public void RequestsPerMinuteValidation_IsEnforced(int value, bool expectedSuccess)
    {
        var state = CreateState();
        var config = state.GetConfig();
        config.RequestsPerMinute = value;

        var result = state.ApplyConfig(config);

        Assert.Equal(expectedSuccess, result.Success);
        if (!expectedSuccess)
        {
            Assert.Contains(result.Errors, error => error.Field == nameof(LiteConfig.RequestsPerMinute));
        }
    }

    [Theory]
    [InlineData(999, false)]
    [InlineData(1000, true)]
    [InlineData(120000, true)]
    [InlineData(500000, true)]
    [InlineData(500001, false)]
    public void TokensPerMinuteValidation_IsEnforced(int value, bool expectedSuccess)
    {
        var state = CreateState();
        var config = state.GetConfig();
        config.TokensPerMinute = value;

        var result = state.ApplyConfig(config);

        Assert.Equal(expectedSuccess, result.Success);
        if (!expectedSuccess)
        {
            Assert.Contains(result.Errors, error => error.Field == nameof(LiteConfig.TokensPerMinute));
        }
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(12, true)]
    [InlineData(100, true)]
    [InlineData(101, false)]
    public void BurstAllowanceValidation_IsEnforced(int value, bool expectedSuccess)
    {
        var state = CreateState();
        var config = state.GetConfig();
        config.BurstAllowance = value;

        var result = state.ApplyConfig(config);

        Assert.Equal(expectedSuccess, result.Success);
        if (!expectedSuccess)
        {
            Assert.Contains(result.Errors, error => error.Field == nameof(LiteConfig.BurstAllowance));
        }
    }

    [Theory]
    [InlineData(70, 70, false)]
    [InlineData(71, 70, true)]
    [InlineData(69, 70, false)]
    [InlineData(95, 40, true)]
    public void HardThresholdMustBeGreaterThanSoftThreshold(int hard, int soft, bool expectedSuccess)
    {
        var state = CreateState();
        var config = state.GetConfig();
        config.HardThresholdPercent = hard;
        config.SoftThresholdPercent = soft;

        var result = state.ApplyConfig(config);

        Assert.Equal(expectedSuccess, result.Success);
        if (!expectedSuccess)
        {
            Assert.Contains(result.Errors, error => error.Field == nameof(LiteConfig.HardThresholdPercent));
        }
    }

    [Theory]
    [InlineData(4, 2, true)]
    [InlineData(4, 4, true)]
    [InlineData(4, 5, false)]
    [InlineData(32, 16, true)]
    public void InteractiveReserveCannotExceedMaxConcurrency(int maxConcurrency, int reserve, bool expectedSuccess)
    {
        var state = CreateState();
        var config = state.GetConfig();
        config.MaxConcurrency = maxConcurrency;
        config.BackgroundCap = Math.Min(config.BackgroundCap, maxConcurrency);
        config.InteractiveReserve = reserve;

        var result = state.ApplyConfig(config);

        Assert.Equal(expectedSuccess, result.Success);
        if (!expectedSuccess)
        {
            Assert.Contains(result.Errors, error => error.Field == nameof(LiteConfig.InteractiveReserve));
        }
    }

    [Theory]
    [InlineData(4, 2, true)]
    [InlineData(4, 4, true)]
    [InlineData(4, 5, false)]
    [InlineData(32, 24, true)]
    public void BackgroundCapCannotExceedMaxConcurrency(int maxConcurrency, int backgroundCap, bool expectedSuccess)
    {
        var state = CreateState();
        var config = state.GetConfig();
        config.MaxConcurrency = maxConcurrency;
        config.BackgroundCap = backgroundCap;

        var result = state.ApplyConfig(config);

        Assert.Equal(expectedSuccess, result.Success);
        if (!expectedSuccess)
        {
            Assert.Contains(result.Errors, error => error.Field == nameof(LiteConfig.BackgroundCap));
        }
    }

    [Theory]
    [InlineData("Quiet")]
    [InlineData("Balanced")]
    [InlineData("Performance")]
    public void ApplyPreset_AcceptsKnownPresets(string presetName)
    {
        var state = CreateState();

        var result = state.ApplyPreset(presetName);

        Assert.True(result.Success);
        Assert.Equal("configuration applied", result.Message);
    }

    [Fact]
    public void ApplyPreset_RejectsUnknownPreset()
    {
        var state = CreateState();

        var result = state.ApplyPreset("Nope");

        Assert.False(result.Success);
        Assert.Contains(result.Errors, error => error.Field == "name");
    }

    [Theory]
    [InlineData("Quiet", true)]
    [InlineData("Balanced", false)]
    [InlineData("Performance", true)]
    [InlineData("Unknown", false)]
    public void PreviewPreset_ReturnsExpectedDiffs(string presetName, bool expectedFound)
    {
        var state = CreateState();

        var result = state.PreviewPreset(presetName);

        Assert.Equal(presetName, result.Name, StringComparer.OrdinalIgnoreCase);
        if (expectedFound)
        {
            Assert.NotEmpty(result.Diffs);
        }
        else
        {
            Assert.Empty(result.Diffs);
        }
    }

    [Theory]
    [InlineData(PressureMode.Normal)]
    [InlineData(PressureMode.Spiky)]
    public void SetPressureMode_UpdatesStatus(PressureMode mode)
    {
        var state = CreateState();

        var command = state.SetPressureMode(mode);
        var status = state.GetStatus();

        Assert.True(command.Success);
        Assert.Equal(mode, status.PressureMode);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BackgroundPause_ReflectsInStatus(bool paused)
    {
        var state = CreateState();

        var command = state.SetBackgroundPause(paused);
        var status = state.GetStatus();

        Assert.True(command.Success);
        Assert.Equal(paused, status.BackgroundPaused);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void NotificationsToggle_RoundTrips(bool enabled)
    {
        var state = CreateState();

        var command = state.SetNotificationsEnabled(enabled);
        var settings = state.GetNotificationsSettings();

        Assert.True(command.Success);
        Assert.Equal(enabled, settings.Enabled);
    }

    [Fact]
    public void StopCommand_SetsDaemonNotRunning()
    {
        var state = CreateState();

        var result = state.Stop();
        var status = state.GetStatus();

        Assert.True(result.Success);
        Assert.False(status.DaemonRunning);
        Assert.Equal(0, status.EffectiveConcurrency);
    }

    [Fact]
    public void StartCommand_AfterStop_SetsDaemonRunning()
    {
        var state = CreateState();
        state.Stop();

        var result = state.Start();
        var status = state.GetStatus();

        Assert.True(result.Success);
        Assert.True(status.DaemonRunning);
    }

    [Fact]
    public void RestartCommand_AlwaysSucceedsAndRuns()
    {
        var state = CreateState();
        state.Stop();

        var result = state.Restart();
        var status = state.GetStatus();

        Assert.True(result.Success);
        Assert.True(status.DaemonRunning);
    }

    [Theory]
    [InlineData(-10, -10)]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(10, 10)]
    [InlineData(25, 30)]
    [InlineData(50, 50)]
    [InlineData(100, 20)]
    [InlineData(20, 100)]
    [InlineData(150, 150)]
    [InlineData(200, 200)]
    [InlineData(300, 200)]
    [InlineData(200, 300)]
    [InlineData(400, 1)]
    [InlineData(1, 400)]
    [InlineData(120, 80)]
    [InlineData(80, 120)]
    [InlineData(199, 199)]
    [InlineData(50, 200)]
    [InlineData(200, 50)]
    [InlineData(500, 500)]
    public void SimulateFlood_ClampsAndMaintainsStatusInvariants(int interactiveRequests, int backgroundRequests)
    {
        var state = CreateState();

        var command = state.SimulateFlood(new SimulateFloodRequest
        {
            InteractiveRequests = interactiveRequests,
            BackgroundRequests = backgroundRequests,
            ClientAppId = $"app-{interactiveRequests}-{backgroundRequests}",
            ProcessName = "test-proc",
            Signature = "sig"
        });
        var status = state.GetStatus();

        Assert.True(command.Success);
        Assert.InRange(status.InteractiveQueueDepth, 0, 500);
        Assert.InRange(status.BackgroundQueueDepth, 0, 500);
        Assert.InRange(status.ActiveCalls, 0, status.EffectiveConcurrency);
        Assert.InRange(status.CpuPercent, 18, 98);
        Assert.InRange(status.AvailableRamPercent, 8, 95);
    }

    [Theory]
    [InlineData(10, 10, 5)]
    [InlineData(20, 20, 5)]
    [InlineData(30, 30, 5)]
    [InlineData(40, 40, 5)]
    [InlineData(50, 50, 5)]
    [InlineData(60, 60, 5)]
    [InlineData(70, 70, 5)]
    [InlineData(80, 80, 5)]
    [InlineData(90, 90, 5)]
    [InlineData(100, 100, 5)]
    [InlineData(120, 120, 5)]
    [InlineData(140, 140, 5)]
    [InlineData(160, 160, 5)]
    [InlineData(180, 180, 5)]
    [InlineData(200, 200, 5)]
    [InlineData(200, 200, 20)]
    [InlineData(150, 150, 25)]
    [InlineData(100, 120, 50)]
    [InlineData(120, 100, 75)]
    [InlineData(200, 200, 120)]
    public void SimulateFlood_BoundsTotalQueueAt500(int interactivePerApp, int backgroundPerApp, int appCount)
    {
        var state = CreateState();

        for (var index = 0; index < appCount; index++)
        {
            state.SimulateFlood(new SimulateFloodRequest
            {
                InteractiveRequests = interactivePerApp,
                BackgroundRequests = backgroundPerApp,
                ClientAppId = $"app-{index}",
                ProcessName = "load-proc",
                Signature = "sig"
            });
        }

        var status = state.GetStatus();

        Assert.InRange(status.InteractiveQueueDepth + status.BackgroundQueueDepth, 0, 500);
        Assert.InRange(status.ActiveCalls, 0, status.EffectiveConcurrency);
    }

    [Theory]
    [InlineData("browser", "Quiet", 2, 1, 80, 40000, true)]
    [InlineData("browser", "Balanced", 6, 4, 120, 120000, true)]
    [InlineData("browser", "Performance", 14, 12, 240, 240000, true)]
    [InlineData("worker", "Quiet", 1, 0, 40, 10000, true)]
    [InlineData("worker", "", 8, 6, 200, 150000, true)]
    [InlineData("cli", "Balanced", 4, 2, 60, 60000, true)]
    [InlineData("sync-tool", "Performance", 16, 8, 300, 200000, true)]
    [InlineData("agent", "Quiet", 3, 1, 50, 30000, true)]
    [InlineData("desktop", "Balanced", 10, 7, 140, 140000, true)]
    [InlineData("", "Balanced", 10, 7, 140, 140000, false)]
    public void SetAppProfile_PersistsOverrideValues(string clientAppId, string presetName, int maxConcurrency, int backgroundCap, int requestsPerMinute, int tokensPerMinute, bool expectedSuccess)
    {
        var state = CreateState();

        var command = state.SetAppProfile(new SetAppProfileRequest
        {
            ClientAppId = clientAppId,
            ProcessName = "proc",
            Signature = "sig",
            PresetName = presetName,
            MaxConcurrency = maxConcurrency,
            BackgroundCap = backgroundCap,
            RequestsPerMinute = requestsPerMinute,
            TokensPerMinute = tokensPerMinute
        });

        Assert.Equal(expectedSuccess, command.Success);

        var snapshot = state.GetProfiles();
        if (!expectedSuccess)
        {
            Assert.DoesNotContain(snapshot.Overrides, overrideEntry => string.IsNullOrWhiteSpace(overrideEntry.ClientAppId));
            return;
        }

        var overrideEntry = Assert.Single(snapshot.Overrides, overrideItem => overrideItem.ClientAppId == clientAppId);
        Assert.Equal(presetName, overrideEntry.PresetName);
        Assert.Equal(maxConcurrency, overrideEntry.MaxConcurrency);
        Assert.Equal(backgroundCap, overrideEntry.BackgroundCap);
        Assert.Equal(requestsPerMinute, overrideEntry.RequestsPerMinute);
        Assert.Equal(tokensPerMinute, overrideEntry.TokensPerMinute);
    }

    [Theory]
    [InlineData(1, 10, 10)]
    [InlineData(2, 20, 20)]
    [InlineData(3, 30, 20)]
    [InlineData(4, 40, 30)]
    [InlineData(5, 50, 40)]
    [InlineData(6, 60, 50)]
    [InlineData(7, 70, 60)]
    [InlineData(8, 80, 70)]
    [InlineData(9, 90, 80)]
    [InlineData(10, 100, 90)]
    public void GetStatus_MaintainsCoreInvariantsAcrossTicks(int ticks, int interactiveRequests, int backgroundRequests)
    {
        var state = CreateState();
        state.SimulateFlood(new SimulateFloodRequest
        {
            InteractiveRequests = interactiveRequests,
            BackgroundRequests = backgroundRequests,
            ClientAppId = "status-test",
            ProcessName = "status-test",
            Signature = "sig"
        });

        StatusSnapshot? status = null;
        for (var index = 0; index < ticks; index++)
        {
            status = state.GetStatus();
        }

        Assert.NotNull(status);
        Assert.True(status!.Connected);
        Assert.True(status.DaemonRunning);
        Assert.InRange(status.CpuPercent, 18, 98);
        Assert.InRange(status.AvailableRamPercent, 8, 95);
        Assert.InRange(status.ActiveCalls, 0, status.EffectiveConcurrency);
        Assert.InRange(status.InteractiveQueueDepth + status.BackgroundQueueDepth, 0, 500);
        Assert.True(status.Uptime >= TimeSpan.Zero);
        Assert.Contains(status.HeatState, new[] { HeatState.Calm, HeatState.Warm, HeatState.Spicy });
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void DiagnosticsPreview_ReflectsRequestedFlags(bool includePaths, bool includeVerbose)
    {
        var state = CreateState();

        var preview = state.GetDiagnosticsPreview(includePaths, includeVerbose);

        Assert.Equal(includePaths, preview.IncludePaths);
        Assert.Equal(includeVerbose, preview.IncludeVerbose);
        Assert.NotEmpty(preview.IncludedSections);
        Assert.NotEmpty(preview.RedactionRules);
        Assert.False(string.IsNullOrWhiteSpace(preview.Summary));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void DiagnosticsExport_WritesBundle(bool includePaths, bool includeVerbose)
    {
        var state = CreateState();

        var result = state.ExportDiagnostics(includePaths, includeVerbose);

        Assert.True(result.Exported);
        Assert.True(File.Exists(result.OutputPath));
        Assert.True(result.BytesWritten > 0);
        Assert.Equal("support bundle exported", result.Message);

        File.Delete(result.OutputPath);
    }

    [Theory]
    [InlineData(-10)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(1000)]
    [InlineData(5000)]
    public void EventTail_ClampsRequestedCount(int requestedCount)
    {
        var state = CreateState();
        state.GetStatus();
        state.GetStatus();
        state.GetStatus();

        var response = state.GetEvents(requestedCount);

        Assert.NotEmpty(response.Events);
        Assert.True(response.Events.Count <= 1000);
        Assert.True(response.Events.SequenceEqual(response.Events.OrderBy(eventItem => eventItem.Id)));
    }
}
