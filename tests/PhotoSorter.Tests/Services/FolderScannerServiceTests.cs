using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using PhotoSorter.Core.Services.Implementations;

namespace PhotoSorter.Tests.Services;

public sealed class FolderScannerServiceTests : IDisposable
{
    private readonly string _root;

    public FolderScannerServiceTests()
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
    public async Task ScanAsync_FindsSupportedFilesRecursively_AndIgnoresUnsupportedOnes()
    {
        File.WriteAllBytes(Path.Combine(_root, "top.jpg"), [1, 2, 3]);
        File.WriteAllBytes(Path.Combine(_root, "notes.txt"), [1]);

        var subFolder = Path.Combine(_root, "nested");
        Directory.CreateDirectory(subFolder);
        File.WriteAllBytes(Path.Combine(subFolder, "deep.png"), [1, 2]);

        var sut = new FolderScannerService(NullLogger<FolderScannerService>.Instance);

        var images = await sut.ScanAsync([_root]);

        Assert.Equal(2, images.Count);
        Assert.Contains(images, i => i.FileName == "top.jpg");
        Assert.Contains(images, i => i.FileName == "deep.png");
        Assert.DoesNotContain(images, i => i.FileName == "notes.txt");
    }

    [Fact]
    public async Task ScanAsync_ReportsCorrectFileSize()
    {
        var bytes = new byte[] { 1, 2, 3, 4, 5 };
        File.WriteAllBytes(Path.Combine(_root, "sized.png"), bytes);

        var sut = new FolderScannerService(NullLogger<FolderScannerService>.Instance);

        var images = await sut.ScanAsync([_root]);

        Assert.Equal(bytes.Length, images.Single().SizeInBytes);
    }

    [Fact]
    public async Task ScanAsync_WhenFolderDoesNotExist_ReturnsEmptyWithoutThrowing()
    {
        var sut = new FolderScannerService(NullLogger<FolderScannerService>.Instance);

        var images = await sut.ScanAsync([Path.Combine(_root, "does-not-exist")]);

        Assert.Empty(images);
    }

    /// <summary>
    /// Regression test (Roadmap Phase 16: Zugriffsfehler): one folder becoming inaccessible
    /// (permission change, external drive removed mid-scan) must not lose the images already
    /// found in the other, unrelated source folders selected in the same batch.
    /// </summary>
    [Fact]
    public async Task ScanAsync_WhenOneFolderIsInaccessible_StillReturnsResultsFromOtherFolders()
    {
        if (OperatingSystem.IsWindows())
        {
            return; // POSIX permission bits aren't a reliable way to force access-denied on Windows.
        }

        var accessibleFolder = Path.Combine(_root, "accessible");
        var lockedFolder = Path.Combine(_root, "locked");
        Directory.CreateDirectory(accessibleFolder);
        Directory.CreateDirectory(lockedFolder);
        File.WriteAllBytes(Path.Combine(accessibleFolder, "a.jpg"), [1]);
        File.SetUnixFileMode(lockedFolder, UnixFileMode.None);

        try
        {
            var sut = new FolderScannerService(NullLogger<FolderScannerService>.Instance);

            var images = await sut.ScanAsync([accessibleFolder, lockedFolder]);

            Assert.Single(images);
            Assert.Equal("a.jpg", images[0].FileName);
        }
        finally
        {
            // Restore permissions so the fixture's own cleanup (recursive delete) can succeed.
            File.SetUnixFileMode(lockedFolder, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }

    [Fact]
    public async Task ScanAsync_AcrossMultipleFolders_CombinesResults()
    {
        var folderA = Path.Combine(_root, "a");
        var folderB = Path.Combine(_root, "b");
        Directory.CreateDirectory(folderA);
        Directory.CreateDirectory(folderB);
        File.WriteAllBytes(Path.Combine(folderA, "a.jpg"), [1]);
        File.WriteAllBytes(Path.Combine(folderB, "b.jpg"), [1]);

        var sut = new FolderScannerService(NullLogger<FolderScannerService>.Instance);

        var images = await sut.ScanAsync([folderA, folderB]);

        Assert.Equal(2, images.Count);
    }
}
