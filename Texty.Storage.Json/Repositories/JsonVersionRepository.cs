using System.Text.Json;
using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Storage.Json.Repositories;

public sealed class JsonVersionRepository : IVersionRepository
{
    private readonly JsonStorageOptions _options;

    /// <summary>
    /// Initialisiert ein Json-basierendes Versionsrepository und stellt sicher, dass das Verzeichnis für Versionen existiert.
    /// </summary>
    /// <param name="options">Konfiguration für den JSON-Speicher, inklusive Pfad der Versionsverzeichnisse.</param>
    public JsonVersionRepository(JsonStorageOptions options)
    {
        _options = options;
        Directory.CreateDirectory(_options.VersionsDirectory);
    }

    /// <summary>
    /// Gibt alle gespeicherten Versionseinträge für das angegebene Snippet zurück, sortiert nach Versionsnummer und Erstellungszeit.
    /// </summary>
    /// <param name="snippetId">Die ID des Snippets, dessen Versionen gelesen werden.</param>
    /// <param name="cancellationToken">Abbruch-Token zur Beendigung des Vorgangs.</param>
    /// <returns>Eine Liste der gefundenen <see cref="VersionEntry"/>-Objekte, sortiert zuerst nach <c>VersionNumber</c> absteigend und dann nach <c>CreatedUtc</c> absteigend; leer, wenn keine Versionen vorhanden sind.</returns>
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

    /// <summary>
    /// Speichert einen Versions-Eintrag als JSON-Datei im Versionsverzeichnis des zugehörigen Snippets.
    /// </summary>
    /// <param name="version">Der zu speichernde Versions-Eintrag; wird unter dem Dateinamenmuster "<VersionNumber:D6>-<Id:N>.json" im Snippet-Verzeichnis abgelegt.</param>
    /// <param name="cancellationToken">Token zum Abbrechen der asynchronen Schreiboperation.</param>
    public async Task AddVersionAsync(VersionEntry version, CancellationToken cancellationToken = default)
    {
        var snippetDir = GetSnippetDirectory(version.SnippetId);
        Directory.CreateDirectory(snippetDir);
        var path = Path.Combine(snippetDir, $"{version.VersionNumber:D6}-{version.Id:N}.json");
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, version, JsonSerializerDefaults.Options, cancellationToken);
    }

    /// <summary>
/// Gibt den Dateisystempfad zum Versionsverzeichnis eines Snippets zurück.
/// </summary>
/// <param name="snippetId">Die GUID des Snippets, formatiert als 32-stellige hexadezimale Zeichenfolge ohne Trennstriche ("N").</param>
/// <returns>Der vollständige Verzeichnis-Pfad für die Versionen des angegebenen Snippets.</returns>
private string GetSnippetDirectory(Guid snippetId) => Path.Combine(_options.VersionsDirectory, snippetId.ToString("N"));
}
