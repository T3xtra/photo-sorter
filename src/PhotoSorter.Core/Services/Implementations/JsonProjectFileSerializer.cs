using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PhotoSorter.Core.Models.Persistence;
using PhotoSorter.Core.Services.Abstractions;

namespace PhotoSorter.Core.Services.Implementations;

/// <inheritdoc cref="IProjectFileSerializer"/>
public sealed class JsonProjectFileSerializer : IProjectFileSerializer
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
    };

    private readonly ILogger<JsonProjectFileSerializer> _logger;

    public JsonProjectFileSerializer(ILogger<JsonProjectFileSerializer> logger)
    {
        _logger = logger;
    }

    public async Task SaveAsync(ProjectFileDto data, string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var stream = File.Create(filePath);
            try
            {
                await JsonSerializer.SerializeAsync(stream, data, SerializerOptions, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                await stream.DisposeAsync().ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogError(ex, "Failed to write project file to {Path}.", filePath);
        }
    }

    public async Task<ProjectFileDto?> LoadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        FileStream stream;
        try
        {
            // File.OpenRead itself can throw (e.g. UnauthorizedAccessException for a permission-
            // denied file), so it must be inside the try too, not just the deserialization below.
            stream = File.OpenRead(filePath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Failed to open project file from {Path}.", filePath);
            return null;
        }

        try
        {
            return await JsonSerializer.DeserializeAsync<ProjectFileDto>(stream, SerializerOptions, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            _logger.LogWarning(ex, "Failed to read project file from {Path}.", filePath);
            return null;
        }
        finally
        {
            await stream.DisposeAsync().ConfigureAwait(false);
        }
    }
}
