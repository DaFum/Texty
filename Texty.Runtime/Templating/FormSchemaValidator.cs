using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Runtime.Templating;

public sealed class FormSchemaValidator : IFormSchemaValidator
{
    /// <summary>
    /// Validiert die Felddefinitionen einer SnippetTemplate auf Schemafehler.
    /// </summary>
    /// <param name="template">Die SnippetTemplate, deren Felder überprüft werden.</param>
    /// <returns>Eine Liste mit Fehlermeldungen für gefundene Probleme; leer, wenn keine Fehler vorhanden sind.</returns>
    public IReadOnlyList<string> Validate(SnippetTemplate template)
    {
        var errors = new List<string>();
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var field in template.Fields)
        {
            if (string.IsNullOrWhiteSpace(field.Key))
            {
                errors.Add("Field key must not be empty.");
                continue;
            }

            if (!keys.Add(field.Key))
            {
                errors.Add($"Duplicate field key '{field.Key}'.");
            }

            if (field.FieldType is FormFieldType.Dropdown or FormFieldType.Radio &&
                (field.Options is null || field.Options.Count == 0))
            {
                errors.Add($"Field '{field.Key}' requires options.");
            }

            if (field.FieldType == FormFieldType.Slider &&
                field.Min is not null &&
                field.Max is not null &&
                field.Min > field.Max)
            {
                errors.Add($"Field '{field.Key}' has Min > Max.");
            }
        }

        return errors;
    }
}
