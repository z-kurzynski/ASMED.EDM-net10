using ASMED.EDM.Core.Entities;

namespace ASMED.EDM.Core.Interfaces.Repositories;

/// <summary>
/// Repozytorium firm (pracodawców) — tabela Access: Firma
/// </summary>
public interface IFirmaRepository : IRepository<Firma>
{
    /// <summary>
    /// Pobiera aktywne firmy, opcjonalnie filtrowane po nazwie lub NIP
    /// </summary>
    Task<IEnumerable<Firma>> GetActiveFirmyAsync(string? searchQuery = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera firmę po dokładnej nazwie (trim, case-insensitive)
    /// </summary>
    Task<Firma?> GetByNazwaAsync(string nazwa, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera firmę po NIP
    /// </summary>
    Task<Firma?> GetByNIPAsync(string nip, CancellationToken cancellationToken = default);
}
