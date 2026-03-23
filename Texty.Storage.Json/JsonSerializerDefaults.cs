using System.Text.Json;
using System.Text.Json.Serialization;

namespace Texty.Storage.Json;

internal static class JsonSerializerDefaults
{
    public static readonly JsonSerializerOptions Options = Create();

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
