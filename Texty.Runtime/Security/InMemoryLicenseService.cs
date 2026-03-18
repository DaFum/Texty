using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Runtime.Security;

public sealed class InMemoryLicenseService : ILicenseService
{
    private readonly LicenseState _state;

    public InMemoryLicenseService(string tier = "SingleUser")
    {
        _state = new LicenseState(true, tier, null, 1);
    }

    public Task<LicenseState> GetCurrentAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_state);

    public Task<bool> ValidateAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_state.IsLicensed);
}
