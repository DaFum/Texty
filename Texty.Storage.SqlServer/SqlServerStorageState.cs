using System.Collections.Concurrent;
using Texty.Core.Models;

namespace Texty.Storage.SqlServer;

public sealed class SqlServerStorageState
{
    public ConcurrentDictionary<Guid, Snippet> Snippets { get; } = new();
    public ConcurrentDictionary<Guid, Folder> Folders { get; } = new();
    public ConcurrentDictionary<Guid, List<VersionEntry>> Versions { get; } = new();
    public ConcurrentDictionary<Guid, TrashEntry> Trash { get; } = new();
}
