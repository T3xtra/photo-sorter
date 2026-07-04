using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using PhotoSorter.Core.Services.Abstractions;

namespace PhotoSorter.App.Services;

/// <inheritdoc cref="IFolderPickerService"/>
public sealed class AvaloniaFolderPickerService : IFolderPickerService
{
    public async Task<IReadOnlyList<string>> PickFoldersAsync(string title, string? suggestedStartLocation = null, CancellationToken cancellationToken = default)
    {
        var storageProvider = GetTopLevel()?.StorageProvider;
        if (storageProvider is null)
        {
            return [];
        }

        var folders = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = true,
            SuggestedStartLocation = await ResolveStartLocationAsync(storageProvider, suggestedStartLocation),
        });

        return folders
            .Select(folder => folder.TryGetLocalPath())
            .Where(path => path is not null)
            .Select(path => path!)
            .ToList();
    }

    public async Task<string?> PickFolderAsync(string title, string? suggestedStartLocation = null, CancellationToken cancellationToken = default)
    {
        var storageProvider = GetTopLevel()?.StorageProvider;
        if (storageProvider is null)
        {
            return null;
        }

        var folders = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            SuggestedStartLocation = await ResolveStartLocationAsync(storageProvider, suggestedStartLocation),
        });

        return folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
    }

    /// <summary>Resolves a remembered path ("Ordner merken", Roadmap Phase 15) to a storage folder, if it still exists.</summary>
    private static async Task<IStorageFolder?> ResolveStartLocationAsync(IStorageProvider storageProvider, string? suggestedStartLocation)
    {
        if (suggestedStartLocation is null || !Directory.Exists(suggestedStartLocation))
        {
            return null;
        }

        return await storageProvider.TryGetFolderFromPathAsync(suggestedStartLocation);
    }

    private static TopLevel? GetTopLevel() =>
        Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? TopLevel.GetTopLevel(desktop.MainWindow)
            : null;
}
