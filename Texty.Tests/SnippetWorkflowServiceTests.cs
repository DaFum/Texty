using Texty.Core.Models;
using Texty.Runtime.Services;
using Texty.Storage.Json;
using Texty.Storage.Json.Repositories;

namespace Texty.Tests;

public sealed class SnippetWorkflowServiceTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "texty-workflow-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task SaveAndDelete_ShouldUpdateIncrementalSearchIndex()
    {
        var options = new JsonStorageOptions { RootDirectory = _root };
        var snippets = new JsonSnippetRepository(options);
        var versions = new JsonVersionRepository(options);
        var index = new JsonSnippetSearchIndex();

        var versioning = new SnippetVersioningService(snippets, versions);
        var workflow = new SnippetWorkflowService(snippets, index, versioning);

        var now = DateTimeOffset.UtcNow;
        var snippet = new Snippet(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Workflow",
            "txwf",
            "hello world",
            "<p>hello world</p>",
            [],
            [],
            [],
            null,
            SnippetHighlightMode.None,
            null,
            false,
            now,
            now,
            "test");

        await workflow.SaveAsync(snippet, createVersion: true);
        var found = await workflow.SearchAsync(new SnippetSearchQuery("hello", null, null, null, 20, false));
        Assert.Contains(found, x => x.Snippet.Id == snippet.Id);

        await workflow.DeleteAsync(snippet.Id);
        var afterDelete = await workflow.SearchAsync(new SnippetSearchQuery("hello", null, null, null, 20, false));
        Assert.DoesNotContain(afterDelete, x => x.Snippet.Id == snippet.Id);
    }

    [Fact]
    public async Task ReplaceAsync_ShouldRespectScopeSnippetIds()
    {
        var options = new JsonStorageOptions { RootDirectory = _root };
        var snippets = new JsonSnippetRepository(options);
        var versions = new JsonVersionRepository(options);
        var index = new JsonSnippetSearchIndex();

        var versioning = new SnippetVersioningService(snippets, versions);
        var workflow = new SnippetWorkflowService(snippets, index, versioning);

        var now = DateTimeOffset.UtcNow;
        var a = new Snippet(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "A",
            "txa",
            "replace-me",
            "<p>replace-me</p>",
            [],
            [],
            [],
            null,
            SnippetHighlightMode.None,
            null,
            false,
            now,
            now,
            "test");
        var b = a with { Id = Guid.NewGuid(), Title = "B" };

        await workflow.SaveAsync(a, createVersion: true);
        await workflow.SaveAsync(b, createVersion: true);

        var result = await workflow.ReplaceAsync(
            new SnippetReplaceRequest(
                "replace-me",
                "done",
                new SnippetReplaceScope(null, null, null, [a.Id], false)),
            "tester");

        Assert.Equal(1, result.Updated);
        var loadedA = await snippets.GetByIdAsync(a.Id);
        var loadedB = await snippets.GetByIdAsync(b.Id);
        Assert.Contains("done", loadedA!.PlainText, StringComparison.Ordinal);
        Assert.DoesNotContain("done", loadedB!.PlainText, StringComparison.Ordinal);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, true);
        }
    }
}
