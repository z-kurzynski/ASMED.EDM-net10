namespace ASMED.EDM.Core.Enums;

/// <summary>
/// Status synchronizacji obiektu z systemem P1 (e-zdrowie)
/// </summary>
public enum P1SyncStatus
{
    /// <summary>Nie podlega synchronizacji / nie wysłano</summary>
    NotSynced = 0,

    /// <summary>Oczekuje na wysłanie do P1</summary>
    Pending = 1,

    /// <summary>Zsynchronizowano poprawnie</summary>
    Synced = 2,

    /// <summary>Błąd synchronizacji — do ponowienia</summary>
    Failed = 3,

    /// <summary>Anulowano w P1</summary>
    Cancelled = 4
}

/// <summary>
/// Typ dokumentu EDM w lokalnym repozytorium
/// </summary>
public enum EdmDocumentType
{
    /// <summary>Orzeczenie o zdolności do pracy (medycyna pracy)</summary>
    WorkAbilityCertificate = 1,

    /// <summary>Inny dokument EDM (HL7 CDA)</summary>
    Other = 99
}

/// <summary>
/// Status podpisu elektronicznego dokumentu EDM
/// </summary>
public enum EdmSignatureStatus
{
    /// <summary>Dokument niepodpisany</summary>
    Unsigned = 0,

    /// <summary>Podpisany pieczęcią placówki (certyfikat)</summary>
    Signed = 1,

    /// <summary>Błąd podpisu</summary>
    SignatureFailed = 2
}
