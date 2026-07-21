using ASMED.EDM.Core.Entities;
using ASMED.EDM.Core.Interfaces.Repositories;
using ASMED.EDM.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace ASMED.EDM.Data.Services.Domain;

/// <summary>
/// Implementacja serwisu audytu
/// </summary>
public class AuditService : IAuditService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AuditService> _logger;

    public AuditService(
        IUnitOfWork unitOfWork,
        ILogger<AuditService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task LogOperationAsync(
        int userId,
        string operationType,
        string entityName,
        string entityId,
        string? oldValues = null,
        string? newValues = null,
        bool isSuccess = true,
        string? errorMessage = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var auditLog = new AuditLog
            {
                Timestamp = DateTime.UtcNow,
                UserId = userId,
                OperationType = operationType,
                EntityName = entityName,
                EntityId = entityId,
                OldValues = oldValues,
                NewValues = newValues,
                IsSuccess = isSuccess,
                ErrorMessage = errorMessage
            };

            await _unitOfWork.AuditLogs.AddAsync(auditLog, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogDebug(
                "Zarejestrowano operację audytu: {OperationType} na {EntityName}:{EntityId}",
                operationType, entityName, entityId);
        }
        catch (Exception ex)
        {
            // Audyt nie powinien blokować głównej operacji
            _logger.LogError(ex, "Błąd podczas zapisywania logu audytu");
        }
    }

    public async Task<IEnumerable<AuditLog>> GetUserActivityAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.AuditLogs.GetByUserIdAsync(userId, cancellationToken);
    }

    public async Task<IEnumerable<AuditLog>> GetEntityHistoryAsync(
        string entityName,
        string entityId,
        CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.AuditLogs.GetByEntityAsync(entityName, entityId, cancellationToken);
    }

    public async Task<IEnumerable<AuditLog>> GetFailedOperationsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.AuditLogs.GetFailedOperationsAsync(cancellationToken);
    }

    public async Task<IEnumerable<AuditLog>> GetAuditsByDateRangeAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.AuditLogs.GetByDateRangeAsync(from, to, cancellationToken);
    }
}
