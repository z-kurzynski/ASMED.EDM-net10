using CommunityToolkit.Mvvm.ComponentModel;

namespace ASMED.EDM.Data.Models;

/// <summary>
/// Kategoria tabeli do migracji
/// </summary>
public enum TableCategory
{
    Glowne,
    Slownikowe,
    Pomocnicze
}

/// <summary>
/// Reprezentuje tabelę dostępną do migracji z Access do MySQL
/// </summary>
public partial class TableInfo : ObservableObject
{
    private bool _isSelected;

    /// <summary>
    /// Nazwa tabeli w bazie danych
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Opis wyświetlany użytkownikowi
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// Kategoria tabeli (Główne / Słownikowe / Pomocnicze)
    /// </summary>
    public TableCategory Category { get; init; }

    /// <summary>
    /// Czy tabela jest zaznaczona do migracji
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public override string ToString() => DisplayName;
}
