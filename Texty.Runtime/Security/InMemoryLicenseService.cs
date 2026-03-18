using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Runtime.Security;

public sealed class InMemoryLicenseService : ILicenseService
{
    private readonly LicenseState _state;

    /// <summary>
    /// Erstellt einen InMemoryLicenseService und initialisiert den internen Lizenzstatus.
    /// </summary>
    /// <remarks>
    /// Der interne LicenseState wird mit IsLicensed = true, LicenseKey = null, Seats = 1 und dem übergebenen Tier initialisiert.
    /// </remarks>
    /// <param name="tier">Die Lizenzstufe (z. B. "SingleUser"), die im internen Lizenzstatus gesetzt wird.</param>
    public InMemoryLicenseService(string tier = "SingleUser")
    {
        _state = new LicenseState(true, tier, null, 1);
    }

    /// <summary>
        /// Gibt den aktuell im Dienst gehaltenen Lizenzzustand zurück.
        /// </summary>
        /// <returns>Der aktuelle <see cref="LicenseState"/> des Dienstes.</returns>
        public Task<LicenseState> GetCurrentAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_state);

    /// <summary>
        /// Gibt an, ob die aktuelle In-Memory-Lizenz als gültig markiert ist.
        /// </summary>
        /// <returns>`true`, wenn die aktuelle In-Memory-Lizenz als gültig markiert ist, `false` sonst.</returns>
        public Task<bool> ValidateAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_state.IsLicensed);
}
