using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using PhotoSorter.Core.Models;
using PhotoSorter.Core.ViewModels;
using PhotoSorter.Tests.TestDoubles;

namespace PhotoSorter.Tests.ViewModels;

public sealed class ThumbnailBarViewModelTests
{
    private static ImageFile MakeImage(string name) => new() { FullPath = $"/{name}", FileName = name, SizeInBytes = 1 };

    [Fact]
    public void ProjectChanged_PopulatesOneThumbnailItemPerImage()
    {
        var project = new FakeProjectService();
        var sut = new ThumbnailBarViewModel(project, new FakeThumbnailGenerator(), new ImmediateUiDispatcher(), NullLogger<ThumbnailBarViewModel>.Instance);

        project.SetImages([MakeImage("a.jpg"), MakeImage("b.jpg")]);
        project.RaiseProjectChanged();

        Assert.Equal(2, sut.Thumbnails.Count);
        Assert.Equal("a.jpg", sut.Thumbnails[0].FileName);
        Assert.Equal("b.jpg", sut.Thumbnails[1].FileName);
    }

    [Fact]
    public void ProjectChanged_MarksFirstThumbnailAsCurrent()
    {
        var project = new FakeProjectService();
        var sut = new ThumbnailBarViewModel(project, new FakeThumbnailGenerator(), new ImmediateUiDispatcher(), NullLogger<ThumbnailBarViewModel>.Instance);

        project.SetImages([MakeImage("a.jpg"), MakeImage("b.jpg")]);
        project.RaiseProjectChanged();

        Assert.True(sut.Thumbnails[0].IsCurrent);
        Assert.False(sut.Thumbnails[1].IsCurrent);
        Assert.Same(sut.Thumbnails[0], sut.SelectedThumbnail);
    }

    [Fact]
    public async Task ProjectChanged_GeneratesThumbnailBytes_ForEachImage()
    {
        var project = new FakeProjectService();
        var generator = new FakeThumbnailGenerator { Result = [1, 2, 3] };
        var sut = new ThumbnailBarViewModel(project, generator, new ImmediateUiDispatcher(), NullLogger<ThumbnailBarViewModel>.Instance);

        project.SetImages([MakeImage("a.jpg"), MakeImage("b.jpg")]);
        project.RaiseProjectChanged();

        await Task.Delay(100);

        Assert.All(sut.Thumbnails, t => Assert.Equal(generator.Result, t.ThumbnailBytes));
        Assert.Equal(2, generator.RequestedPaths.Count);
    }

    [Fact]
    public void SelectCommand_MovesProjectToThatImage()
    {
        var project = new FakeProjectService();
        var sut = new ThumbnailBarViewModel(project, new FakeThumbnailGenerator(), new ImmediateUiDispatcher(), NullLogger<ThumbnailBarViewModel>.Instance);
        project.SetImages([MakeImage("a.jpg"), MakeImage("b.jpg"), MakeImage("c.jpg")]);
        project.RaiseProjectChanged();

        sut.Thumbnails[2].SelectCommand.Execute(null);

        Assert.Equal(2, project.CurrentIndex);
    }

    [Fact]
    public void CurrentIndexChanged_UpdatesHighlightAcrossThumbnails()
    {
        var project = new FakeProjectService();
        var sut = new ThumbnailBarViewModel(project, new FakeThumbnailGenerator(), new ImmediateUiDispatcher(), NullLogger<ThumbnailBarViewModel>.Instance);
        project.SetImages([MakeImage("a.jpg"), MakeImage("b.jpg")]);
        project.RaiseProjectChanged();

        project.MoveNext();

        Assert.False(sut.Thumbnails[0].IsCurrent);
        Assert.True(sut.Thumbnails[1].IsCurrent);
    }

    [Fact]
    public void DecisionRecorded_RemovesSortedThumbnail_ButKeepsOpenOnes()
    {
        var project = new FakeProjectService();
        var sut = new ThumbnailBarViewModel(project, new FakeThumbnailGenerator(), new ImmediateUiDispatcher(), NullLogger<ThumbnailBarViewModel>.Instance);
        project.SetImages([MakeImage("a.jpg"), MakeImage("b.jpg"), MakeImage("c.jpg")]);
        project.RaiseProjectChanged();

        project.RecordDecision(SortAction.Left);

        Assert.Equal(2, sut.Thumbnails.Count);
        Assert.DoesNotContain(sut.Thumbnails, t => t.FileName == "a.jpg");
    }

    [Fact]
    public void DecisionRecorded_HighlightStaysCorrect_AfterRemoval()
    {
        var project = new FakeProjectService();
        var sut = new ThumbnailBarViewModel(project, new FakeThumbnailGenerator(), new ImmediateUiDispatcher(), NullLogger<ThumbnailBarViewModel>.Instance);
        project.SetImages([MakeImage("a.jpg"), MakeImage("b.jpg"), MakeImage("c.jpg")]);
        project.RaiseProjectChanged();

        project.RecordDecision(SortAction.Left); // a.jpg decided, moves to b.jpg

        Assert.True(sut.Thumbnails.Single(t => t.FileName == "b.jpg").IsCurrent);
        Assert.Same(sut.SelectedThumbnail, sut.Thumbnails.Single(t => t.FileName == "b.jpg"));
    }

    [Fact]
    public async Task ProjectChanged_WhenOneThumbnailFailsToGenerate_MarksOnlyThatOneAsError()
    {
        var project = new FakeProjectService();
        var generator = new FakeThumbnailGenerator { ExceptionToThrow = new System.IO.IOException("corrupt") };
        generator.FailingPaths.Add("/broken.jpg");
        var sut = new ThumbnailBarViewModel(project, generator, new ImmediateUiDispatcher(), NullLogger<ThumbnailBarViewModel>.Instance);

        project.SetImages([MakeImage("ok.jpg"), MakeImage("broken.jpg")]);
        project.RaiseProjectChanged();
        await Task.Delay(100);

        var okThumbnail = sut.Thumbnails.Single(t => t.FileName == "ok.jpg");
        var brokenThumbnail = sut.Thumbnails.Single(t => t.FileName == "broken.jpg");

        Assert.False(okThumbnail.HasError);
        Assert.NotNull(okThumbnail.ThumbnailBytes);
        Assert.True(brokenThumbnail.HasError);
        Assert.Null(brokenThumbnail.ThumbnailBytes);
        Assert.Contains("nicht geladen", brokenThumbnail.ToolTipText);
    }

    [Fact]
    public async Task ProjectChanged_WhenThumbnailGenerationThrowsUnauthorizedAccess_DoesNotCrashAndMarksError()
    {
        var project = new FakeProjectService();
        var generator = new FakeThumbnailGenerator { ExceptionToThrow = new UnauthorizedAccessException("denied") };
        var sut = new ThumbnailBarViewModel(project, generator, new ImmediateUiDispatcher(), NullLogger<ThumbnailBarViewModel>.Instance);

        project.SetImages([MakeImage("locked.jpg")]);
        project.RaiseProjectChanged();
        await Task.Delay(100);

        Assert.True(sut.Thumbnails.Single().HasError);
    }
}
