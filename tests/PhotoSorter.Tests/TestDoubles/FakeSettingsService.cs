using System;
using System.Threading;
using System.Threading.Tasks;
using PhotoSorter.Core.Configuration;
using PhotoSorter.Core.Services.Abstractions;

namespace PhotoSorter.Tests.TestDoubles;

/// <summary>An in-memory <see cref="ISettingsService"/> for tests.</summary>
public sealed class FakeSettingsService : ISettingsService
{
    public AppSettings Current { get; private set; } = new();

    public event EventHandler? SettingsChanged;

    public int SaveCallCount { get; private set; }

    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task SaveAsync(CancellationToken cancellationToken = default)
    {
        SaveCallCount++;
        SettingsChanged?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }
}
