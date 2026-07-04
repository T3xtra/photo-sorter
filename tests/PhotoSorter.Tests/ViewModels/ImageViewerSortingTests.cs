using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using PhotoSorter.Core.Models;
using PhotoSorter.Core.ViewModels;
using PhotoSorter.Tests.TestDoubles;

namespace PhotoSorter.Tests.ViewModels;

public sealed class ImageViewerSortingTests
{
    private static ImageFile MakeImage(string name) => new() { FullPath = $"/{name}", FileName = name, SizeInBytes = 1 };

    /// <summary>Animations disabled by default here: sort commands then complete synchronously-awaited, without a real 150 ms wait per test.</summary>
    private static (ImageViewerViewModel Sut, FakeProjectService Project) CreateSut(int imageCount = 2, bool animationsEnabled = false)
    {
        var project = new FakeProjectService();
        var images = new ImageFile[imageCount];
        for (var i = 0; i < imageCount; i++)
        {
            images[i] = MakeImage($"img{i}.jpg");
        }

        project.SetImages(images);

        var settings = new FakeSettingsService();
        settings.Current.AnimationsEnabled = animationsEnabled;
        var sut = new ImageViewerViewModel(project, new FakeImageCache(), new ImmediateUiDispatcher(), settings, NullLogger<ImageViewerViewModel>.Instance);
        return (sut, project);
    }

    [Fact]
    public void Constructor_ReadsAnimationsEnabledFromSettings()
    {
        var settingsOn = new FakeSettingsService();
        settingsOn.Current.AnimationsEnabled = true;
        var settingsOff = new FakeSettingsService();
        settingsOff.Current.AnimationsEnabled = false;

        var sutOn = new ImageViewerViewModel(new FakeProjectService(), new FakeImageCache(), new ImmediateUiDispatcher(), settingsOn, NullLogger<ImageViewerViewModel>.Instance);
        var sutOff = new ImageViewerViewModel(new FakeProjectService(), new FakeImageCache(), new ImmediateUiDispatcher(), settingsOff, NullLogger<ImageViewerViewModel>.Instance);

        Assert.True(sutOn.AnimationsEnabled);
        Assert.False(sutOff.AnimationsEnabled);
    }

    [Fact]
    public async Task SortLeftCommand_RecordsLeftDecision_AndAdvances()
    {
        var (sut, project) = CreateSut();

        await sut.SortLeftCommand.ExecuteAsync(null);

        Assert.Equal(1, project.Current.LeftCount);
        Assert.Equal(1, project.CurrentIndex);
    }

    [Fact]
    public async Task SortRightCommand_RecordsRightDecision()
    {
        var (sut, project) = CreateSut();

        await sut.SortRightCommand.ExecuteAsync(null);

        Assert.Equal(1, project.Current.RightCount);
    }

    [Fact]
    public void SkipCommand_AdvancesWithoutRecordingADecision()
    {
        var (sut, project) = CreateSut();

        sut.SkipCommand.Execute(null);

        Assert.Equal(0, project.Current.LeftCount);
        Assert.Equal(0, project.Current.RightCount);
        Assert.Equal(1, project.CurrentIndex);
    }

    [Fact]
    public void HasCurrentImage_IsFalse_WhenNothingLoaded()
    {
        var project = new FakeProjectService();
        var sut = new ImageViewerViewModel(project, new FakeImageCache(), new ImmediateUiDispatcher(), new FakeSettingsService(), NullLogger<ImageViewerViewModel>.Instance);

        Assert.False(sut.HasCurrentImage);
        Assert.False(sut.SortLeftCommand.CanExecute(null));
    }

    [Fact]
    public async Task CanUndo_IsFalse_UntilADecisionIsMade()
    {
        var (sut, _) = CreateSut();

        Assert.False(sut.CanUndo);

        await sut.SortLeftCommand.ExecuteAsync(null);

        Assert.True(sut.CanUndo);
    }

    [Fact]
    public async Task UndoCommand_ReversesLastDecision_AndJumpsBack()
    {
        var (sut, project) = CreateSut();
        await sut.SortLeftCommand.ExecuteAsync(null);

        sut.UndoCommand.Execute(null);

        Assert.Equal(0, project.Current.LeftCount);
        Assert.Equal(0, project.CurrentIndex);
        Assert.False(sut.CanUndo);
    }

    [Fact]
    public async Task SortLeftCommand_WhenAnimationsDisabled_DoesNotChangeSwipeProperties()
    {
        var (sut, _) = CreateSut(animationsEnabled: false);

        await sut.SortLeftCommand.ExecuteAsync(null);

        Assert.Equal(0, sut.SwipeOffsetX);
        Assert.Equal(0, sut.SwipeRotationAngle);
        Assert.Equal(1, sut.SwipeOpacity);
    }

    [Fact]
    public async Task SortLeftCommand_WhenAnimationsEnabled_ResetsSwipePropertiesAfterCompletion()
    {
        var (sut, project) = CreateSut(animationsEnabled: true);

        await sut.SortLeftCommand.ExecuteAsync(null);

        // The decision must be recorded and the transform reset back to neutral once the
        // animate-then-record sequence has fully completed.
        Assert.Equal(1, project.Current.LeftCount);
        Assert.Equal(0, sut.SwipeOffsetX);
        Assert.Equal(0, sut.SwipeRotationAngle);
        Assert.Equal(1, sut.SwipeOpacity);
    }

    [Fact]
    public async Task SortLeftCommand_WhenAnimationsEnabled_MovesOffscreenToTheLeftDuringTheAnimation()
    {
        var (sut, project) = CreateSut(animationsEnabled: true);

        var executeTask = sut.SortLeftCommand.ExecuteAsync(null);
        await Task.Delay(30); // well before the ~150 ms animation completes

        Assert.True(sut.SwipeOffsetX < 0, "Expected the image to be mid-flight towards the left.");
        Assert.Equal(0, project.Current.LeftCount); // decision not yet recorded mid-animation

        await executeTask;
    }

    [Fact]
    public async Task SortRightCommand_WhenAnimationsEnabled_MovesOffscreenToTheRightDuringTheAnimation()
    {
        var (sut, _) = CreateSut(animationsEnabled: true);

        var executeTask = sut.SortRightCommand.ExecuteAsync(null);
        await Task.Delay(30);

        Assert.True(sut.SwipeOffsetX > 0, "Expected the image to be mid-flight towards the right.");

        await executeTask;
    }
}
