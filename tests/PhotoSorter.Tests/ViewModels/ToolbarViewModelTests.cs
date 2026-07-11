using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using PhotoSorter.Core.Models;
using PhotoSorter.Core.Services.Implementations;
using PhotoSorter.Core.ViewModels;
using PhotoSorter.Tests.TestDoubles;

namespace PhotoSorter.Tests.ViewModels;

public sealed class ToolbarViewModelTests
{
    private static ToolbarViewModel CreateSut(
        FakeFolderPickerService? picker = null,
        FakeProjectService? projectService = null,
        FakeFileMoveService? fileMoveService = null,
        FakeApplyConfirmationDialogService? confirmationDialog = null,
        FakeMessageDialogService? messageDialog = null,
        FakeSettingsDialogService? settingsDialog = null,
        FakeSettingsService? settingsService = null,
        HotkeyService? hotkeyService = null) => new(
            picker ?? new FakeFolderPickerService(),
            projectService ?? new FakeProjectService(),
            fileMoveService ?? new FakeFileMoveService(),
            confirmationDialog ?? new FakeApplyConfirmationDialogService(),
            messageDialog ?? new FakeMessageDialogService(),
            settingsDialog ?? new FakeSettingsDialogService(),
            settingsService ?? new FakeSettingsService(),
            hotkeyService ?? new HotkeyService(new FakeSettingsService()),
            NullLogger<ToolbarViewModel>.Instance);

    [Fact]
    public async Task SelectSourceFolderCommand_WhenFoldersPicked_LoadsProject()
    {
        var picker = new FakeFolderPickerService { Selection = ["/photos"] };
        var projectService = new FakeProjectService();
        var sut = CreateSut(picker, projectService);

        await sut.SelectSourceFolderCommand.ExecuteAsync(null);

        Assert.Equal(1, projectService.LoadCallCount);
        Assert.Equal(["/photos"], projectService.LastRequestedFolders);
    }

    [Fact]
    public async Task SelectSourceFolderCommand_WhenSelectionCancelled_DoesNotLoadProject()
    {
        var picker = new FakeFolderPickerService { Selection = [] };
        var projectService = new FakeProjectService();
        var sut = CreateSut(picker, projectService);

        await sut.SelectSourceFolderCommand.ExecuteAsync(null);

        Assert.Equal(0, projectService.LoadCallCount);
    }

    [Fact]
    public async Task SelectSourceFolderCommand_ResetsIsLoadingImages_AfterCompletion()
    {
        var picker = new FakeFolderPickerService { Selection = ["/photos"] };
        var sut = CreateSut(picker);

        await sut.SelectSourceFolderCommand.ExecuteAsync(null);

        Assert.False(sut.IsLoadingImages);
        Assert.True(sut.CanSelectSourceFolder);
    }

    [Fact]
    public async Task SelectSourceFolderCommand_WhenFoldersPicked_RemembersThemInSettings()
    {
        var picker = new FakeFolderPickerService { Selection = ["/photos"] };
        var settings = new FakeSettingsService();
        var sut = CreateSut(picker, settingsService: settings);

        await sut.SelectSourceFolderCommand.ExecuteAsync(null);

        Assert.Equal(["/photos"], settings.Current.LastSourceFolders);
    }

    [Fact]
    public async Task SelectSourceFolderCommand_UsesLastRememberedFolderAsPickerStartLocation()
    {
        var picker = new FakeFolderPickerService { Selection = ["/photos"] };
        var settings = new FakeSettingsService();
        settings.Current.LastSourceFolders = ["/previous"];
        var sut = CreateSut(picker, settingsService: settings);

        await sut.SelectSourceFolderCommand.ExecuteAsync(null);

        Assert.Equal("/previous", picker.LastSuggestedStartLocation);
    }

    [Fact]
    public void Constructor_RestoresRememberedTrashLeftTarget()
    {
        var settings = new FakeSettingsService();
        settings.Current.LastLeftTargetIsTrash = true;
        var projectService = new FakeProjectService();

        CreateSut(projectService: projectService, settingsService: settings);

        Assert.True(projectService.Current.LeftTarget!.IsTrash);
    }

    [Fact]
    public void Constructor_RestoresRememberedRightTarget_WhenFolderStillExists()
    {
        var existingFolder = System.IO.Path.GetTempPath();
        var settings = new FakeSettingsService();
        settings.Current.LastRightTargetPath = existingFolder;
        var projectService = new FakeProjectService();

        CreateSut(projectService: projectService, settingsService: settings);

        Assert.Equal(existingFolder, projectService.Current.RightTarget!.Path);
    }

    [Fact]
    public void Constructor_IgnoresRememberedTarget_WhenFolderNoLongerExists()
    {
        var settings = new FakeSettingsService();
        settings.Current.LastRightTargetPath = "/this/folder/does/not/exist/at/all";
        var projectService = new FakeProjectService();

        CreateSut(projectService: projectService, settingsService: settings);

        Assert.Null(projectService.Current.RightTarget);
    }

    [Fact]
    public async Task OpenSettingsCommand_ShowsSettingsDialog()
    {
        var settingsDialog = new FakeSettingsDialogService();
        var sut = CreateSut(settingsDialog: settingsDialog);

        await sut.OpenSettingsCommand.ExecuteAsync(null);

        Assert.Equal(1, settingsDialog.ShowCallCount);
    }

    [Fact]
    public async Task ShowHelpCommand_ShowsCurrentHotkeyBindings()
    {
        var hotkeyService = new HotkeyService(new FakeSettingsService());
        hotkeyService.SetBinding(HotkeyAction.NextImage, new HotkeyChord("PageDown"));
        var messageDialog = new FakeMessageDialogService();
        var sut = CreateSut(messageDialog: messageDialog, hotkeyService: hotkeyService);

        await sut.ShowHelpCommand.ExecuteAsync(null);

        Assert.Single(messageDialog.ShownMessages);
        var (title, message) = messageDialog.ShownMessages[0];
        Assert.Equal("Hilfe", title);
        Assert.Contains("Nach links sortieren: Left", message);
        // Reflects the actual current binding, including a rebind made through Settings (Phase 15).
        Assert.Contains("Nächstes Bild: PageDown", message);
        Assert.Contains("Zoom zurücksetzen: Ctrl+D0", message);
    }

    private static ImageFile MakeImage(string name) => new() { FullPath = $"/{name}", FileName = name, SizeInBytes = 1 };

    [Fact]
    public void CanApply_IsFalse_UntilADecisionExists()
    {
        var projectService = new FakeProjectService();
        var sut = CreateSut(projectService: projectService);

        Assert.False(sut.CanApply);

        projectService.SetImages([MakeImage("a.jpg")]);
        projectService.RecordDecision(SortAction.Left);

        Assert.True(sut.CanApply);
    }

    [Fact]
    public async Task ApplyCommand_WhenCancelled_DoesNotCallFileMoveService()
    {
        var projectService = new FakeProjectService();
        projectService.SetImages([MakeImage("a.jpg")]);
        projectService.RecordDecision(SortAction.Left);
        var fileMoveService = new FakeFileMoveService();
        var confirmationDialog = new FakeApplyConfirmationDialogService { ConfirmResult = false };
        var sut = CreateSut(projectService: projectService, fileMoveService: fileMoveService, confirmationDialog: confirmationDialog);

        await sut.ApplyCommand.ExecuteAsync(null);

        Assert.Equal(0, fileMoveService.CallCount);
    }

    [Fact]
    public async Task ApplyCommand_WhenConfirmed_CallsFileMoveServiceWithCurrentDecisionsAndTargets()
    {
        var projectService = new FakeProjectService();
        projectService.SetImages([MakeImage("a.jpg")]);
        projectService.SetLeftTarget(TargetFolder.Trash());
        projectService.RecordDecision(SortAction.Left);
        var fileMoveService = new FakeFileMoveService();
        var sut = CreateSut(projectService: projectService, fileMoveService: fileMoveService);

        await sut.ApplyCommand.ExecuteAsync(null);

        Assert.Equal(1, fileMoveService.CallCount);
        Assert.Single(fileMoveService.LastDecisions!);
        Assert.True(fileMoveService.LastLeftTarget!.IsTrash);
    }

    [Fact]
    public async Task ApplyCommand_RemovesSucceededImagesFromProject()
    {
        var image = MakeImage("a.jpg");
        var projectService = new FakeProjectService();
        projectService.SetImages([image]);
        projectService.RecordDecision(SortAction.Left);
        var fileMoveService = new FakeFileMoveService
        {
            Result = new FileMoveSummary { SucceededImages = [image] },
        };
        var sut = CreateSut(projectService: projectService, fileMoveService: fileMoveService);

        await sut.ApplyCommand.ExecuteAsync(null);

        Assert.Empty(projectService.Current.Images);
    }

    [Fact]
    public async Task ApplyCommand_WhenErrorsOccur_ShowsErrorMessage()
    {
        var image = MakeImage("a.jpg");
        var projectService = new FakeProjectService();
        projectService.SetImages([image]);
        projectService.RecordDecision(SortAction.Left);
        var fileMoveService = new FakeFileMoveService
        {
            Result = new FileMoveSummary { Errors = [new FileMoveError(image, "Zugriff verweigert")] },
        };
        var messageDialog = new FakeMessageDialogService();
        var sut = CreateSut(projectService: projectService, fileMoveService: fileMoveService, messageDialog: messageDialog);

        await sut.ApplyCommand.ExecuteAsync(null);

        Assert.Single(messageDialog.ShownMessages);
        Assert.Contains("Fehler", messageDialog.ShownMessages[0].Title);
    }
}
