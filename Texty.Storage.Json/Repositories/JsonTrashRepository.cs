using System.Text.Json;
using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Storage.Json.Repositories;

public sealed class JsonTrashRepository : ITrashRepository
{
    private readonly JsonStorageOptions _options;

    /// <summary>
    /// Initialisiert eine neue JsonTrashRepository-Instanz und stellt sicher, dass das Papierkorb-Verzeichnis existiert.
    /// </summary>
    /// <param name="options">Konfiguration für die JSON-Speicherung; erwartet insbesondere die Eigenschaft <c>TrashDirectory</c> mit dem Verzeichnis, in dem Trash-Einträge abgelegt werden.</param>
    public JsonTrashRepository(JsonStorageOptions options)
    {
        _options = options;
        Directory.CreateDirectory(_options.TrashDirectory);
    }

    /// <summary>
    /// Liest alle TrashEntry-Dateien aus dem konfigurierten Trash-Verzeichnis und gibt die gefundenen Einträge nach Löschzeit absteigend sortiert zurück.
    /// </summary>
    /// <returns>Eine Liste aller deserialisierten <see cref="TrashEntry"/>-Objekte, sortiert nach DeletedUtc in absteigender Reihenfolge.</returns>
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

    /// <summary>
    /// Schreibt den angegebenen TrashEntry als JSON-Datei in das konfigurierte Papierkorb-Verzeichnis.
    /// </summary>
    /// <param name="entry">Der zu speichernde TrashEntry; die Datei erhält den Namen `{Id:N}.json`.</param>
    /// <param name="cancellationToken">Token zum Abbrechen der asynchronen Dateischreiboperation.</param>
    public async Task MoveToTrashAsync(TrashEntry entry, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(_options.TrashDirectory, $"{entry.Id:N}.json");
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, entry, JsonSerializerDefaults.Options, cancellationToken);
    }

    /// <summary>
    /// Entfernt die JSON-Datei des angegebenen Papierkorb-Eintrags aus dem Papierkorbverzeichnis, falls sie vorhanden ist.
    /// </summary>
    /// <param name="trashEntryId">Die ID des zu entfernenden Papierkorb-Eintrags.</param>
    /// <returns>Eine Task, die abgeschlossen wird, nachdem der Löschversuch ausgeführt wurde.</returns>
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
