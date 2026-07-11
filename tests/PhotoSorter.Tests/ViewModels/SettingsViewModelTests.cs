using System.Linq;
using PhotoSorter.Core.Models;
using PhotoSorter.Core.Services.Implementations;
using PhotoSorter.Core.ViewModels;
using PhotoSorter.Tests.TestDoubles;

namespace PhotoSorter.Tests.ViewModels;

public sealed class SettingsViewModelTests
{
    private static TargetFoldersViewModel CreateTargetFolders(FakeSettingsService? settings = null) =>
        new(new FakeFolderPickerService(), new FakeProjectService(), settings ?? new FakeSettingsService());

    private static (SettingsViewModel Sut, FakeSettingsService Settings, HotkeyService Hotkeys) CreateSut()
    {
        var settings = new FakeSettingsService();
        var hotkeys = new HotkeyService(settings);
        return (new SettingsViewModel(settings, hotkeys, CreateTargetFolders(settings)), settings, hotkeys);
    }

    [Fact]
    public void Constructor_InitializesAnimationsEnabledFromSettings()
    {
        var settings = new FakeSettingsService();
        settings.Current.AnimationsEnabled = false;
        var hotkeys = new HotkeyService(settings);

        var sut = new SettingsViewModel(settings, hotkeys, CreateTargetFolders(settings));

        Assert.False(sut.AnimationsEnabled);
    }

    [Fact]
    public void Constructor_ExposesTheGivenTargetFoldersViewModel()
    {
        var targetFolders = CreateTargetFolders();
        var settings = new FakeSettingsService();
        var hotkeys = new HotkeyService(settings);

        var sut = new SettingsViewModel(settings, hotkeys, targetFolders);

        Assert.Same(targetFolders, sut.TargetFolders);
    }

    [Fact]
    public void DarkModeEnabled_IsAlwaysTrue()
    {
        var (sut, _, _) = CreateSut();

        Assert.True(sut.DarkModeEnabled);
    }

    [Fact]
    public void SettingAnimationsEnabled_PersistsToSettingsService()
    {
        var (sut, settings, _) = CreateSut();

        sut.AnimationsEnabled = false;

        Assert.False(settings.Current.AnimationsEnabled);
        Assert.Equal(1, settings.SaveCallCount);
    }

    [Fact]
    public void HotkeyBindings_ContainsOneRowPerHotkeyAction()
    {
        var (sut, _, _) = CreateSut();

        var actions = sut.HotkeyBindings.Select(b => b.Action).ToList();

        foreach (HotkeyAction action in System.Enum.GetValues(typeof(HotkeyAction)))
        {
            Assert.Contains(action, actions);
        }
    }

    [Fact]
    public void ResetHotkeysCommand_RestoresDefaultsAcrossAllRows()
    {
        var (sut, _, hotkeys) = CreateSut();
        var nextImageRow = sut.HotkeyBindings.Single(b => b.Action == HotkeyAction.NextImage);
        nextImageRow.ApplyCapturedChord(new HotkeyChord("PageDown"));

        sut.ResetHotkeysCommand.Execute(null);

        Assert.Equal(new HotkeyChord("Down"), nextImageRow.Chord);
        Assert.Equal(HotkeyAction.NextImage, hotkeys.Resolve(new HotkeyChord("Down")));
    }

    [Fact]
    public void CloseCommand_RaisesRequestClose()
    {
        var (sut, _, _) = CreateSut();
        var raised = false;
        sut.RequestClose += (_, _) => raised = true;

        sut.CloseCommand.Execute(null);

        Assert.True(raised);
    }
}
