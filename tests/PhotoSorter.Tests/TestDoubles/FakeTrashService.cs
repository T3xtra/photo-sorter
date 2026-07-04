using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PhotoSorter.Core.Services.Abstractions;

namespace PhotoSorter.Tests.TestDoubles;

/// <summary>An <see cref="ITrashService"/> that records requested paths instead of touching the real trash.</summary>
public sealed class FakeTrashService : ITrashService
{
    public List<string> TrashedPaths { get; } = [];

    public Task MoveToTrashAsync(string filePath, CancellationToken cancellationToken = default)
    {
        TrashedPaths.Add(filePath);
        return Task.CompletedTask;
    }
}
