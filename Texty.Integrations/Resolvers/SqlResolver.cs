using Microsoft.Data.SqlClient;
using System.Diagnostics;
using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Integrations.Resolvers;

public sealed class SqlResolver : IExternalDataResolver
{
    private readonly IAuditLogger? _auditLogger;

    public SqlResolver(IAuditLogger? auditLogger = null)
    {
        _auditLogger = auditLogger;
    }

    public string Name => "sql";

    public async Task<string?> ResolveAsync(ExternalValueRequest request, CancellationToken cancellationToken = default)
    {
        var correlationId = Guid.NewGuid().ToString("N");
        var parts = request.Expression.Split('|', 3, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            await WriteAuditAsync(correlationId, false, AuditDecision.Error, "SQL expression is invalid.", cancellationToken);
            return null;
        }

        var connectionString = ResolveConnectionString(parts[0]);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            await WriteAuditAsync(correlationId, false, AuditDecision.Deny, "SQL connection string missing or unresolved.", cancellationToken);
            return null;
        }

        var sql = parts[1];
        var parameterExpression = parts.Length == 3 ? parts[2] : null;

        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandType = System.Data.CommandType.Text;
            command.CommandTimeout = 30;

            foreach (var parameter in ParseParameters(parameterExpression))
            {
                var sqlParameter = command.Parameters.AddWithValue(parameter.Key, (object?)parameter.Value ?? DBNull.Value);
                sqlParameter.IsNullable = true;
            }

            var result = await command.ExecuteScalarAsync(cancellationToken);
            if (result is null || result == DBNull.Value)
            {
                await WriteAuditAsync(correlationId, false, AuditDecision.Error, "SQL query returned null.", cancellationToken);
                return null;
            }

            var value = Convert.ToString(result, System.Globalization.CultureInfo.InvariantCulture);
            await WriteAuditAsync(correlationId, true, AuditDecision.Allow, "SQL value resolved.", cancellationToken);
            return value;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await WriteAuditAsync(correlationId, false, AuditDecision.Error, "SQL resolve canceled.", CancellationToken.None);
            throw;
        }
        catch (Exception ex)
        {
            Trace.TraceWarning($"SqlResolver failed: {ex.Message}");
            await WriteAuditAsync(correlationId, false, AuditDecision.Error, ex.Message, cancellationToken);
            return null;
        }
    }

    private static IEnumerable<KeyValuePair<string, string>> ParseParameters(string? expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            yield break;
        }

        var segments = expression.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        foreach (var segment in segments)
        {
            var pair = segment.Split('=', 2, StringSplitOptions.TrimEntries);
            if (pair.Length != 2)
            {
                continue;
            }

            var key = pair[0];
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            var normalizedKey = key.StartsWith("@", StringComparison.Ordinal) ? key : $"@{key}";
            yield return new KeyValuePair<string, string>(normalizedKey, pair[1]);
        }
    }

    private static string? ResolveConnectionString(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        if (token.StartsWith("env:", StringComparison.OrdinalIgnoreCase))
        {
            return Environment.GetEnvironmentVariable(token[4..]);
        }

        if (token.Contains('=') && token.Contains(';'))
        {
            return token;
        }

        return Environment.GetEnvironmentVariable(token);
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
                "resolver.sql",
                decision,
                correlationId,
                success,
                message),
            cancellationToken);
    }
}
