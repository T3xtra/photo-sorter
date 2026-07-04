using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PhotoSorter.Core.Models;
using PhotoSorter.Core.Services.Abstractions;

namespace PhotoSorter.Core.Services.Implementations;

/// <inheritdoc cref="IFileMoveService"/>
public sealed class FileMoveService : IFileMoveService
{
    private readonly ITrashService _trashService;
    private readonly ILogger<FileMoveService> _logger;

    public FileMoveService(ITrashService trashService, ILogger<FileMoveService> logger)
    {
        _trashService = trashService;
        _logger = logger;
    }

    public async Task<FileMoveSummary> ApplyAsync(
        IReadOnlyList<SortDecision> decisions,
        TargetFolder? leftTarget,
        TargetFolder? rightTarget,
        CancellationToken cancellationToken = default)
    {
        var succeeded = new List<ImageFile>();
        var errors = new List<FileMoveError>();

        foreach (var decision in decisions)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var target = decision.Action == SortAction.Left ? leftTarget : rightTarget;
            if (target is null)
            {
                errors.Add(new FileMoveError(decision.Image, "Kein Zielordner ausgewählt."));
                continue;
            }

            try
            {
                await MoveOneAsync(decision.Image.FullPath, target, cancellationToken).ConfigureAwait(false);
                succeeded.Add(decision.Image);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _logger.LogError(ex, "Failed to move {Path}.", decision.Image.FullPath);
                errors.Add(new FileMoveError(decision.Image, ex.Message));
            }
        }

        _logger.LogInformation("Apply finished: {Succeeded} succeeded, {Failed} failed.", succeeded.Count, errors.Count);

        return new FileMoveSummary { SucceededImages = succeeded, Errors = errors };
    }

    private async Task MoveOneAsync(string sourcePath, TargetFolder target, CancellationToken cancellationToken)
    {
        if (!File.Exists(sourcePath))
        {
            throw new IOException($"Datei nicht gefunden: {sourcePath}");
        }

        if (target.IsTrash)
        {
            await _trashService.MoveToTrashAsync(sourcePath, cancellationToken).ConfigureAwait(false);
            return;
        }

        Directory.CreateDirectory(target.Path!);
        var destinationPath = EnsureUniqueDestination(Path.Combine(target.Path!, Path.GetFileName(sourcePath)));

        await Task.Run(() => File.Move(sourcePath, destinationPath), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Avoids silently overwriting an existing file with the same name at the target.</summary>
    private static string EnsureUniqueDestination(string destinationPath)
    {
        if (!File.Exists(destinationPath))
        {
            return destinationPath;
        }

        var directory = Path.GetDirectoryName(destinationPath)!;
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(destinationPath);
        var extension = Path.GetExtension(destinationPath);

        for (var i = 1; ; i++)
        {
            var candidate = Path.Combine(directory, $"{nameWithoutExtension} ({i}){extension}");
            if (!File.Exists(candidate))
            {
                return candidate;
            }
        }
    }
}
