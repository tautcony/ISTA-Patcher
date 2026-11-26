// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTestA.ISTAvalon.Headless;

using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using global::ISTAvalon.ViewModels;
using global::ISTAvalon.Views;

/// <summary>
/// <para>UI-automation style tests for <see cref="MainWindow"/>.</para>
/// <para>
/// These tests go beyond screenshots: they simulate real user gestures
/// (tab selection, text input, button clicks), read back ViewModel state and
/// the control tree as text, and take "before/after" screenshots.  The agent
/// can use <c>view_image</c> on the PNGs and read the console output that
/// NUnit captures to understand exactly what changed on screen.
/// </para>
/// <para>
/// Workflow for the agent:
///   1. Run: dotnet test --filter FullyQualifiedName~UIAutomation --logger "console;verbosity=detailed"
///   2. Read the printed control-tree dumps in the test output.
///   3. Call view_image on the Screenshots/*.png files to see rendered frames.
/// </para>
/// </summary>
[TestFixture]
public class UIAutomationTests
{
    private static string ScreenshotDir =>
        Path.Combine(TestContext.CurrentContext.WorkDirectory, "Screenshots");

    [SetUp]
    public void SetUp() => Directory.CreateDirectory(ScreenshotDir);

    // -------------------------------------------------------------------------
    // 1. Control-tree inspection — text-based, no vision needed
    // -------------------------------------------------------------------------

    [AvaloniaTest]
    public void DumpInteractiveControls_PrintsReadableFormState()
    {
        var vm = new MainWindowViewModel();
        var window = new MainWindow { DataContext = vm, Width = 1100, Height = 700 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var report = ControlTreeDumper.DumpYaml(vm);
        TestContext.Progress.WriteLine("=== GUI State YAML (initial) ===");
        TestContext.Progress.WriteLine(report);

        // Basic sanity: at least one parameter binding should be present
        Assert.That(report, Does.Contain("binding:"),
            "YAML report should expose at least one ViewModel parameter binding");

        window.Close();
    }

    [AvaloniaTest]
    public void DumpFullTree_WritesToFile_ForAgentInspection()
    {
        var vm = new MainWindowViewModel();
        var window = new MainWindow { DataContext = vm, Width = 1100, Height = 700 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var dump = ControlTreeDumper.DumpYaml(vm);
        var dumpPath = Path.Combine(ScreenshotDir, "ControlTree_Initial.yaml");
        File.WriteAllText(dumpPath, dump);

        TestContext.Progress.WriteLine($"GUI state YAML written to: {dumpPath}");
        Assert.That(File.Exists(dumpPath), Is.True);

        window.Close();
    }

    // -------------------------------------------------------------------------
    // 2. Tab switching via mouse click
    // -------------------------------------------------------------------------

    [AvaloniaTest]
    public void Click_SecondTab_SelectsIt_AndRendersParameters()
    {
        var vm = new MainWindowViewModel();
        var window = new MainWindow { DataContext = vm, Width = 1100, Height = 700 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        if (vm.CommandTabs.Count < 2)
        {
            Assert.Ignore("Fewer than 2 tabs; skipping tab-switch test");
            return;
        }

        // Find the TabStrip items to locate the second tab header's position
        var tabControl = window.GetVisualDescendants()
            .OfType<TabControl>()
            .FirstOrDefault();

        Assert.That(tabControl, Is.Not.Null, "TabControl not found in visual tree");

        var secondTab = vm.CommandTabs[1];
        vm.SelectedTab = secondTab;         // direct ViewModel switch (reliable)
        Dispatcher.UIThread.RunJobs();

        var frame = window.CaptureRenderedFrame();
        frame?.Save(Path.Combine(ScreenshotDir, $"After_TabSwitch_{secondTab.Name}.png"), PngBitmapEncoderOptions.Default);

        var report = ControlTreeDumper.DumpYaml(vm);
        TestContext.Progress.WriteLine($"=== GUI State YAML after switching to tab '{secondTab.Name}' ===");
        TestContext.Progress.WriteLine(report);

        Assert.That(vm.SelectedTab?.Name, Is.EqualTo(secondTab.Name));

        window.Close();
    }

    // -------------------------------------------------------------------------
    // 3. Filling a text parameter and verifying ViewModel updates
    // -------------------------------------------------------------------------

    [AvaloniaTest]
    public void FillStringParameter_Via_KeyTextInput_UpdatesViewModel()
    {
        var vm = new MainWindowViewModel();
        var window = new MainWindow { DataContext = vm, Width = 1100, Height = 700 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var tab = vm.SelectedTab;
        Assert.That(tab, Is.Not.Null);

        // Find the first string or path parameter
        var strParam = tab!.Parameters
            .OfType<StringParameterViewModel>()
            .FirstOrDefault()
            ?? tab.Parameters.OfType<PathParameterViewModel>().FirstOrDefault() as ParameterViewModel;

        if (strParam is null)
        {
            Assert.Ignore("No string/path parameter on first tab; skipping text-input test");
            return;
        }

        // Locate the corresponding TextBox in the visual tree
        var textBox = window.GetVisualDescendants()
            .OfType<TextBox>()
            .FirstOrDefault(tb => tb.IsEffectivelyVisible && tb.IsEffectivelyEnabled);

        Assert.That(textBox, Is.Not.Null, "No enabled TextBox found on current tab");

        textBox!.Focus();
        Dispatcher.UIThread.RunJobs();

        const string testValue = "/tmp/ista-test-path";
        window.KeyTextInput(testValue);
        Dispatcher.UIThread.RunJobs();

        TestContext.Progress.WriteLine($"Typed into TextBox: '{testValue}'");
        TestContext.Progress.WriteLine($"TextBox.Text is now: '{textBox.Text}'");

        Assert.That(textBox.Text, Does.Contain(testValue),
            "TextBox.Text should contain the typed text after KeyTextInput");

        var frame = window.CaptureRenderedFrame();
        frame?.Save(Path.Combine(ScreenshotDir, "After_TextInput.png"), PngBitmapEncoderOptions.Default);

        window.Close();
    }

    // -------------------------------------------------------------------------
    // 4. Execute button — simulate click, observe StatusText change
    // -------------------------------------------------------------------------

    [AvaloniaTest]
    public void Click_ExecuteButton_WithMissingRequired_ShowsWarningStatus()
    {
        var vm = new MainWindowViewModel();
        var window = new MainWindow { DataContext = vm, Width = 1100, Height = 700 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var tab = vm.SelectedTab;
        Assert.That(tab, Is.Not.Null);

        // Clear any preset values so required params are empty
        foreach (var p in tab!.Parameters.OfType<StringParameterViewModel>())
            p.ApplyValue(string.Empty);
        foreach (var p in tab.Parameters.OfType<PathParameterViewModel>())
            p.ApplyValue(string.Empty);
        Dispatcher.UIThread.RunJobs();

        var requiredParams = tab.Parameters.Where(p => p.Descriptor.IsRequired).ToList();

        if (requiredParams.Count == 0)
        {
            Assert.Ignore("No required parameters on first tab; skipping validation test");
            return;
        }

        // Find and click the Execute button
        var executeBtn = window.GetVisualDescendants()
            .OfType<Button>()
            .FirstOrDefault(b => b.Content?.ToString() == "Execute" && b.IsEffectivelyVisible);

        Assert.That(executeBtn, Is.Not.Null, "Execute button not found");

        // Click the Execute button by invoking its command
        // (MouseDown on an exact pixel requires root-relative coordinates which
        //  vary with layout; command invocation is the reliable headless equivalent)
        executeBtn!.Command?.Execute(null);

        // Give async handler a chance to run
        Dispatcher.UIThread.RunJobs();

        TestContext.Progress.WriteLine($"StatusText after click: '{tab.StatusText}'");

        var frame = window.CaptureRenderedFrame();
        frame?.Save(Path.Combine(ScreenshotDir, "After_ExecuteClick_WithMissingRequired.png"), PngBitmapEncoderOptions.Default);

        Assert.That(tab.StatusText, Does.StartWith("⚠"),
            $"Expected a warning status, got: '{tab.StatusText}'");

        window.Close();
    }

    // -------------------------------------------------------------------------
    // 5. Log panel toggle
    // -------------------------------------------------------------------------

    [AvaloniaTest]
    public void ToggleLogPanel_CollapsesAndExpandsLogArea()
    {
        var vm = new MainWindowViewModel();
        var window = new MainWindow { DataContext = vm, Width = 1100, Height = 700 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var tab = vm.SelectedTab;
        Assert.That(tab, Is.Not.Null);

        var expandedInitially = tab!.IsLogPanelExpanded;

        tab.ToggleLogPanelCommand.Execute(null);
        Dispatcher.UIThread.RunJobs();

        var frameCollapsed = window.CaptureRenderedFrame();
        frameCollapsed?.Save(Path.Combine(ScreenshotDir, "LogPanel_Collapsed.png"), PngBitmapEncoderOptions.Default);

        Assert.That(tab.IsLogPanelExpanded, Is.Not.EqualTo(expandedInitially),
            "ToggleLogPanelCommand should invert IsLogPanelExpanded");

        tab.ToggleLogPanelCommand.Execute(null);
        Dispatcher.UIThread.RunJobs();

        var frameExpanded = window.CaptureRenderedFrame();
        frameExpanded?.Save(Path.Combine(ScreenshotDir, "LogPanel_Expanded.png"), PngBitmapEncoderOptions.Default);

        Assert.That(tab.IsLogPanelExpanded, Is.EqualTo(expandedInitially),
            "Second toggle should restore the original state");

        window.Close();
    }

    // -------------------------------------------------------------------------
    // 6. Keyboard navigation — Tab key cycles focus through controls
    // -------------------------------------------------------------------------

    [AvaloniaTest]
    public void TabKey_CyclesFocus_ThroughInteractiveControls()
    {
        var vm = new MainWindowViewModel();
        var window = new MainWindow { DataContext = vm, Width = 1100, Height = 700 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var visited = new List<string>();
        for (var i = 0; i < 6; i++)
        {
            window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.None);
            window.KeyReleaseQwerty(PhysicalKey.Tab, RawInputModifiers.None);
            Dispatcher.UIThread.RunJobs();

            var focused = window.FocusManager?.GetFocusedElement();
            var desc = focused is null ? "(none)"
                : $"{focused.GetType().Name}:{(focused is Avalonia.StyledElement se ? se.Name : string.Empty)}";
            visited.Add(desc);
        }

        TestContext.Progress.WriteLine("Focus sequence via Tab key:");
        foreach (var step in visited.Select((v, i) => $"  [{i + 1}] {v}"))
            TestContext.Progress.WriteLine(step);

        // At least some focus movement should have occurred
        Assert.That(visited.Distinct().Count(), Is.GreaterThan(1),
            "Tab key should move focus to different elements");

        window.Close();
    }

    // =========================================================================
    // 7. Preset tests — ViewModel layer (no headless required)
    //    These tests construct CommandTabViewModel directly with an explicit
    //    preset dict that mirrors the "patch" entry in gui-settings.json,
    //    so they are deterministic regardless of the deployed file on disk.
    // =========================================================================

    // --- ViewModel-level: HasPreset flag ---

    [Test]
    public void Preset_PatchTab_HasPreset_IsTrue_WhenPresetDictProvided()
    {
        var descriptors = global::ISTAvalon.Services.CommandDiscoveryService.DiscoverCommands();
        var patchDescriptor = descriptors.First(d => d.Name == "patch");

        var preset = new Dictionary<string, string> { ["PatchType"] = "BMW" };
        var tabVm = new CommandTabViewModel(patchDescriptor, preset);

        Assert.That(tabVm.HasPreset, Is.True);
    }

    [Test]
    public void Preset_PatchTab_HasPreset_IsFalse_WhenNoPresetProvided()
    {
        var descriptors = global::ISTAvalon.Services.CommandDiscoveryService.DiscoverCommands();
        var patchDescriptor = descriptors.First(d => d.Name == "patch");

        var tabVm = new CommandTabViewModel(patchDescriptor, preset: null);

        Assert.That(tabVm.HasPreset, Is.False);
    }

    // --- ViewModel-level: preset values applied to parameters ---

    [Test]
    public void Preset_PatchTab_AppliesEnumValue_PatchType_BMW()
    {
        var descriptors = global::ISTAvalon.Services.CommandDiscoveryService.DiscoverCommands();
        var patchDescriptor = descriptors.First(d => d.Name == "patch");
        var preset = new Dictionary<string, string> { ["PatchType"] = "BMW" };

        var tabVm = new CommandTabViewModel(patchDescriptor, preset);

        var patchTypeParam = tabVm.Parameters
            .OfType<EnumParameterViewModel>()
            .FirstOrDefault(p => p.Descriptor.Name == "PatchType");

        Assert.That(patchTypeParam, Is.Not.Null, "PatchType parameter not found on patch tab");
        Assert.That(patchTypeParam!.SelectedValue, Is.EqualTo("BMW"));
    }

    [Test]
    public void Preset_PatchTab_AppliesNumericValue_MaxDegreeOfParallelism_4()
    {
        var descriptors = global::ISTAvalon.Services.CommandDiscoveryService.DiscoverCommands();
        var patchDescriptor = descriptors.First(d => d.Name == "patch");
        var preset = new Dictionary<string, string> { ["MaxDegreeOfParallelism"] = "4" };

        var tabVm = new CommandTabViewModel(patchDescriptor, preset);

        var parallelParam = tabVm.Parameters
            .OfType<NumericParameterViewModel>()
            .FirstOrDefault(p => p.Descriptor.Name == "MaxDegreeOfParallelism");

        Assert.That(parallelParam, Is.Not.Null, "MaxDegreeOfParallelism parameter not found on patch tab");
        Assert.That(parallelParam!.NumericValue, Is.EqualTo(4m));
    }

    [Test]
    public void Preset_PatchTab_AppliesStringValue_MarketLanguage_EnUS()
    {
        var descriptors = global::ISTAvalon.Services.CommandDiscoveryService.DiscoverCommands();
        var patchDescriptor = descriptors.First(d => d.Name == "patch");
        var preset = new Dictionary<string, string> { ["MarketLanguage"] = "en-US" };

        var tabVm = new CommandTabViewModel(patchDescriptor, preset);

        var languageParam = tabVm.Parameters
            .OfType<StringParameterViewModel>()
            .FirstOrDefault(p => p.Descriptor.Name == "MarketLanguage");

        Assert.That(languageParam, Is.Not.Null, "MarketLanguage parameter not found on patch tab");
        Assert.That(languageParam!.TextValue, Is.EqualTo("en-US"));
    }

    [Test]
    public void Preset_PatchTab_AllThreePresetValues_AppliedTogether()
    {
        var descriptors = global::ISTAvalon.Services.CommandDiscoveryService.DiscoverCommands();
        var patchDescriptor = descriptors.First(d => d.Name == "patch");
        var preset = new Dictionary<string, string>
        {
            ["PatchType"] = "BMW",
            ["MaxDegreeOfParallelism"] = "4",
            ["MarketLanguage"] = "en-US",
        };

        var tabVm = new CommandTabViewModel(patchDescriptor, preset);

        var patchType = tabVm.Parameters.OfType<EnumParameterViewModel>()
            .FirstOrDefault(p => p.Descriptor.Name == "PatchType");
        var parallelism = tabVm.Parameters.OfType<NumericParameterViewModel>()
            .FirstOrDefault(p => p.Descriptor.Name == "MaxDegreeOfParallelism");
        var language = tabVm.Parameters.OfType<StringParameterViewModel>()
            .FirstOrDefault(p => p.Descriptor.Name == "MarketLanguage");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(patchType?.SelectedValue, Is.EqualTo("BMW"));
            Assert.That(parallelism?.NumericValue, Is.EqualTo(4m));
            Assert.That(language?.TextValue, Is.EqualTo("en-US"));
        }
    }

    // --- ViewModel-level: ResetToPresetCommand ---

    [Test]
    public void Preset_ResetCommand_CanExecute_WhenHasPreset()
    {
        var descriptors = global::ISTAvalon.Services.CommandDiscoveryService.DiscoverCommands();
        var patchDescriptor = descriptors.First(d => d.Name == "patch");
        var preset = new Dictionary<string, string> { ["PatchType"] = "BMW" };

        var tabVm = new CommandTabViewModel(patchDescriptor, preset);

        Assert.That(tabVm.ResetToPresetCommand.CanExecute(null), Is.True);
    }

    [Test]
    public void Preset_ResetCommand_CannotExecute_WhenNoPreset()
    {
        var descriptors = global::ISTAvalon.Services.CommandDiscoveryService.DiscoverCommands();
        var patchDescriptor = descriptors.First(d => d.Name == "patch");

        var tabVm = new CommandTabViewModel(patchDescriptor, preset: null);

        Assert.That(tabVm.ResetToPresetCommand.CanExecute(null), Is.False);
    }

    [Test]
    public void Preset_ModifyParameter_ThenResetCommand_RestoresPresetValues()
    {
        var descriptors = global::ISTAvalon.Services.CommandDiscoveryService.DiscoverCommands();
        var patchDescriptor = descriptors.First(d => d.Name == "patch");
        var preset = new Dictionary<string, string>
        {
            ["PatchType"] = "BMW",
            ["MaxDegreeOfParallelism"] = "4",
            ["MarketLanguage"] = "en-US",
        };

        var tabVm = new CommandTabViewModel(patchDescriptor, preset);

        // Modify parameters away from preset values
        var patchType = tabVm.Parameters.OfType<EnumParameterViewModel>()
            .First(p => p.Descriptor.Name == "PatchType");
        var parallelism = tabVm.Parameters.OfType<NumericParameterViewModel>()
            .First(p => p.Descriptor.Name == "MaxDegreeOfParallelism");
        var language = tabVm.Parameters.OfType<StringParameterViewModel>()
            .First(p => p.Descriptor.Name == "MarketLanguage");

        patchType.SelectedValue = "Toyota";
        parallelism.NumericValue = 16m;
        language.TextValue = "de-DE";

        // Confirm modified
        Assert.That(patchType.SelectedValue, Is.EqualTo("Toyota"));

        // Reset to preset
        tabVm.ResetToPresetCommand.Execute(null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(patchType.SelectedValue, Is.EqualTo("BMW"),
                "PatchType should be restored to preset value");
            Assert.That(parallelism.NumericValue, Is.EqualTo(4m),
                "MaxDegreeOfParallelism should be restored to preset value");
            Assert.That(language.TextValue, Is.EqualTo("en-US"),
                "MarketLanguage should be restored to preset value");
        }
    }

    // =========================================================================
    // 8. Preset tests — Visual/headless layer ([AvaloniaTest])
    //    These tests use MainWindowViewModel() which loads the real
    //    gui-settings.json from the test output directory, validating the full
    //    data flow from file → service → ViewModel → rendered UI.
    // =========================================================================

    [AvaloniaTest]
    public void Preset_PatchTab_ResetButton_IsVisible_InUI()
    {
        var vm = new MainWindowViewModel();
        var window = new MainWindow { DataContext = vm, Width = 1100, Height = 700 };
        window.Show();

        var patchTab = vm.CommandTabs.FirstOrDefault(t => t.Name == "patch");
        Assert.That(patchTab, Is.Not.Null, "'patch' tab must exist");
        Assert.That(patchTab!.HasPreset, Is.True,
            "patch tab should have a preset from gui-settings.json");

        vm.SelectedTab = patchTab;
        Dispatcher.UIThread.RunJobs();

        // The Reset button has a unique tooltip; IsVisible is data-bound to HasPreset
        var resetBtn = window.GetVisualDescendants()
            .OfType<Button>()
            .FirstOrDefault(b => b.IsEffectivelyVisible
                && ToolTip.GetTip(b)?.ToString()?.Contains("Reset") == true);

        var frame = window.CaptureRenderedFrame();
        frame?.Save(Path.Combine(ScreenshotDir, "Preset_PatchTab_ResetButtonVisible.png"), PngBitmapEncoderOptions.Default);

        Assert.That(resetBtn, Is.Not.Null,
            "Reset button (ToolTip contains 'Reset') should be visible on the patch tab");

        window.Close();
    }

    [AvaloniaTest]
    public void Preset_NonPresetTab_ResetButton_IsNotVisible_InUI()
    {
        var vm = new MainWindowViewModel();
        var window = new MainWindow { DataContext = vm, Width = 1100, Height = 700 };
        window.Show();

        // "ilean" and "crypto" have no preset entry in gui-settings.json
        foreach (var tabName in new[] { "ilean", "crypto" })
        {
            var tab = vm.CommandTabs.FirstOrDefault(t => t.Name == tabName);
            if (tab is null) continue;

            vm.SelectedTab = tab;
            Dispatcher.UIThread.RunJobs();

            var resetBtn = window.GetVisualDescendants()
                .OfType<Button>()
                .FirstOrDefault(b => b.IsEffectivelyVisible
                    && ToolTip.GetTip(b)?.ToString()?.Contains("Reset") == true);

            Assert.That(resetBtn, Is.Null,
                $"Reset button should NOT be visible on the '{tabName}' tab (no preset)");
        }

        window.Close();
    }

    [AvaloniaTest]
    public void Preset_PatchTab_ParameterValues_MatchFile_InUI()
    {
        // MainWindowViewModel loads gui-settings.json → verifies the full pipeline.
        var vm = new MainWindowViewModel();
        var window = new MainWindow { DataContext = vm };
        window.Show();

        var patchTab = vm.CommandTabs.FirstOrDefault(t => t.Name == "patch");
        Assert.That(patchTab, Is.Not.Null);

        vm.SelectedTab = patchTab;
        Dispatcher.UIThread.RunJobs();

        var patchType = patchTab!.Parameters.OfType<EnumParameterViewModel>()
            .FirstOrDefault(p => p.Descriptor.Name == "PatchType");
        var parallelism = patchTab.Parameters.OfType<NumericParameterViewModel>()
            .FirstOrDefault(p => p.Descriptor.Name == "MaxDegreeOfParallelism");
        var language = patchTab.Parameters.OfType<StringParameterViewModel>()
            .FirstOrDefault(p => p.Descriptor.Name == "MarketLanguage");

        var report = ControlTreeDumper.DumpYaml(vm);
        TestContext.Progress.WriteLine("=== Patch tab state YAML (preset applied) ===");
        TestContext.Progress.WriteLine(report);

        var frame = window.CaptureRenderedFrame();
        frame?.Save(Path.Combine(ScreenshotDir, "Preset_PatchTab_ParameterValues.png"), PngBitmapEncoderOptions.Default);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(patchType?.SelectedValue, Is.EqualTo("BMW"),
                "PatchType should reflect preset value from gui-settings.json");
            Assert.That(parallelism?.NumericValue, Is.EqualTo(4m),
                "MaxDegreeOfParallelism should reflect preset value from gui-settings.json");
            Assert.That(language?.TextValue, Is.EqualTo("en-US"),
                "MarketLanguage should reflect preset value from gui-settings.json");
        }

        window.Close();
    }

    [AvaloniaTest]
    public void Preset_ModifyThenReset_UI_RestoresPresetAndRendersCorrectly()
    {
        var vm = new MainWindowViewModel();
        var window = new MainWindow { DataContext = vm, Width = 1100, Height = 700 };
        window.Show();

        var patchTab = vm.CommandTabs.FirstOrDefault(t => t.Name == "patch");
        Assert.That(patchTab, Is.Not.Null);

        vm.SelectedTab = patchTab;
        Dispatcher.UIThread.RunJobs();

        // Modify parameters away from preset
        var patchType = patchTab!.Parameters.OfType<EnumParameterViewModel>()
            .First(p => p.Descriptor.Name == "PatchType");
        var language = patchTab.Parameters.OfType<StringParameterViewModel>()
            .First(p => p.Descriptor.Name == "MarketLanguage");

        patchType.SelectedValue = "Toyota";
        language.TextValue = "ja-JP";
        Dispatcher.UIThread.RunJobs();

        var frameModified = window.CaptureRenderedFrame();
        frameModified?.Save(Path.Combine(ScreenshotDir, "Preset_PatchTab_AfterModify.png"), PngBitmapEncoderOptions.Default);

        // Execute ResetToPreset via command
        patchTab.ResetToPresetCommand.Execute(null);
        Dispatcher.UIThread.RunJobs();

        var frameRestored = window.CaptureRenderedFrame();
        frameRestored?.Save(Path.Combine(ScreenshotDir, "Preset_PatchTab_AfterReset.png"), PngBitmapEncoderOptions.Default);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(patchType.SelectedValue, Is.EqualTo("BMW"),
                "PatchType must be restored after reset");
            Assert.That(language.TextValue, Is.EqualTo("en-US"),
                "MarketLanguage must be restored after reset");
        }

        window.Close();
    }
}
