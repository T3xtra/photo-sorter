using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhotoSorter.Core.Models;
using PhotoSorter.Core.Services.Abstractions;

namespace PhotoSorter.Core.ViewModels;

/// <summary>
/// ViewModel for the "Zielordner" settings tab: choosing the left (optionally trash) and right
/// sort targets. Moved out of the toolbar (Roadmap Phase 17 follow-up UI cleanup) into
/// Settings, since picking a target folder is an occasional setup action, not something that
/// needs to stay one click away during active sorting. Constructed fresh each time the Settings
/// dialog opens (see <see cref="ISettingsDialogService"/>), consistent with
/// <see cref="SettingsViewModel"/>/<see cref="HotkeyBindingViewModel"/> - its displayed state is
/// always driven reactively from <see cref="IProjectService"/>, so freshness only matters for
/// consistency with those siblings, not correctness.
/// </summary>
public sealed partial class TargetFoldersViewModel : ViewModelBase
{
    private readonly IFolderPickerService _folderPicker;
    private readonly IProjectService _projectService;
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    private string _leftTargetDisplay = "Nicht gewählt";

    [ObservableProperty]
    private string _rightTargetDisplay = "Nicht gewählt";

    public TargetFoldersViewModel(IFolderPickerService folderPicker, IProjectService projectService, ISettingsService settingsService)
    {
        _folderPicker = folderPicker;
        _projectService = projectService;
        _settingsService = settingsService;

        _projectService.TargetsChanged += (_, _) => UpdateTargetDisplays();
        _projectService.ProjectChanged += (_, _) => UpdateTargetDisplays();
        UpdateTargetDisplays();
    }

    /// <summary>The left target may be the trash (SoftwareDesign.md), so it has no folder dialog on its own.</summary>
    [RelayCommand]
    private void SelectLeftTargetTrash()
    {
        _projectService.SetLeftTarget(TargetFolder.Trash());
        _settingsService.Current.LastLeftTargetIsTrash = true;
        _settingsService.Current.LastLeftTargetPath = null;
        _ = _settingsService.SaveAsync();
    }

    [RelayCommand]
    private async Task SelectLeftTargetFolderAsync()
    {
        var path = await _folderPicker.PickFolderAsync("Linken Zielordner auswählen", _settingsService.Current.LastLeftTargetPath);
        if (path is not null)
        {
            _projectService.SetLeftTarget(TargetFolder.At(path));
            _settingsService.Current.LastLeftTargetIsTrash = false;
            _settingsService.Current.LastLeftTargetPath = path;
            _ = _settingsService.SaveAsync();
        }
    }

    [RelayCommand]
    private async Task SelectRightTargetAsync()
    {
        var path = await _folderPicker.PickFolderAsync("Rechten Zielordner auswählen", _settingsService.Current.LastRightTargetPath);
        if (path is not null)
        {
            _projectService.SetRightTarget(TargetFolder.At(path));
            _settingsService.Current.LastRightTargetPath = path;
            _ = _settingsService.SaveAsync();
        }
    }

    private void UpdateTargetDisplays()
    {
        LeftTargetDisplay = Describe(_projectService.Current.LeftTarget);
        RightTargetDisplay = Describe(_projectService.Current.RightTarget);
    }

    private static string Describe(TargetFolder? target) => target switch
    {
        null => "Nicht gewählt",
        { IsTrash: true } => "Papierkorb",
        _ => target.Path!,
    };
}
