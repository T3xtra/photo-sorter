using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using PhotoSorter.App.Views.Dialogs;
using PhotoSorter.Core.Services.Abstractions;
using PhotoSorter.Core.ViewModels;

namespace PhotoSorter.App.Services;

/// <inheritdoc cref="IMessageDialogService"/>
public sealed class AvaloniaMessageDialogService : IMessageDialogService
{
    public async Task ShowAsync(string title, string message, CancellationToken cancellationToken = default)
    {
        var owner = GetMainWindow();
        if (owner is null)
        {
            return;
        }

        var viewModel = new MessageDialogViewModel(title, message);
        var window = new MessageDialogWindow(viewModel);

        await window.ShowDialog(owner);
    }

    private static Window? GetMainWindow() =>
        Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;
}
