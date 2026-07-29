namespace ASMED.EDM.UI.Models.BadaniaNew;

/// <summary>
/// DTO dla słownika cennika (combo select_cennik)
/// Ładowane przed główną listą badań w BadaniaNewView
/// </summary>
public class CennikDto
{
    public int Identyfikator { get; set; }
    public string? bn_cennik { get; set; }  // NAZWA cennika (tekst)
    public bool bn_Cen_activ { get; set; }

    // Dla ComboBox DisplayMember - pokazujemy samą nazwę
    public string DisplayText => bn_cennik ?? $"ID: {Identyfikator}";

    // Ceny z cennika (dictionary z kolumnami dynamicznymi)
    public Dictionary<string, decimal?>? Prices { get; set; }
}
