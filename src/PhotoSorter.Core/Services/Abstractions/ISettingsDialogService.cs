using System.Threading;
using System.Threading.Tasks;

namespace PhotoSorter.Core.Services.Abstractions;

/// <summary>Opens the "Einstellungen" window (Hotkeys, Darstellung, Animationen).</summary>
public interface ISettingsDialogService
{
    Task ShowAsync(CancellationToken cancellationToken = default);
}
