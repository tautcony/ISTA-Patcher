// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTestA;

using System.Text.Json;
using ISTAvalon.Models;
using ISTAvalon.Services;

/// <summary>
/// Tests for GuiSettingsService focusing on uncovered branches:
/// Save(), LoadPresets() when file exists, OverlayUserPreferences() when file exists.
/// Files are placed into AppContext.BaseDirectory which is the test output directory.
/// </summary>
public class GuiSettingsServiceTests
{
    private string _settingsFilePath = null!;
    private string _userPrefsFilePath = null!;

    [SetUp]
    public void Setup()
    {
        _settingsFilePath = Path.Combine(AppContext.BaseDirectory, "gui-settings.json");
        _userPrefsFilePath = Path.Combine(AppContext.BaseDirectory, "user-preferences.json");

        // Remove user-prefs before each test so tests are isolated
        if (File.Exists(_userPrefsFilePath))
        {
            File.Delete(_userPrefsFilePath);
        }
    }

    [TearDown]
    public void TearDown()
    {
        if (File.Exists(_userPrefsFilePath))
        {
            File.Delete(_userPrefsFilePath);
        }
    }

    // ────────────── Load without files ──────────────

    [Test]
    public void Load_NoSettingsFile_ReturnsDefault()
    {
        // Temporarily rename the settings file if it exists
        var backup = _settingsFilePath + ".bak";
        var existed = File.Exists(_settingsFilePath);
        if (existed)
        {
            File.Move(_settingsFilePath, backup);
        }

        try
        {
            var settings = GuiSettingsService.Load();
            Assert.That(settings.Theme, Is.EqualTo("Default"));
            Assert.That(settings.Presets, Is.Not.Null);
        }
        finally
        {
            if (existed)
            {
                File.Move(backup, _settingsFilePath);
            }
        }
    }

    // ────────────── Load with gui-settings.json ──────────────

    [Test]
    public void Load_WithSettingsFile_ParsesThemeAndPresets()
    {
        var backup = _settingsFilePath + ".bak";
        var existed = File.Exists(_settingsFilePath);
        if (existed)
        {
            File.Move(_settingsFilePath, backup);
        }

        try
        {
            var json = """
                {
                  "Theme": "Dark",
                  "Presets": {
                    "patch": { "PatchType": "Toyota" }
                  }
                }
                """;
            File.WriteAllText(_settingsFilePath, json);

            var settings = GuiSettingsService.Load();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(settings.Theme, Is.EqualTo("Dark"));
                Assert.That(settings.Presets["patch"]["PatchType"], Is.EqualTo("Toyota"));
            }
        }
        finally
        {
            File.Delete(_settingsFilePath);
            if (existed)
            {
                File.Move(backup, _settingsFilePath);
            }
        }
    }

    [Test]
    public void Load_WithInvalidSettingsJson_FallsBackToDefault()
    {
        var backup = _settingsFilePath + ".bak";
        var existed = File.Exists(_settingsFilePath);
        if (existed)
        {
            File.Move(_settingsFilePath, backup);
        }

        try
        {
            File.WriteAllText(_settingsFilePath, "not-valid-json{{{");

            var settings = GuiSettingsService.Load();

            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.Theme, Is.EqualTo("Default"));
        }
        finally
        {
            File.Delete(_settingsFilePath);
            if (existed)
            {
                File.Move(backup, _settingsFilePath);
            }
        }
    }

    // ────────────── Save ──────────────

    [Test]
    public void Save_WritesUserPreferencesJson()
    {
        var settings = new GuiSettings { Theme = "Light" };

        GuiSettingsService.Save(settings);

        Assert.That(File.Exists(_userPrefsFilePath), Is.True);
        var json = File.ReadAllText(_userPrefsFilePath);
        var prefs = JsonSerializer.Deserialize<UserPreferences>(json);
        Assert.That(prefs?.Theme, Is.EqualTo("Light"));
    }

    // ────────────── OverlayUserPreferences ──────────────

    [Test]
    public void Load_WithUserPrefsFile_OverlaysTheme()
    {
        var prefs = new UserPreferences { Theme = "Dark" };
        File.WriteAllText(_userPrefsFilePath, JsonSerializer.Serialize(prefs));

        var settings = GuiSettingsService.Load();

        Assert.That(settings.Theme, Is.EqualTo("Dark"));
    }

    [Test]
    public void Load_WithInvalidUserPrefsJson_DoesNotThrow()
    {
        File.WriteAllText(_userPrefsFilePath, "not-valid-json{{{");

        Assert.DoesNotThrow(() => GuiSettingsService.Load());
    }

    [Test]
    public void Load_WithUserPrefsEmptyTheme_KeepsPresetTheme()
    {
        // Write user-prefs with empty theme — should not override
        var prefs = new UserPreferences { Theme = "" };
        File.WriteAllText(_userPrefsFilePath, JsonSerializer.Serialize(prefs));

        var settings = GuiSettingsService.Load();

        // Theme should remain whatever the presets file provides (Default if no gui-settings.json)
        Assert.That(settings.Theme, Is.Not.Empty);
    }

    // ────────────── Save round-trip ──────────────

    [Test]
    public void Save_ThenLoad_RoundTripsTheme()
    {
        var original = new GuiSettings { Theme = "Light" };
        GuiSettingsService.Save(original);

        var loaded = GuiSettingsService.Load();

        Assert.That(loaded.Theme, Is.EqualTo("Light"));
    }
}
