namespace Texty.AI.Providers;

public sealed class Gpt4AllProvider : ApiKeyHttpAiProvider
{
    public Gpt4AllProvider(HttpClient httpClient)
        : base(
            httpClient,
            apiKeyEnvironmentVariable: null,
            endpoint: "http://localhost:4891/v1/chat/completions",
            endpointEnvironmentVariable: "GPT4ALL_ENDPOINT",
            requireApiKey: false)
    {
    }

    public override string Name => "GPT4All";

    protected override string DefaultModel =>
        Environment.GetEnvironmentVariable("GPT4ALL_MODEL")?.Trim() ?? "gpt4all";

    protected override string ResolveEndpoint()
    {
        var explicitEndpoint = Environment.GetEnvironmentVariable("GPT4ALL_ENDPOINT")?.Trim();
        if (!string.IsNullOrWhiteSpace(explicitEndpoint))
        {
            return explicitEndpoint;
        }

        var baseUrl = Environment.GetEnvironmentVariable("GPT4ALL_BASE_URL")?.Trim();
        if (!string.IsNullOrWhiteSpace(baseUrl))
        {
            return $"{baseUrl.TrimEnd('/')}/v1/chat/completions";
        }

        return base.ResolveEndpoint();
    }

    protected override string HealthPath => "/models";
}
