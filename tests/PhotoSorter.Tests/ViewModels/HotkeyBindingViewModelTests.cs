using PhotoSorter.Core.Models;
using PhotoSorter.Core.Services.Implementations;
using PhotoSorter.Core.ViewModels;
using PhotoSorter.Tests.TestDoubles;

namespace PhotoSorter.Tests.ViewModels;

public sealed class HotkeyBindingViewModelTests
{
    private static HotkeyBindingViewModel CreateSut(HotkeyService hotkeyService, HotkeyAction action = HotkeyAction.NextImage) =>
        new(action, "Nächstes Bild", hotkeyService, a => a.ToString());

    [Fact]
    public void Constructor_ReadsCurrentChordFromHotkeyService()
    {
        var hotkeyService = new HotkeyService(new FakeSettingsService());
        var sut = CreateSut(hotkeyService);

        Assert.Equal(new HotkeyChord("Down"), sut.Chord);
    }

    [Fact]
    public void ApplyCapturedChord_WhenUnused_RebindsAndClearsCapturing()
    {
        var hotkeyService = new HotkeyService(new FakeSettingsService());
        var sut = CreateSut(hotkeyService);
        sut.BeginCaptureCommand.Execute(null);

        sut.ApplyCapturedChord(new HotkeyChord("PageDown"));

        Assert.Equal(new HotkeyChord("PageDown"), sut.Chord);
        Assert.False(sut.IsCapturing);
        Assert.Null(sut.ConflictMessage);
        Assert.Equal(HotkeyAction.NextImage, hotkeyService.Resolve(new HotkeyChord("PageDown")));
    }

    [Fact]
    public void ApplyCapturedChord_WhenAlreadyBoundToAnotherAction_ReportsConflictAndDoesNotRebind()
    {
        var hotkeyService = new HotkeyService(new FakeSettingsService());
        var sut = CreateSut(hotkeyService, HotkeyAction.NextImage);
        sut.BeginCaptureCommand.Execute(null);

        sut.ApplyCapturedChord(new HotkeyChord("Left")); // already SortLeft

        Assert.Equal(new HotkeyChord("Down"), sut.Chord); // unchanged
        Assert.True(sut.IsCapturing); // capture stays open so the user can try another key
        Assert.NotNull(sut.ConflictMessage);
        Assert.True(sut.HasConflict);
        Assert.Equal(HotkeyAction.SortLeft, hotkeyService.Resolve(new HotkeyChord("Left")));
    }

    [Fact]
    public void ApplyCapturedChord_ReboundToItsOwnCurrentChord_IsAllowed()
    {
        var hotkeyService = new HotkeyService(new FakeSettingsService());
        var sut = CreateSut(hotkeyService, HotkeyAction.NextImage);

        sut.ApplyCapturedChord(new HotkeyChord("Down"));

        Assert.Equal(new HotkeyChord("Down"), sut.Chord);
        Assert.Null(sut.ConflictMessage);
    }

    [Fact]
    public void CancelCapture_ClearsCapturingAndConflict()
    {
        var hotkeyService = new HotkeyService(new FakeSettingsService());
        var sut = CreateSut(hotkeyService, HotkeyAction.NextImage);
        sut.BeginCaptureCommand.Execute(null);
        sut.ApplyCapturedChord(new HotkeyChord("Left"));

        sut.CancelCaptureCommand.Execute(null);

        Assert.False(sut.IsCapturing);
        Assert.Null(sut.ConflictMessage);
    }

    [Fact]
    public void RefreshFromService_ReReadsCurrentBindingAndClearsTransientState()
    {
        var hotkeyService = new HotkeyService(new FakeSettingsService());
        var sut = CreateSut(hotkeyService, HotkeyAction.NextImage);
        sut.ApplyCapturedChord(new HotkeyChord("PageDown"));
        hotkeyService.ResetToDefaults();

        sut.RefreshFromService();

        Assert.Equal(new HotkeyChord("Down"), sut.Chord);
        Assert.False(sut.IsCapturing);
        Assert.Null(sut.ConflictMessage);
    }
}
