using System.Text.Json;
using LeaseGateLite.Contracts;

namespace LeaseGateLite.Daemon;

public sealed class DaemonState
{
    private readonly object _lock = new();
    private readonly Random _random = new();
    private readonly List<EventEntry> _events = new();
    private readonly string _runtimeDirectory;
    private readonly string _configPath;
    private readonly string _diagnosticsDirectory;
    private readonly string _eventLogPath;
    private LiteConfig _config;
    private bool _running;
    private DateTimeOffset _startedAtUtc;
    private int _activeCalls;
    private int _interactiveQueueDepth;
    private int _backgroundQueueDepth;
    private int _effectiveConcurrency;
    private int _cpuPercent;
    private int _availableRamPercent;
    private ThrottleReason _lastThrottleReason = ThrottleReason.None;
    private bool _adaptiveClampActive;
    private PressureMode _pressureMode = PressureMode.Normal;
    private long _nextEventId = 1;
    private DateTimeOffset _lastPressureSampleUtc = DateTimeOffset.MinValue;
    private DateTimeOffset _lastLeaseSignalUtc = DateTimeOffset.MinValue;
    private bool _lastClampState;
    private double _smoothedPressure;
    private int _interactiveDemand;
    private int _backgroundDemand;

    public DaemonState()
    {
        _runtimeDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LeaseGateLite");
        _configPath = Path.Combine(_runtimeDirectory, "leasegatelite.config.json");
        _diagnosticsDirectory = Path.Combine(_runtimeDirectory, "diagnostics");
        _eventLogPath = Path.Combine(_runtimeDirectory, "leasegatelite-events.jsonl");
        Directory.CreateDirectory(_runtimeDirectory);
        Directory.CreateDirectory(_diagnosticsDirectory);

        _config = LoadConfig() ?? DefaultConfig();
        _running = true;
        _startedAtUtc = DateTimeOffset.UtcNow;
        _effectiveConcurrency = _config.MaxConcurrency;
        _smoothedPressure = 45;

        AddEvent(EventCategory.Service, "info", "daemon started", "startup");
    }

    public StatusSnapshot GetStatus()
    {
        lock (_lock)
        {
            SimulateTick();

            return new StatusSnapshot
            {
                Connected = true,
                DaemonRunning = _running,
                DaemonVersion = "0.2.0-lite",
                Uptime = _running ? DateTimeOffset.UtcNow - _startedAtUtc : TimeSpan.Zero,
                Endpoint = "http://localhost:5177",
                ConfigFilePath = _configPath,
                HeatState = DeriveHeatState(),
                ActiveCalls = _activeCalls,
                InteractiveQueueDepth = _interactiveQueueDepth,
                BackgroundQueueDepth = _backgroundQueueDepth,
                EffectiveConcurrency = _effectiveConcurrency,
                CpuPercent = _cpuPercent,
                AvailableRamPercent = _availableRamPercent,
                LastThrottleReason = _lastThrottleReason,
                AdaptiveClampActive = _adaptiveClampActive,
                PressureMode = _pressureMode
            };
        }
    }

    public LiteConfig GetConfig()
    {
        lock (_lock)
        {
            return Clone(_config);
        }
    }

    public LiteConfig GetDefaults()
    {
        return DefaultConfig();
    }

    public ConfigApplyResponse ApplyConfig(LiteConfig incoming)
    {
        lock (_lock)
        {
            return ApplyConfigCore(incoming);
        }
    }

    public ConfigApplyResponse ResetConfig(bool apply)
    {
        lock (_lock)
        {
            var defaults = DefaultConfig();
            if (apply)
            {
                _config = defaults;
                PersistConfig();
                AddEvent(EventCategory.Config, "info", "configuration reset to defaults", "local user");
                return new ConfigApplyResponse
                {
                    Success = true,
                    Message = "defaults applied",
                    AppliedConfig = Clone(_config)
                };
            }

            AddEvent(EventCategory.Config, "info", "defaults requested", "local user");
            return new ConfigApplyResponse
            {
                Success = true,
                Message = "defaults returned",
                AppliedConfig = defaults
            };
        }
    }

    public List<PresetDefinition> GetPresets()
    {
        return new List<PresetDefinition>
        {
            new()
            {
                Name = "Quiet",
                Description = "Laptop-first profile with aggressive cooldown and tighter caps.",
                Config = new LiteConfig
                {
                    ConfigVersion = 1,
                    MaxConcurrency = 4,
                    InteractiveReserve = 2,
                    BackgroundCap = 2,
                    CooldownBehavior = CooldownBehavior.Aggressive,
                    SoftThresholdPercent = 60,
                    HardThresholdPercent = 80,
                    RecoveryRatePercent = 15,
                    SmoothingPercent = 70,
                    MaxOutputTokensClamp = 512,
                    MaxPromptTokensClamp = 2048,
                    OverflowBehavior = OverflowBehavior.QueueOnly,
                    MaxRetries = 1,
                    RetryBackoffMs = 700,
                    RequestsPerMinute = 80,
                    TokensPerMinute = 80_000,
                    BurstAllowance = 8
                }
            },
            new()
            {
                Name = "Balanced",
                Description = "Default home profile for mixed interactive and background work.",
                Config = new LiteConfig()
            },
            new()
            {
                Name = "Performance",
                Description = "Desktop profile with higher throughput and lighter damping.",
                Config = new LiteConfig
                {
                    ConfigVersion = 1,
                    MaxConcurrency = 14,
                    InteractiveReserve = 2,
                    BackgroundCap = 12,
                    CooldownBehavior = CooldownBehavior.Off,
                    SoftThresholdPercent = 78,
                    HardThresholdPercent = 95,
                    RecoveryRatePercent = 35,
                    SmoothingPercent = 25,
                    MaxOutputTokensClamp = 2048,
                    MaxPromptTokensClamp = 8192,
                    OverflowBehavior = OverflowBehavior.TrimOldest,
                    MaxRetries = 3,
                    RetryBackoffMs = 400,
                    RequestsPerMinute = 240,
                    TokensPerMinute = 240_000,
                    BurstAllowance = 20
                }
            }
        };
    }

    public PresetPreviewResponse PreviewPreset(string name)
    {
        lock (_lock)
        {
            var preset = GetPresets().FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
            if (preset is null)
            {
                return new PresetPreviewResponse { Name = name };
            }

            return new PresetPreviewResponse
            {
                Name = preset.Name,
                Diffs = BuildDiff(_config, preset.Config)
            };
        }
    }

    public ConfigApplyResponse ApplyPreset(string name)
    {
        lock (_lock)
        {
            var preset = GetPresets().FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
            if (preset is null)
            {
                return new ConfigApplyResponse
                {
                    Success = false,
                    Message = "preset not found",
                    AppliedConfig = Clone(_config),
                    Errors = new List<ValidationError>
                    {
                        new() { Field = "name", Message = "preset name is unknown" }
                    }
                };
            }

            var applied = ApplyConfigCore(preset.Config);
            if (applied.Success)
            {
                AddEvent(EventCategory.Preset, "info", "preset applied", preset.Name);
            }

            return applied;
        }
    }

    private ConfigApplyResponse ApplyConfigCore(LiteConfig incoming)
    {
        var errors = ValidateConfig(incoming);
        if (errors.Count > 0)
        {
            return new ConfigApplyResponse
            {
                Success = false,
                Message = "config validation failed",
                AppliedConfig = Clone(_config),
                Errors = errors
            };
        }

        _config = Clone(incoming);
        PersistConfig();
        _effectiveConcurrency = Math.Clamp(_effectiveConcurrency, 1, Math.Max(1, _config.MaxConcurrency));
        AddEvent(EventCategory.Config, "info", "configuration applied", "local user");

        return new ConfigApplyResponse
        {
            Success = true,
            Message = "configuration applied",
            AppliedConfig = Clone(_config)
        };
    }

    public ServiceCommandResponse Start()
    {
        lock (_lock)
        {
            if (_running)
            {
                return new ServiceCommandResponse { Success = true, Message = "daemon already running" };
            }

            _running = true;
            _startedAtUtc = DateTimeOffset.UtcNow;
            AddEvent(EventCategory.Service, "info", "daemon started", "service command");
            return new ServiceCommandResponse { Success = true, Message = "daemon started" };
        }
    }

    public ServiceCommandResponse Stop()
    {
        lock (_lock)
        {
            if (!_running)
            {
                return new ServiceCommandResponse { Success = true, Message = "daemon already stopped" };
            }

            _running = false;
            _activeCalls = 0;
            _interactiveQueueDepth = 0;
            _backgroundQueueDepth = 0;
            _effectiveConcurrency = 0;
            _lastThrottleReason = ThrottleReason.ManualClamp;
            AddEvent(EventCategory.Service, "warn", "daemon stopped", "service command");
            return new ServiceCommandResponse { Success = true, Message = "daemon stopped" };
        }
    }

    public ServiceCommandResponse Restart()
    {
        lock (_lock)
        {
            _running = true;
            _startedAtUtc = DateTimeOffset.UtcNow;
            _lastThrottleReason = ThrottleReason.None;
            AddEvent(EventCategory.Service, "info", "daemon restarted", "service command");
            return new ServiceCommandResponse { Success = true, Message = "daemon restarted" };
        }
    }

    public EventTailResponse GetEvents(int n)
    {
        lock (_lock)
        {
            var limit = Math.Clamp(n, 1, 1000);
            var items = _events.TakeLast(limit).ToList();
            return new EventTailResponse { Events = items };
        }
    }

    public EventStreamResponse GetEventsSince(long sinceId, int timeoutMs)
    {
        lock (_lock)
        {
            var timeout = Math.Clamp(timeoutMs, 100, 10_000);
            var deadline = DateTimeOffset.UtcNow.AddMilliseconds(timeout);

            while (true)
            {
                var events = _events.Where(e => e.Id > sinceId).Take(300).ToList();
                if (events.Count > 0)
                {
                    return new EventStreamResponse
                    {
                        LastEventId = events[^1].Id,
                        Events = events
                    };
                }

                var remaining = deadline - DateTimeOffset.UtcNow;
                if (remaining <= TimeSpan.Zero)
                {
                    return new EventStreamResponse
                    {
                        LastEventId = sinceId,
                        Events = new List<EventEntry>()
                    };
                }

                Monitor.Wait(_lock, remaining);
            }
        }
    }

    public DiagnosticsExportResponse ExportDiagnostics()
    {
        lock (_lock)
        {
            var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss");
            var outputPath = Path.Combine(_diagnosticsDirectory, $"leasegatelite-diagnostics-{timestamp}.json");
            var payload = new
            {
                generatedAtUtc = DateTimeOffset.UtcNow,
                status = GetStatus(),
                config = _config,
                events = _events.TakeLast(200).ToList()
            };

            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(outputPath, json);

            var bytes = new FileInfo(outputPath).Length;
            AddEvent(EventCategory.Diagnostics, "info", "diagnostics exported", outputPath);

            return new DiagnosticsExportResponse
            {
                Exported = true,
                OutputPath = outputPath,
                BytesWritten = bytes,
                Message = "diagnostics exported"
            };
        }
    }

    public ServiceCommandResponse SetPressureMode(PressureMode mode)
    {
        lock (_lock)
        {
            _pressureMode = mode;
            AddEvent(EventCategory.Pressure, "info", "pressure mode updated", mode.ToString());
            return new ServiceCommandResponse
            {
                Success = true,
                Message = $"pressure mode set to {mode}"
            };
        }
    }

    public ServiceCommandResponse SimulateFlood(SimulateFloodRequest request)
    {
        lock (_lock)
        {
            _interactiveDemand = Math.Clamp(request.InteractiveRequests, 0, 200);
            _backgroundDemand = Math.Clamp(request.BackgroundRequests, 0, 200);
            AddEvent(EventCategory.Lease, "warn", "flood simulation triggered", $"interactive={_interactiveDemand}; background={_backgroundDemand}");
            return new ServiceCommandResponse
            {
                Success = true,
                Message = "flood simulation queued"
            };
        }
    }

    private void SimulateTick()
    {
        if (!_running)
        {
            return;
        }

        var delta = _pressureMode == PressureMode.Spiky ? 18 : 8;
        _cpuPercent = Math.Clamp(_cpuPercent + _random.Next(-delta, delta + 1), 18, 98);
        _availableRamPercent = Math.Clamp(_availableRamPercent + _random.Next(-delta, delta + 1), 8, 95);

        if (_cpuPercent == 0)
        {
            _cpuPercent = _random.Next(28, 65);
        }

        if (_availableRamPercent == 0)
        {
            _availableRamPercent = _random.Next(35, 80);
        }

        _interactiveDemand = Math.Clamp(_interactiveDemand + _random.Next(-2, 4), 0, 24);
        _backgroundDemand = Math.Clamp(_backgroundDemand + _random.Next(-2, 5), 0, 40);

        var rawPressure = Math.Max(_cpuPercent, 100 - _availableRamPercent);
        var alpha = 0.05 + ((100 - Math.Clamp(_config.SmoothingPercent, 5, 95)) / 100.0) * 0.55;
        _smoothedPressure = (_smoothedPressure * (1 - alpha)) + (rawPressure * alpha);

        var soft = _config.SoftThresholdPercent;
        var hard = _config.HardThresholdPercent;
        var target = _config.MaxConcurrency;

        if (_smoothedPressure > soft)
        {
            var ratio = Math.Clamp((_smoothedPressure - soft) / Math.Max(1.0, hard - soft), 0.0, 1.0);
            var reduction = (int)Math.Round((target - 1) * ratio);
            target = Math.Max(1, target - reduction);
            _adaptiveClampActive = true;
            _lastThrottleReason = _cpuPercent >= (100 - _availableRamPercent) ? ThrottleReason.CpuPressure : ThrottleReason.MemoryPressure;
        }
        else
        {
            _adaptiveClampActive = false;
            _lastThrottleReason = ThrottleReason.None;
        }

        if (_smoothedPressure >= hard)
        {
            if (_config.CooldownBehavior != CooldownBehavior.Off)
            {
                target = Math.Max(1, Math.Min(target, _config.InteractiveReserve + 1));
                _lastThrottleReason = ThrottleReason.Cooldown;
            }
        }

        if (_config.RequestsPerMinute < 30)
        {
            target = Math.Max(1, Math.Min(target, _config.MaxConcurrency / 2));
            _lastThrottleReason = ThrottleReason.RateLimit;
        }

        var recoverLerp = Math.Clamp(_config.RecoveryRatePercent, 5, 100) / 100.0;
        _effectiveConcurrency = (int)Math.Round((_effectiveConcurrency * (1 - recoverLerp)) + (target * recoverLerp));
        _effectiveConcurrency = Math.Clamp(_effectiveConcurrency, _running ? 1 : 0, _config.MaxConcurrency);

        var reservedInteractive = Math.Min(_config.InteractiveReserve, _effectiveConcurrency);
        var availableForBackground = Math.Max(0, Math.Min(_config.BackgroundCap, _effectiveConcurrency - reservedInteractive));

        var activeInteractive = Math.Min(_interactiveDemand, Math.Max(1, reservedInteractive));
        var activeBackground = Math.Min(_backgroundDemand, availableForBackground);

        _activeCalls = activeInteractive + activeBackground;
        _interactiveQueueDepth = Math.Max(0, _interactiveDemand - activeInteractive);
        _backgroundQueueDepth = Math.Max(0, _backgroundDemand - activeBackground);

        EmitPressureSample((int)Math.Round(_smoothedPressure));
        EmitLeaseSignals();
        EmitClampTransitions();
    }

    private void EmitPressureSample(int pressure)
    {
        var now = DateTimeOffset.UtcNow;
        if (now - _lastPressureSampleUtc < TimeSpan.FromSeconds(2))
        {
            return;
        }

        _lastPressureSampleUtc = now;
        AddEvent(
            EventCategory.Pressure,
            "info",
            "pressure sample",
            $"cpu={_cpuPercent}; availableRam={_availableRamPercent}; pressure={pressure}; effectiveConcurrency={_effectiveConcurrency}");
    }

    private void EmitLeaseSignals()
    {
        var now = DateTimeOffset.UtcNow;
        if (now - _lastLeaseSignalUtc < TimeSpan.FromSeconds(2))
        {
            return;
        }

        _lastLeaseSignalUtc = now;

        if (_activeCalls < _effectiveConcurrency)
        {
            AddEvent(EventCategory.Lease, "info", "lease granted", $"active={_activeCalls}; effective={_effectiveConcurrency}");
            return;
        }

        if (_interactiveQueueDepth + _backgroundQueueDepth > 15)
        {
            AddEvent(EventCategory.Lease, "warn", "lease denied", "queue pressure high");
        }
        else
        {
            AddEvent(EventCategory.Lease, "info", "lease queued", $"interactive={_interactiveQueueDepth}; background={_backgroundQueueDepth}");
        }
    }

    private void EmitClampTransitions()
    {
        if (_adaptiveClampActive == _lastClampState)
        {
            return;
        }

        _lastClampState = _adaptiveClampActive;
        if (_adaptiveClampActive)
        {
            AddEvent(EventCategory.Pressure, "warn", "adaptive clamp engaged", _lastThrottleReason.ToString());
        }
        else
        {
            AddEvent(EventCategory.Pressure, "info", "adaptive clamp released", "pressure normalized");
        }
    }

    private HeatState DeriveHeatState()
    {
        var pressure = _smoothedPressure;
        if (pressure >= _config.HardThresholdPercent)
        {
            return HeatState.Spicy;
        }

        if (pressure >= _config.SoftThresholdPercent)
        {
            return HeatState.Warm;
        }

        return HeatState.Calm;
    }

    private List<ValidationError> ValidateConfig(LiteConfig incoming)
    {
        var errors = new List<ValidationError>();

        if (incoming.ConfigVersion != 1)
        {
            errors.Add(new ValidationError { Field = nameof(LiteConfig.ConfigVersion), Message = "unsupported configVersion; expected 1" });
        }

        ValidateRange(errors, nameof(LiteConfig.MaxConcurrency), incoming.MaxConcurrency, 1, 32);
        ValidateRange(errors, nameof(LiteConfig.InteractiveReserve), incoming.InteractiveReserve, 0, 16);
        ValidateRange(errors, nameof(LiteConfig.BackgroundCap), incoming.BackgroundCap, 0, 32);
        ValidateRange(errors, nameof(LiteConfig.SoftThresholdPercent), incoming.SoftThresholdPercent, 40, 95);
        ValidateRange(errors, nameof(LiteConfig.HardThresholdPercent), incoming.HardThresholdPercent, 50, 99);
        ValidateRange(errors, nameof(LiteConfig.RecoveryRatePercent), incoming.RecoveryRatePercent, 5, 100);
        ValidateRange(errors, nameof(LiteConfig.SmoothingPercent), incoming.SmoothingPercent, 5, 95);
        ValidateRange(errors, nameof(LiteConfig.MaxOutputTokensClamp), incoming.MaxOutputTokensClamp, 64, 4096);
        ValidateRange(errors, nameof(LiteConfig.MaxPromptTokensClamp), incoming.MaxPromptTokensClamp, 256, 32000);
        ValidateRange(errors, nameof(LiteConfig.MaxRetries), incoming.MaxRetries, 0, 8);
        ValidateRange(errors, nameof(LiteConfig.RetryBackoffMs), incoming.RetryBackoffMs, 100, 5000);
        ValidateRange(errors, nameof(LiteConfig.RequestsPerMinute), incoming.RequestsPerMinute, 10, 1000);
        ValidateRange(errors, nameof(LiteConfig.TokensPerMinute), incoming.TokensPerMinute, 1000, 500000);
        ValidateRange(errors, nameof(LiteConfig.BurstAllowance), incoming.BurstAllowance, 1, 100);

        if (incoming.HardThresholdPercent <= incoming.SoftThresholdPercent)
        {
            errors.Add(new ValidationError
            {
                Field = nameof(LiteConfig.HardThresholdPercent),
                Message = "hard threshold must be greater than soft threshold"
            });
        }

        if (incoming.InteractiveReserve > incoming.MaxConcurrency)
        {
            errors.Add(new ValidationError
            {
                Field = nameof(LiteConfig.InteractiveReserve),
                Message = "interactive reserve cannot exceed max concurrency"
            });
        }

        if (incoming.BackgroundCap > incoming.MaxConcurrency)
        {
            errors.Add(new ValidationError
            {
                Field = nameof(LiteConfig.BackgroundCap),
                Message = "background cap cannot exceed max concurrency"
            });
        }

        return errors;
    }

    private static void ValidateRange(List<ValidationError> errors, string field, int value, int min, int max)
    {
        if (value < min || value > max)
        {
            errors.Add(new ValidationError { Field = field, Message = $"must be between {min} and {max}" });
        }
    }

    private void PersistConfig()
    {
        var json = JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_configPath, json);
    }

    private LiteConfig? LoadConfig()
    {
        if (!File.Exists(_configPath))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(_configPath);
            return JsonSerializer.Deserialize<LiteConfig>(json);
        }
        catch
        {
            return null;
        }
    }

    private static LiteConfig DefaultConfig()
    {
        return new LiteConfig();
    }

    private static LiteConfig Clone(LiteConfig config)
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

    private static List<PresetDiffItem> BuildDiff(LiteConfig current, LiteConfig target)
    {
        var diffs = new List<PresetDiffItem>();
        var properties = typeof(LiteConfig).GetProperties();
        foreach (var property in properties)
        {
            var before = property.GetValue(current)?.ToString() ?? string.Empty;
            var after = property.GetValue(target)?.ToString() ?? string.Empty;
            if (!string.Equals(before, after, StringComparison.Ordinal))
            {
                diffs.Add(new PresetDiffItem
                {
                    Field = property.Name,
                    Before = before,
                    After = after
                });
            }
        }

        return diffs;
    }

    private void AddEvent(EventCategory category, string level, string message, string detail)
    {
        var entry = new EventEntry
        {
            Id = _nextEventId++,
            TimestampUtc = DateTimeOffset.UtcNow,
            Category = category,
            Level = level,
            Message = message,
            Detail = detail
        };

        _events.Add(entry);

        if (_events.Count > 2000)
        {
            _events.RemoveRange(0, _events.Count - 2000);
        }

        File.AppendAllText(_eventLogPath, JsonSerializer.Serialize(entry) + Environment.NewLine);
        Monitor.PulseAll(_lock);
    }
}
