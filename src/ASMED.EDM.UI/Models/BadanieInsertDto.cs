namespace ASMED.EDM.UI.Models;

public class BadanieInsertDto
{
    public int Bad_S_ID { get; set; }
    public int Bad_P_ID { get; set; }
    public int Bad_F_ID { get; set; }
    public string? Bad_bn_cennik { get; set; }
    public string? Bad_Typ { get; set; }
    public DateTime? Bad_Data { get; set; }
    public DateTime? Bad_Data_Do { get; set; }
    public string? Bad_Wynik { get; set; }
    public decimal Bad_Cena1 { get; set; }
    public decimal Bad_Cena2 { get; set; }
    public decimal Bad_Cena3 { get; set; }
    public decimal Bad_Cena4 { get; set; }
    public decimal Bad_Cena5 { get; set; }
    public decimal Bad_Cena6 { get; set; }
    public decimal Bad_Cena7 { get; set; }
    public decimal Bad_Cena8 { get; set; }
    public decimal Bad_Razem { get; set; }
    public string? Bad_Nr_KS { get; set; }
    public bool Bad_END { get; set; }
}
