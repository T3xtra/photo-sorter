using System.Threading;
using System.Threading.Tasks;

namespace PhotoSorter.Core.Services.Abstractions;

/// <summary>Moves a file to the OS trash/recycle bin ("Der linke Zielordner darf alternativ der Papierkorb sein").</summary>
public interface ITrashService
{
    Task MoveToTrashAsync(string filePath, CancellationToken cancellationToken = default);
}
