using Texty.Core.Models;

namespace Texty.App.ViewModels.Models;

public sealed record TemplateFieldDesignerItemModel(
    string Key,
    string Label,
    FormFieldType FieldType,
    bool Required,
    string Placeholder,
    string Min,
    string Max,
    string DefaultValue,
    string Options)
{
    public string Display =>
        $"{Label} ({Key}) | {FieldType} | {(Required ? "required" : "optional")}";

    public TemplateField ToTemplateField()
    {
        return new TemplateField(
            Key.Trim(),
            string.IsNullOrWhiteSpace(Label) ? Key.Trim() : Label.Trim(),
            FieldType,
            Required,
            string.IsNullOrWhiteSpace(Placeholder) ? null : Placeholder.Trim(),
            ParseNullableDouble(Min),
            ParseNullableDouble(Max),
            ParseOptions(Options),
            string.IsNullOrWhiteSpace(DefaultValue) ? null : DefaultValue.Trim());
    }

    public static TemplateFieldDesignerItemModel FromTemplateField(TemplateField field)
    {
        return new TemplateFieldDesignerItemModel(
            field.Key,
            field.Label,
            field.FieldType,
            field.Required,
            field.Placeholder ?? string.Empty,
            field.Min?.ToString() ?? string.Empty,
            field.Max?.ToString() ?? string.Empty,
            field.DefaultValue ?? string.Empty,
            SerializeOptions(field.Options));
    }

    private static double? ParseNullableDouble(string value)
    {
        return double.TryParse(value, out var parsed) ? parsed : null;
    }

    private static IReadOnlyList<TemplateFieldOption>? ParseOptions(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var parts = value
            .Split([';', '\n', '\r'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .ToList();
        if (parts.Count == 0)
        {
            return null;
        }

        var options = new List<TemplateFieldOption>(parts.Count);
        foreach (var part in parts)
        {
            var split = part.Split(':', 2, StringSplitOptions.TrimEntries);
            var key = split[0];
            var label = split.Length > 1 ? split[1] : split[0];
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            options.Add(new TemplateFieldOption(key, label));
        }

        return options.Count > 0 ? options : null;
    }

    private static string SerializeOptions(IReadOnlyList<TemplateFieldOption>? options)
    {
        if (options is null || options.Count == 0)
        {
            return string.Empty;
        }

        return string.Join(
            "; ",
            options.Select(option =>
                string.Equals(option.Key, option.Label, StringComparison.Ordinal)
                    ? option.Key
                    : $"{option.Key}:{option.Label}"));
    }
}

