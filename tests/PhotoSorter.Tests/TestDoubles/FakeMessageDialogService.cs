using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PhotoSorter.Core.Services.Abstractions;

namespace PhotoSorter.Tests.TestDoubles;

/// <summary>An <see cref="IMessageDialogService"/> that records shown messages instead of displaying anything.</summary>
public sealed class FakeMessageDialogService : IMessageDialogService
{
    public List<(string Title, string Message)> ShownMessages { get; } = [];

    public Task ShowAsync(string title, string message, CancellationToken cancellationToken = default)
    {
        ShownMessages.Add((title, message));
        return Task.CompletedTask;
    }
}
