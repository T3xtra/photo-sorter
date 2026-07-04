using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using PhotoSorter.Core.Models;
using PhotoSorter.Core.Models.Persistence;
using PhotoSorter.Core.Services.Implementations;

namespace PhotoSorter.Tests.Services;

public sealed class JsonProjectFileSerializerTests : IDisposable
{
    private readonly string _root;

    public JsonProjectFileSerializerTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "PhotoSorterTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAsync_ThenLoadAsync_RoundTripsAllFields()
    {
        var sut = new JsonProjectFileSerializer(NullLogger<JsonProjectFileSerializer>.Instance);
        var path = Path.Combine(_root, "session.photosort");
        var data = new ProjectFileDto
        {
            SourceFolders = ["/photos/a", "/photos/b"],
            ImagePaths = ["/photos/a/1.jpg", "/photos/a/2.jpg"],
            LeftTarget = new TargetFolderDto { IsTrash = true },
            RightTarget = new TargetFolderDto { IsTrash = false, Path = "/export" },
            Decisions = [new DecisionDto { ImagePath = "/photos/a/1.jpg", Action = SortAction.Left }],
            CurrentIndex = 1,
        };

        await sut.SaveAsync(data, path);
        var loaded = await sut.LoadAsync(path);

        Assert.NotNull(loaded);
        Assert.Equal(data.SourceFolders, loaded!.SourceFolders);
        Assert.Equal(data.ImagePaths, loaded.ImagePaths);
        Assert.True(loaded.LeftTarget!.IsTrash);
        Assert.Equal("/export", loaded.RightTarget!.Path);
        Assert.Single(loaded.Decisions);
        Assert.Equal(SortAction.Left, loaded.Decisions[0].Action);
        Assert.Equal(1, loaded.CurrentIndex);
    }

    [Fact]
    public async Task LoadAsync_WhenFileDoesNotExist_ReturnsNull()
    {
        var sut = new JsonProjectFileSerializer(NullLogger<JsonProjectFileSerializer>.Instance);

        var loaded = await sut.LoadAsync(Path.Combine(_root, "missing.photosort"));

        Assert.Null(loaded);
    }

    [Fact]
    public async Task LoadAsync_WhenFileIsCorrupt_ReturnsNullWithoutThrowing()
    {
        var sut = new JsonProjectFileSerializer(NullLogger<JsonProjectFileSerializer>.Instance);
        var path = Path.Combine(_root, "corrupt.photosort");
        await File.WriteAllTextAsync(path, "{ not valid json");

        var loaded = await sut.LoadAsync(path);

        Assert.Null(loaded);
    }

    /// <summary>
    /// Regression test (Roadmap Phase 16: Zugriffsfehler / Projektwiederherstellung): the previous
    /// implementation called File.OpenRead outside its try block, so a permission-denied project
    /// file (e.g. the autosave file restored at startup) would throw an unhandled
    /// UnauthorizedAccessException instead of the graceful "restore failed" path.
    /// </summary>
    [Fact]
    public async Task LoadAsync_WhenFileIsUnreadable_ReturnsNullWithoutThrowing()
    {
        if (OperatingSystem.IsWindows())
        {
            return; // POSIX permission bits aren't a reliable way to force access-denied on Windows.
        }

        var sut = new JsonProjectFileSerializer(NullLogger<JsonProjectFileSerializer>.Instance);
        var path = Path.Combine(_root, "unreadable.photosort");
        await File.WriteAllTextAsync(path, "{}");
        File.SetUnixFileMode(path, UnixFileMode.None);

        var loaded = await sut.LoadAsync(path);

        Assert.Null(loaded);
    }

    [Fact]
    public async Task SaveAsync_CreatesMissingDirectory()
    {
        var sut = new JsonProjectFileSerializer(NullLogger<JsonProjectFileSerializer>.Instance);
        var nestedPath = Path.Combine(_root, "nested", "session.photosort");

        await sut.SaveAsync(new ProjectFileDto(), nestedPath);

        Assert.True(File.Exists(nestedPath));
    }
}
