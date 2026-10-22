namespace ASMED.EDM.UI.Services;

/// <summary>
/// Stan uruchomienia aplikacji (singleton).
/// Tryb konfiguracji bazy danych jest włączany, gdy przy starcie żadna baza danych nie odpowiada
/// (np. czysta instalacja na nowym komputerze) — aplikacja otwiera wtedy wyłącznie zakładkę
/// „Baza / Ustawienia”, bez logowania i bez widoków wymagających połączenia z bazą.
/// </summary>
public sealed class AppStartupState
{
    /// <summary>
    /// Czy aplikacja działa w trybie konfiguracji bazy danych (brak połączenia przy starcie).
    /// </summary>
    public bool IsDatabaseConfigurationMode { get; private set; }

    /// <summary>
    /// Przyczyna braku połączenia z bazą danych (komunikat dla użytkownika).
    /// </summary>
    public string? DatabaseUnavailableReason { get; private set; }

    /// <summary>
    /// Włącza tryb konfiguracji bazy danych.
    /// </summary>
    public void EnterDatabaseConfigurationMode(string? reason)
    {
        IsDatabaseConfigurationMode = true;
        DatabaseUnavailableReason = reason;
    }
}
