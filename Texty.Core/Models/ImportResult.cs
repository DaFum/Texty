namespace Texty.Core.Models;

public sealed record ImportResult(int ImportedSnippets, IReadOnlyList<string> Warnings);
