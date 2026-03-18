namespace Texty.Core.Interfaces;

using Texty.Core.Models;

public interface ITriggerProvider
{
    string Name { get; }
    /// <summary>
/// Stellt einen fortlaufenden Stream von Trigger-Signalen zur Verfügung, die beim Eintreten ausgeliefert werden.
/// </summary>
/// <param name="cancellationToken">Token zum Abbrechen des Abhörvorgangs.</param>
/// <returns>Eine Auflistung, die nacheinander empfangene TriggerSignal-Ereignisse liefert.</returns>
IAsyncEnumerable<TriggerSignal> ListenAsync(CancellationToken cancellationToken = default);
}

public interface ITriggerEvaluator
{
    /// <summary>
        /// Ermittelt alle Trigger-Treffer, die mit dem übergebenen Signal übereinstimmen.
        /// </summary>
        /// <param name="rules">Die zu evaluierenden Trigger-Regeln.</param>
        /// <param name="signal">Das zu prüfende Trigger-Signal.</param>
        /// <param name="cancellationToken">Token zum Abbrechen der Auswertung.</param>
        /// <returns>Eine readonly-Liste von TriggerMatch-Objekten, die die gefundenen Treffer enthält; leer, wenn keine Treffer vorhanden sind.</returns>
        Task<IReadOnlyList<TriggerMatch>> EvaluateAsync(
        IEnumerable<TriggerRule> rules,
        TriggerSignal signal,
        CancellationToken cancellationToken = default);
}
