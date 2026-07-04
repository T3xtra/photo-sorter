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

    public AvaloniaSettingsDialogService(ISettingsService settingsService, IHotkeyService hotkeyService)
    {
        _settingsService = settingsService;
        _hotkeyService = hotkeyService;
    }

    public async Task ShowAsync(CancellationToken cancellationToken = default)
    {
        var owner = GetMainWindow();
        if (owner is null)
        {
            return;
        }

        var viewModel = new SettingsViewModel(_settingsService, _hotkeyService);
        var window = new SettingsWindow(viewModel);

        await window.ShowDialog(owner);
    }

    private static Window? GetMainWindow() =>
        Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;
}
