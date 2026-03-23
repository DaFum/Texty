using Texty.Runtime.Bootstrap;
using System.Diagnostics;

namespace Texty.App.Services;

public static class AppRuntimeService
{
    private static readonly SemaphoreSlim InitLock = new(1, 1);
    private static readonly object HotkeyRunnerSync = new();
    private static CancellationTokenSource? _hotkeyRunnerCts;
    private static Task? _hotkeyRunnerTask;
    private static bool _processExitHookRegistered;

    public static TextyRuntimeContext? RuntimeContext { get; private set; }

    public static async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (RuntimeContext is not null)
        {
            EnsureHotkeyRunner(RuntimeContext);
            return;
        }

        await InitLock.WaitAsync(cancellationToken);
        try
        {
            if (RuntimeContext is null)
            {
                RuntimeContext = await TextyRuntimeBootstrap.CreateDefaultAsync(cancellationToken: cancellationToken);
            }

            if (RuntimeContext is not null)
            {
                EnsureHotkeyRunner(RuntimeContext);
            }
        }
        finally
        {
            InitLock.Release();
        }
    }

    private static void EnsureHotkeyRunner(TextyRuntimeContext context)
    {
        lock (HotkeyRunnerSync)
        {
            if (_hotkeyRunnerTask is { IsCompleted: true })
            {
                _hotkeyRunnerTask = null;
                _hotkeyRunnerCts?.Dispose();
                _hotkeyRunnerCts = null;
            }

            if (_hotkeyRunnerTask is not null)
            {
                return;
            }

            _hotkeyRunnerCts = new CancellationTokenSource();
            _hotkeyRunnerTask = Task.Run(
                async () =>
                {
                    try
                    {
                        await context.HotkeyInsertionService.RunAsync(_hotkeyRunnerCts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected on shutdown/cancel.
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError($"Hotkey runner terminated unexpectedly: {ex}");
                    }
                    finally
                    {
                        lock (HotkeyRunnerSync)
                        {
                            _hotkeyRunnerTask = null;
                            _hotkeyRunnerCts?.Dispose();
                            _hotkeyRunnerCts = null;
                        }
                    }
                },
                _hotkeyRunnerCts.Token);

            if (_processExitHookRegistered)
            {
                return;
            }

            AppDomain.CurrentDomain.ProcessExit += (_, _) =>
            {
                try
                {
                    _hotkeyRunnerCts?.Cancel();
                    _hotkeyRunnerTask?.Wait(TimeSpan.FromMilliseconds(500));
                    if (RuntimeContext is IDisposable disposableRuntime)
                    {
                        disposableRuntime.Dispose();
                        RuntimeContext = null;
                    }
                }
                catch
                {
                    // Ignore shutdown cancellation issues.
                }
            };
            _processExitHookRegistered = true;
        }
    }
}
