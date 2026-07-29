using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ASMED.EDM.UI.Models.BadaniaNew;

/// <summary>
/// DTO dla firmy z przypisanym cennikiem – używany w widoku Cenniki (lewa kolumna)
/// </summary>
public class FirmaWithCennikDto : INotifyPropertyChanged
{
    public int Id { get; set; }
    public string? Nazwa { get; set; }
    public string? NIP { get; set; }

    private string? _cennik;
    public string? Cennik
    {
        get => _cennik;
        set { _cennik = value; OnPropertyChanged(); }
    }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
