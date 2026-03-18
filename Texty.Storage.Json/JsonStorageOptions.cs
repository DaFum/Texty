namespace Texty.Storage.Json;

public sealed class JsonStorageOptions
{
    public required string RootDirectory { get; init; }

    public string SnippetsDirectory => Path.Combine(RootDirectory, "snippets");
    public string FoldersDirectory => Path.Combine(RootDirectory, "folders");
    public string VersionsDirectory => Path.Combine(RootDirectory, "versions");
    public string TrashDirectory => Path.Combine(RootDirectory, "trash");
    public string AssetsDirectory => Path.Combine(RootDirectory, "assets");
}
