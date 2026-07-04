using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using PhotoSorter.Core.Services.Abstractions;

namespace PhotoSorter.Core.Services.Implementations;

/// <summary>
/// <inheritdoc cref="ITrashService"/>
/// Windows uses the real Recycle Bin. macOS asks Finder to delete the file (which moves it to
/// Trash), since .NET has no built-in cross-platform trash API. Both branches live in one class
/// and switch at runtime (<see cref="RuntimeInformation.IsOSPlatform"/>) rather than needing
/// separate builds - matching SoftwareDesign.md's "macOS optional, ohne Codeänderungen".
/// Platforms without a reliable trash mechanism (Linux without a desktop trash helper) fall back
/// to permanent deletion rather than silently doing nothing.
/// </summary>
public sealed class PlatformTrashService : ITrashService
{
    public async Task MoveToTrashAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            await Task.Run(
                () => Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(
                    filePath,
                    Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                    Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin),
                cancellationToken).ConfigureAwait(false);
            return;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            await MoveToTrashOnMacAsync(filePath, cancellationToken).ConfigureAwait(false);
            return;
        }

        File.Delete(filePath);
    }

    private static async Task MoveToTrashOnMacAsync(string filePath, CancellationToken cancellationToken)
    {
        var escapedPath = filePath.Replace("\\", "\\\\").Replace("\"", "\\\"");
        var script = $"tell application \"Finder\" to delete POSIX file \"{escapedPath}\"";

        var startInfo = new ProcessStartInfo("osascript")
        {
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add("-e");
        startInfo.ArgumentList.Add(script);

        using var process = Process.Start(startInfo) ?? throw new IOException("Failed to start osascript.");
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            throw new IOException($"osascript failed to move file to trash: {error}");
        }
    }
}
