// Zmień modyfikator dostępu klasy AssignedBadanieDto z internal na public
public class AssignedBadanieDto
{
    public object Bad_ID { get; internal set; } = new();
    public object? Bad_Cena1 { get; internal set; }
    public object? Bad_Cena2 { get; internal set; }
    public object? Bad_Cena4 { get; internal set; }
    public object? Bad_Cena3 { get; internal set; }
    public object? Bad_Cena5 { get; internal set; }
    public object? Bad_Cena7 { get; internal set; }
    public object? Bad_Cena6 { get; internal set; }
    public object? Bad_Cena8 { get; internal set; }
    public object? Bad_Razem { get; internal set; }
    public object? Bad_Data_Do { get; internal set; }
    public object? Bad_Nr_KS { get; internal set; }
    public object? Bad_Typ { get; internal set; }
    public string? Bad_Wynik { get; internal set; }
    public DateTime? Bad_Data { get; internal set; }
    public string? P_zawod { get; internal set; }
    public string P_imie { get; internal set; } = string.Empty;
    public string P_nazwisko { get; internal set; } = string.Empty;
    public string FirmaNazwa { get; internal set; } = string.Empty;
}