using System;

namespace ASMED.EDM.UI.Models.Orzeczenie;

/// <summary>
/// Wiersz historii badań pacjenta w zakładce "Historia" (OrzeczenieView).
/// Jedno skierowanie (B_Skierowania) odpowiada jednemu badaniu (Badanie) - relacja 1:1.
/// </summary>
public class HistoriaItem
{
    public int? B_ID { get; set; }
    public DateTime? B_DataSkierowania { get; set; }
    public DateTime? R_Data { get; set; }
    public DateTime? Bad_Data { get; set; }
    public DateTime? Bad_Data_Do { get; set; }

    public bool B_laryngolog { get; set; }
    public bool B_okulista { get; set; }
    public bool B_ksiazeczka { get; set; }
    public bool B_Urlop { get; set; }

    // Właściwości pomocnicze dla UI - ikony/emoji dla flag boolowskich.
    public string B_laryngologIcon => B_laryngolog ? "✔" : "—";
    public string B_okulistaIcon => B_okulista ? "✔" : "—";
    public string B_ksiazeczkaIcon => B_ksiazeczka ? "✔" : "—";
    public string B_UrlopIcon => B_Urlop ? "✔" : "—";

    public string? B_Typ { get; internal set; }
}
