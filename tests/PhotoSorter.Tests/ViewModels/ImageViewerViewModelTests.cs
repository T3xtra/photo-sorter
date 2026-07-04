using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using PhotoSorter.Core.Models;
using PhotoSorter.Core.ViewModels;
using PhotoSorter.Tests.TestDoubles;
using SixLabors.ImageSharp;

namespace PhotoSorter.Tests.ViewModels;

public sealed class ImageViewerViewModelTests
{
    private static ImageFile MakeImage(string name) => new() { FullPath = $"/{name}", FileName = name, SizeInBytes = 1 };

    private static async Task<(ImageViewerViewModel Sut, FakeProjectService Project, FakeImageCache Cache)> CreateLoadedSutAsync(int imageCount = 3)
    {
        var project = new FakeProjectService();
        var cache = new FakeImageCache();
        var sut = new ImageViewerViewModel(project, cache, new ImmediateUiDispatcher(), new FakeSettingsService(), NullLogger<ImageViewerViewModel>.Instance);

        var images = new ImageFile[imageCount];
        for (var i = 0; i < imageCount; i++)
        {
            images[i] = MakeImage($"img{i}.jpg");
        }

        project.SetImages(images);
        project.RaiseProjectChanged();

        // The load triggered by ProjectChanged is fire-and-forget; give it a beat to complete.
        await Task.Delay(50);

        return (sut, project, cache);
    }

    [Fact]
    public async Task ProjectChanged_WithImages_LoadsFirstImage()
    {
        var (sut, _, cache) = await CreateLoadedSutAsync();

        Assert.Equal(cache.Result, sut.CurrentImageBytes);
        Assert.Contains("/img0.jpg", cache.RequestedPaths);
    }

    [Fact]
    public async Task NextImageCommand_AdvancesToSecondImage()
    {
        var (sut, _, cache) = await CreateLoadedSutAsync();

        sut.NextImageCommand.Execute(null);
        await Task.Delay(50);

        Assert.Contains("/img1.jpg", cache.RequestedPaths);
    }

    [Fact]
    public async Task CanGoToPrevious_IsFalse_OnFirstImage()
    {
        var (sut, _, _) = await CreateLoadedSutAsync();

        Assert.False(sut.CanGoToPrevious);
        Assert.True(sut.CanGoToNext);
    }

    [Fact]
    public async Task CanGoToNext_IsFalse_OnLastImage()
    {
        var (sut, project, _) = await CreateLoadedSutAsync(imageCount: 2);

        sut.NextImageCommand.Execute(null);
        await Task.Delay(50);

        Assert.False(sut.CanGoToNext);
        Assert.True(sut.CanGoToPrevious);
        Assert.Equal(1, project.CurrentIndex);
    }

    [Fact]
    public void ProjectChanged_WithNoImages_ClearsCurrentImage()
    {
        var project = new FakeProjectService();
        var cache = new FakeImageCache();
        var sut = new ImageViewerViewModel(project, cache, new ImmediateUiDispatcher(), new FakeSettingsService(), NullLogger<ImageViewerViewModel>.Instance);

        project.RaiseProjectChanged();

        Assert.Null(sut.CurrentImageBytes);
        Assert.False(sut.CanGoToNext);
        Assert.False(sut.CanGoToPrevious);
    }

    [Fact]
    public async Task ProjectChanged_WithImages_UpdatesCacheWindowAroundCurrentIndex()
    {
        var (_, _, cache) = await CreateLoadedSutAsync(imageCount: 3);

        var lastWindow = cache.WindowUpdates[^1];
        Assert.Equal(["/img0.jpg", "/img1.jpg", "/img2.jpg"], lastWindow);
    }

    [Fact]
    public async Task NextImageCommand_ShiftsCacheWindow()
    {
        var (sut, _, cache) = await CreateLoadedSutAsync(imageCount: 5);
        cache.WindowUpdates.Clear();

        sut.NextImageCommand.Execute(null);
        await Task.Delay(50);

        // Radius 2 around index 1 (img1) spans img0..img3 (img4 is out of range on the right by one).
        var lastWindow = cache.WindowUpdates[^1];
        Assert.Equal(["/img0.jpg", "/img1.jpg", "/img2.jpg", "/img3.jpg"], lastWindow);
    }

    [Fact]
    public async Task ProjectChanged_WhenDecodingFails_SetsFriendlyErrorMessageInsteadOfCrashing()
    {
        var project = new FakeProjectService();
        var cache = new FakeImageCache { ExceptionToThrow = new InvalidImageContentException("corrupt") };
        var sut = new ImageViewerViewModel(project, cache, new ImmediateUiDispatcher(), new FakeSettingsService(), NullLogger<ImageViewerViewModel>.Instance);

        project.SetImages([MakeImage("broken.jpg")]);
        project.RaiseProjectChanged();
        await Task.Delay(50);

        Assert.Null(sut.CurrentImageBytes);
        Assert.True(sut.HasImageLoadError);
        Assert.Contains("broken.jpg", sut.ImageLoadErrorMessage);
    }

    [Fact]
    public async Task ProjectChanged_WhenFileMissing_MentionsMissingFileInErrorMessage()
    {
        var project = new FakeProjectService();
        var cache = new FakeImageCache { ExceptionToThrow = new FileNotFoundException("not found", "/missing.jpg") };
        var sut = new ImageViewerViewModel(project, cache, new ImmediateUiDispatcher(), new FakeSettingsService(), NullLogger<ImageViewerViewModel>.Instance);

        project.SetImages([MakeImage("missing.jpg")]);
        project.RaiseProjectChanged();
        await Task.Delay(50);

        Assert.Contains("nicht gefunden", sut.ImageLoadErrorMessage);
    }

    [Fact]
    public async Task ProjectChanged_WhenAccessDenied_MentionsAccessInErrorMessage()
    {
        var project = new FakeProjectService();
        var cache = new FakeImageCache { ExceptionToThrow = new UnauthorizedAccessException("denied") };
        var sut = new ImageViewerViewModel(project, cache, new ImmediateUiDispatcher(), new FakeSettingsService(), NullLogger<ImageViewerViewModel>.Instance);

        project.SetImages([MakeImage("locked.jpg")]);
        project.RaiseProjectChanged();
        await Task.Delay(50);

        Assert.Contains("Zugriff", sut.ImageLoadErrorMessage);
    }

    [Fact]
    public async Task NextImageCommand_AfterAFailedLoad_ClearsErrorOnceANewImageLoadsSuccessfully()
    {
        var project = new FakeProjectService();
        var cache = new FakeImageCache();
        var sut = new ImageViewerViewModel(project, cache, new ImmediateUiDispatcher(), new FakeSettingsService(), NullLogger<ImageViewerViewModel>.Instance);

        project.SetImages([MakeImage("broken.jpg"), MakeImage("ok.jpg")]);
        cache.ExceptionToThrow = new UnknownImageFormatException("bad format");
        project.RaiseProjectChanged();
        await Task.Delay(50);
        Assert.True(sut.HasImageLoadError);

        cache.ExceptionToThrow = null;
        sut.NextImageCommand.Execute(null);
        await Task.Delay(50);

        Assert.False(sut.HasImageLoadError);
        Assert.Null(sut.ImageLoadErrorMessage);
        Assert.Equal(cache.Result, sut.CurrentImageBytes);
    }
}
