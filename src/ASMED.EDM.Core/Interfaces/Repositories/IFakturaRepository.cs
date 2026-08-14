using ASMED.EDM.Core.Entities;

namespace ASMED.EDM.Core.Interfaces.Repositories;

/// <summary>
/// Repozytorium faktur — tabela Access: Faktura
/// </summary>
public interface IFakturaRepository : IRepository<Faktura>
{
    /// <summary>
    /// Pobiera fakturę z relacją do firmy i listy badań
    /// </summary>
    Task<Faktura?> GetWithDetailsAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera faktury dla danej firmy — Access: FK_Firma_ID
    /// </summary>
    Task<IEnumerable<Faktura>> GetByFirmaAsync(int firmaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Szuka faktury po numerze — Access: TRIM(FK_Numer) = TRIM(?)
    /// </summary>
    Task<Faktura?> GetByNumerAsync(string numer, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera faktury w zakresie dat wystawienia
    /// </summary>
    Task<IEnumerable<Faktura>> GetByDataRangeAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);
}
