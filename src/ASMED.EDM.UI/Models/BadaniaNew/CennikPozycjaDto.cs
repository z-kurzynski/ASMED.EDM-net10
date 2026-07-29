namespace ASMED.EDM.UI.Models.BadaniaNew;

/// <summary>
/// Pozycja (wiersz) cennika do edycji – kolumna 3 widoku Cenniki
/// </summary>
public class CennikPozycjaDto : System.ComponentModel.INotifyPropertyChanged
{
    public string Nazwa { get; set; } = string.Empty;

    private decimal? _cena;
    public decimal? Cena
    {
        get => _cena;
        set { _cena = value; OnPropertyChanged(); }
    }

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
}
