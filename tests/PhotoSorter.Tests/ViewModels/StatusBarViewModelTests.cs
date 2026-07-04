using PhotoSorter.Core.Models;
using PhotoSorter.Core.ViewModels;
using PhotoSorter.Tests.TestDoubles;

namespace PhotoSorter.Tests.ViewModels;

public sealed class StatusBarViewModelTests
{
    private static StatusBarViewModel CreateSut(FakeProjectService? projectService = null) =>
        new(projectService ?? new FakeProjectService(), new ImmediateUiDispatcher());

    [Fact]
    public void ImagePositionDisplay_WhenNothingLoaded_ShowsPlaceholder()
    {
        var sut = CreateSut();

        Assert.Equal("Kein Bild geladen", sut.ImagePositionDisplay);
    }

    [Fact]
    public void ImagePositionDisplay_WhenImagesLoaded_ShowsCurrentOverTotal()
    {
        var sut = CreateSut();
        sut.CurrentIndex = 127;
        sut.TotalCount = 984;

        Assert.Equal("Bild 127 / 984", sut.ImagePositionDisplay);
    }

    [Fact]
    public void ImagePositionDisplay_RaisesChangeNotification_WhenTotalCountChanges()
    {
        var sut = CreateSut();
        var raisedProperties = new List<string?>();
        sut.PropertyChanged += (_, e) => raisedProperties.Add(e.PropertyName);

        sut.TotalCount = 10;

        Assert.Contains(nameof(StatusBarViewModel.ImagePositionDisplay), raisedProperties);
    }

    [Fact]
    public void Constructor_WhenProjectAlreadyHasImages_ReflectsThemImmediately()
    {
        var projectService = new FakeProjectService();
        projectService.SetImages(
        [
            new ImageFile { FullPath = "/a.jpg", FileName = "a.jpg", SizeInBytes = 1 },
            new ImageFile { FullPath = "/b.jpg", FileName = "b.jpg", SizeInBytes = 1 },
        ]);

        var sut = CreateSut(projectService);

        Assert.Equal(2, sut.TotalCount);
        Assert.Equal(2, sut.OpenCount);
        Assert.Equal(1, sut.CurrentIndex);
    }

    [Fact]
    public void ProjectChanged_WhenRaised_UpdatesCounts()
    {
        var projectService = new FakeProjectService();
        var sut = CreateSut(projectService);

        projectService.SetImages(
        [
            new ImageFile { FullPath = "/a.jpg", FileName = "a.jpg", SizeInBytes = 1 },
            new ImageFile { FullPath = "/b.jpg", FileName = "b.jpg", SizeInBytes = 1 },
            new ImageFile { FullPath = "/c.jpg", FileName = "c.jpg", SizeInBytes = 1 },
        ]);
        projectService.RaiseProjectChanged();

        Assert.Equal(3, sut.TotalCount);
        Assert.Equal(3, sut.OpenCount);
        Assert.Equal("Bild 1 / 3", sut.ImagePositionDisplay);
    }

    [Fact]
    public void DecisionsChanged_WhenRaised_UpdatesLeftRightAndOpenCounts()
    {
        var projectService = new FakeProjectService();
        projectService.SetImages(
        [
            new ImageFile { FullPath = "/a.jpg", FileName = "a.jpg", SizeInBytes = 1 },
            new ImageFile { FullPath = "/b.jpg", FileName = "b.jpg", SizeInBytes = 1 },
            new ImageFile { FullPath = "/c.jpg", FileName = "c.jpg", SizeInBytes = 1 },
        ]);
        var sut = CreateSut(projectService);

        projectService.RecordDecision(SortAction.Left);

        Assert.Equal(1, sut.LeftCount);
        Assert.Equal(0, sut.RightCount);
        Assert.Equal(2, sut.OpenCount);
    }
}
