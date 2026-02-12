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

    public DaemonState()
    {
        _runtimeDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LeaseGateLite");
        _configPath = Path.Combine(_runtimeDirectory, "leasegatelite.config.json");
        _diagnosticsDirectory = Path.Combine(_runtimeDirectory, "diagnostics");
        Directory.CreateDirectory(_runtimeDirectory);
        Directory.CreateDirectory(_diagnosticsDirectory);

        _config = LoadConfig() ?? DefaultConfig();
        _running = true;
        _startedAtUtc = DateTimeOffset.UtcNow;
        _effectiveConcurrency = _config.MaxConcurrency;

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

        _activeCalls = Math.Clamp(_activeCalls + _random.Next(-2, 3), 0, _config.MaxConcurrency + 8);
        _interactiveQueueDepth = Math.Clamp(_interactiveQueueDepth + _random.Next(-2, 3), 0, 25);
        _backgroundQueueDepth = Math.Clamp(_backgroundQueueDepth + _random.Next(-2, 4), 0, 40);

        var pressure = Math.Max(_cpuPercent, 100 - _availableRamPercent);
        var target = _config.MaxConcurrency;

        if (pressure >= _config.HardThresholdPercent)
        {
            target = Math.Max(1, _config.MaxConcurrency / 2);
            _adaptiveClampActive = true;
            _lastThrottleReason = _cpuPercent >= (100 - _availableRamPercent) ? ThrottleReason.CpuPressure : ThrottleReason.MemoryPressure;
        }
        else if (pressure >= _config.SoftThresholdPercent)
        {
            target = Math.Max(1, _config.MaxConcurrency - Math.Max(1, _config.MaxConcurrency / 3));
            _adaptiveClampActive = true;
            _lastThrottleReason = _cpuPercent >= (100 - _availableRamPercent) ? ThrottleReason.CpuPressure : ThrottleReason.MemoryPressure;
        }
        else
        {
            _adaptiveClampActive = false;
            _lastThrottleReason = ThrottleReason.None;
        }

        var smoothingFactor = Math.Clamp(_config.SmoothingPercent, 5, 95) / 100.0;
        _effectiveConcurrency = (int)Math.Round((_effectiveConcurrency * smoothingFactor) + (target * (1.0 - smoothingFactor)));
        _effectiveConcurrency = Math.Clamp(_effectiveConcurrency, _running ? 1 : 0, _config.MaxConcurrency);
    }

    private HeatState DeriveHeatState()
    {
        var pressure = Math.Max(_cpuPercent, 100 - _availableRamPercent);
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

    private void AddEvent(EventCategory category, string level, string message, string detail)
    {
        _events.Add(new EventEntry
        {
            Id = _nextEventId++,
            TimestampUtc = DateTimeOffset.UtcNow,
            Category = category,
            Level = level,
            Message = message,
            Detail = detail
        });

        if (_events.Count > 2000)
        {
            _events.RemoveRange(0, _events.Count - 2000);
        }
    }
}
