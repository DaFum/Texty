using Texty.Core.Models;
using Texty.Storage.Json;
using Texty.Storage.Json.Repositories;

namespace Texty.Tests;

public sealed class JsonSnippetRepositoryTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "texty-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task SaveAndSearch_ShouldRoundtripSnippet()
    {
        var options = new JsonStorageOptions { RootDirectory = _root };
        var sut = new JsonSnippetRepository(options);
        var snippet = CreateSnippet("Offer Letter", "txoffer", "Hello candidate");
        await sut.SaveAsync(snippet);

        var loaded = await sut.GetByIdAsync(snippet.Id);
        Assert.NotNull(loaded);
        Assert.Equal("Offer Letter", loaded!.Title);

        var search = await sut.SearchAsync(new SnippetSearchQuery("candidate", null, null, null, 10, false));
        Assert.Single(search);
        Assert.Equal(snippet.Id, search[0].Snippet.Id);
    }

    [Fact]
    public async Task Search_ShouldFilterByTagAndFolder()
    {
        var options = new JsonStorageOptions { RootDirectory = _root };
        var sut = new JsonSnippetRepository(options);
        var folderA = Guid.NewGuid();
        var folderB = Guid.NewGuid();

        await sut.SaveAsync(CreateSnippet("A", "txa", "alpha", folderA, tags: [new Tag("hr")]));
        await sut.SaveAsync(CreateSnippet("B", "txb", "beta", folderB, tags: [new Tag("it")]));

        var results = await sut.SearchAsync(new SnippetSearchQuery(null, folderA, "hr", null, 50, false));
        Assert.Single(results);
        Assert.Equal("A", results[0].Snippet.Title);
    }

    private static Snippet CreateSnippet(
        string title,
        string shortcut,
        string text,
        Guid? folderId = null,
        IReadOnlyList<Tag>? tags = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new Snippet(
            Guid.NewGuid(),
            folderId ?? Guid.NewGuid(),
            title,
            shortcut,
            text,
            $"<p>{text}</p>",
            tags ?? [],
            [],
            [],
            null,
            SnippetHighlightMode.None,
            "Segoe UI",
            false,
            now,
            now,
            "test");
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
