using System.Text.Json;
using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Storage.Json.Repositories;

public sealed class JsonVersionRepository : IVersionRepository
{
    private readonly JsonStorageOptions _options;

    public JsonVersionRepository(JsonStorageOptions options)
    {
        _options = options;
        Directory.CreateDirectory(_options.VersionsDirectory);
    }

    public async Task<IReadOnlyList<VersionEntry>> GetVersionsAsync(Guid snippetId, CancellationToken cancellationToken = default)
    {
        var snippetDir = GetSnippetDirectory(snippetId);
        if (!Directory.Exists(snippetDir))
        {
            return Array.Empty<VersionEntry>();
        }

        var files = Directory.GetFiles(snippetDir, "*.json", SearchOption.TopDirectoryOnly);
        var list = new List<VersionEntry>(files.Length);

        foreach (var file in files)
        {
            await using var stream = File.OpenRead(file);
            var item = await JsonSerializer.DeserializeAsync<VersionEntry>(stream, JsonSerializerDefaults.Options, cancellationToken);
            if (item is not null)
            {
                list.Add(item);
            }
        }

        return list
            .OrderByDescending(x => x.VersionNumber)
            .ThenByDescending(x => x.CreatedUtc)
            .ToList();
    }

    public async Task AddVersionAsync(VersionEntry version, CancellationToken cancellationToken = default)
    {
        var snippetDir = GetSnippetDirectory(version.SnippetId);
        Directory.CreateDirectory(snippetDir);
        var path = Path.Combine(snippetDir, $"{version.VersionNumber:D6}-{version.Id:N}.json");
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, version, JsonSerializerDefaults.Options, cancellationToken);
    }

    private string GetSnippetDirectory(Guid snippetId) => Path.Combine(_options.VersionsDirectory, snippetId.ToString("N"));
}
