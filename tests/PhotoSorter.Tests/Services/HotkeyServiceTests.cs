using PhotoSorter.Core.Models;
using PhotoSorter.Core.Services.Implementations;
using PhotoSorter.Tests.TestDoubles;

namespace PhotoSorter.Tests.Services;

public sealed class HotkeyServiceTests
{
    [Theory]
    [InlineData("Left", HotkeyAction.SortLeft)]
    [InlineData("Right", HotkeyAction.SortRight)]
    [InlineData("Up", HotkeyAction.PreviousImage)]
    [InlineData("Down", HotkeyAction.NextImage)]
    [InlineData("Space", HotkeyAction.Skip)]
    [InlineData("Back", HotkeyAction.Undo)]
    [InlineData("F", HotkeyAction.ToggleFullscreen)]
    public void Resolve_DefaultBindings_MatchSoftwareDesignDocument(string key, HotkeyAction expected)
    {
        var sut = new HotkeyService(new FakeSettingsService());

        Assert.Equal(expected, sut.Resolve(new HotkeyChord(key)));
    }

    [Fact]
    public void Resolve_ZoomBindings_RequireCtrlModifier()
    {
        var sut = new HotkeyService(new FakeSettingsService());

        Assert.Equal(HotkeyAction.ZoomIn, sut.Resolve(new HotkeyChord("OemPlus", Ctrl: true)));
        Assert.Equal(HotkeyAction.ZoomOut, sut.Resolve(new HotkeyChord("OemMinus", Ctrl: true)));
        Assert.Equal(HotkeyAction.ResetZoom, sut.Resolve(new HotkeyChord("D0", Ctrl: true)));
        Assert.Null(sut.Resolve(new HotkeyChord("OemPlus")));
    }

    [Fact]
    public void Resolve_UnboundChord_ReturnsNull()
    {
        var sut = new HotkeyService(new FakeSettingsService());

        Assert.Null(sut.Resolve(new HotkeyChord("Q")));
    }

    [Fact]
    public void SetBinding_Rebinds_AndOldChordNoLongerResolves()
    {
        var sut = new HotkeyService(new FakeSettingsService());

        sut.SetBinding(HotkeyAction.NextImage, new HotkeyChord("PageDown"));

        Assert.Equal(HotkeyAction.NextImage, sut.Resolve(new HotkeyChord("PageDown")));
        Assert.Null(sut.Resolve(new HotkeyChord("Down")));
    }

    [Fact]
    public void ResetToDefaults_UndoesCustomBindings()
    {
        var sut = new HotkeyService(new FakeSettingsService());
        sut.SetBinding(HotkeyAction.NextImage, new HotkeyChord("PageDown"));

        sut.ResetToDefaults();

        Assert.Equal(HotkeyAction.NextImage, sut.Resolve(new HotkeyChord("Down")));
        Assert.Null(sut.Resolve(new HotkeyChord("PageDown")));
    }

    [Fact]
    public void HotkeyChord_ToString_IncludesModifiersInOrder()
    {
        var chord = new HotkeyChord("OemPlus", Ctrl: true, Shift: true);

        Assert.Equal("Ctrl+Shift+OemPlus", chord.ToString());
    }

    [Fact]
    public void SetBinding_PersistsToSettings()
    {
        var settings = new FakeSettingsService();
        var sut = new HotkeyService(settings);

        sut.SetBinding(HotkeyAction.NextImage, new HotkeyChord("PageDown"));

        Assert.Equal(1, settings.SaveCallCount);
        Assert.Equal(new HotkeyChord("PageDown"), settings.Current.HotkeyBindings[nameof(HotkeyAction.NextImage)]);
    }

    [Fact]
    public void Constructor_LoadsCustomBindingsFromSettings_FallingBackToDefaultsForTheRest()
    {
        var settings = new FakeSettingsService();
        settings.Current.HotkeyBindings[nameof(HotkeyAction.NextImage)] = new HotkeyChord("PageDown");

        var sut = new HotkeyService(settings);

        Assert.Equal(HotkeyAction.NextImage, sut.Resolve(new HotkeyChord("PageDown")));
        Assert.Equal(HotkeyAction.SortLeft, sut.Resolve(new HotkeyChord("Left"))); // untouched default
    }
}
