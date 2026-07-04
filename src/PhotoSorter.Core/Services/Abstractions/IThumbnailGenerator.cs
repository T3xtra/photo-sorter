using System.Threading;
using System.Threading.Tasks;

namespace PhotoSorter.Core.Services.Abstractions;

/// <summary>Generates a small PNG-encoded preview image for the thumbnail strip.</summary>
public interface IThumbnailGenerator
{
    Task<byte[]> GenerateAsync(string filePath, CancellationToken cancellationToken = default);
}
