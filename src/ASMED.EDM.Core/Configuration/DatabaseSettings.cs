namespace ASMED.EDM.Core.Configuration;

/// <summary>
/// Konfiguracja połączeń z bazami danych MySQL
/// </summary>
public class DatabaseSettings
{
    /// <summary>
    /// Główne połączenie do bazy produkcyjnej
    /// </summary>
    public string PrimaryConnection { get; set; } = string.Empty;

    /// <summary>
    /// Połączenie backup (używane gdy główne niedostępne)
    /// </summary>
    public string BackupConnection { get; set; } = string.Empty;

    /// <summary>
    /// Połączenie lokalne (używane gdy brak internetu)
    /// </summary>
    public string LocalConnection { get; set; } = string.Empty;

    /// <summary>
    /// Timeout dla testowania połączenia (w sekundach)
    /// </summary>
    public int ConnectionTimeout { get; set; } = 3;

    /// <summary>
    /// Czy używać automatycznego failover (Primary → Backup → Local)
    /// </summary>
    public bool EnableFailover { get; set; } = true;
}
