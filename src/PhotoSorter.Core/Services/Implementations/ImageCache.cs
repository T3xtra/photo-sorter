using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PhotoSorter.Core.Services.Abstractions;

namespace PhotoSorter.Core.Services.Implementations;

/// <inheritdoc cref="IImageCache"/>
public sealed class ImageCache : IImageCache
{
    private readonly IImageDecoder _decoder;
    private readonly ILogger<ImageCache> _logger;
    private readonly Dictionary<string, byte[]> _entries = new();
    private readonly object _lock = new();

    public ImageCache(IImageDecoder decoder, ILogger<ImageCache> logger)
    {
        _decoder = decoder;
        _logger = logger;
    }

    public async Task<byte[]> GetAsync(string path, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_entries.TryGetValue(path, out var cached))
            {
                return cached;
            }
        }

        var bytes = await _decoder.DecodeToPngAsync(path, cancellationToken).ConfigureAwait(false);

        lock (_lock)
        {
            _entries[path] = bytes;
        }

        return bytes;
    }

    public void UpdateWindow(IReadOnlyList<string> pathsToKeep)
    {
        var keep = new HashSet<string>(pathsToKeep);
        List<string> uncached;

        lock (_lock)
        {
            foreach (var key in _entries.Keys.Where(k => !keep.Contains(k)).ToList())
            {
                _entries.Remove(key);
            }

            uncached = pathsToKeep.Where(p => !_entries.ContainsKey(p)).ToList();
        }

        foreach (var path in uncached)
        {
            _ = PreloadAsync(path);
        }
    }

    private async Task PreloadAsync(string path)
    {
        try
        {
            await GetAsync(path).ConfigureAwait(false);
        }
        catch (Exception ex) when (RecoverableImageErrors.IsRecoverable(ex))
        {
            _logger.LogWarning(ex, "Failed to preload {Path}.", path);
        }
    }
}
