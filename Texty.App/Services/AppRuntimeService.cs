using Texty.Runtime.Bootstrap;

namespace Texty.App.Services;

public static class AppRuntimeService
{
    private static readonly SemaphoreSlim InitLock = new(1, 1);
    public static TextyRuntimeContext? RuntimeContext { get; private set; }

    /// <summary>
    /// Initialisiert den gemeinsamen RuntimeContext einmalig und thread-sicher.
    /// </summary>
    /// <param name="cancellationToken">Abbruchtoken, das sowohl das Warten auf die Initialisierungs-Sperre als auch den Bootstrap-Vorgang abbrechen kann.</param>
    /// <returns>Eine Task, die abgeschlossen ist, wenn der RuntimeContext erstellt wurde oder bereits vorhanden war.</returns>
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
