using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using PhotoSorter.Core.Services.Implementations;
using PhotoSorter.Tests.TestDoubles;

namespace PhotoSorter.Tests.Services;

public sealed class ImageCacheTests
{
    [Fact]
    public async Task GetAsync_DecodesOnce_ThenServesFromCache()
    {
        var decoder = new FakeImageDecoder { Result = [9, 9, 9] };
        var sut = new ImageCache(decoder, NullLogger<ImageCache>.Instance);

        var first = await sut.GetAsync("/a.jpg");
        var second = await sut.GetAsync("/a.jpg");

        Assert.Equal(decoder.Result, first);
        Assert.Same(first, second); // same cached array instance, not re-decoded
        Assert.Single(decoder.RequestedPaths);
    }

    [Fact]
    public async Task UpdateWindow_EvictsEntriesNotInTheNewWindow()
    {
        var decoder = new FakeImageDecoder();
        var sut = new ImageCache(decoder, NullLogger<ImageCache>.Instance);
        await sut.GetAsync("/a.jpg");
        await sut.GetAsync("/b.jpg");

        sut.UpdateWindow(["/b.jpg"]);
        decoder.RequestedPaths.Clear();

        // "/a.jpg" was evicted, so fetching it again must decode again; "/b.jpg" stays cached.
        await sut.GetAsync("/a.jpg");
        await sut.GetAsync("/b.jpg");

        Assert.Equal(["/a.jpg"], decoder.RequestedPaths);
    }

    [Fact]
    public async Task UpdateWindow_PreloadsUncachedPathsInBackground()
    {
        var decoder = new FakeImageDecoder();
        var sut = new ImageCache(decoder, NullLogger<ImageCache>.Instance);

        sut.UpdateWindow(["/a.jpg", "/b.jpg"]);
        await Task.Delay(50); // preloading is fire-and-forget

        Assert.Contains("/a.jpg", decoder.RequestedPaths);
        Assert.Contains("/b.jpg", decoder.RequestedPaths);
    }

    [Fact]
    public async Task UpdateWindow_WithPathAlreadyCached_DoesNotRedecodeIt()
    {
        var decoder = new FakeImageDecoder();
        var sut = new ImageCache(decoder, NullLogger<ImageCache>.Instance);
        await sut.GetAsync("/a.jpg");
        decoder.RequestedPaths.Clear();

        sut.UpdateWindow(["/a.jpg"]);
        await Task.Delay(50);

        Assert.Empty(decoder.RequestedPaths);
    }
}
