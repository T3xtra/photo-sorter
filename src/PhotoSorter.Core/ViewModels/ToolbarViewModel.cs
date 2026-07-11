using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PhotoSorter.Core.Models;
using PhotoSorter.Core.Services.Abstractions;

namespace PhotoSorter.Core.ViewModels;

/// <summary>
/// ViewModel for the toolbar (source folder, apply, help, settings). Left/right target
/// selection lives in Settings (<see cref="TargetFoldersViewModel"/>) instead of here.
/// </summary>
public sealed partial class ToolbarViewModel : ViewModelBase
{
    private readonly IFolderPickerService _folderPicker;
    private readonly IProjectService _projectService;
    private readonly IFileMoveService _fileMoveService;
    private readonly IApplyConfirmationDialogService _confirmationDialog;
    private readonly IMessageDialogService _messageDialog;
    private readonly ISettingsDialogService _settingsDialog;
    private readonly ISettingsService _settingsService;
    private readonly IHotkeyService _hotkeyService;
    private readonly ILogger<ToolbarViewModel> _logger;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSelectSourceFolder))]
    [NotifyCanExecuteChangedFor(nameof(SelectSourceFolderCommand))]
    private bool _isLoadingImages;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
    private bool _isApplying;

    public ToolbarViewModel(
        IFolderPickerService folderPicker,
        IProjectService projectService,
        IFileMoveService fileMoveService,
        IApplyConfirmationDialogService confirmationDialog,
        IMessageDialogService messageDialog,
        ISettingsDialogService settingsDialog,
        ISettingsService settingsService,
        IHotkeyService hotkeyService,
        ILogger<ToolbarViewModel> logger)
    {
        _folderPicker = folderPicker;
        _projectService = projectService;
        _fileMoveService = fileMoveService;
        _confirmationDialog = confirmationDialog;
        _messageDialog = messageDialog;
        _settingsDialog = settingsDialog;
        _settingsService = settingsService;
        _hotkeyService = hotkeyService;
        _logger = logger;

        _projectService.DecisionsChanged += (_, _) => ApplyCommand.NotifyCanExecuteChanged();
        RestoreRememberedTargets();
    }

    /// <summary>
    /// "Ordner merken" (Roadmap Phase 15): unlike the source folder (a different photo set each
    /// session, so it's only offered as the folder picker's starting point), left/right targets
    /// tend to be fixed "sort into" folders reused across sessions, so they're restored as the
    /// actual project targets on startup - provided the folder still exists. Left/right selection
    /// UI has since moved to Settings (<see cref="TargetFoldersViewModel"/>), but this restore
    /// stays here since <see cref="ToolbarViewModel"/> is what's guaranteed constructed at app
    /// startup (Settings is only built when the user opens it).
    /// </summary>
    private void RestoreRememberedTargets()
    {
        var settings = _settingsService.Current;

        if (settings.LastLeftTargetIsTrash)
        {
            _projectService.SetLeftTarget(TargetFolder.Trash());
        }
        else if (settings.LastLeftTargetPath is { } leftPath && Directory.Exists(leftPath))
        {
            _projectService.SetLeftTarget(TargetFolder.At(leftPath));
        }

        if (settings.LastRightTargetPath is { } rightPath && Directory.Exists(rightPath))
        {
            _projectService.SetRightTarget(TargetFolder.At(rightPath));
        }
    }

    public bool CanSelectSourceFolder => !IsLoadingImages;

    public bool CanApply => !IsApplying && _projectService.Current.Decisions.Count > 0;

    [RelayCommand(CanExecute = nameof(CanSelectSourceFolder))]
    private async Task SelectSourceFolderAsync()
    {
        var lastSourceFolder = _settingsService.Current.LastSourceFolders.FirstOrDefault();
        var folders = await _folderPicker.PickFoldersAsync("Quellordner auswählen", lastSourceFolder);
        if (folders.Count == 0)
        {
            return;
        }

        IsLoadingImages = true;
        try
        {
            await _projectService.LoadFromFoldersAsync(folders);
            _settingsService.Current.LastSourceFolders = folders.ToList();
            _ = _settingsService.SaveAsync();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogError(ex, "Failed to load images from selected folders.");
        }
        finally
        {
            IsLoadingImages = false;
        }
    }

    [RelayCommand]
    private Task OpenSettingsAsync() => _settingsDialog.ShowAsync();

    /// <summary>"Hilfe": current keyboard shortcuts (Roadmap Phase 17 - shown, not hardcoded, since Phase 15 made them rebindable).</summary>
    [RelayCommand]
    private Task ShowHelpAsync() => _messageDialog.ShowAsync("Hilfe", BuildShortcutOverview());

    private string BuildShortcutOverview()
    {
        var lines = new List<string> { "Tastenkürzel:", string.Empty };
        lines.AddRange(HotkeyActionDisplayNames.Ordered.Select(entry =>
        {
            var chord = _hotkeyService.Bindings.TryGetValue(entry.Action, out var bound) ? bound.ToString() : "-";
            return $"{entry.DisplayName}: {chord}";
        }));
        lines.Add(string.Empty);
        lines.Add("Strg+Z: Rückgängig (zusätzlich zur oben gezeigten Taste)");
        lines.Add(string.Empty);
        lines.Add("Tastenkürzel lassen sich unter \"Einstellungen\" anpassen.");
        return string.Join('\n', lines);
    }

    /// <summary>
    /// "Nach Bestätigung: alle Verschiebeoperationen durchführen" (SoftwareDesign.md). Any images
    /// still undecided at this point count as "Übersprungen" in the confirmation and are simply
    /// left in place - the session continues with just those remaining.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanApply))]
    private async Task ApplyAsync()
    {
        var project = _projectService.Current;
        var summary = new ApplySummary(project.LeftCount, project.RightCount, project.OpenCount);

        if (!await _confirmationDialog.ConfirmAsync(summary))
        {
            return;
        }

        IsApplying = true;
        try
        {
            var result = await _fileMoveService.ApplyAsync(project.Decisions.ToList(), project.LeftTarget, project.RightTarget);
            _projectService.RemoveAppliedImages(result.SucceededImages);

            if (result.Errors.Count > 0)
            {
                var details = string.Join('\n', result.Errors.Select(e => $"{e.Image.FileName}: {e.Message}"));
                await _messageDialog.ShowAsync(
                    "Fehler beim Verschieben",
                    $"{result.SucceededImages.Count} Datei(en) erfolgreich verschoben.\n" +
                    $"{result.Errors.Count} Datei(en) fehlgeschlagen:\n{details}");
            }
            else
            {
                await _messageDialog.ShowAsync("Fertig", $"{result.SucceededImages.Count} Datei(en) erfolgreich verschoben.");
            }
        }
        finally
        {
            IsApplying = false;
        }
    }
}
