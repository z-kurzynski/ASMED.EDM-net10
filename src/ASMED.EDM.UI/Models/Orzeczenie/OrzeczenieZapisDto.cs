using System;

namespace ASMED.EDM.UI.Models.Orzeczenie;

/// <summary>
/// Komplet danych orzeczenia (tabela Orzeczenie) wraz z badaniami specjalistycznymi
/// (Badanie_Laryngologiczne / _Okulistyczne / _Sanitarne / _Urlop) — używany zarówno
/// do odczytu istniejącego orzeczenia, jak i do zapisu przy zatwierdzeniu.
/// Pola ukryte w UI (Visibility=Collapsed) mają wartości domyślne i są zapisywane jawnie.
/// </summary>
public sealed class OrzeczenieZapisDto
{
    // ── Powiązania ──────────────────────────────────────────────────────────
    public int? OId { get; set; }
    public int BId { get; set; }
    public int? RId { get; set; }
    public int? PId { get; set; }

    // ── Rejestr / numeracja (nadawane przy pierwszym zatwierdzeniu) ─────────
    public string? Numer { get; set; }
    public string? Status { get; set; }
    public DateTime? ZatwierdzonoAt { get; set; }

    // ── Orzeczenie ──────────────────────────────────────────────────────────
    public string? TypBadania { get; set; }
    public string? Stanowisko { get; set; }
    public DateTime? DataBadania { get; set; }
    public DateTime? DataSkierowania { get; set; }
    public DateTime? DataNastepnegoBadania { get; set; }

    /// <summary>Symbol z WynikBadania: "21" / "22" / "23".</summary>
    public string Werdykt { get; set; } = "21";

    public DateTime? DataUtratyZdolnosci { get; set; }
    public string? Uzasadnienie { get; set; }

    // ── Orzecznik (migawka w chwili podpisu) ────────────────────────────────
    public int? DoctorId { get; set; }
    public string? Lekarz { get; set; }
    public string? LekarzPwz { get; set; }
    public DateTime? PodpisanoAt { get; set; }
    public int? CreatedByUserId { get; set; }

    // ── Badanie laryngologiczne ─────────────────────────────────────────────
    public bool Laryngolog { get; set; }
    public DateTime? LarData { get; set; }
    public bool LarWykonano { get; set; } = true;
    public bool LarVhiWypelniona { get; set; }
    public string? LarVhiOpis { get; set; }
    public bool LarKrtanPrawidlowa { get; set; } = true;
    public bool LarNosogardloPrawidlowe { get; set; } = true;
    public bool LarSluchPrawidlowy { get; set; } = true;
    public bool LarBlonyPrawidlowe { get; set; } = true;
    public bool LarRownowagaPrawidlowa { get; set; } = true;
    public bool LarPrzeciwwskazaniaGlos { get; set; }
    public bool LarWynikPrawidlowy { get; set; } = true;
    public string? LarUwagi { get; set; }

    // ── Badanie okulistyczne ────────────────────────────────────────────────
    public bool Okulista { get; set; }
    public DateTime? OkuData { get; set; }
    public bool OkuWykonano { get; set; } = true;
    public bool OkuBarwyPrawidlowe { get; set; } = true;
    public bool OkuPolePrawidlowe { get; set; } = true;
    public bool OkuObuocznePrawidlowe { get; set; } = true;
    public bool OkuWynikPrawidlowy { get; set; } = true;
    public bool OkuPrzeciwwskazaniaMonitor { get; set; }
    public bool OkuUzywaOkularow { get; set; }
    public string? OkuUwagi { get; set; }

    // ── Badanie sanitarno-epidemiologiczne ──────────────────────────────────
    public bool Sanitarne { get; set; }
    public DateTime? SanData { get; set; }
    public string SanWynik { get; set; } = "Pozytywny";
    public string? SanUwagi { get; set; }

    // ── Urlop dla poratowania zdrowia ───────────────────────────────────────
    public bool Urlop { get; set; }
    public DateTime? UrlopOd { get; set; }
    public DateTime? UrlopDo { get; set; }
    public string? UrlopUzasadnienie { get; set; }
}
