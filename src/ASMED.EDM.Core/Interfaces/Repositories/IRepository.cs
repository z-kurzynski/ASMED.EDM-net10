using System.Linq.Expressions;

namespace ASMED.EDM.Core.Interfaces.Repositories;

/// <summary>
/// Generyczny interfejs repozytorium dla operacji CRUD
/// </summary>
/// <typeparam name="T">Typ encji</typeparam>
public interface IRepository<T> where T : class
{
    // Queries
    /// <summary>
    /// Pobiera encję po ID
    /// </summary>
    Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera wszystkie encje
    /// </summary>
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera encje spełniające warunek
    /// </summary>
    Task<IEnumerable<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera pierwszą encję spełniającą warunek lub null
    /// </summary>
    Task<T?> FirstOrDefaultAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sprawdza czy istnieje encja spełniająca warunek
    /// </summary>
    Task<bool> AnyAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera liczbę encji spełniających warunek
    /// </summary>
    Task<int> CountAsync(
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera encje z paginacją
    /// </summary>
    Task<IEnumerable<T>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    // Commands
    /// <summary>
    /// Dodaje nową encję
    /// </summary>
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dodaje wiele encji
    /// </summary>
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Aktualizuje encję
    /// </summary>
    void Update(T entity);

    /// <summary>
    /// Aktualizuje wiele encji
    /// </summary>
    void UpdateRange(IEnumerable<T> entities);

    /// <summary>
    /// Usuwa encję (hard delete)
    /// </summary>
    void Remove(T entity);

    /// <summary>
    /// Usuwa wiele encji (hard delete)
    /// </summary>
    void RemoveRange(IEnumerable<T> entities);

    /// <summary>
    /// Soft delete - oznacza encję jako usuniętą bez fizycznego usuwania
    /// </summary>
    Task SoftDeleteAsync(int id, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Przywraca soft-deleted encję
    /// </summary>
    Task RestoreAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera wszystkie encje włącznie z usuniętymi (soft delete)
    /// </summary>
    Task<IEnumerable<T>> GetAllIncludingDeletedAsync(CancellationToken cancellationToken = default);
}
