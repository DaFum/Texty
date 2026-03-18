using System.Text.Json;
using System.Text.Json.Serialization;

namespace Texty.Storage.Json;

internal static class JsonSerializerDefaults
{
    public static readonly JsonSerializerOptions Options = Create();

    /// <summary>
    /// Erzeugt eine vorkonfigurierte JsonSerializerOptions-Instanz.
    /// </summary>
    /// <returns>Eine JsonSerializerOptions-Instanz mit WriteIndented aktiviert, PropertyNameCaseInsensitive aktiviert und einem angefügten JsonStringEnumConverter.</returns>
    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
        };

        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
