using Texty.Runtime.Sync;

namespace Texty.Tests;

public sealed class SyncOrchestratorTests : IDisposable
{
    private readonly string _source = Path.Combine(Path.GetTempPath(), "texty-tests-src", Guid.NewGuid().ToString("N"));
    private readonly string _target = Path.Combine(Path.GetTempPath(), "texty-tests-dst", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task Sync_ShouldCopyFilesAndDetectConflicts()
    {
        Directory.CreateDirectory(_source);
        Directory.CreateDirectory(_target);

        await File.WriteAllTextAsync(Path.Combine(_source, "a.txt"), "one");
        await File.WriteAllTextAsync(Path.Combine(_target, "a.txt"), "two");
        await File.WriteAllTextAsync(Path.Combine(_source, "b.txt"), "three");

        var sut = new FolderSyncOrchestrator();
        var result = await sut.SyncAsync(_source, _target);

        Assert.Equal(1, result.Copied);
        Assert.Single(result.Conflicts);
        Assert.Contains(result.Conflicts, c => c.RelativePath == "a.txt");
    }

    public void Dispose()
    {
        if (Directory.Exists(_source))
        {
            Directory.Delete(_source, true);
        }

        if (Directory.Exists(_target))
        {
            Directory.Delete(_target, true);
        }
    }
}
