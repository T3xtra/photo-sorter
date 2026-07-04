using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using PhotoSorter.Core.Models;
using PhotoSorter.Core.Models.Persistence;
using PhotoSorter.Core.Services.Abstractions;
using PhotoSorter.Core.Services.Implementations;
using PhotoSorter.Tests.TestDoubles;

namespace PhotoSorter.Tests.Services;

public sealed class ProjectServiceTests : IDisposable
{
    private readonly List<TempAppPathProvider> _pathProviders = [];

    public void Dispose()
    {
        foreach (var provider in _pathProviders)
        {
            provider.Dispose();
        }
    }

    private ProjectService CreateSut(IFolderScannerService? scanner = null, IProjectFileSerializer? serializer = null)
    {
        var pathProvider = new TempAppPathProvider();
        _pathProviders.Add(pathProvider);
        return new ProjectService(
            scanner ?? new FakeFolderScannerService(),
            serializer ?? new FakeProjectFileSerializer(),
            pathProvider,
            NullLogger<ProjectService>.Instance);
    }

    [Fact]
    public async Task LoadFromFoldersAsync_PopulatesCurrentFromScanner()
    {
        var scanner = new FakeFolderScannerService
        {
            Result =
            [
                new ImageFile { FullPath = "/a.jpg", FileName = "a.jpg", SizeInBytes = 1 },
            ],
        };
        var sut = CreateSut(scanner);

        await sut.LoadFromFoldersAsync(["/photos"]);

        Assert.Single(sut.Current.Images);
        Assert.Equal(["/photos"], sut.Current.SourceFolders);
        Assert.Equal(["/photos"], scanner.LastRequestedFolders);
    }

    [Fact]
    public async Task LoadFromFoldersAsync_RaisesProjectChanged()
    {
        var sut = CreateSut();
        var raised = false;
        sut.ProjectChanged += (_, _) => raised = true;

        await sut.LoadFromFoldersAsync(["/photos"]);

        Assert.True(raised);
    }

    [Fact]
    public async Task LoadFromFoldersAsync_WithImages_SetsCurrentIndexToZero()
    {
        var scanner = new FakeFolderScannerService
        {
            Result = [new ImageFile { FullPath = "/a.jpg", FileName = "a.jpg", SizeInBytes = 1 }],
        };
        var sut = CreateSut(scanner);

        await sut.LoadFromFoldersAsync(["/photos"]);

        Assert.Equal(0, sut.CurrentIndex);
    }

    [Fact]
    public async Task LoadFromFoldersAsync_WithNoImages_SetsCurrentIndexToMinusOne()
    {
        var sut = CreateSut();

        await sut.LoadFromFoldersAsync(["/empty"]);

        Assert.Equal(-1, sut.CurrentIndex);
    }

    [Fact]
    public async Task MoveNext_AdvancesIndex_AndRaisesCurrentIndexChanged()
    {
        var scanner = new FakeFolderScannerService
        {
            Result =
            [
                new ImageFile { FullPath = "/a.jpg", FileName = "a.jpg", SizeInBytes = 1 },
                new ImageFile { FullPath = "/b.jpg", FileName = "b.jpg", SizeInBytes = 1 },
            ],
        };
        var sut = CreateSut(scanner);
        await sut.LoadFromFoldersAsync(["/photos"]);
        var raised = false;
        sut.CurrentIndexChanged += (_, _) => raised = true;

        var moved = sut.MoveNext();

        Assert.True(moved);
        Assert.True(raised);
        Assert.Equal(1, sut.CurrentIndex);
    }

    [Fact]
    public async Task MoveNext_AtLastImage_ReturnsFalse_AndDoesNotMove()
    {
        var scanner = new FakeFolderScannerService
        {
            Result = [new ImageFile { FullPath = "/a.jpg", FileName = "a.jpg", SizeInBytes = 1 }],
        };
        var sut = CreateSut(scanner);
        await sut.LoadFromFoldersAsync(["/photos"]);

        var moved = sut.MoveNext();

        Assert.False(moved);
        Assert.Equal(0, sut.CurrentIndex);
    }

    [Fact]
    public void MovePrevious_AtFirstImage_ReturnsFalse()
    {
        var sut = CreateSut();

        Assert.False(sut.MovePrevious());
    }

    private ProjectService CreateWithThreeImages()
    {
        var scanner = new FakeFolderScannerService
        {
            Result =
            [
                new ImageFile { FullPath = "/a.jpg", FileName = "a.jpg", SizeInBytes = 1 },
                new ImageFile { FullPath = "/b.jpg", FileName = "b.jpg", SizeInBytes = 1 },
                new ImageFile { FullPath = "/c.jpg", FileName = "c.jpg", SizeInBytes = 1 },
            ],
        };
        return CreateSut(scanner);
    }

    [Fact]
    public async Task RecordDecision_AddsDecision_AndAdvancesToNextOpenImage()
    {
        var sut = CreateWithThreeImages();
        await sut.LoadFromFoldersAsync(["/photos"]);

        sut.RecordDecision(SortAction.Left);

        Assert.Equal(1, sut.Current.LeftCount);
        Assert.Equal(1, sut.CurrentIndex);
    }

    [Fact]
    public async Task RecordDecision_RaisesDecisionsChangedAndCurrentIndexChanged()
    {
        var sut = CreateWithThreeImages();
        await sut.LoadFromFoldersAsync(["/photos"]);
        var decisionsChanged = false;
        var indexChanged = false;
        sut.DecisionsChanged += (_, _) => decisionsChanged = true;
        sut.CurrentIndexChanged += (_, _) => indexChanged = true;

        sut.RecordDecision(SortAction.Right);

        Assert.True(decisionsChanged);
        Assert.True(indexChanged);
    }

    [Fact]
    public async Task MoveNext_SkipsOverAlreadyDecidedImages()
    {
        var sut = CreateWithThreeImages();
        await sut.LoadFromFoldersAsync(["/photos"]);
        // Decide image 0 (advances to 1), then manually decide image 1 too via RecordDecision
        // so both 0 and 1 are decided, leaving only image 2 open.
        sut.RecordDecision(SortAction.Left);
        sut.RecordDecision(SortAction.Right);

        Assert.Equal(2, sut.CurrentIndex);
        Assert.False(sut.HasNextImage);
    }

    [Fact]
    public async Task RecordDecision_WhenLastImageDecided_SetsCurrentIndexToMinusOne()
    {
        var scanner = new FakeFolderScannerService
        {
            Result = [new ImageFile { FullPath = "/a.jpg", FileName = "a.jpg", SizeInBytes = 1 }],
        };
        var sut = CreateSut(scanner);
        await sut.LoadFromFoldersAsync(["/photos"]);

        sut.RecordDecision(SortAction.Left);

        Assert.Equal(-1, sut.CurrentIndex);
        Assert.Equal(0, sut.Current.OpenCount);
    }

    [Fact]
    public async Task RecordDecision_WhenNoCurrentImage_IsNoOp()
    {
        var sut = CreateSut();
        await sut.LoadFromFoldersAsync(["/empty"]);

        sut.RecordDecision(SortAction.Left);

        Assert.Equal(0, sut.Current.LeftCount);
    }

    [Fact]
    public async Task Undo_RemovesLastDecision_AndJumpsBackToThatImage()
    {
        var sut = CreateWithThreeImages();
        await sut.LoadFromFoldersAsync(["/photos"]);
        sut.RecordDecision(SortAction.Left); // decides a.jpg, moves to index 1 (b.jpg)

        var undone = sut.Undo();

        Assert.True(undone);
        Assert.Equal(0, sut.Current.LeftCount);
        Assert.Equal(3, sut.Current.OpenCount);
        Assert.Equal(0, sut.CurrentIndex);
    }

    [Fact]
    public async Task Undo_RaisesDecisionsChanged()
    {
        var sut = CreateWithThreeImages();
        await sut.LoadFromFoldersAsync(["/photos"]);
        sut.RecordDecision(SortAction.Left);
        var raised = false;
        sut.DecisionsChanged += (_, _) => raised = true;

        sut.Undo();

        Assert.True(raised);
    }

    [Fact]
    public void Undo_WhenNothingToUndo_ReturnsFalse()
    {
        var sut = CreateSut();

        Assert.False(sut.Undo());
    }

    [Fact]
    public async Task Undo_MultipleSteps_RestoresEachInReverseOrder()
    {
        var sut = CreateWithThreeImages();
        await sut.LoadFromFoldersAsync(["/photos"]);
        sut.RecordDecision(SortAction.Left); // a.jpg
        sut.RecordDecision(SortAction.Right); // b.jpg

        sut.Undo(); // undoes b.jpg
        Assert.Equal(1, sut.CurrentIndex);
        Assert.Equal(1, sut.Current.LeftCount);
        Assert.Equal(0, sut.Current.RightCount);

        sut.Undo(); // undoes a.jpg
        Assert.Equal(0, sut.CurrentIndex);
        Assert.Equal(0, sut.Current.LeftCount);
        Assert.Equal(3, sut.Current.OpenCount);
    }

    [Fact]
    public async Task Undo_AfterEverythingSorted_RestoresLastImage()
    {
        var scanner = new FakeFolderScannerService
        {
            Result = [new ImageFile { FullPath = "/a.jpg", FileName = "a.jpg", SizeInBytes = 1 }],
        };
        var sut = CreateSut(scanner);
        await sut.LoadFromFoldersAsync(["/photos"]);
        sut.RecordDecision(SortAction.Left);
        Assert.Equal(-1, sut.CurrentIndex);

        sut.Undo();

        Assert.Equal(0, sut.CurrentIndex);
        Assert.Equal(1, sut.Current.OpenCount);
    }

    [Fact]
    public void SetLeftTarget_ToTrash_UpdatesCurrentAndRaisesTargetsChanged()
    {
        var sut = CreateSut();
        var raised = false;
        sut.TargetsChanged += (_, _) => raised = true;

        sut.SetLeftTarget(TargetFolder.Trash());

        Assert.True(sut.Current.LeftTarget!.IsTrash);
        Assert.True(raised);
    }

    [Fact]
    public void SetRightTarget_ToFolder_UpdatesCurrent()
    {
        var sut = CreateSut();

        sut.SetRightTarget(TargetFolder.At("/export"));

        Assert.False(sut.Current.RightTarget!.IsTrash);
        Assert.Equal("/export", sut.Current.RightTarget.Path);
    }

    [Fact]
    public async Task SaveAsync_ThenOpenAsync_RestoresImagesTargetsDecisionsAndPosition()
    {
        // OpenAsync re-stats each image path and skips missing files, so this round trip needs
        // files that actually exist, unlike the other tests here which use fake paths.
        var tempRoot = Path.Combine(Path.GetTempPath(), "PhotoSorterTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        try
        {
            var pathA = Path.Combine(tempRoot, "a.jpg");
            var pathB = Path.Combine(tempRoot, "b.jpg");
            File.WriteAllBytes(pathA, [1]);
            File.WriteAllBytes(pathB, [1, 2]);

            var scanner = new FakeFolderScannerService
            {
                Result =
                [
                    new ImageFile { FullPath = pathA, FileName = "a.jpg", SizeInBytes = 1 },
                    new ImageFile { FullPath = pathB, FileName = "b.jpg", SizeInBytes = 2 },
                ],
            };
            var serializer = new FakeProjectFileSerializer();
            var sut = CreateSut(scanner, serializer);
            await sut.LoadFromFoldersAsync([tempRoot]);
            sut.SetLeftTarget(TargetFolder.Trash());
            sut.SetRightTarget(TargetFolder.At("/export"));
            sut.RecordDecision(SortAction.Left); // a.jpg decided, moves to b.jpg
            var filePath = Path.Combine(tempRoot, "session.photosort");
            await sut.SaveAsync(filePath);

            var reopened = CreateSut(serializer: serializer);
            var opened = await reopened.OpenAsync(filePath);

            Assert.True(opened);
            Assert.Equal(2, reopened.Current.Images.Count);
            Assert.True(reopened.Current.LeftTarget!.IsTrash);
            Assert.Equal("/export", reopened.Current.RightTarget!.Path);
            Assert.Equal(1, reopened.Current.LeftCount);
            Assert.Equal(1, reopened.CurrentIndex);
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task OpenAsync_WhenFileMissingFromDisk_SkipsItButRestoresRest()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "PhotoSorterTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        try
        {
            var pathA = Path.Combine(tempRoot, "a.jpg");
            File.WriteAllBytes(pathA, [1]);

            var serializer = new FakeProjectFileSerializer();
            var filePath = Path.Combine(tempRoot, "session.photosort");
            await serializer.SaveAsync(
                new ProjectFileDto
                {
                    SourceFolders = [tempRoot],
                    ImagePaths = [pathA, Path.Combine(tempRoot, "does-not-exist.jpg")],
                    CurrentIndex = 0,
                },
                filePath);

            var sut = CreateSut(serializer: serializer);
            var opened = await sut.OpenAsync(filePath);

            Assert.True(opened);
            Assert.Single(sut.Current.Images);
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task OpenAsync_WhenFileDoesNotExist_ReturnsFalse()
    {
        var sut = CreateSut();

        var opened = await sut.OpenAsync("/does-not-exist.photosort");

        Assert.False(opened);
    }

    [Fact]
    public async Task RemoveAppliedImages_RemovesFromProject_AndRaisesProjectChanged()
    {
        var sut = CreateWithThreeImages();
        await sut.LoadFromFoldersAsync(["/photos"]);
        sut.RecordDecision(SortAction.Left); // a.jpg decided
        var appliedImage = sut.Current.Images.Single(i => i.FileName == "a.jpg");
        var raised = false;
        sut.ProjectChanged += (_, _) => raised = true;

        sut.RemoveAppliedImages([appliedImage]);

        Assert.Equal(2, sut.Current.Images.Count);
        Assert.True(raised);
    }

    [Fact]
    public async Task RemoveAppliedImages_PreservesCurrentImage_EvenIfIndexShifts()
    {
        var sut = CreateWithThreeImages();
        await sut.LoadFromFoldersAsync(["/photos"]);
        var decidedImage = sut.Current.Images[0]; // a.jpg
        sut.RecordDecision(SortAction.Left); // CurrentIndex now points at b.jpg (index 1)
        var currentImageBeforeRemoval = sut.Current.Images[sut.CurrentIndex];

        sut.RemoveAppliedImages([decidedImage]);

        // b.jpg is now at index 0 after a.jpg was removed; CurrentIndex must follow it.
        Assert.Equal(currentImageBeforeRemoval, sut.Current.Images[sut.CurrentIndex]);
    }

    [Fact]
    public void RemoveAppliedImages_WithEmptyList_DoesNotRaiseProjectChanged()
    {
        var sut = CreateSut();
        var raised = false;
        sut.ProjectChanged += (_, _) => raised = true;

        sut.RemoveAppliedImages([]);

        Assert.False(raised);
    }
}
