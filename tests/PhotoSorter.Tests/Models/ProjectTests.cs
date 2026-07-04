using PhotoSorter.Core.Models;

namespace PhotoSorter.Tests.Models;

public sealed class ProjectTests
{
    private static ImageFile MakeImage(string name) => new() { FullPath = $"/{name}", FileName = name, SizeInBytes = 1 };

    [Fact]
    public void AddDecision_UpdatesCountsAndIsDecided()
    {
        var sut = new Project();
        var a = MakeImage("a.jpg");
        var b = MakeImage("b.jpg");
        sut.Reset([], [a, b]);

        sut.AddDecision(new SortDecision(a, SortAction.Left));

        Assert.Equal(1, sut.LeftCount);
        Assert.Equal(0, sut.RightCount);
        Assert.Equal(1, sut.OpenCount);
        Assert.True(sut.IsDecided(a));
        Assert.False(sut.IsDecided(b));
    }

    [Fact]
    public void RemoveLastDecision_UndoesMostRecentOnly()
    {
        var sut = new Project();
        var a = MakeImage("a.jpg");
        var b = MakeImage("b.jpg");
        sut.Reset([], [a, b]);
        sut.AddDecision(new SortDecision(a, SortAction.Left));
        sut.AddDecision(new SortDecision(b, SortAction.Right));

        var removed = sut.RemoveLastDecision();

        Assert.Equal(b, removed?.Image);
        Assert.True(sut.IsDecided(a));
        Assert.False(sut.IsDecided(b));
        Assert.Equal(1, sut.OpenCount);
    }

    [Fact]
    public void RemoveLastDecision_WhenEmpty_ReturnsNull()
    {
        var sut = new Project();

        Assert.Null(sut.RemoveLastDecision());
    }

    [Fact]
    public void Reset_ClearsPreviousDecisions()
    {
        var sut = new Project();
        var a = MakeImage("a.jpg");
        sut.Reset([], [a]);
        sut.AddDecision(new SortDecision(a, SortAction.Left));

        sut.Reset([], [MakeImage("b.jpg")]);

        Assert.Equal(0, sut.LeftCount);
        Assert.Equal(1, sut.OpenCount);
    }

    [Fact]
    public void RemoveImages_RemovesFromImagesAndDecisions_LeavesOthersIntact()
    {
        var sut = new Project();
        var a = MakeImage("a.jpg");
        var b = MakeImage("b.jpg");
        var c = MakeImage("c.jpg");
        sut.Reset([], [a, b, c]);
        sut.AddDecision(new SortDecision(a, SortAction.Left));
        sut.AddDecision(new SortDecision(b, SortAction.Right));

        sut.RemoveImages([a]);

        Assert.Equal(2, sut.Images.Count);
        Assert.DoesNotContain(a, sut.Images);
        Assert.Contains(b, sut.Images);
        Assert.Contains(c, sut.Images);
        Assert.Equal(0, sut.LeftCount);
        Assert.Equal(1, sut.RightCount);
        Assert.False(sut.IsDecided(a));
        Assert.True(sut.IsDecided(b));
    }

    [Fact]
    public void RemoveImages_WithEmptyCollection_IsNoOp()
    {
        var sut = new Project();
        var a = MakeImage("a.jpg");
        sut.Reset([], [a]);

        sut.RemoveImages([]);

        Assert.Single(sut.Images);
    }
}
