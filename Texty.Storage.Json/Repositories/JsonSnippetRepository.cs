using System.Text.Json;
using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Storage.Json.Repositories;

public sealed class JsonSnippetRepository : ISnippetRepository
{
    private readonly JsonStorageOptions _options;

    /// <summary>
    /// Initialisiert eine JsonSnippetRepository-Instanz und stellt sicher, dass das konfigurierte Snippets-Verzeichnis existiert.
    /// </summary>
    /// <param name="options">Konfigurationsoptionen für den JSON-Speicher, inklusive Pfad zum Snippets-Verzeichnis.</param>
    public JsonSnippetRepository(JsonStorageOptions options)
    {
        _options = options;
        Directory.CreateDirectory(_options.SnippetsDirectory);
    }

    /// <summary>
    /// Lädt alle Snippet-Dateien aus dem konfigurierten Snippets-Verzeichnis und gibt die gefundenen Snippets zurück.
    /// </summary>
    /// <returns>Eine Liste aller erfolgreich deserialisierten Snippets aus dem Verzeichnis, sortiert nach Title (Groß-/Kleinschreibung ignoriert).</returns>
    public async Task<IReadOnlyList<Snippet>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var files = Directory.GetFiles(_options.SnippetsDirectory, "*.json", SearchOption.TopDirectoryOnly);
        var list = new List<Snippet>(files.Length);

        foreach (var file in files)
        {
            await using var stream = File.OpenRead(file);
            var snippet = await JsonSerializer.DeserializeAsync<Snippet>(stream, JsonSerializerDefaults.Options, cancellationToken);
            if (snippet is not null)
            {
                list.Add(snippet);
            }
        }

        return list
            .OrderBy(x => x.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Lädt ein Snippet aus dem JSON-Speicher anhand seiner ID.
    /// </summary>
    /// <param name="id">Die ID des gesuchten Snippets.</param>
    /// <returns>Das gefundene <see cref="Snippet"/>-Objekt, oder <c>null</c>, wenn keine Datei für die ID existiert oder die Deserialisierung kein Objekt liefert.</returns>
    public async Task<Snippet?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var path = GetPath(id);
        if (!File.Exists(path))
        {
            return null;
        }

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<Snippet>(stream, JsonSerializerDefaults.Options, cancellationToken);
    }

    /// <summary>
    /// Persistiert ein Snippet als JSON-Datei im konfigurierten Snippets-Verzeichnis.
    /// </summary>
    /// <remarks>
    /// Erstellt oder überschreibt die Datei, deren Name aus der ID des Snippets abgeleitet wird.
    /// </remarks>
    /// <param name="snippet">Das zu speichernde Snippet; die Datei wird anhand seiner Id benannt.</param>
    public async Task SaveAsync(Snippet snippet, CancellationToken cancellationToken = default)
    {
        var path = GetPath(snippet.Id);
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, snippet, JsonSerializerDefaults.Options, cancellationToken);
    }

    /// <summary>
    /// Löscht die JSON-Datei des Snippets mit der angegebenen ID, falls sie vorhanden ist.
    /// </summary>
    /// <param name="id">Die ID des zu löschenden Snippets.</param>
    /// <param name="cancellationToken">Token zur Abbruchsignalisierung.</param>
    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var path = GetPath(id);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Durchsucht gespeicherte Snippets anhand der übergebenen Suchkriterien, filtert nicht relevante Einträge und liefert die besten Treffer sortiert nach Relevanz und Titel.
    /// </summary>
    /// <param name="query">Suchkriterien (z. B. Term, IncludeHidden, FolderId, Tag, Limit und TargetProcess) die Filter-, Bewertungs- und Limit-Parameter für die Suche festlegen.</param>
    /// <returns>Eine Liste von SnippetSearchResult mit positiver Relevanzbewertung, sortiert nach Score (absteigend) und anschließend nach Snippet.Title (alphabetisch); höchstens `query.Limit` Einträge.</returns>
    public async Task<IReadOnlyList<SnippetSearchResult>> SearchAsync(SnippetSearchQuery query, CancellationToken cancellationToken = default)
    {
        var snippets = await GetAllAsync(cancellationToken);
        var results = snippets
            .Where(s => !s.Deleted)
            .Where(s => query.IncludeHidden || s.HighlightMode != SnippetHighlightMode.Hidden)
            .Where(s => query.FolderId is null || s.FolderId == query.FolderId.Value)
            .Where(s => string.IsNullOrWhiteSpace(query.Tag) || s.Tags.Any(t => string.Equals(t.Value, query.Tag, StringComparison.OrdinalIgnoreCase)))
            .Select(s => new SnippetSearchResult(s, Score(query, s)))
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Snippet.Title, StringComparer.OrdinalIgnoreCase)
            .Take(query.Limit)
            .ToList();

        return results;
    }

    /// <summary>
    /// Berechnet einen Relevanzscore eines Snippets für eine gegebene Suchanfrage.
    /// </summary>
    /// <param name="query">Die Suchanfrage; relevant sind insbesondere `Term` und optional `TargetProcess`.</param>
    /// <param name="snippet">Das zu bewertende Snippet.</param>
    /// <returns>Ein double-Wert, der die Relevanz angibt. Wenn `query.Term` leer oder nur Whitespace ist, wird `1` zurückgegeben. Andernfalls ist der Score die Summe gewichteter Treffer: Title (+5), Shortcut (+4), PlainText (+2), HtmlText (+1), Tags (+2) und ein zusätzlicher Punkt (+1), falls `query.TargetProcess` angegeben ist und ein Trigger des Snippets dieses Zielprozess-Feld case-insensitiv matcht.</returns>
    private static double Score(SnippetSearchQuery query, Snippet snippet)
    {
        if (string.IsNullOrWhiteSpace(query.Term))
        {
            return 1;
        }

        var term = query.Term.Trim();
        double score = 0;

        if (snippet.Title.Contains(term, StringComparison.OrdinalIgnoreCase))
        {
            score += 5;
        }

        if (snippet.Shortcut.Contains(term, StringComparison.OrdinalIgnoreCase))
        {
            score += 4;
        }

        if (snippet.PlainText.Contains(term, StringComparison.OrdinalIgnoreCase))
        {
            score += 2;
        }

        if (snippet.HtmlText.Contains(term, StringComparison.OrdinalIgnoreCase))
        {
            score += 1;
        }

        if (snippet.Tags.Any(t => t.Value.Contains(term, StringComparison.OrdinalIgnoreCase)))
        {
            score += 2;
        }

        if (!string.IsNullOrWhiteSpace(query.TargetProcess) &&
            snippet.Triggers.Any(t => string.Equals(t.TargetProcess, query.TargetProcess, StringComparison.OrdinalIgnoreCase)))
        {
            score += 1;
        }

        return score;
    }

    /// <summary>
/// Erzeugt den vollständigen Dateipfad zur JSON-Datei, die das Snippet mit der angegebenen ID enthält.
/// </summary>
/// <param name="id">Die ID des Snippets.</param>
/// <returns>Der Dateipfad zur Snippet-Datei (Dateiname: ID ohne Trennzeichen mit der Erweiterung .json).</returns>
private string GetPath(Guid id) => Path.Combine(_options.SnippetsDirectory, $"{id:N}.json");
}
