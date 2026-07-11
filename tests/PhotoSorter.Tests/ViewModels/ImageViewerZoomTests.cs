using Microsoft.Extensions.Logging.Abstractions;
using PhotoSorter.Core.ViewModels;
using PhotoSorter.Tests.TestDoubles;

namespace PhotoSorter.Tests.ViewModels;

public sealed class ImageViewerZoomTests
{
    private static ImageViewerViewModel CreateSut() =>
        new(new FakeProjectService(), new FakeImageCache(), new ImmediateUiDispatcher(), new FakeSettingsService(), NullLogger<ImageViewerViewModel>.Instance);

    [Fact]
    public void InitialState_IsFitToWindow()
    {
        var sut = CreateSut();

        Assert.Equal(ZoomMode.FitToWindow, sut.ZoomMode);
        Assert.Equal(1.0, sut.ZoomFactor);
    }

    [Fact]
    public void ApplyZoomDelta_Positive_SwitchesToManualAndZoomsIn()
    {
        var sut = CreateSut();

        sut.ApplyZoomDelta(1);

        Assert.Equal(ZoomMode.Manual, sut.ZoomMode);
        Assert.True(sut.ZoomFactor > 1.0);
    }

    [Fact]
    public void ApplyZoomDelta_Negative_ZoomsOut()
    {
        var sut = CreateSut();

        sut.ApplyZoomDelta(1);
        var afterZoomIn = sut.ZoomFactor;
        sut.ApplyZoomDelta(-1);

        Assert.True(sut.ZoomFactor < afterZoomIn);
    }

    [Fact]
    public void ApplyZoomDelta_RepeatedZoomOut_ClampsAtMinimum()
    {
        var sut = CreateSut();

        for (var i = 0; i < 200; i++)
        {
            sut.ApplyZoomDelta(-1);
        }

        Assert.True(sut.ZoomFactor >= 0.1);
    }

    [Fact]
    public void ApplyZoomDelta_RepeatedZoomIn_ClampsAtMaximum()
    {
        var sut = CreateSut();

        for (var i = 0; i < 200; i++)
        {
            sut.ApplyZoomDelta(1);
        }

        Assert.True(sut.ZoomFactor <= 8.0);
    }

    [Fact]
    public void SetPan_WhileFitToWindow_IsIgnored()
    {
        var sut = CreateSut();

        sut.SetPan(50, 50);

        Assert.Equal(0, sut.PanOffsetX);
        Assert.Equal(0, sut.PanOffsetY);
    }

    [Fact]
    public void SetPan_WhileManual_UpdatesOffset()
    {
        var sut = CreateSut();
        sut.ApplyZoomDelta(1);

        sut.SetPan(20, -10);

        Assert.Equal(20, sut.PanOffsetX);
        Assert.Equal(-10, sut.PanOffsetY);
    }

    [Fact]
    public void ToggleZoomModeCommand_FromFit_SwitchesToManualAt100Percent()
    {
        var sut = CreateSut();

        sut.ToggleZoomModeCommand.Execute(null);

        Assert.Equal(ZoomMode.Manual, sut.ZoomMode);
        Assert.Equal(1.0, sut.ZoomFactor);
    }

    [Fact]
    public void ToggleZoomModeCommand_TwiceAfterPanning_ReturnsToFitAndResetsPan()
    {
        var sut = CreateSut();
        sut.ToggleZoomModeCommand.Execute(null);
        sut.SetPan(30, 30);

        sut.ToggleZoomModeCommand.Execute(null);

        Assert.Equal(ZoomMode.FitToWindow, sut.ZoomMode);
        Assert.Equal(0, sut.PanOffsetX);
        Assert.Equal(0, sut.PanOffsetY);
    }

    [Fact]
    public void ResetZoomCommand_WhileZoomedAndPanned_ReturnsToFitAndResetsPan()
    {
        var sut = CreateSut();
        sut.ApplyZoomDelta(1);
        sut.SetPan(40, -20);

        sut.ResetZoomCommand.Execute(null);

        Assert.Equal(ZoomMode.FitToWindow, sut.ZoomMode);
        Assert.Equal(1.0, sut.ZoomFactor);
        Assert.Equal(0, sut.PanOffsetX);
        Assert.Equal(0, sut.PanOffsetY);
    }

    [Fact]
    public void ResetZoomCommand_UnlikeToggleZoomMode_AlwaysLandsOnFitToWindow()
    {
        var sut = CreateSut();
        sut.ToggleZoomModeCommand.Execute(null); // now Manual @ 100%

        sut.ResetZoomCommand.Execute(null);

        Assert.Equal(ZoomMode.FitToWindow, sut.ZoomMode);
    }
}
