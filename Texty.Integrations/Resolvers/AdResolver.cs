using System.DirectoryServices.Protocols;
using System.Diagnostics;
using System.Net;
using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Integrations.Resolvers;

public sealed class AdResolver : IExternalDataResolver
{
    private readonly IAuditLogger? _auditLogger;

    public AdResolver(IAuditLogger? auditLogger = null)
    {
        _auditLogger = auditLogger;
    }

    public string Name => "ad";

    public async Task<string?> ResolveAsync(ExternalValueRequest request, CancellationToken cancellationToken = default)
    {
        var correlationId = Guid.NewGuid().ToString("N");
        var parts = request.Expression.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 4)
        {
            await WriteAuditAsync(correlationId, false, AuditDecision.Error, "AD expression is invalid.", cancellationToken);
            return null;
        }

        var server = ResolveServer(parts[0]);
        var baseDn = parts[1];
        var filter = parts[2];
        var attribute = parts[3];

        if (string.IsNullOrWhiteSpace(server) ||
            string.IsNullOrWhiteSpace(baseDn) ||
            string.IsNullOrWhiteSpace(filter) ||
            string.IsNullOrWhiteSpace(attribute))
        {
            await WriteAuditAsync(correlationId, false, AuditDecision.Error, "AD expression contains empty required tokens.", cancellationToken);
            return null;
        }

        var credential = ResolveCredential(parts);

        try
        {
            var identifier = new LdapDirectoryIdentifier(server);
            using var connection = credential is null
                ? new LdapConnection(identifier)
                : new LdapConnection(identifier, credential, AuthType.Basic);

            connection.SessionOptions.ProtocolVersion = 3;
            connection.Timeout = TimeSpan.FromSeconds(10);

            var searchRequest = new SearchRequest(baseDn, filter, SearchScope.Subtree, attribute);
            var response = await Task.Run(
                () => (SearchResponse)connection.SendRequest(searchRequest),
                cancellationToken);

            var entry = response.Entries.Cast<SearchResultEntry>().FirstOrDefault();
            if (entry is null)
            {
                await WriteAuditAsync(correlationId, false, AuditDecision.Error, "AD query returned no entry.", cancellationToken);
                return null;
            }

            var values = entry.Attributes[attribute]?.GetValues(typeof(string));
            var value = values is null || values.Length == 0
                ? null
                : Convert.ToString(values[0], System.Globalization.CultureInfo.InvariantCulture);
            await WriteAuditAsync(
                correlationId,
                value is not null,
                value is not null ? AuditDecision.Allow : AuditDecision.Error,
                value is not null ? "AD value resolved." : "AD attribute has no values.",
                cancellationToken);
            return value;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await WriteAuditAsync(correlationId, false, AuditDecision.Error, "AD resolve canceled.", CancellationToken.None);
            throw;
        }
        catch (Exception ex)
        {
            Trace.TraceWarning($"AdResolver failed for server '{server}': {ex.Message}");
            await WriteAuditAsync(correlationId, false, AuditDecision.Error, ex.Message, cancellationToken);
            return null;
        }
    }

    private static string ResolveServer(string token)
    {
        if (!string.IsNullOrWhiteSpace(token) && !token.Equals("default", StringComparison.OrdinalIgnoreCase))
        {
            return token;
        }

        return Environment.GetEnvironmentVariable("USERDNSDOMAIN")
            ?? Environment.GetEnvironmentVariable("USERDOMAIN")
            ?? string.Empty;
    }

    private static NetworkCredential? ResolveCredential(IReadOnlyList<string> parts)
    {
        if (parts.Count >= 6)
        {
            return new NetworkCredential(parts[4], parts[5]);
        }

        return null;
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
                "resolver.ad",
                decision,
                correlationId,
                success,
                message),
            cancellationToken);
    }
}
