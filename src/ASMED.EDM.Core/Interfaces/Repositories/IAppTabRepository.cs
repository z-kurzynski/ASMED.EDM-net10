using ASMED.EDM.Core.Entities;

namespace ASMED.EDM.Core.Interfaces.Repositories;

/// <summary>
/// Repozytorium definicji zakładek aplikacji (lista predefiniowana programistycznie).
/// </summary>
public interface IAppTabRepository
{
    /// <summary>
    /// Pobiera wszystkie zdefiniowane zakładki (główne i podzakładki), posortowane po SortOrder.
    /// </summary>
    Task<IEnumerable<AppTab>> GetAllTabsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera zakładkę po unikalnym kluczu.
    /// </summary>
    Task<AppTab?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
}
