namespace ASMED.EDM.Core.Enums;

/// <summary>
/// Status faktury — odpowiada FK_Status z tabeli Access Faktura
/// </summary>
public enum FakturaStatus
{
    /// <summary>Robocza — lista w trakcie tworzenia</summary>
    Robocza = 0,

    /// <summary>Wystawiona — faktura gotowa do wysłania</summary>
    Wystawiona = 1,

    /// <summary>Opłacona — należność uregulowana</summary>
    Oplacona = 2,

    /// <summary>Anulowana</summary>
    Anulowana = 3
}
