using System.Threading;
using System.Threading.Tasks;
using PhotoSorter.Core.Models;
using PhotoSorter.Core.Services.Abstractions;

namespace PhotoSorter.Tests.TestDoubles;

/// <summary>An <see cref="IApplyConfirmationDialogService"/> that returns a fixed, pre-set answer.</summary>
public sealed class FakeApplyConfirmationDialogService : IApplyConfirmationDialogService
{
    public bool ConfirmResult { get; set; } = true;

    public ApplySummary? LastSummary { get; private set; }

    public Task<bool> ConfirmAsync(ApplySummary summary, CancellationToken cancellationToken = default)
    {
        LastSummary = summary;
        return Task.FromResult(ConfirmResult);
    }
}
