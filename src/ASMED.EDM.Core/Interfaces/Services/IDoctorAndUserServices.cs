using ASMED.EDM.Core.Entities;

namespace ASMED.EDM.Core.Interfaces.Services;

/// <summary>
/// Serwis zarządzania lekarzami
/// </summary>
public interface IDoctorService
{
    // Queries
    Task<Doctor?> GetDoctorByIdAsync(int doctorId, CancellationToken cancellationToken = default);
    Task<Doctor?> GetDoctorByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Doctor>> GetAllDoctorsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Doctor>> GetBySpecializationAsync(string specialization, CancellationToken cancellationToken = default);
    Task<IEnumerable<Doctor>> GetAcceptingNewPatientsAsync(CancellationToken cancellationToken = default);

    // Commands
    Task<Doctor> CreateDoctorAsync(Doctor doctor, CancellationToken cancellationToken = default);
    Task<Doctor> UpdateDoctorAsync(Doctor doctor, CancellationToken cancellationToken = default);
    Task DeleteDoctorAsync(int doctorId, int userId, CancellationToken cancellationToken = default);

    // Schedules
    Task<DoctorSchedule> AddScheduleAsync(DoctorSchedule schedule, CancellationToken cancellationToken = default);
    Task<IEnumerable<DoctorSchedule>> GetDoctorScheduleAsync(int doctorId, CancellationToken cancellationToken = default);
    Task UpdateScheduleAsync(DoctorSchedule schedule, CancellationToken cancellationToken = default);
}

/// <summary>
/// Serwis zarządzania użytkownikami systemu
/// </summary>
public interface IUserService
{
    // Queries
    Task<User?> GetUserByIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<User?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetActiveUsersAsync(CancellationToken cancellationToken = default);

    // Commands
    Task<User> CreateUserAsync(User user, string password, CancellationToken cancellationToken = default);
    Task<User> UpdateUserAsync(User user, CancellationToken cancellationToken = default);
    Task DeleteUserAsync(int userId, int deletedBy, CancellationToken cancellationToken = default);

    // Authentication & Security
    Task<User?> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default);
    Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword, CancellationToken cancellationToken = default);
    Task<string> GeneratePasswordResetTokenAsync(int userId, CancellationToken cancellationToken = default);
    Task<bool> ResetPasswordAsync(string token, string newPassword, CancellationToken cancellationToken = default);
    Task LockAccountAsync(int userId, string reason, CancellationToken cancellationToken = default);
    Task UnlockAccountAsync(int userId, CancellationToken cancellationToken = default);
}
