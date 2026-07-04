using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using PhotoSorter.Core.Models;
using PhotoSorter.Core.Services.Implementations;
using PhotoSorter.Tests.TestDoubles;

namespace PhotoSorter.Tests.Services;

public sealed class FileMoveServiceTests : IDisposable
{
    private readonly string _root;

    public FileMoveServiceTests()
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

    private ImageFile CreateSourceFile(string name)
    {
        var path = Path.Combine(_root, name);
        File.WriteAllBytes(path, [1, 2, 3]);
        return new ImageFile { FullPath = path, FileName = name, SizeInBytes = 3 };
    }

    [Fact]
    public async Task ApplyAsync_MovesFileToRealFolderTarget()
    {
        var image = CreateSourceFile("a.jpg");
        var targetDir = Path.Combine(_root, "target");
        var sut = new FileMoveService(new FakeTrashService(), NullLogger<FileMoveService>.Instance);

        var result = await sut.ApplyAsync([new SortDecision(image, SortAction.Right)], null, TargetFolder.At(targetDir));

        Assert.Single(result.SucceededImages);
        Assert.Empty(result.Errors);
        Assert.True(File.Exists(Path.Combine(targetDir, "a.jpg")));
        Assert.False(File.Exists(image.FullPath));
    }

    [Fact]
    public async Task ApplyAsync_ForLeftTrashTarget_CallsTrashService()
    {
        var image = CreateSourceFile("a.jpg");
        var trash = new FakeTrashService();
        var sut = new FileMoveService(trash, NullLogger<FileMoveService>.Instance);

        var result = await sut.ApplyAsync([new SortDecision(image, SortAction.Left)], TargetFolder.Trash(), null);

        Assert.Single(result.SucceededImages);
        Assert.Contains(image.FullPath, trash.TrashedPaths);
    }

    [Fact]
    public async Task ApplyAsync_WhenTargetNotConfigured_RecordsError_AndContinuesWithOthers()
    {
        var imageWithoutTarget = CreateSourceFile("a.jpg");
        var imageWithTarget = CreateSourceFile("b.jpg");
        var targetDir = Path.Combine(_root, "target");
        var sut = new FileMoveService(new FakeTrashService(), NullLogger<FileMoveService>.Instance);

        var result = await sut.ApplyAsync(
            [
                new SortDecision(imageWithoutTarget, SortAction.Left), // no left target configured
                new SortDecision(imageWithTarget, SortAction.Right),
            ],
            leftTarget: null,
            rightTarget: TargetFolder.At(targetDir));

        Assert.Single(result.Errors);
        Assert.Equal(imageWithoutTarget, result.Errors[0].Image);
        Assert.Single(result.SucceededImages);
        Assert.Equal(imageWithTarget, result.SucceededImages[0]);
    }

    [Fact]
    public async Task ApplyAsync_WhenSourceFileMissing_RecordsErrorWithoutThrowing()
    {
        var missingImage = new ImageFile { FullPath = Path.Combine(_root, "missing.jpg"), FileName = "missing.jpg", SizeInBytes = 1 };
        var sut = new FileMoveService(new FakeTrashService(), NullLogger<FileMoveService>.Instance);

        var result = await sut.ApplyAsync([new SortDecision(missingImage, SortAction.Right)], null, TargetFolder.At(_root));

        Assert.Single(result.Errors);
        Assert.Empty(result.SucceededImages);
    }

    [Fact]
    public async Task ApplyAsync_WhenDestinationFileAlreadyExists_RenamesWithSuffix()
    {
        var image = CreateSourceFile("a.jpg");
        var targetDir = Path.Combine(_root, "target");
        Directory.CreateDirectory(targetDir);
        File.WriteAllBytes(Path.Combine(targetDir, "a.jpg"), [9]);
        var sut = new FileMoveService(new FakeTrashService(), NullLogger<FileMoveService>.Instance);

        var result = await sut.ApplyAsync([new SortDecision(image, SortAction.Right)], null, TargetFolder.At(targetDir));

        Assert.Single(result.SucceededImages);
        Assert.True(File.Exists(Path.Combine(targetDir, "a (1).jpg")));
        // The pre-existing file at the original destination name must survive untouched.
        Assert.Equal(9, File.ReadAllBytes(Path.Combine(targetDir, "a.jpg"))[0]);
    }
}
