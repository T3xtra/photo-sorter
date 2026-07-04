using System.Threading;
using System.Threading.Tasks;
using PhotoSorter.Core.Models.Persistence;
using PhotoSorter.Core.Services.Abstractions;

namespace PhotoSorter.Tests.TestDoubles;

/// <summary>An in-memory <see cref="IProjectFileSerializer"/>, keyed by file path.</summary>
public sealed class FakeProjectFileSerializer : IProjectFileSerializer
{
    private readonly System.Collections.Generic.Dictionary<string, ProjectFileDto> _files = new();

    public int SaveCallCount { get; private set; }

    public Task SaveAsync(ProjectFileDto data, string filePath, CancellationToken cancellationToken = default)
    {
        SaveCallCount++;
        _files[filePath] = data;
        return Task.CompletedTask;
    }

    public Task<ProjectFileDto?> LoadAsync(string filePath, CancellationToken cancellationToken = default) =>
        Task.FromResult(_files.GetValueOrDefault(filePath));
}
