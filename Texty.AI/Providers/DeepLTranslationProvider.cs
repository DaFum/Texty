using System.Net.Http.Json;
using System.Text.Json;
using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.AI.Providers;

public sealed class DeepLTranslationProvider : ITranslationProvider
{
    private readonly HttpClient _httpClient;

    public DeepLTranslationProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public string Name => "DeepL";

    public async Task<TranslationResult> TranslateAsync(TranslationRequest request, CancellationToken cancellationToken = default)
    {
        var apiKey = Environment.GetEnvironmentVariable("DEEPL_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new TranslationResult(Name, $"[{request.TargetLanguage}] {request.Text}");
        }

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api-free.deepl.com/v2/translate")
        {
            Content = new FormUrlEncodedContent(
            [
                new KeyValuePair<string, string>("auth_key", apiKey),
                new KeyValuePair<string, string>("text", request.Text),
                new KeyValuePair<string, string>("source_lang", request.SourceLanguage.ToUpperInvariant()),
                new KeyValuePair<string, string>("target_lang", request.TargetLanguage.ToUpperInvariant()),
            ]),
        };

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return new TranslationResult(Name, $"[DeepL error] {response.StatusCode}: {body}");
        }

        using var json = JsonDocument.Parse(body);
        var text = json.RootElement.GetProperty("translations")[0].GetProperty("text").GetString() ?? request.Text;
        return new TranslationResult(Name, text);
    }
}
