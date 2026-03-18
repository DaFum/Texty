namespace Texty.Core.Models;

public sealed record TemplateFieldOption(string Key, string Label);

public sealed record TemplateField(
    string Key,
    string Label,
    FormFieldType FieldType,
    bool Required,
    string? Placeholder,
    double? Min,
    double? Max,
    IReadOnlyList<TemplateFieldOption>? Options,
    string? DefaultValue);

public sealed record SnippetTemplate(
    string BodyTemplate,
    IReadOnlyList<TemplateField> Fields);
