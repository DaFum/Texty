using Texty.Core.Models;

namespace Texty.App.ViewModels.Models;

public sealed record VersionEntryItemModel(VersionEntry Source)
{
    public Guid Id => Source.Id;
    public int VersionNumber => Source.VersionNumber;
    public string CreatedText => Source.CreatedUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
    public string Editor => string.IsNullOrWhiteSpace(Source.CreatedBy) ? "-" : Source.CreatedBy!;
    public string Display => $"v{VersionNumber} | {CreatedText} | {Editor}";
}
