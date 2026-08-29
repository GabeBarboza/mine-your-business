using MineYourBusiness.Application;
using Xunit;

namespace MineYourBusiness.Domain.Tests;

public sealed class PlayerExperienceTests
{
    [Fact]
    public void PreferencesAreNormalizedAndRoundTripThroughJson()
    {
        string directory = Path.Combine(Path.GetTempPath(), "mine-your-business-tests", Guid.NewGuid().ToString("N"));
        string path = Path.Combine(directory, "settings.json");

        try
        {
            PlayerPreferencesStore.Save(path, new PlayerPreferences
            {
                UiScale = 3,
                MasterVolume = -1,
                HighContrast = true,
                TutorialCompleted = true,
            });

            PlayerPreferences loaded = PlayerPreferencesStore.Load(path);

            Assert.Equal(1.5, loaded.UiScale);
            Assert.Equal(0, loaded.MasterVolume);
            Assert.True(loaded.HighContrast);
            Assert.True(loaded.TutorialCompleted);
            Assert.False(loaded.TelemetryEnabled);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }

    [Fact]
    public void InvalidPreferencesFallBackToPrivacyPreservingDefaults()
    {
        string path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "not json");
            PlayerPreferences loaded = PlayerPreferencesStore.Load(path);

            Assert.False(loaded.TelemetryEnabled);
            Assert.False(loaded.TutorialCompleted);
            Assert.Equal(1, loaded.UiScale);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void TelemetryWritesOnlyAfterExplicitOptInAndCanBeDeleted()
    {
        string directory = Path.Combine(Path.GetTempPath(), "mine-your-business-tests", Guid.NewGuid().ToString("N"));
        string path = Path.Combine(directory, "beta-telemetry.jsonl");
        PrivacyTelemetry telemetry = new(path, false, () => DateTimeOffset.UnixEpoch);

        try
        {
            Assert.False(telemetry.Track("match_started"));
            Assert.False(File.Exists(path));

            telemetry.SetEnabled(true);
            Assert.True(telemetry.Track("match_started", new Dictionary<string, long> { ["player_count"] = 3 }));

            string line = File.ReadAllText(path);
            Assert.Contains("match_started", line, StringComparison.Ordinal);
            Assert.Contains("player_count", line, StringComparison.Ordinal);
            Assert.DoesNotContain("playerName", line, StringComparison.OrdinalIgnoreCase);

            telemetry.DeleteStoredData();
            Assert.False(File.Exists(path));
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }

    [Fact]
    public void TelemetryRejectsFreeTextFields()
    {
        string path = Path.Combine(Path.GetTempPath(), "mine-your-business-tests", Guid.NewGuid().ToString("N"), "telemetry.jsonl");
        PrivacyTelemetry telemetry = new(path, true);

        Assert.Throws<ArgumentException>(() => telemetry.Track("player joined the room"));
        Assert.Throws<ArgumentException>(() => telemetry.Track(
            "room_joined",
            new Dictionary<string, long> { ["player name"] = 1 }));
        Assert.False(File.Exists(path));
    }
}
