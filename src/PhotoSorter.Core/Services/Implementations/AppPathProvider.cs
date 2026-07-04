using System;
using System.IO;
using PhotoSorter.Core.Services.Abstractions;

namespace PhotoSorter.Core.Services.Implementations;

/// <inheritdoc cref="IAppPathProvider"/>
public sealed class AppPathProvider : IAppPathProvider
{
    private const string AppFolderName = "PhotoSorter";

    public AppPathProvider()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            AppFolderName);

        SettingsFilePath = Path.Combine(root, "settings.json");
        LogDirectory = Path.Combine(root, "logs");
        CacheDirectory = Path.Combine(root, "cache");
        AutoSaveFilePath = Path.Combine(root, "autosave.photosort");

        Directory.CreateDirectory(root);
        Directory.CreateDirectory(LogDirectory);
        Directory.CreateDirectory(CacheDirectory);
    }

    public string SettingsFilePath { get; }

    public string LogDirectory { get; }

    public string CacheDirectory { get; }

    public string AutoSaveFilePath { get; }
}
