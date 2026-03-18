namespace Texty.Core.Models;

public sealed record InsertionPayload(
    string PlainText,
    string? HtmlText,
    IReadOnlyList<string> PostActions);

public sealed record InsertionContext(
    string? TargetProcess,
    bool IsEmail,
    bool IsTextFile,
    IReadOnlyDictionary<string, string>? Variables);

public enum InsertionStep
{
    Prepare,
    Insert,
    Restore,
    PostProcess
}

public sealed record InsertionStepResult(InsertionStep Step, bool Success, string? Details = null);
