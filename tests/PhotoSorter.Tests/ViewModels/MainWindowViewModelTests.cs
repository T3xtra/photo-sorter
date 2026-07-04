using Microsoft.Extensions.Logging.Abstractions;
using PhotoSorter.Core.Services.Implementations;
using PhotoSorter.Core.ViewModels;
using PhotoSorter.Tests.TestDoubles;

namespace PhotoSorter.Tests.ViewModels;

public sealed class MainWindowViewModelTests
{
    private static MainWindowViewModel CreateSut()
    {
        var projectService = new FakeProjectService();
        var uiDispatcher = new ImmediateUiDispatcher();

        var toolbar = new ToolbarViewModel(
            new FakeFolderPickerService(),
            projectService,
            new FakeFileMoveService(),
            new FakeApplyConfirmationDialogService(),
            new FakeMessageDialogService(),
            new FakeSettingsDialogService(),
            new FakeSettingsService(),
            new HotkeyService(new FakeSettingsService()),
            NullLogger<ToolbarViewModel>.Instance);
        var thumbnailBar = new ThumbnailBarViewModel(projectService, new FakeThumbnailGenerator(), uiDispatcher, NullLogger<ThumbnailBarViewModel>.Instance);
        var imageViewer = new ImageViewerViewModel(projectService, new FakeImageCache(), uiDispatcher, new FakeSettingsService(), NullLogger<ImageViewerViewModel>.Instance);
        var statusBar = new StatusBarViewModel(projectService, uiDispatcher);

        return new MainWindowViewModel(toolbar, thumbnailBar, imageViewer, statusBar);
    }

    [Fact]
    public void Constructor_InitializesZoomDisplayTo100Percent()
    {
        var sut = CreateSut();

        Assert.Equal(100, sut.StatusBarViewModel.ZoomPercentage);
    }

    [Fact]
    public void ImageViewerZoomFactorChange_UpdatesStatusBarZoomPercentage()
    {
        var sut = CreateSut();

        sut.ImageViewerViewModel.ApplyZoomDelta(1);

        Assert.Equal((int)System.Math.Round(sut.ImageViewerViewModel.ZoomFactor * 100), sut.StatusBarViewModel.ZoomPercentage);
        Assert.True(sut.StatusBarViewModel.ZoomPercentage > 100);
    }
}
