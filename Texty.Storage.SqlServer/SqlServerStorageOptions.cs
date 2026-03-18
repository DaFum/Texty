namespace Texty.Storage.SqlServer;

public sealed class SqlServerStorageOptions
{
    public string? ConnectionString { get; init; }

    public bool UseInMemoryFallback { get; init; } = true;
}
