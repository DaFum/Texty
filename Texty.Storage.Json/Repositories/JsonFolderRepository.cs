using System.Text.Json;
using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Storage.Json.Repositories;

public sealed class JsonFolderRepository : IFolderRepository
{
    private readonly JsonStorageOptions _options;

    /// <summary>
    /// Initialisiert eine neue Instanz von <see cref="JsonFolderRepository"/> und stellt sicher, dass das Zielverzeichnis für Ordner existiert.
    /// </summary>
    /// <param name="options">Konfigurationen für die JSON-Speicherung; insbesondere die Eigenschaft <c>FoldersDirectory</c> gibt das Verzeichnis an, das erstellt wird, falls es nicht existiert.</param>
    public JsonFolderRepository(JsonStorageOptions options)
    {
        _options = options;
        Directory.CreateDirectory(_options.FoldersDirectory);
    }

    /// <summary>
    /// Lädt alle im konfigurierten Ordner gespeicherten Folder-Objekte aus den .json-Dateien.
    /// </summary>
    /// <param name="cancellationToken">Token zum Abbrechen des Lese-/Deserialisierungsvorgangs.</param>
    /// <returns>Eine Liste der geladenen Folder-Objekte, sortiert nach <c>SortOrder</c> und anschließend nach <c>Name</c> (Groß-/Kleinschreibung wird nicht berücksichtigt).</returns>
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

    /// <summary>
    /// Speichert ein Folder-Objekt als JSON-Datei im konfigurierten Ordnerverzeichnis.
    /// </summary>
    /// <param name="folder">Das zu speichernde Folder-Objekt; die Datei wird unter "{Id:N}.json" abgelegt.</param>
    /// <param name="cancellationToken">Token zum Abbrechen der asynchronen Schreiboperation.</param>
    public async Task SaveAsync(Folder folder, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(_options.FoldersDirectory, $"{folder.Id:N}.json");
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, folder, JsonSerializerDefaults.Options, cancellationToken);
    }

    /// <summary>
    /// Löscht die JSON-Datei für den Ordner mit der angegebenen Id aus dem konfigurierten Verzeichnis; falls die Datei nicht existiert, passiert nichts.
    /// </summary>
    /// <param name="id">Id des Ordners; der Dateiname wird im N-Format (32-stellige Hex) mit der Erweiterung .json gebildet.</param>
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
