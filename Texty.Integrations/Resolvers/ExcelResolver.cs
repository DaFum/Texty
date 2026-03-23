using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Diagnostics;
using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Integrations.Resolvers;

public sealed class ExcelResolver : IExternalDataResolver
{
    private readonly IAuditLogger? _auditLogger;
    private readonly CsvResolver _csvFallback;

    public ExcelResolver(IAuditLogger? auditLogger = null)
    {
        _auditLogger = auditLogger;
        _csvFallback = new CsvResolver(auditLogger);
    }

    public string Name => "excel";

    public async Task<string?> ResolveAsync(ExternalValueRequest request, CancellationToken cancellationToken = default)
    {
        var correlationId = Guid.NewGuid().ToString("N");
        var parts = request.Expression.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            await WriteAuditAsync(correlationId, false, AuditDecision.Error, "Excel expression is invalid.", cancellationToken);
            return null;
        }

        var path = parts[0];
        if (!File.Exists(path))
        {
            await WriteAuditAsync(correlationId, false, AuditDecision.Error, $"Excel source not found: {path}", cancellationToken);
            return null;
        }

        var extension = Path.GetExtension(path);
        if (extension.Equals(".csv", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".txt", StringComparison.OrdinalIgnoreCase))
        {
            return await _csvFallback.ResolveAsync(request, cancellationToken);
        }

        cancellationToken.ThrowIfCancellationRequested();

        string? sheetSelector;
        string cellReference;
        if (parts.Length == 2)
        {
            sheetSelector = null;
            cellReference = parts[1];
        }
        else
        {
            sheetSelector = parts[1];
            cellReference = parts[2];
        }

        if (!IsCellReference(cellReference))
        {
            await WriteAuditAsync(correlationId, false, AuditDecision.Error, "Excel cell reference is invalid.", cancellationToken);
            return null;
        }

        try
        {
            await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, useAsync: true);
            using var document = SpreadsheetDocument.Open(stream, false);

            var workbookPart = document.WorkbookPart;
            if (workbookPart?.Workbook.Sheets is null)
            {
                await WriteAuditAsync(correlationId, false, AuditDecision.Error, "Workbook has no sheets.", cancellationToken);
                return null;
            }

            var worksheetPart = ResolveWorksheetPart(workbookPart, sheetSelector);
            if (worksheetPart?.Worksheet is null)
            {
                await WriteAuditAsync(correlationId, false, AuditDecision.Error, "Worksheet not found.", cancellationToken);
                return null;
            }

            var cell = worksheetPart.Worksheet.Descendants<Cell>()
                .FirstOrDefault(c => string.Equals(c.CellReference?.Value, cellReference, StringComparison.OrdinalIgnoreCase));
            if (cell is null)
            {
                await WriteAuditAsync(correlationId, false, AuditDecision.Error, "Cell not found.", cancellationToken);
                return null;
            }

            cancellationToken.ThrowIfCancellationRequested();
            var value = ResolveCellValue(cell, workbookPart);
            await WriteAuditAsync(
                correlationId,
                value is not null,
                value is not null ? AuditDecision.Allow : AuditDecision.Error,
                value is not null ? "Excel value resolved." : "Excel cell resolved to null.",
                cancellationToken);
            return value;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await WriteAuditAsync(correlationId, false, AuditDecision.Error, "Excel resolve canceled.", CancellationToken.None);
            throw;
        }
        catch (Exception ex)
        {
            Trace.TraceWarning($"ExcelResolver failed for '{path}': {ex.Message}");
            await WriteAuditAsync(correlationId, false, AuditDecision.Error, ex.Message, cancellationToken);
            return null;
        }
    }

    private static WorksheetPart? ResolveWorksheetPart(WorkbookPart workbookPart, string? sheetSelector)
    {
        var sheets = workbookPart.Workbook.Sheets?.Elements<Sheet>().ToList() ?? [];
        if (sheets.Count == 0)
        {
            return null;
        }

        Sheet? sheet;
        if (string.IsNullOrWhiteSpace(sheetSelector))
        {
            sheet = sheets[0];
        }
        else if (int.TryParse(sheetSelector, out var index) && index > 0 && index <= sheets.Count)
        {
            sheet = sheets[index - 1];
        }
        else
        {
            sheet = sheets.FirstOrDefault(s => string.Equals(s.Name?.Value, sheetSelector, StringComparison.OrdinalIgnoreCase));
        }

        if (sheet?.Id is null)
        {
            return null;
        }

        var relationId = sheet.Id?.Value;
        if (string.IsNullOrWhiteSpace(relationId))
        {
            return null;
        }

        return workbookPart.GetPartById(relationId) as WorksheetPart;
    }

    private static string? ResolveCellValue(Cell cell, WorkbookPart workbookPart)
    {
        if (cell.CellValue is null && cell.InlineString is null)
        {
            return string.Empty;
        }

        var raw = cell.CellValue?.Text ?? cell.InnerText;
        if (cell.DataType is null)
        {
            return raw;
        }

        var dataType = cell.DataType.Value;
        if (dataType == CellValues.SharedString)
        {
            return ResolveSharedString(raw, workbookPart);
        }

        if (dataType == CellValues.Boolean)
        {
            return raw == "1" ? "true" : "false";
        }

        if (dataType == CellValues.InlineString)
        {
            return cell.InnerText;
        }

        return raw;
    }

    private static string? ResolveSharedString(string? raw, WorkbookPart workbookPart)
    {
        if (!int.TryParse(raw, out var index))
        {
            return raw;
        }

        var table = workbookPart.SharedStringTablePart?.SharedStringTable;
        var sharedItem = table?.Elements<SharedStringItem>().ElementAtOrDefault(index);
        return sharedItem?.InnerText;
    }

    private static bool IsCellReference(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var i = 0;
        while (i < value.Length && char.IsLetter(value[i]))
        {
            i++;
        }

        if (i == 0 || i == value.Length)
        {
            return false;
        }

        return value[i..].All(char.IsDigit);
    }

    private Task WriteAuditAsync(
        string correlationId,
        bool success,
        AuditDecision decision,
        string message,
        CancellationToken cancellationToken)
    {
        if (_auditLogger is null)
        {
            return Task.CompletedTask;
        }

        return _auditLogger.WriteAsync(
            new AuditLogEntry(
                DateTimeOffset.UtcNow,
                AuditCategory.Resolver,
                "resolver.excel",
                decision,
                correlationId,
                success,
                message),
            cancellationToken);
    }
}
