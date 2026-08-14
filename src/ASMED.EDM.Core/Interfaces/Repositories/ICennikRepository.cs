using ASMED.EDM.Core.Entities;

namespace ASMED.EDM.Core.Interfaces.Repositories;

/// <summary>
/// Repozytorium cenników badań — tabela Access: BAD_Cennik
/// </summary>
public interface ICennikRepository : IRepository<Cennik>
{
    /// <summary>
    /// Pobiera aktywne cenniki — Access: WHERE b_activ = TRUE
    /// </summary>
    Task<IEnumerable<Cennik>> GetActiveCennikiAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera cennik po nazwie (trim, case-insensitive) — Access: b_Cennik
    /// </summary>
    Task<Cennik?> GetByNazwaAsync(string nazwa, CancellationToken cancellationToken = default);
}
