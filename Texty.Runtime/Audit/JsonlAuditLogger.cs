using System.Text.Json;
using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Runtime.Audit;

public sealed class JsonlAuditLogger : IAuditLogger, IDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = false };
    private readonly string _directory;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _disposed;

    public JsonlAuditLogger(string rootDirectory)
    {
        _directory = Path.Combine(rootDirectory, "audit");
        Directory.CreateDirectory(_directory);
    }

    public async Task WriteAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        var path = GetPath(entry.TimestampUtc);
        var line = JsonSerializer.Serialize(entry, SerializerOptions) + Environment.NewLine;

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

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _gate.Dispose();
        GC.SuppressFinalize(this);
    }

    private string GetPath(DateTimeOffset timestampUtc)
    {
        var fileName = $"audit-{timestampUtc:yyyy-MM-dd}.jsonl";
        return Path.Combine(_directory, fileName);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(JsonlAuditLogger));
        }
    }
}
