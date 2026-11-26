// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTestA.ISTAvalon.Headless;

using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using global::ISTAvalon.Models;
using global::ISTAvalon.ViewModels;
using global::ISTAvalon.Views;

/// <summary>
/// Headless UI tests for <see cref="MainWindow"/>.
/// Each <c>[AvaloniaTest]</c> runs on the Avalonia UI thread with the headless
/// Skia renderer active, so <c>CaptureRenderedFrame()</c> produces real pixel
/// output that the agent can inspect with <c>view_image</c>.
///
/// Screenshots are written to <c>TestResults/Screenshots/</c> relative to the
/// test working directory.  After a test run the agent can call
/// <c>view_image</c> on those PNGs to "see" the GUI and decide what to fix.
/// </summary>
[TestFixture]
public class MainWindowHeadlessTests
{
    private static string ScreenshotDir =>
        Path.Combine(TestContext.CurrentContext.WorkDirectory, "Screenshots");

    [SetUp]
    public void SetUp() => Directory.CreateDirectory(ScreenshotDir);

    // -------------------------------------------------------------------------
    // Smoke tests
    // -------------------------------------------------------------------------

    [AvaloniaTest]
    public void MainWindow_Renders_InitialState()
    {
        var vm = new MainWindowViewModel();
        var window = new MainWindow { DataContext = vm, Width = 1100, Height = 700 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var frame = window.CaptureRenderedFrame();
        frame?.Save(Path.Combine(ScreenshotDir, "MainWindow_Initial.png"), PngBitmapEncoderOptions.Default);

        Assert.That(frame, Is.Not.Null, "CaptureRenderedFrame returned null — renderer may not be active");
        window.Close();
    }

    [AvaloniaTest]
    public void MainWindow_HasExpectedCommandTabs()
    {
        var vm = new MainWindowViewModel();
        var window = new MainWindow { DataContext = vm };
        window.Show();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(vm.CommandTabs, Is.Not.Empty, "CommandTabs should be populated by discovery");
            Assert.That(vm.SelectedTab, Is.Not.Null, "A tab should be selected by default");
        }
        window.Close();
    }

    // -------------------------------------------------------------------------
    // Per-tab screenshot: agent can open each PNG to review layout
    // -------------------------------------------------------------------------

    [AvaloniaTest]
    public void AllCommandTabs_Render_WithoutException()
    {
        var vm = new MainWindowViewModel();
        var window = new MainWindow { DataContext = vm, Width = 1100, Height = 700 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        foreach (var tab in vm.CommandTabs)
        {
            vm.SelectedTab = tab;
            Dispatcher.UIThread.RunJobs();

            var frame = window.CaptureRenderedFrame();
            var safeName = string.Join("_", tab.Name.Split(Path.GetInvalidFileNameChars()));
            frame?.Save(Path.Combine(ScreenshotDir, $"Tab_{safeName}.png"), PngBitmapEncoderOptions.Default);

            Assert.That(frame, Is.Not.Null, $"Tab '{tab.Name}' rendered null frame");
        }

        window.Close();
    }

    // -------------------------------------------------------------------------
    // Theme switching
    // -------------------------------------------------------------------------

    [AvaloniaTest]
    public void MainWindow_DarkTheme_Renders()
    {
        var vm = new MainWindowViewModel();
        var window = new MainWindow { DataContext = vm, Width = 1100, Height = 700 };
        window.Show();

        vm.CurrentTheme = AppTheme.Dark;
        Dispatcher.UIThread.RunJobs();

        var frame = window.CaptureRenderedFrame();
        frame?.Save(Path.Combine(ScreenshotDir, "MainWindow_Dark.png"), PngBitmapEncoderOptions.Default);

        Assert.That(frame, Is.Not.Null);
        window.Close();
    }

    [AvaloniaTest]
    public void ToggleThemeCommand_AdvancesToNextTheme()
    {
        var vm = new MainWindowViewModel();
        var window = new MainWindow { DataContext = vm };
        window.Show();

        vm.CurrentTheme = AppTheme.Default;
        var before = vm.CurrentTheme;
        vm.ToggleThemeCommand.Execute(null);

        Assert.That(vm.CurrentTheme, Is.Not.EqualTo(before),
            "Each toggle click must advance to the next theme");
        window.Close();
    }

    // -------------------------------------------------------------------------
    // ViewModel binding checks
    // -------------------------------------------------------------------------

    [AvaloniaTest]
    public void CommandTab_SelectedCommand_ChangesParameters()
    {
        var vm = new MainWindowViewModel();
        var window = new MainWindow { DataContext = vm };
        window.Show();

        var tab = vm.SelectedTab;
        Assert.That(tab, Is.Not.Null);

        if (tab!.AvailableCommands.Count > 1)
        {
            var second = tab.AvailableCommands[1];
            tab.SelectedCommand = second;
            Dispatcher.UIThread.RunJobs();

            Assert.That(tab.SelectedCommand, Is.EqualTo(second));
        }

        window.Close();
    }

    // -------------------------------------------------------------------------
    // Three-state theme correctness
    // Cycle: Default (follow OS) → Light → Dark → Default
    // -------------------------------------------------------------------------

    [AvaloniaTest]
    public void Theme_SetDark_AppliesThemeVariantDark()
    {
        var vm = new MainWindowViewModel();
        var window = new MainWindow { DataContext = vm };
        window.Show();

        vm.CurrentTheme = AppTheme.Dark;
        Dispatcher.UIThread.RunJobs();

        Assert.That(Application.Current!.RequestedThemeVariant, Is.EqualTo(ThemeVariant.Dark));
        window.Close();
    }

    [AvaloniaTest]
    public void Theme_SetLight_AppliesThemeVariantLight()
    {
        var vm = new MainWindowViewModel();
        var window = new MainWindow { DataContext = vm };
        window.Show();

        vm.CurrentTheme = AppTheme.Light;
        Dispatcher.UIThread.RunJobs();

        Assert.That(Application.Current!.RequestedThemeVariant, Is.EqualTo(ThemeVariant.Light));
        window.Close();
    }

    [AvaloniaTest]
    public void Theme_SetDefault_AppliesThemeVariantDefault()
    {
        var vm = new MainWindowViewModel();
        var window = new MainWindow { DataContext = vm };
        window.Show();

        vm.CurrentTheme = AppTheme.Default;
        Dispatcher.UIThread.RunJobs();

        Assert.That(Application.Current!.RequestedThemeVariant, Is.EqualTo(ThemeVariant.Default));
        window.Close();
    }

    [AvaloniaTest]
    public void Theme_Toggle_CyclesDefaultLightDark()
    {
        var vm = new MainWindowViewModel();
        var window = new MainWindow { DataContext = vm };
        window.Show();

        // Anchor to a known starting point.
        vm.CurrentTheme = AppTheme.Default;
        Dispatcher.UIThread.RunJobs();
        Assert.That(Application.Current!.RequestedThemeVariant, Is.EqualTo(ThemeVariant.Default),
            "Default → ThemeVariant.Default");

        // Default → Light
        vm.ToggleThemeCommand.Execute(null);
        Dispatcher.UIThread.RunJobs();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(vm.CurrentTheme, Is.EqualTo(AppTheme.Light), "1st toggle: should be Light");
            Assert.That(Application.Current!.RequestedThemeVariant, Is.EqualTo(ThemeVariant.Light));
        }

        // Light → Dark
        vm.ToggleThemeCommand.Execute(null);
        Dispatcher.UIThread.RunJobs();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(vm.CurrentTheme, Is.EqualTo(AppTheme.Dark), "2nd toggle: should be Dark");
            Assert.That(Application.Current!.RequestedThemeVariant, Is.EqualTo(ThemeVariant.Dark));
        }

        // Dark → Default
        vm.ToggleThemeCommand.Execute(null);
        Dispatcher.UIThread.RunJobs();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(vm.CurrentTheme, Is.EqualTo(AppTheme.Default), "3rd toggle: should wrap back to Default");
            Assert.That(Application.Current!.RequestedThemeVariant, Is.EqualTo(ThemeVariant.Default));
        }

        window.Close();
    }

    [AvaloniaTest]
    public void Theme_ThemeIcon_IsCorrectForEachMode()
    {
        var vm = new MainWindowViewModel();
        var window = new MainWindow { DataContext = vm };
        window.Show();

        vm.CurrentTheme = AppTheme.Default;
        Assert.That(vm.ThemeIcon, Is.EqualTo("fa-solid fa-sun"),
            "Default mode: icon points to next state (Light/sun)");

        vm.CurrentTheme = AppTheme.Light;
        Assert.That(vm.ThemeIcon, Is.EqualTo("fa-solid fa-moon"),
            "Light mode: icon points to next state (Dark/moon)");

        vm.CurrentTheme = AppTheme.Dark;
        Assert.That(vm.ThemeIcon, Is.EqualTo("fa-solid fa-circle-half-stroke"),
            "Dark mode: icon points to next state (System/circle-half)");

        window.Close();
    }

    [AvaloniaTest]
    public void Theme_ThemeTooltip_IsCorrectForEachMode()
    {
        var vm = new MainWindowViewModel();
        var window = new MainWindow { DataContext = vm };
        window.Show();

        vm.CurrentTheme = AppTheme.Default;
        Assert.That(vm.ThemeTooltip, Is.EqualTo("Switch to Light Theme"));

        vm.CurrentTheme = AppTheme.Light;
        Assert.That(vm.ThemeTooltip, Is.EqualTo("Switch to Dark Theme"));

        vm.CurrentTheme = AppTheme.Dark;
        Assert.That(vm.ThemeTooltip, Is.EqualTo("Follow System Theme"));

        window.Close();
    }

    [AvaloniaTest]
    public void Theme_SavedDarkSetting_LoadsAsDark()
    {
        var original = new global::ISTAvalon.Models.GuiSettings { Theme = "Dark" };
        global::ISTAvalon.Services.GuiSettingsService.Save(original);

        var vm = new MainWindowViewModel();
        var window = new MainWindow { DataContext = vm };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(vm.CurrentTheme, Is.EqualTo(AppTheme.Dark));
            Assert.That(Application.Current!.RequestedThemeVariant, Is.EqualTo(ThemeVariant.Dark));
        }

        window.Close();
    }

    [AvaloniaTest]
    public void Theme_SavedLightSetting_LoadsAsLight()
    {
        var original = new global::ISTAvalon.Models.GuiSettings { Theme = "Light" };
        global::ISTAvalon.Services.GuiSettingsService.Save(original);

        var vm = new MainWindowViewModel();
        var window = new MainWindow { DataContext = vm };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(vm.CurrentTheme, Is.EqualTo(AppTheme.Light));
            Assert.That(Application.Current!.RequestedThemeVariant, Is.EqualTo(ThemeVariant.Light));
        }

        window.Close();
    }

    [AvaloniaTest]
    public void Theme_SavedDefaultSetting_LoadsAsDefault()
    {
        var original = new global::ISTAvalon.Models.GuiSettings { Theme = "Default" };
        global::ISTAvalon.Services.GuiSettingsService.Save(original);

        var vm = new MainWindowViewModel();
        var window = new MainWindow { DataContext = vm };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(vm.CurrentTheme, Is.EqualTo(AppTheme.Default));
            Assert.That(Application.Current!.RequestedThemeVariant, Is.EqualTo(ThemeVariant.Default));
        }

        window.Close();
    }

    [AvaloniaTest]
    public void Theme_UnrecognisedSetting_FallsBackToDefault()
    {
        var original = new global::ISTAvalon.Models.GuiSettings { Theme = "UnknownValue" };
        global::ISTAvalon.Services.GuiSettingsService.Save(original);

        var vm = new MainWindowViewModel();
        var window = new MainWindow { DataContext = vm };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.That(vm.CurrentTheme, Is.EqualTo(AppTheme.Default),
            "Unrecognised theme string must fall back to Default (follow OS)");

        window.Close();
    }

    [TearDown]
    public void TearDown()
    {
        // Restore system theme so later tests start from a clean state.
        if (Application.Current is { } app)
        {
            app.RequestedThemeVariant = ThemeVariant.Default;
        }
    }
}
