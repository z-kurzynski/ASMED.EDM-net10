using ASMED.EDM.Core.DTOs;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ASMED.EDM.UI.Models.ListaDoFaktur;

/// <summary>
/// Wrapper na BadanieRowDto używany po lewej stronie (lista dostępnych badań).
/// Obsługuje checkbox IsMarked.
/// Odpowiednik AssignedBadanieWrapper z legacy.
/// </summary>
public class BadanieWrapper : INotifyPropertyChanged
{
    public BadanieRowDto Dto { get; }

    public BadanieWrapper(BadanieRowDto dto)
    {
        Dto = dto;
    }

    private bool _isMarked;
    public bool IsMarked
    {
        get => _isMarked;
        set { _isMarked = value; OnPropertyChanged(); }
    }

    // Delegaty właściwości DTO do bindowania w ListView
    public string? PacjentDisplay => Dto.PacjentDisplay;
    public DateTime? Bad_Data => Dto.Bad_Data;
    public decimal? Bad_Razem => Dto.Bad_Razem;
    public string? FirmaNazwa => Dto.FirmaNazwa;

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>
/// Lista badań przypisana do faktury (prawa strona).
/// Odpowiednik AccessDbContext.ListyBadanDto z legacy.
/// </summary>
public class ListyBadanDto : INotifyPropertyChanged
{
    public int? Identyfikator { get; set; }
    public string? Nazwa { get; set; }
    public DateTime? FK_Data { get; set; }
    public string? FK_Numer { get; set; }
    public decimal? FK_Kwota { get; set; }

    public ObservableCollection<BadanieRowDto> Badania { get; set; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>
/// DTO firmy do wyboru w oknie — dane podstawowe.
/// </summary>
public class FirmaDto
{
    public int Id { get; set; }
    public string? Nazwa { get; set; }
    public string? NIP { get; set; }
    public bool? Activ { get; set; }
    public string? Cennik { get; set; }
    public string? FkEmail { get; set; }
}
