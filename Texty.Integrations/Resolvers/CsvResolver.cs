using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Integrations.Resolvers;

public sealed class CsvResolver : IExternalDataResolver
{
    public string Name => "csv";

    public async Task<string?> ResolveAsync(ExternalValueRequest request, CancellationToken cancellationToken = default)
    {
        var parts = request.Expression.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3)
        {
            return null;
        }

        if (!int.TryParse(parts[1], out var rowIndex) || !int.TryParse(parts[2], out var columnIndex))
        {
            return null;
        }

        var path = parts[0];
        if (!File.Exists(path))
        {
            return null;
        }

        var lines = await File.ReadAllLinesAsync(path, cancellationToken);
        if (rowIndex < 0 || rowIndex >= lines.Length)
        {
            return null;
        }

        var line = lines[rowIndex];
        var cells = line.Split([',', ';', '\t']);
        if (columnIndex < 0 || columnIndex >= cells.Length)
        {
            return null;
        }

        return cells[columnIndex];
    }
}
