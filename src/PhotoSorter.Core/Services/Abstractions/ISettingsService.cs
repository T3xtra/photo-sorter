using System;
using System.Threading;
using System.Threading.Tasks;
using PhotoSorter.Core.Configuration;

namespace PhotoSorter.Core.Services.Abstractions;

/// <summary>
/// Loads and persists the user's <see cref="AppSettings"/>.
/// </summary>
public interface ISettingsService
{
    /// <summary>The currently loaded settings. Defaults are used until <see cref="LoadAsync"/> completes.</summary>
    AppSettings Current { get; }

    /// <summary>Raised after <see cref="SaveAsync"/> persists a change, so already-constructed singletons (e.g. animation state) can react live.</summary>
    event EventHandler? SettingsChanged;

    /// <summary>Loads settings from disk, falling back to defaults if none exist yet or the file is invalid.</summary>
    Task LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>Persists the current settings to disk and raises <see cref="SettingsChanged"/>.</summary>
    Task SaveAsync(CancellationToken cancellationToken = default);
}
