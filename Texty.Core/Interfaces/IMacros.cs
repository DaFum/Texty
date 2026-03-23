namespace Texty.Core.Interfaces;

using Texty.Core.Models;

public interface IDslFunctionLibrary
{
    bool TryInvoke(string functionName, IReadOnlyList<string> args, MacroExecutionContext context, out string? result);
}

public interface IMacroEngine
{
    MacroExecutionResult Execute(string script, MacroExecutionContext context, CancellationToken cancellationToken = default);
}

public interface IPowerShellActionRunner
{
    /// <summary>
    /// Executes the provided PowerShell script under the supplied <see cref="PowerShellExecutionPolicy"/>.
    /// </summary>
    /// <param name="script">The PowerShell script text to execute.</param>
    /// <param name="policy">The effective trust policy used to allow or block execution.</param>
    /// <param name="cancellationToken">Cancellation token for cooperative cancellation.</param>
    /// <returns>
    /// A <see cref="PowerShellExecutionResult"/> describing execution outcome.
    /// When policy trust is denied, implementations should return a result with <c>Success = false</c>
    /// instead of throwing. Callers must inspect <see cref="PowerShellExecutionResult.Success"/> and must
    /// not rely on <see cref="PowerShellExecutionResult.Output"/> alone.
    /// </returns>
    /// <exception cref="OperationCanceledException">
    /// Thrown when <paramref name="cancellationToken"/> is canceled during execution.
    /// </exception>
    Task<PowerShellExecutionResult> ExecuteAsync(
        string script,
        PowerShellExecutionPolicy policy,
        CancellationToken cancellationToken = default);
}
