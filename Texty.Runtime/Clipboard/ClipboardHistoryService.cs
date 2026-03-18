using Texty.Core.Models;

namespace Texty.Runtime.Clipboard;

public sealed class ClipboardHistoryService
{
    private readonly LinkedList<ClipboardItem> _items = [];
    private readonly int _capacity;

    /// <summary>
    /// Erstellt eine neue ClipboardHistoryService-Instanz mit einer maximalen Kapazität für Einträge.
    /// </summary>
    /// <param name="capacity">Maximale Anzahl gespeicherter Einträge; wird auf mindestens 1 gesetzt.</param>
    public ClipboardHistoryService(int capacity = 50)
    {
        _capacity = Math.Max(1, capacity);
    }

    /// <summary>
    /// Fügt das übergebene ClipboardItem als neuesten Eintrag in die Verlaufsliste ein und entfernt bei Bedarf die ältesten Einträge, bis die maximale Kapazität wieder eingehalten ist.
    /// </summary>
    /// <param name="item">Das einzufügende ClipboardItem.</param>
    public void Add(ClipboardItem item)
    {
        _items.AddFirst(item);
        while (_items.Count > _capacity)
        {
            _items.RemoveLast();
        }
    }

    /// <summary>
/// Liefert eine Momentaufnahme der gesamten Zwischenablage-Historie in Reihenfolge vom neuesten zum ältesten Eintrag.
/// </summary>
/// <returns>Eine schreibgeschützte Liste der aktuellen ClipboardItem-Einträge; Änderungen an der zurückgegebenen Liste beeinflussen nicht den internen Verlauf.</returns>
public IReadOnlyList<ClipboardItem> GetAll() => _items.ToList();
}
