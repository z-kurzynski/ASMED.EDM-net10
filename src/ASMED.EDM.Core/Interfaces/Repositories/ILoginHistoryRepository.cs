using ASMED.EDM.Core.Entities;

namespace ASMED.EDM.Core.Interfaces.Repositories;

/// <summary>
/// Repozytorium historii logowań użytkowników.
/// </summary>
public interface ILoginHistoryRepository
{
    /// <summary>
    /// Pobiera historię logowań dla konkretnego użytkownika, posortowaną od najnowszych.
    /// </summary>
    Task<IEnumerable<LoginHistory>> GetByUserIdAsync(int userId, int maxRecords = 50, CancellationToken cancellationToken = default);

    Task<LoginHistory> AddAsync(LoginHistory entity, CancellationToken cancellationToken = default);
}
