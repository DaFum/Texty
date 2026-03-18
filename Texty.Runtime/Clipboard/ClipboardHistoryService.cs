using Texty.Core.Models;

namespace Texty.Runtime.Clipboard;

public sealed class ClipboardHistoryService
{
    private readonly LinkedList<ClipboardItem> _items = [];
    private readonly int _capacity;

    public ClipboardHistoryService(int capacity = 50)
    {
        _capacity = Math.Max(1, capacity);
    }

    public void Add(ClipboardItem item)
    {
        _items.AddFirst(item);
        while (_items.Count > _capacity)
        {
            _items.RemoveLast();
        }
    }

    public IReadOnlyList<ClipboardItem> GetAll() => _items.ToList();
}
