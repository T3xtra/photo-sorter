namespace PhotoSorter.Core.Services.Abstractions;

/// <summary>
/// Resolves file system locations used by the application for persisted state
/// (settings, logs, cache). Centralizing this behind an interface keeps
/// per-OS path conventions in one place and makes storage-dependent services testable.
/// </summary>
public interface IAppPathProvider
{
    /// <summary>Full path to the user settings JSON file.</summary>
    string SettingsFilePath { get; }

    /// <summary>Directory where log files are written.</summary>
    string LogDirectory { get; }

    /// <summary>Directory used for cached data (e.g. thumbnails).</summary>
    string CacheDirectory { get; }

    /// <summary>
    /// Full path to the automatic session save file ("Nach Absturz kann die Sitzung
    /// vollständig wiederhergestellt werden", SoftwareDesign.md). Always the same path -
    /// PhotoSorter has exactly one active session at a time.
    /// </summary>
    string AutoSaveFilePath { get; }
}
