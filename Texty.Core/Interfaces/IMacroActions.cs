namespace Texty.Core.Interfaces;

using Texty.Core.Models;

public interface IMacroActionExecutor
{
    Task<MacroActionResult> ExecuteAsync(MacroActionRequest request, CancellationToken cancellationToken = default);
}
