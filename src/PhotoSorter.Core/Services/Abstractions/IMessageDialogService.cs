using System.Threading;
using System.Threading.Tasks;

namespace PhotoSorter.Core.Services.Abstractions;

/// <summary>Shows a simple informational message with an OK button (e.g. apply results, errors).</summary>
public interface IMessageDialogService
{
    Task ShowAsync(string title, string message, CancellationToken cancellationToken = default);
}
