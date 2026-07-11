using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using PhotoSorter.App.Views.Dialogs;
using PhotoSorter.Core.Services.Abstractions;
using PhotoSorter.Core.ViewModels;

namespace PhotoSorter.App.Services;

/// <inheritdoc cref="ISettingsDialogService"/>
public sealed class AvaloniaSettingsDialogService : ISettingsDialogService
{
    private readonly ISettingsService _settingsService;
    private readonly IHotkeyService _hotkeyService;
    private readonly IFolderPickerService _folderPicker;
    private readonly IProjectService _projectService;

    public AvaloniaSettingsDialogService(
        ISettingsService settingsService,
        IHotkeyService hotkeyService,
        IFolderPickerService folderPicker,
        IProjectService projectService)
    {
        _settingsService = settingsService;
        _hotkeyService = hotkeyService;
        _folderPicker = folderPicker;
        _projectService = projectService;
    }

    public async Task ShowAsync(CancellationToken cancellationToken = default)
    {
        var owner = GetMainWindow();
        if (owner is null)
        {
            return;
        }

        var targetFolders = new TargetFoldersViewModel(_folderPicker, _projectService, _settingsService);
        var viewModel = new SettingsViewModel(_settingsService, _hotkeyService, targetFolders);
        var window = new SettingsWindow(viewModel);

        await window.ShowDialog(owner);
    }

    private static Window? GetMainWindow() =>
        Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;
}
