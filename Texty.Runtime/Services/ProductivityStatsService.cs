namespace Texty.Runtime.Services;

public sealed class ProductivityStatsService
{
    private long _insertions;
    private long _estimatedSecondsSaved;

    public void TrackInsertion(int estimatedManualSeconds = 10)
    {
        Interlocked.Increment(ref _insertions);
        Interlocked.Add(ref _estimatedSecondsSaved, estimatedManualSeconds);
    }

    public (long Insertions, long SecondsSaved) Snapshot()
    {
        return (Interlocked.Read(ref _insertions), Interlocked.Read(ref _estimatedSecondsSaved));
    }
}
