using ASMED.EDM.Core.Entities;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ASMED.EDM.UI.ViewModels.Faktura;

/// <summary>
/// Reprezentuje pojedyncze badanie jako wiersz DataGrid na liście do faktury.
/// Mapuje pola encji <see cref="Badanie"/> na właściwości bindowalne.
/// </summary>
public class BadanieListItemViewModel : ObservableObject
{
    // ── pola ──────────────────────────────────────────────────────────────────
    private bool    _isSelected;
    private decimal _cena;

    // ── właściwości tylko-do-odczytu (niezmienne po konstruktorze) ────────────
    public int      Id                    { get; }
    public string?  TypBadania            { get; }
    public DateTime? DataBadania          { get; }
    public string?  ImieNazwiskoPacjenta  { get; }
    public string?  NrKsiegi              { get; }
    public string?  NazwaCennika          { get; }

    // ── właściwości bindowalne ─────────────────────────────────────────────────
    public decimal Cena
    {
        get => _cena;
        set => SetProperty(ref _cena, value);
    }

    /// <summary>Czy badanie jest zaznaczone do umieszczenia na liście.</summary>
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    // ── konstruktor ──────────────────────────────────────────────────────────
    public BadanieListItemViewModel(Badanie b)
    {
        ArgumentNullException.ThrowIfNull(b);

        Id           = b.Id;
        TypBadania   = b.TypBadania;
        DataBadania  = b.DataBadania;
        NrKsiegi     = b.NrKsiegi;
        NazwaCennika = b.NazwaCennika;
        Cena         = b.Razem ?? b.Cena1Podstawowa ?? 0m;

        if (b.Patient != null)
            ImieNazwiskoPacjenta = $"{b.Patient.FirstName} {b.Patient.LastName}".Trim();
    }
}
