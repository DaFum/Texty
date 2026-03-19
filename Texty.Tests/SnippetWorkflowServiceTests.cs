using Texty.Core.Models;
using Texty.Runtime.Services;
using Texty.Storage.Json;
using Texty.Storage.Json.Repositories;

namespace Texty.Tests;

public sealed class SnippetWorkflowServiceTests
{
    [Fact]
    public async Task SaveAndDelete_ShouldUpdateIncrementalSearchIndex()
    {
        var root = CreateTestRoot();
        try
        {
            var deps = CreateWorkflowDependencies(root);

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

            await deps.Workflow.SaveAsync(snippet, createVersion: true);
            var found = await deps.Workflow.SearchAsync(new SnippetSearchQuery("hello", null, null, null, 20, false));
            Assert.Contains(found, x => x.Snippet.Id == snippet.Id);

            await deps.Workflow.DeleteAsync(snippet.Id);
            var afterDelete = await deps.Workflow.SearchAsync(new SnippetSearchQuery("hello", null, null, null, 20, false));
            Assert.DoesNotContain(afterDelete, x => x.Snippet.Id == snippet.Id);
        }
        finally
        {
            CleanupTestRoot(root);
        }
    }

    [Fact]
    public async Task ReplaceAsync_ShouldRespectScopeSnippetIds()
    {
        var root = CreateTestRoot();
        try
        {
            var deps = CreateWorkflowDependencies(root);

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

            await deps.Workflow.SaveAsync(a, createVersion: true);
            await deps.Workflow.SaveAsync(b, createVersion: true);

            var result = await deps.Workflow.ReplaceAsync(
                new SnippetReplaceRequest(
                    "replace-me",
                    "done",
                    new SnippetReplaceScope(null, null, null, [a.Id], false)),
                "tester");

            Assert.Equal(1, result.Updated);
            var loadedA = await deps.Snippets.GetByIdAsync(a.Id);
            var loadedB = await deps.Snippets.GetByIdAsync(b.Id);
            Assert.Contains("done", loadedA!.PlainText, StringComparison.Ordinal);
            Assert.DoesNotContain("done", loadedB!.PlainText, StringComparison.Ordinal);
        }
        finally
        {
            CleanupTestRoot(root);
        }
    }

    private static WorkflowDependencies CreateWorkflowDependencies(string root)
    {
        var options = new JsonStorageOptions { RootDirectory = root };
        var snippets = new JsonSnippetRepository(options);
        var versions = new JsonVersionRepository(options);
        var trash = new JsonTrashRepository(options);
        var index = new JsonSnippetSearchIndex();
        var versioning = new SnippetVersioningService(snippets, versions);
        var workflow = new SnippetWorkflowService(snippets, trash, index, versioning);
        return new WorkflowDependencies(snippets, workflow);
    }

    private static string CreateTestRoot()
    {
        return Path.Combine(Path.GetTempPath(), "texty-workflow-tests", Guid.NewGuid().ToString("N"));
    }

    private static void CleanupTestRoot(string root)
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, true);
        }
    }

    private sealed record WorkflowDependencies(
        JsonSnippetRepository Snippets,
        SnippetWorkflowService Workflow);
}
