using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Storage.SqlServer.Repositories;

public sealed class SqlServerVersionRepository : IVersionRepository
{
    private readonly SqlServerStorageState _state;

    /// <summary>
    /// Initialisiert eine neue Instanz von <see cref="SqlServerVersionRepository"/> mit dem angegebenen Speicherzustand.
    /// </summary>
    /// <param name="state">Der <see cref="SqlServerStorageState"/>, der die Versionsdaten und die zugrunde liegende Speicherstruktur bereitstellt.</param>
    public SqlServerVersionRepository(SqlServerStorageState state)
    {
        _state = state;
    }

    /// <summary>
    /// Liefert die Versionseinträge für das angegebene Snippet, sortiert nach Versionsnummer (absteigend) und Erstellungszeit (absteigend).
    /// </summary>
    /// <param name="snippetId">Die ID des Snippets, für das Versionen abgerufen werden sollen.</param>
    /// <returns>Eine schreibgeschützte Liste der Versionseinträge in der angegebenen Reihenfolge; eine leere Liste, wenn keine Versionen vorhanden sind.</returns>
    public Task<IReadOnlyList<VersionEntry>> GetVersionsAsync(Guid snippetId, CancellationToken cancellationToken = default)
    {
        if (!_state.Versions.TryGetValue(snippetId, out var values))
        {
            return Task.FromResult<IReadOnlyList<VersionEntry>>(Array.Empty<VersionEntry>());
        }

        IReadOnlyList<VersionEntry> ordered = values
            .OrderByDescending(v => v.VersionNumber)
            .ThenByDescending(v => v.CreatedUtc)
            .ToList();

        return Task.FromResult(ordered);
    }

    /// <summary>
    /// Fügt einen Versions-Eintrag für das zugehörige Snippet in den internen Speicher ein und stellt sicher, dass die Einfügung threadsicher erfolgt.
    /// </summary>
    /// <param name="version">Der hinzuzufügende Versions-Eintrag; dessen SnippetId bestimmt die Ziel-Liste.</param>
    /// <param name="cancellationToken">Wird nicht verwendet und hat keinen Einfluss auf die Operation.</param>
    /// <returns>Eine bereits abgeschlossene Task, die das Ende der Einfügeoperation signalisiert.</returns>
    public Task AddVersionAsync(VersionEntry version, CancellationToken cancellationToken = default)
    {
        var list = _state.Versions.GetOrAdd(version.SnippetId, _ => []);
        lock (list)
        {
            list.Add(version);
        }

        return Task.CompletedTask;
    }
}
