using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PhotoSorter.Core.Models;
using PhotoSorter.Core.Services.Abstractions;

namespace PhotoSorter.Tests.TestDoubles;

/// <summary>An <see cref="IFileMoveService"/> that returns a fixed, pre-set result without touching the file system.</summary>
public sealed class FakeFileMoveService : IFileMoveService
{
    public FileMoveSummary Result { get; set; } = new();

    public IReadOnlyList<SortDecision>? LastDecisions { get; private set; }

    public TargetFolder? LastLeftTarget { get; private set; }

    public TargetFolder? LastRightTarget { get; private set; }

    public int CallCount { get; private set; }

    public Task<FileMoveSummary> ApplyAsync(
        IReadOnlyList<SortDecision> decisions,
        TargetFolder? leftTarget,
        TargetFolder? rightTarget,
        CancellationToken cancellationToken = default)
    {
        CallCount++;
        LastDecisions = decisions;
        LastLeftTarget = leftTarget;
        LastRightTarget = rightTarget;
        return Task.FromResult(Result);
    }
}
