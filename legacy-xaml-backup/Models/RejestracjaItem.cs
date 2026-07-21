using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ASMED.WPF.Models
{
    /// <summary>
    /// Model reprezentujący pojedynczą rejestrację/wizytę pacjenta
    /// Mapuje pola z tabeli Rejestracja w AccessDB + JOIN (Pacjent, Firma, Skierowanie)
    /// </summary>
    public class RejestracjaItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Pola z tabeli Rejestracja

        private int _rId;
        public int R_ID
        {
            get => _rId;
            set { _rId = value; OnPropertyChanged(); }
        }

        private int? _rBId;
        public int? R_B_ID
        {
            get => _rBId;
            set { _rBId = value; OnPropertyChanged(); }
        }

        private DateTime? _rData;
        public DateTime? R_Data
        {
            get => _rData;
            set { _rData = value; OnPropertyChanged(); OnPropertyChanged(nameof(DataFormatted)); }
        }

        private string? _rStatus;
        public string? R_Status
        {
            get => _rStatus;
            set { _rStatus = value; OnPropertyChanged(); }
        }

        private int? _rEmployeeId;
        public int? R_Employee_ID
        {
            get => _rEmployeeId;
            set { _rEmployeeId = value; OnPropertyChanged(); }
        }

        private int? _rSId;
        public int? R_S_ID
        {
            get => _rSId;
            set { _rSId = value; OnPropertyChanged(); OnPropertyChanged(nameof(SkierowanieNumer)); }
        }

        private int? _rPId;
        public int? R_P_ID
        {
            get => _rPId;
            set { _rPId = value; OnPropertyChanged(); }
        }

        private DateTime? _rGgMm;
        public DateTime? R_GG_MM
        {
            get => _rGgMm;
            set { _rGgMm = value; OnPropertyChanged(); OnPropertyChanged(nameof(GodzinaFormatted)); }
        }

        private string? _rUwagi;
        public string? R_Uwagi
        {
            get => _rUwagi;
            set { _rUwagi = value; OnPropertyChanged(); }
        }

        private string? _rSubject;
        public string? R_Subject
        {
            get => _rSubject;
            set { _rSubject = value; OnPropertyChanged(); }
        }

        // ✅ NOWE: Pola z JOIN (Firma + Pacjent)
        private string? _firmaNazwa;
        public string? Firma_Nazwa
        {
            get => _firmaNazwa;
            set { _firmaNazwa = value; OnPropertyChanged(); }
        }

        private string? _pImie;
        public string? P_Imie
        {
            get => _pImie;
            set { _pImie = value; OnPropertyChanged(); }
        }

        private string? _pNazwisko;
        public string? P_Nazwisko
        {
            get => _pNazwisko;
            set { _pNazwisko = value; OnPropertyChanged(); }
        }

        // ═══════════════════════════════════════════════════════
        // ✅ NOWE: Pola PACJENTA (z JOIN P_Pacjent)
        // ═══════════════════════════════════════════════════════
        private string? _pPesel;
        public string? P_Pesel
        {
            get => _pPesel;
            set { _pPesel = value; OnPropertyChanged(); }
        }

        private bool? _brakPesel;
        public bool? BrakPESEL
        {
            get => _brakPesel;
            set { _brakPesel = value; OnPropertyChanged(); }
        }

        private int? _pId;
        public int? P_ID
        {
            get => _pId;
            set { _pId = value; OnPropertyChanged(); }
        }

        private string? _pTelefon;
        public string? P_Telefon
        {
            get => _pTelefon;
            set { _pTelefon = value; OnPropertyChanged(); }
        }

        private string? _pEmail;
        public string? P_Email
        {
            get => _pEmail;
            set { _pEmail = value; OnPropertyChanged(); }
        }

        private string? _pPlec;
        public string? P_Plec
        {
            get => _pPlec;
            set { _pPlec = value; OnPropertyChanged(); }
        }

        private DateTime? _pDataUrodzenia;
        public DateTime? P_DataUrodzenia
        {
            get => _pDataUrodzenia;
            set { _pDataUrodzenia = value; OnPropertyChanged(); }
        }

        private string? _pZawod;
        public string? P_Zawod
        {
            get => _pZawod;
            set { _pZawod = value; OnPropertyChanged(); }
        }

        private string? _pAdresUlica;
        public string? P_AdresUlica
        {
            get => _pAdresUlica;
            set { _pAdresUlica = value; OnPropertyChanged(); }
        }

        private string? _pAdresKod;
        public string? P_AdresKod
        {
            get => _pAdresKod;
            set { _pAdresKod = value; OnPropertyChanged(); }
        }

        private string? _pAdresMiasto;
        public string? P_AdresMiasto
        {
            get => _pAdresMiasto;
            set { _pAdresMiasto = value; OnPropertyChanged(); }
        }

        private int? _pFirmaId;
        public int? P_FirmaId
        {
            get => _pFirmaId;
            set { _pFirmaId = value; OnPropertyChanged(); }
        }

        // ═══════════════════════════════════════════════════════
        // ✅ NOWE: Pola FIRMY (z JOIN Firma)
        // ═══════════════════════════════════════════════════════
        private string? _firmaKod;
        public string? Firma_Kod
        {
            get => _firmaKod;
            set { _firmaKod = value; OnPropertyChanged(); }
        }

        private string? _firmaMiejscowosc;
        public string? Firma_Miejscowosc
        {
            get => _firmaMiejscowosc;
            set { _firmaMiejscowosc = value; OnPropertyChanged(); }
        }

        private string? _firmaUlica;
        public string? Firma_Ulica
        {
            get => _firmaUlica;
            set { _firmaUlica = value; OnPropertyChanged(); }
        }

        private string? _firmaEmail;
        public string? Firma_Email
        {
            get => _firmaEmail;
            set { _firmaEmail = value; OnPropertyChanged(); }
        }

        // ═══════════════════════════════════════════════════════
        // ✅ NOWE: Pola SKIEROWANIA (z JOIN B_Skierowania)
        // ═══════════════════════════════════════════════════════
        private int? _bId;
        public int? B_ID
        {
            get => _bId;
            set { _bId = value; OnPropertyChanged(); }
        }

        private DateTime? _bDataSkierowania;
        public DateTime? B_DataSkierowania
        {
            get => _bDataSkierowania;
            set { _bDataSkierowania = value; OnPropertyChanged(); }
        }

        private string? _bTypBadania;
        public string? B_TypBadania
        {
            get => _bTypBadania;
            set { _bTypBadania = value; OnPropertyChanged(); }
        }

        private string? _bStanowisko;
        public string? B_Stanowisko
        {
            get => _bStanowisko;
            set { _bStanowisko = value; OnPropertyChanged(); }
        }

        private DateTime? _bRegistrationDate;
        public DateTime? B_RegistrationDate
        {
            get => _bRegistrationDate;
            set { _bRegistrationDate = value; OnPropertyChanged(); }
        }

        // Czynniki szkodliwe
        private bool? _bCzynnikFizyczny;
        public bool? B_CzynnikFizyczny
        {
            get => _bCzynnikFizyczny;
            set { _bCzynnikFizyczny = value; OnPropertyChanged(); }
        }

        private string? _bCzynnikFizycznyOpis;
        public string? B_CzynnikFizycznyOpis
        {
            get => _bCzynnikFizycznyOpis;
            set { _bCzynnikFizycznyOpis = value; OnPropertyChanged(); }
        }

        private bool? _bCzynnikPylowy;
        public bool? B_CzynnikPylowy
        {
            get => _bCzynnikPylowy;
            set { _bCzynnikPylowy = value; OnPropertyChanged(); }
        }

        private string? _bCzynnikPylowyOpis;
        public string? B_CzynnikPylowyOpis
        {
            get => _bCzynnikPylowyOpis;
            set { _bCzynnikPylowyOpis = value; OnPropertyChanged(); }
        }

        private bool? _bCzynnikChemiczny;
        public bool? B_CzynnikChemiczny
        {
            get => _bCzynnikChemiczny;
            set { _bCzynnikChemiczny = value; OnPropertyChanged(); }
        }

        private string? _bCzynnikChemicznyOpis;
        public string? B_CzynnikChemicznyOpis
        {
            get => _bCzynnikChemicznyOpis;
            set { _bCzynnikChemicznyOpis = value; OnPropertyChanged(); }
        }

        private bool? _bCzynnikBiologiczny;
        public bool? B_CzynnikBiologiczny
        {
            get => _bCzynnikBiologiczny;
            set { _bCzynnikBiologiczny = value; OnPropertyChanged(); }
        }

        private string? _bCzynnikBiologicznyOpis;
        public string? B_CzynnikBiologicznyOpis
        {
            get => _bCzynnikBiologicznyOpis;
            set { _bCzynnikBiologicznyOpis = value; OnPropertyChanged(); }
        }

        private bool? _bCzynnikInny;
        public bool? B_CzynnikInny
        {
            get => _bCzynnikInny;
            set { _bCzynnikInny = value; OnPropertyChanged(); }
        }

        private string? _bCzynnikInnyOpis;
        public string? B_CzynnikInnyOpis
        {
            get => _bCzynnikInnyOpis;
            set { _bCzynnikInnyOpis = value; OnPropertyChanged(); }
        }

        // Dokumenty
        private bool? _bZaswiadczenie;
        public bool? B_Zaswiadczenie
        {
            get => _bZaswiadczenie;
            set { _bZaswiadczenie = value; OnPropertyChanged(); }
        }

        private bool? _bKsiazeczka;
        public bool? B_Ksiazeczka
        {
            get => _bKsiazeczka;
            set { _bKsiazeczka = value; OnPropertyChanged(); }
        }

        #endregion

        #region Właściwości pomocnicze dla UI

        /// <summary>
        /// Formatowana data (dd.MM.yyyy)
        /// </summary>
        public string DataFormatted => R_Data?.ToString("dd.MM.yyyy") ?? "-";

        /// <summary>
        /// Formatowana godzina (HH:mm)
        /// </summary>
        public string GodzinaFormatted => R_GG_MM?.ToString("HH:mm") ?? "-";

        /// <summary>
        /// Numer skierowania jako string
        /// </summary>
        public string SkierowanieNumer => R_S_ID?.ToString() ?? "-";

        /// <summary>
        /// Status wizyty jako tekst czytelny dla użytkownika
        /// </summary>
        public string? StatusWizytyTekst
        {
            get
            {
                if (string.IsNullOrWhiteSpace(R_Status))
                    return "Nieznany";

                return R_Status.ToLower() switch
                {
                    "zaplanowana" => "✔️ Zaplanowana",
                    "odbyta" => "✅ Odbyta",
                    "anulowana" => "❌ Anulowana",
                    "w trakcie" => "🔄 W trakcie",
                    "przełożona" => "📅 Przełożona",
                    "nieobecność" => "⛔ Nieobecny",
                    _ => R_Status // Zwróć raw value jeśli nie pasuje
                };
            }
        }

        /// <summary>
        /// Pełny opis wizyty (Subject + Uwagi)
        /// </summary>
        public string? PelnaNazwa
        {
            get
            {
                if (!string.IsNullOrEmpty(R_Subject) && !string.IsNullOrEmpty(R_Uwagi))
                    return $"{R_Subject} - {R_Uwagi}";
                return R_Subject ?? R_Uwagi ?? "-";
            }
        }

        /// <summary>
        /// QR Code data (numer skierowania)
        /// </summary>
        public string? QRCodeData => $"SKIER-{R_S_ID ?? 0}";

        /// <summary>
        /// Barcode data (nazwisko pacjenta - wyciągnięte z R_Subject)
        /// </summary>
        public string? BarcodeData
        {
            get
            {
                if (string.IsNullOrEmpty(R_Subject)) return "UNKNOWN";

                // Zakładamy że R_Subject zawiera: "Nazwisko Imię - Firma"
                var parts = R_Subject.Split(new[] { " - ", "-" }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0)
                {
                    return parts[0].Trim().ToUpperInvariant();
                }
                return R_Subject.Trim().ToUpperInvariant();
            }
        }

        /// <summary>
        /// ✅ NOWY: Pełny adres pacjenta
        /// </summary>
        public string? PelnyAdres
        {
            get
            {
                var ulica = P_AdresUlica ?? "";
                var kod = P_AdresKod ?? "";
                var miasto = P_AdresMiasto ?? "";

                if (string.IsNullOrEmpty(ulica) && string.IsNullOrEmpty(kod) && string.IsNullOrEmpty(miasto))
                    return "-";

                return $"{ulica}, {kod} {miasto}".Trim();
            }
        }

        /// <summary>
        /// ✅ NOWY: Pełny adres firmy
        /// </summary>
        public string? FirmaAdres
        {
            get
            {
                var ulica = Firma_Ulica ?? "";
                var kod = Firma_Kod ?? "";
                var miasto = Firma_Miejscowosc ?? "";

                if (string.IsNullOrEmpty(ulica) && string.IsNullOrEmpty(kod) && string.IsNullOrEmpty(miasto))
                    return "-";

                return $"{ulica}, {kod} {miasto}".Trim();
            }
        }

        /// <summary>
        /// ✅ NOWY: Wiek pacjenta (obliczony z P_DataUrodzenia)
        /// </summary>
        public int? Wiek
        {
            get
            {
                if (!P_DataUrodzenia.HasValue)
                    return null;

                var dzisiaj = DateTime.Today;
                var wiek = dzisiaj.Year - P_DataUrodzenia.Value.Year;

                // Korekta jeśli urodziny jeszcze nie były w tym roku
                if (P_DataUrodzenia.Value.Date > dzisiaj.AddYears(-wiek))
                    wiek--;

                return wiek;
            }
        }

        /// <summary>
        /// ✅ NOWY: Tooltip z danymi kontaktowymi
        /// </summary>
        public string? KontaktTooltip
        {
            get
            {
                var lines = new System.Collections.Generic.List<string>();

                if (!string.IsNullOrWhiteSpace(P_Telefon))
                    lines.Add($"📞 {P_Telefon}");

                if (!string.IsNullOrWhiteSpace(P_Email))
                    lines.Add($"📧 {P_Email}");

                if (!string.IsNullOrWhiteSpace(P_Pesel))
                    lines.Add($"🆔 {P_Pesel}");

                if (Wiek.HasValue)
                    lines.Add($"🎂 {Wiek} lat");

                return lines.Count > 0 ? string.Join("\n", lines) : "Brak danych kontaktowych";
            }
        }

        #endregion

        #region Konstruktory

        public RejestracjaItem()
        {
        }

        /// <summary>
        /// Tworzy RejestracjaItem z dynamicznego obiektu z AccessDB
        /// </summary>
        public static RejestracjaItem? FromDbRecord(dynamic record)
        {
            try
            {
                return new RejestracjaItem
                {
                    R_ID = record.R_ID ?? 0,
                    R_B_ID = record.R_B_ID,
                    R_Data = record.R_Data,
                    R_Status = record.R_Status,
                    R_Employee_ID = record.R_Employee_ID,
                    R_S_ID = record.R_S_ID,
                    R_P_ID = record.R_P_ID,
                    R_GG_MM = record.R_GG_MM,
                    R_Uwagi = record.R_Uwagi,
                    R_Subject = record.R_Subject,

                    // ✅ Pacjent (istniejące + nowe)
                    P_Imie = record.P_Imie,
                    P_Nazwisko = record.P_Nazwisko,
                    P_Pesel = record.P_Pesel,
                    P_Telefon = record.P_Telefon,
                    P_Email = record.P_Email,
                    P_Plec = record.P_Plec,
                    P_DataUrodzenia = record.P_DataUrodzenia,
                    P_Zawod = record.P_Zawod,
                    P_AdresUlica = record.P_AdresUlica,
                    P_AdresKod = record.P_AdresKode,
                    P_AdresMiasto = record.P_AdresMiasto,
                    P_ID = record.P_ID,
                    BrakPESEL = record.BrakPESEL,
                    P_FirmaId = record.P_FirmaId,

                    // ✅ Firma (istniejące + nowe)
                    Firma_Nazwa = record.Firma_Nazwa,
                    Firma_Kod = record.Firma_Kod,
                    Firma_Miejscowosc = record.Firma_Miejscowosc,
                    Firma_Ulica = record.Firma_Ulica,

                    // ✅ Skierowanie (NOWE!)
                    B_ID = record.B_ID,
                    B_DataSkierowania = record.B_DataSkierowania,
                    B_TypBadania = record.B_TypBadania,
                    B_Stanowisko = record.B_Stanowisko,
                    B_RegistrationDate = record.B_RegistrationDate,
                    B_CzynnikFizyczny = record.B_CzynnikFizyczny,
                    B_CzynnikFizycznyOpis = record.B_CzynnikFizycznyOpis,
                    B_CzynnikPylowy = record.B_CzynnikPylowy,
                    B_CzynnikPylowyOpis = record.B_CzynnikPylowyOpis,
                    B_CzynnikChemiczny = record.B_CzynnikChemiczny,
                    B_CzynnikChemicznyOpis = record.B_CzynnikChemicznyOpis,
                    B_CzynnikBiologiczny = record.B_CzynnikBiologiczny,
                    B_CzynnikBiologicznyOpis = record.B_CzynnikBiologicznyOpis,
                    B_CzynnikInny = record.B_CzynnikInny,
                    B_CzynnikInnyOpis = record.B_CzynnikInnyOpis,
                    B_Zaswiadczenie = record.B_Zaswiadczenie,
                    B_Ksiazeczka = record.B_Ksiazeczka
                };
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"❌ Błąd konwersji rekordu DB: {ex.Message}");
                return null;
            }
        }

        #endregion
    }
}
