using ASMED.EDM.Core.Entities;

namespace ASMED.EDM.Core.Interfaces.Repositories;

/// <summary>
/// Repozytorium badań medycznych — tabela Access: Badanie
/// </summary>
public interface IBadanieRepository : IRepository<Badanie>
{
    /// <summary>
    /// Pobiera badania dostępne do fakturowania (niezapisane na liście).
    /// Access: WHERE Bad_L_ID IS NULL OR Bad_L_ID = 0
    /// </summary>
    Task<IEnumerable<Badanie>> GetAvailableBadaniaAsync(int? firmaId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera badania przypisane do wskazanej listy badań — Access: Bad_L_ID = ?
    /// </summary>
    Task<IEnumerable<Badanie>> GetByListaBadanAsync(int listaBadanId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera badania dla danego pacjenta wraz z danymi pacjenta
    /// </summary>
    Task<IEnumerable<Badanie>> GetByPatientAsync(int patientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera badanie z pełnymi danymi (pacjent, lista, faktura)
    /// </summary>
    Task<Badanie?> GetWithDetailsAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Przypisuje badania do listy — ustawia Bad_L_ID dla wskazanych ID badań
    /// </summary>
    Task AssignToListaAsync(IEnumerable<int> badanieIds, int listaBadanId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Odpina badania od listy i faktury — Access: Bad_L_ID = 0, Bad_F_ID = 0
    /// </summary>
    Task UnassignFromListaAsync(IEnumerable<int> badanieIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Aktualizuje FK do faktury dla badań na liście — Access: Bad_F_ID = ?
    /// </summary>
    Task SetFakturaIdAsync(int listaBadanId, int fakturaId, CancellationToken cancellationToken = default);
}
