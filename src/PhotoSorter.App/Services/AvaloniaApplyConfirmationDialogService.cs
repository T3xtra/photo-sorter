using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using PhotoSorter.App.Views.Dialogs;
using PhotoSorter.Core.Models;
using PhotoSorter.Core.Services.Abstractions;
using PhotoSorter.Core.ViewModels;

namespace PhotoSorter.App.Services;

/// <inheritdoc cref="IApplyConfirmationDialogService"/>
public sealed class AvaloniaApplyConfirmationDialogService : IApplyConfirmationDialogService
{
    public async Task<bool> ConfirmAsync(ApplySummary summary, CancellationToken cancellationToken = default)
    {
        var owner = GetMainWindow();
        if (owner is null)
        {
            return false;
        }

        var viewModel = new ApplyConfirmationViewModel(summary.LeftCount, summary.RightCount, summary.SkippedCount);
        var window = new ApplyConfirmationWindow(viewModel);

        var result = await window.ShowDialog<bool?>(owner);
        return result ?? false;
    }

    private static Window? GetMainWindow() =>
        Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;
}
