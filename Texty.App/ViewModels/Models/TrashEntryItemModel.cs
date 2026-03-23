using Texty.Core.Models;

namespace Texty.App.ViewModels.Models;

public sealed record TrashEntryItemModel(TrashEntry Source)
{
    public Guid Id => Source.Id;
    public Guid SnippetId => Source.Snapshot.Id;
    public string Title => Source.Snapshot.Title;
    public string DeletedBy => string.IsNullOrWhiteSpace(Source.DeletedBy) ? "-" : Source.DeletedBy;
    public string DeletedAtText => Source.DeletedUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
    public string Display => $"{Title} | {DeletedAtText} | {DeletedBy}";
}
