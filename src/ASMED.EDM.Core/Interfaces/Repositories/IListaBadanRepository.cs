using ASMED.EDM.Core.Entities;

namespace ASMED.EDM.Core.Interfaces.Repositories;

/// <summary>
/// Repozytorium list badań do fakturowania — tabela Access: Listy_Badan
/// </summary>
public interface IListaBadanRepository : IRepository<ListaBadan>
{
    /// <summary>
    /// Pobiera listę badań wraz z przypisanymi badaniami i danymi pacjentów
    /// </summary>
    Task<ListaBadan?> GetWithBadaniaAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera listy badań dla danej firmy
    /// </summary>
    Task<IEnumerable<ListaBadan>> GetByFirmaAsync(int firmaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera listy jeszcze niezafakturowane (FakturaId == null)
    /// </summary>
    Task<IEnumerable<ListaBadan>> GetNiezafakturowanieAsync(CancellationToken cancellationToken = default);
}
