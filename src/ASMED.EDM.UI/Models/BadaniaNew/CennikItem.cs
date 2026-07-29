namespace ASMED.EDM.UI.Models.BadaniaNew;

/// <summary>
/// Pozycja listy cenników (BAD_Lista) – środkowa kolumna widoku Cenniki
/// </summary>
public class CennikItem
{
    public int Identyfikator { get; set; }
    public string? Nazwa { get; set; }   // = bn_cennik
    public bool IsActive { get; set; }
}
