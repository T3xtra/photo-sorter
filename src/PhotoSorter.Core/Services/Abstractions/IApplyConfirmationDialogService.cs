using System.Threading;
using System.Threading.Tasks;
using PhotoSorter.Core.Models;

namespace PhotoSorter.Core.Services.Abstractions;

/// <summary>Shows the "Sortierung abgeschlossen" confirmation (UI-Design.md) and returns the user's choice.</summary>
public interface IApplyConfirmationDialogService
{
    /// <summary>Returns true if the user chose "Anwenden", false for "Abbrechen".</summary>
    Task<bool> ConfirmAsync(ApplySummary summary, CancellationToken cancellationToken = default);
}
