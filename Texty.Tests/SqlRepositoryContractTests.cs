using Texty.Core.Models;
using Texty.Storage.SqlServer;
using Texty.Storage.SqlServer.Repositories;

namespace Texty.Tests;

public sealed class SqlRepositoryContractTests
{
    [Fact]
    public async Task SnippetRepository_ShouldBehaveLikeCrudRepository()
    {
        var state = new SqlServerStorageState();
        var repository = new SqlServerSnippetRepository(new SqlServerStorageOptions(), state);
        var now = DateTimeOffset.UtcNow;
        var snippet = new Snippet(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Contract",
            "txcontract",
            "body",
            "<p>body</p>",
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

        await repository.SaveAsync(snippet);
        var loaded = await repository.GetByIdAsync(snippet.Id);
        Assert.NotNull(loaded);

        var all = await repository.GetAllAsync();
        Assert.Single(all);

        await repository.DeleteAsync(snippet.Id);
        var deleted = await repository.GetByIdAsync(snippet.Id);
        Assert.Null(deleted);
    }
}
