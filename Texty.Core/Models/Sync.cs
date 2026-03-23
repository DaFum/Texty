namespace Texty.Core.Models;

public sealed record SyncConflict(string RelativePath, string LocalHash, string RemoteHash);

public sealed record SyncResult(int Copied, int Skipped, IReadOnlyList<SyncConflict> Conflicts);
