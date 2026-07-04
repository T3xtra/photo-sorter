using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PhotoSorter.Core.Models;

namespace PhotoSorter.Core.Services.Abstractions;

/// <summary>
/// Executes sort decisions as real file moves. Called only after explicit user confirmation
/// ("Nach Bestätigung: alle Verschiebeoperationen durchführen") - never during sorting itself.
/// Continues past individual failures ("Restliche Dateien weiter bearbeiten").
/// </summary>
public interface IFileMoveService
{
    Task<FileMoveSummary> ApplyAsync(
        IReadOnlyList<SortDecision> decisions,
        TargetFolder? leftTarget,
        TargetFolder? rightTarget,
        CancellationToken cancellationToken = default);
}
