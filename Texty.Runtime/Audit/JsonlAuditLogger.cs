using System.Text.Json;
using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Runtime.Audit;

public sealed class JsonlAuditLogger : IAuditLogger
{
    private readonly string _directory;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public JsonlAuditLogger(string rootDirectory)
    {
        _directory = Path.Combine(rootDirectory, "audit");
        Directory.CreateDirectory(_directory);
    }

    public async Task WriteAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
    {
        var path = GetPath(entry.TimestampUtc);
        var line = JsonSerializer.Serialize(
                       entry,
                       new JsonSerializerOptions { WriteIndented = false }) +
                   Environment.NewLine;

        await _gate.WaitAsync(cancellationToken);
        try
        {
            await File.AppendAllTextAsync(path, line, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private string GetPath(DateTimeOffset timestampUtc)
    {
        var fileName = $"audit-{timestampUtc:yyyy-MM-dd}.jsonl";
        return Path.Combine(_directory, fileName);
    }
}
