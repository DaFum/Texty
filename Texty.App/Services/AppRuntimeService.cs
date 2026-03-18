using Texty.Runtime.Bootstrap;

namespace Texty.App.Services;

public static class AppRuntimeService
{
    private static readonly SemaphoreSlim InitLock = new(1, 1);
    public static TextyRuntimeContext? RuntimeContext { get; private set; }

    public static async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (RuntimeContext is not null)
        {
            return;
        }

        await InitLock.WaitAsync(cancellationToken);
        try
        {
            if (RuntimeContext is null)
            {
                RuntimeContext = await TextyRuntimeBootstrap.CreateDefaultAsync(cancellationToken: cancellationToken);
            }
        }
        finally
        {
            InitLock.Release();
        }
    }
}
