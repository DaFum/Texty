namespace Texty.Core.Interfaces;

using Texty.Core.Models;

public interface IDslFunctionLibrary
{
    bool TryInvoke(string functionName, IReadOnlyList<string> args, MacroExecutionContext context, out string? result);
}

public interface IMacroEngine
{
    MacroExecutionResult Execute(string script, MacroExecutionContext context);
}

public interface IPowerShellActionRunner
{
    Task<PowerShellExecutionResult> ExecuteAsync(
        string script,
        PowerShellExecutionPolicy policy,
        CancellationToken cancellationToken = default);
}
