using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PhotoSorter.Core.Configuration;
using PhotoSorter.Core.Services.Abstractions;

namespace PhotoSorter.Core.Services.Implementations;

/// <inheritdoc cref="ISettingsService"/>
public sealed class JsonSettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
    };

    private readonly IAppPathProvider _pathProvider;
    private readonly ILogger<JsonSettingsService> _logger;

    public JsonSettingsService(IAppPathProvider pathProvider, ILogger<JsonSettingsService> logger)
    {
        _pathProvider = pathProvider;
        _logger = logger;
    }

    public AppSettings Current { get; private set; } = new();

    public event EventHandler? SettingsChanged;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        var path = _pathProvider.SettingsFilePath;

        if (!File.Exists(path))
        {
            _logger.LogInformation("No settings file found at {Path}, using defaults.", path);
            Current = new AppSettings();
            return;
        }

        // ConfigureAwait(false) throughout: this is Core library code that callers may invoke
        // via GetAwaiter().GetResult() from a UI thread (e.g. during app startup, before a
        // window exists to await on). Without it, a continuation trying to resume on the
        // captured UI SynchronizationContext would deadlock against that same blocked thread.
        FileStream stream;
        try
        {
            // File.OpenRead itself can throw (e.g. UnauthorizedAccessException for a permission-
            // denied settings file) - since this runs synchronously-over-async before any window
            // exists, an unhandled exception here would crash the app before it can even start.
            stream = File.OpenRead(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Failed to open settings from {Path}, falling back to defaults.", path);
            Current = new AppSettings();
            return;
        }

        try
        {
            var loaded = await JsonSerializer.DeserializeAsync<AppSettings>(stream, SerializerOptions, cancellationToken)
                .ConfigureAwait(false);
            Current = loaded ?? new AppSettings();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            _logger.LogWarning(ex, "Failed to read settings from {Path}, falling back to defaults.", path);
            Current = new AppSettings();
        }
        finally
        {
            await stream.DisposeAsync().ConfigureAwait(false);
        }
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        var path = _pathProvider.SettingsFilePath;

        try
        {
            var stream = File.Create(path);
            try
            {
                await JsonSerializer.SerializeAsync(stream, Current, SerializerOptions, cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                await stream.DisposeAsync().ConfigureAwait(false);
            }

            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogError(ex, "Failed to write settings to {Path}.", path);
        }
    }
}
