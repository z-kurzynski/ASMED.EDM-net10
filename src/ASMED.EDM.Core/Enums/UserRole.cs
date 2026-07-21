namespace ASMED.EDM.Core.Enums;

/// <summary>
/// Rola użytkownika w systemie
/// </summary>
public enum UserRole
{
    /// <summary>
    /// Administrator systemu
    /// </summary>
    Administrator = 0,

    /// <summary>
    /// Lekarz
    /// </summary>
    Doctor = 1,

    /// <summary>
    /// Pielęgniarka
    /// </summary>
    Nurse = 2,

    /// <summary>
    /// Recepcjonista
    /// </summary>
    Receptionist = 3,

    /// <summary>
    /// Użytkownik z dostępem tylko do odczytu
    /// </summary>
    ReadOnly = 4
}
