namespace Texty.Core.Interfaces;

using Texty.Core.Models;

public interface IAiProvider
{
    string Name { get; }
    /// <summary>
/// Erzeugt eine KI-Antwort basierend auf den Angaben im <see cref="AiRequest"/>.
/// </summary>
/// <param name="request">Eingabe, Optionen und Metadaten für die Generierung.</param>
/// <param name="cancellationToken">Token, mit dem die asynchrone Operation abgebrochen werden kann.</param>
/// <returns>Ein <see cref="AiResponse"/>-Objekt mit dem generierten Inhalt, zugehörigen Metadaten und Statusinformationen.</returns>
Task<AiResponse> GenerateAsync(AiRequest request, CancellationToken cancellationToken = default);
}

public interface ITranslationProvider
{
    string Name { get; }
    /// <summary>
/// Erstellt eine Übersetzung basierend auf der übergebenen Anfrage.
/// </summary>
/// <param name="request">Die Eingabe- und Konfigurationsdaten für die Übersetzungsanfrage.</param>
/// <param name="cancellationToken">Token zur Abbruchsteuerung der asynchronen Operation.</param>
/// <returns>Das Ergebnis der Übersetzung als <see cref="TranslationResult"/>.</returns>
Task<TranslationResult> TranslateAsync(TranslationRequest request, CancellationToken cancellationToken = default);
}
