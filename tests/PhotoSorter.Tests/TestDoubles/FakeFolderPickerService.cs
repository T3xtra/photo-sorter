using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PhotoSorter.Core.Services.Abstractions;

namespace PhotoSorter.Tests.TestDoubles;

/// <summary>An <see cref="IFolderPickerService"/> that returns a fixed, pre-set selection.</summary>
public sealed class FakeFolderPickerService : IFolderPickerService
{
    public IReadOnlyList<string> Selection { get; set; } = [];

    public string? SingleSelection { get; set; }

    public int CallCount { get; private set; }

    public string? LastSuggestedStartLocation { get; private set; }

    public Task<IReadOnlyList<string>> PickFoldersAsync(string title, string? suggestedStartLocation = null, CancellationToken cancellationToken = default)
    {
        CallCount++;
        LastSuggestedStartLocation = suggestedStartLocation;
        return Task.FromResult(Selection);
    }

    public Task<string?> PickFolderAsync(string title, string? suggestedStartLocation = null, CancellationToken cancellationToken = default)
    {
        CallCount++;
        LastSuggestedStartLocation = suggestedStartLocation;
        return Task.FromResult(SingleSelection);
    }
}
