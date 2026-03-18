namespace Texty.Runtime.Services;

public sealed class ProductivityStatsService
{
    private long _insertions;
    private long _estimatedSecondsSaved;

    /// <summary>
    /// Erfasst eine einzelne Einfügung und akkumuliert die geschätzten manuell eingesparten Sekunden.
    /// </summary>
    /// <param name="estimatedManualSeconds">Geschätzte Anzahl an Sekunden, die durch diese Einfügung manuell eingespart wurden (Standard: 10).</param>
    public void TrackInsertion(int estimatedManualSeconds = 10)
    {
        Interlocked.Increment(ref _insertions);
        Interlocked.Add(ref _estimatedSecondsSaved, estimatedManualSeconds);
    }

    /// <summary>
    /// Erstellt einen konsistenten Schnappschuss der aktuellen Zählerwerte.
    /// </summary>
    /// <returns>`Insertions`: Gesamtzahl der erfassten Einfügungen; `SecondsSaved`: aufsummierte geschätzte Sekunden, die durch diese Einfügungen eingespart wurden.</returns>
    public (long Insertions, long SecondsSaved) Snapshot()
    {
        return (Interlocked.Read(ref _insertions), Interlocked.Read(ref _estimatedSecondsSaved));
    }
}
