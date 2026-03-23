using System.Text.Json;
using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Storage.Json.Repositories;

public sealed class JsonFolderRepository : IFolderRepository
{
    private readonly JsonStorageOptions _options;

    public JsonFolderRepository(JsonStorageOptions options)
    {
        _options = options;
        Directory.CreateDirectory(_options.FoldersDirectory);
    }

    public async Task<IReadOnlyList<Folder>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var files = Directory.GetFiles(_options.FoldersDirectory, "*.json", SearchOption.TopDirectoryOnly);
        var list = new List<Folder>(files.Length);

        foreach (var file in files)
        {
            await using var stream = File.OpenRead(file);
            var folder = await JsonSerializer.DeserializeAsync<Folder>(stream, JsonSerializerDefaults.Options, cancellationToken);
            if (folder is not null)
            {
                list.Add(folder);
            }
        }

        return list
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task SaveAsync(Folder folder, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(_options.FoldersDirectory, $"{folder.Id:N}.json");
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, folder, JsonSerializerDefaults.Options, cancellationToken);
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(_options.FoldersDirectory, $"{id:N}.json");
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }
}
