using Texty.Core.Models;
using Texty.Runtime.Audit;

namespace Texty.Tests;

public sealed class AuditLoggerTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "texty-audit-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task JsonlAuditLogger_ShouldAppendEntry()
    {
        var logger = new JsonlAuditLogger(_root);
        var entry = new AuditLogEntry(
            DateTimeOffset.UtcNow,
            AuditCategory.Insertion,
            "insert",
            AuditDecision.Allow,
            "abc123",
            true,
            "ok");

        await logger.WriteAsync(entry);

        var auditDir = Path.Combine(_root, "audit");
        var file = Directory.GetFiles(auditDir, "audit-*.jsonl").Single();
        var lines = await File.ReadAllLinesAsync(file);
        Assert.Single(lines);
        Assert.Contains("\"Action\":\"insert\"", lines[0], StringComparison.Ordinal);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, true);
        }
    }
}
