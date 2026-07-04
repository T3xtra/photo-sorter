using System.Threading;
using System.Threading.Tasks;
using PhotoSorter.Core.Models.Persistence;

namespace PhotoSorter.Core.Services.Abstractions;

/// <summary>Reads and writes <c>.photosort</c> project files.</summary>
public interface IProjectFileSerializer
{
    Task SaveAsync(ProjectFileDto data, string filePath, CancellationToken cancellationToken = default);

    /// <summary>Returns null if the file doesn't exist or isn't valid JSON.</summary>
    Task<ProjectFileDto?> LoadAsync(string filePath, CancellationToken cancellationToken = default);
}
