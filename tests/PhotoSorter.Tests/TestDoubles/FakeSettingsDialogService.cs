using System.Threading;
using System.Threading.Tasks;
using PhotoSorter.Core.Services.Abstractions;

namespace PhotoSorter.Tests.TestDoubles;

public sealed class FakeSettingsDialogService : ISettingsDialogService
{
    public int ShowCallCount { get; private set; }

    public Task ShowAsync(CancellationToken cancellationToken = default)
    {
        ShowCallCount++;
        return Task.CompletedTask;
    }
}
