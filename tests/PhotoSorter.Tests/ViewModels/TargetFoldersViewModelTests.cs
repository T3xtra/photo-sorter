using System.Threading.Tasks;
using PhotoSorter.Core.Models;
using PhotoSorter.Core.ViewModels;
using PhotoSorter.Tests.TestDoubles;

namespace PhotoSorter.Tests.ViewModels;

public sealed class TargetFoldersViewModelTests
{
    private static TargetFoldersViewModel CreateSut(
        FakeFolderPickerService? picker = null,
        FakeProjectService? projectService = null,
        FakeSettingsService? settingsService = null) => new(
            picker ?? new FakeFolderPickerService(),
            projectService ?? new FakeProjectService(),
            settingsService ?? new FakeSettingsService());

    [Fact]
    public void SelectLeftTargetTrashCommand_SetsLeftTargetToTrash()
    {
        var projectService = new FakeProjectService();
        var sut = CreateSut(projectService: projectService);

        sut.SelectLeftTargetTrashCommand.Execute(null);

        Assert.True(projectService.Current.LeftTarget!.IsTrash);
        Assert.Equal("Papierkorb", sut.LeftTargetDisplay);
    }

    [Fact]
    public async Task SelectLeftTargetFolderCommand_WhenFolderPicked_SetsLeftTargetToThatFolder()
    {
        var picker = new FakeFolderPickerService { SingleSelection = "/trash-alternative" };
        var projectService = new FakeProjectService();
        var sut = CreateSut(picker, projectService);

        await sut.SelectLeftTargetFolderCommand.ExecuteAsync(null);

        Assert.False(projectService.Current.LeftTarget!.IsTrash);
        Assert.Equal("/trash-alternative", sut.LeftTargetDisplay);
    }

    [Fact]
    public async Task SelectRightTargetCommand_WhenCancelled_LeavesTargetUnset()
    {
        var picker = new FakeFolderPickerService { SingleSelection = null };
        var projectService = new FakeProjectService();
        var sut = CreateSut(picker, projectService);

        await sut.SelectRightTargetCommand.ExecuteAsync(null);

        Assert.Null(projectService.Current.RightTarget);
        Assert.Equal("Nicht gewählt", sut.RightTargetDisplay);
    }

    [Fact]
    public async Task SelectRightTargetCommand_WhenFolderPicked_UpdatesDisplay()
    {
        var picker = new FakeFolderPickerService { SingleSelection = "/export" };
        var sut = CreateSut(picker);

        await sut.SelectRightTargetCommand.ExecuteAsync(null);

        Assert.Equal("/export", sut.RightTargetDisplay);
    }

    [Fact]
    public void SelectLeftTargetTrashCommand_RemembersTrashInSettings()
    {
        var settings = new FakeSettingsService();
        settings.Current.LastLeftTargetPath = "/old-folder";
        var sut = CreateSut(settingsService: settings);

        sut.SelectLeftTargetTrashCommand.Execute(null);

        Assert.True(settings.Current.LastLeftTargetIsTrash);
        Assert.Null(settings.Current.LastLeftTargetPath);
    }

    [Fact]
    public async Task SelectLeftTargetFolderCommand_WhenFolderPicked_RemembersItInSettings()
    {
        var picker = new FakeFolderPickerService { SingleSelection = "/trash-alternative" };
        var settings = new FakeSettingsService { Current = { LastLeftTargetIsTrash = true } };
        var sut = CreateSut(picker, settingsService: settings);

        await sut.SelectLeftTargetFolderCommand.ExecuteAsync(null);

        Assert.False(settings.Current.LastLeftTargetIsTrash);
        Assert.Equal("/trash-alternative", settings.Current.LastLeftTargetPath);
    }

    [Fact]
    public async Task SelectRightTargetCommand_WhenFolderPicked_RemembersItInSettings()
    {
        var picker = new FakeFolderPickerService { SingleSelection = "/export" };
        var settings = new FakeSettingsService();
        var sut = CreateSut(picker, settingsService: settings);

        await sut.SelectRightTargetCommand.ExecuteAsync(null);

        Assert.Equal("/export", settings.Current.LastRightTargetPath);
    }

    [Fact]
    public void Constructor_ReflectsAlreadySetProjectTargets()
    {
        var projectService = new FakeProjectService();
        projectService.SetLeftTarget(TargetFolder.Trash());
        projectService.SetRightTarget(TargetFolder.At("/export"));

        var sut = CreateSut(projectService: projectService);

        Assert.Equal("Papierkorb", sut.LeftTargetDisplay);
        Assert.Equal("/export", sut.RightTargetDisplay);
    }
}
