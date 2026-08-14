namespace ASMED.EDM.Core.Entities;

/// <summary>
/// Wpis historii logowania użytkownika. Mapuje tabelę LoginHistory.
/// </summary>
public class LoginHistory
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public string? Username { get; set; }

    public DateTime? LoginTime { get; set; }

    public DateTime? LogoutTime { get; set; }

    public string? ComputerName { get; set; }

    public string? IpAddress { get; set; }

    public string? FailureReason { get; set; }

    public virtual User? User { get; set; }
}
