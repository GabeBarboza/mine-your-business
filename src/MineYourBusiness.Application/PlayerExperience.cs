using System.Text.Json;
using System.Text.Json.Serialization;

namespace MineYourBusiness.Application;

/// <summary>Persistent, player-controlled presentation and privacy preferences.</summary>
public sealed record PlayerPreferences
{
    public double UiScale { get; init; } = 1.0;

    public double MasterVolume { get; init; } = 0.75;

    public bool HighContrast { get; init; }

    public bool ReducedMotion { get; init; }

    public bool TelemetryEnabled { get; init; }

    public bool TutorialCompleted { get; init; }

    public PlayerPreferences Normalize() => this with
    {
        UiScale = Math.Clamp(UiScale, 0.8, 1.5),
        MasterVolume = Math.Clamp(MasterVolume, 0.0, 1.0),
    };
}

/// <summary>Version-tolerant JSON persistence with an atomic replacement.</summary>
public static class PlayerPreferencesStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    public static PlayerPreferences Load(string path)
    {
        if (!File.Exists(path))
        {
            return new PlayerPreferences();
        }

        try
        {
            string json = File.ReadAllText(path);
            return (JsonSerializer.Deserialize<PlayerPreferences>(json, JsonOptions) ?? new PlayerPreferences()).Normalize();
        }
        catch (JsonException)
        {
            return new PlayerPreferences();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return new PlayerPreferences();
        }
    }

    public static void Save(string path, PlayerPreferences preferences)
    {
        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string temporaryPath = path + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(preferences.Normalize(), JsonOptions));
        File.Move(temporaryPath, path, true);
    }
}

/// <summary>
/// Opt-in, local-only beta telemetry. It accepts only named counters and never
/// records player names, room codes, addresses, hands, roles, or free text.
/// </summary>
public sealed class PrivacyTelemetry
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly string _path;
    private readonly Func<DateTimeOffset> _clock;

    public PrivacyTelemetry(string path, bool isEnabled, Func<DateTimeOffset>? clock = null)
    {
        _path = path;
        IsEnabled = isEnabled;
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
    }

    public bool IsEnabled { get; private set; }

    public string StoragePath => _path;

    public void SetEnabled(bool enabled) => IsEnabled = enabled;

    public bool Track(string eventName, IReadOnlyDictionary<string, long>? counters = null)
    {
        if (!IsEnabled)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(eventName) || eventName.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not '_' and not '-'))
        {
            throw new ArgumentException("Telemetry event names may contain only letters, numbers, underscores, and hyphens.", nameof(eventName));
        }

        Dictionary<string, long>? safeCounters = counters?.ToDictionary(
            pair => SanitizeCounterName(pair.Key),
            pair => pair.Value,
            StringComparer.Ordinal);
        TelemetryEntry entry = new(eventName, _clock(), safeCounters);
        string? directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.AppendAllText(_path, JsonSerializer.Serialize(entry, JsonOptions) + Environment.NewLine);
        return true;
    }

    public void DeleteStoredData()
    {
        if (File.Exists(_path))
        {
            File.Delete(_path);
        }
    }

    private static string SanitizeCounterName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not '_' and not '-'))
        {
            throw new ArgumentException("Telemetry counter names may contain only letters, numbers, underscores, and hyphens.", nameof(name));
        }

        return name;
    }

    private sealed record TelemetryEntry(
        string Event,
        DateTimeOffset Timestamp,
        IReadOnlyDictionary<string, long>? Counters);
}
