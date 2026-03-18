using System.Text.Json;
using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Storage.Json.Repositories;

public sealed class JsonTrashRepository : ITrashRepository
{
    private readonly JsonStorageOptions _options;

    public JsonTrashRepository(JsonStorageOptions options)
    {
        _options = options;
        Directory.CreateDirectory(_options.TrashDirectory);
    }

    public async Task<IReadOnlyList<TrashEntry>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var files = Directory.GetFiles(_options.TrashDirectory, "*.json", SearchOption.TopDirectoryOnly);
        var list = new List<TrashEntry>(files.Length);

        foreach (var file in files)
        {
            await using var stream = File.OpenRead(file);
            var item = await JsonSerializer.DeserializeAsync<TrashEntry>(stream, JsonSerializerDefaults.Options, cancellationToken);
            if (item is not null)
            {
                list.Add(item);
            }
        }

        return list
            .OrderByDescending(x => x.DeletedUtc)
            .ToList();
    }

    public async Task MoveToTrashAsync(TrashEntry entry, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(_options.TrashDirectory, $"{entry.Id:N}.json");
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, entry, JsonSerializerDefaults.Options, cancellationToken);
    }

    public Task RemoveAsync(Guid trashEntryId, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(_options.TrashDirectory, $"{trashEntryId:N}.json");
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }
}
