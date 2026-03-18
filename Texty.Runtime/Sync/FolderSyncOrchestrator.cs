using System.Security.Cryptography;
using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Runtime.Sync;

public sealed class FolderSyncOrchestrator : ISyncOrchestrator
{
    public async Task<SyncResult> SyncAsync(string sourceDirectory, string targetDirectory, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(sourceDirectory))
        {
            return new SyncResult(0, 0, []);
        }

        Directory.CreateDirectory(targetDirectory);

        var copied = 0;
        var skipped = 0;
        var conflicts = new List<SyncConflict>();

        var files = Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories);
        foreach (var sourcePath in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var relativePath = Path.GetRelativePath(sourceDirectory, sourcePath);
            var targetPath = Path.Combine(targetDirectory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

            if (!File.Exists(targetPath))
            {
                File.Copy(sourcePath, targetPath, overwrite: false);
                copied++;
                continue;
            }

            var sourceHash = await HashAsync(sourcePath, cancellationToken);
            var targetHash = await HashAsync(targetPath, cancellationToken);
            if (sourceHash == targetHash)
            {
                skipped++;
                continue;
            }

            var conflictCopy = $"{targetPath}.conflict.{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";
            File.Copy(sourcePath, conflictCopy, overwrite: true);
            conflicts.Add(new SyncConflict(relativePath, sourceHash, targetHash));
        }

        return new SyncResult(copied, skipped, conflicts);
    }

    private static async Task<string> HashAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash);
    }
}
