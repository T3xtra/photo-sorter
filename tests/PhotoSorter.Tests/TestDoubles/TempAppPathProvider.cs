using System;
using System.IO;
using PhotoSorter.Core.Services.Abstractions;

namespace PhotoSorter.Tests.TestDoubles;

/// <summary>
/// An <see cref="IAppPathProvider"/> rooted in a fresh temp directory, so tests never touch
/// the real user AppData location. Implements <see cref="IDisposable"/> to clean up after itself.
/// </summary>
public sealed class TempAppPathProvider : IAppPathProvider, IDisposable
{
    private readonly string _root;

    public TempAppPathProvider()
    {
        _root = Path.Combine(Path.GetTempPath(), "PhotoSorterTests", Guid.NewGuid().ToString("N"));
        SettingsFilePath = Path.Combine(_root, "settings.json");
        LogDirectory = Path.Combine(_root, "logs");
        CacheDirectory = Path.Combine(_root, "cache");
        AutoSaveFilePath = Path.Combine(_root, "autosave.photosort");

        Directory.CreateDirectory(_root);
        Directory.CreateDirectory(LogDirectory);
        Directory.CreateDirectory(CacheDirectory);
    }

    public string SettingsFilePath { get; }

    public string LogDirectory { get; }

    public string CacheDirectory { get; }

    public string AutoSaveFilePath { get; }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
