using System;

namespace ASMED.WPF.Models
{
    /// <summary>
    /// Model dla otwartej (niezamkniętej) karty badań
    /// Używany w dialogu wyboru karty do edycji
    /// </summary>
    public class OtwartaKartaBadanDto
    {
        /// <summary>
        /// ID skierowania (B_ID)
        /// </summary>
        public int B_ID { get; set; }

        /// <summary>
        /// Data skierowania (B_DataSkierowania)
        /// </summary>
        public DateTime? B_DataSkierowania { get; set; }

        /// <summary>
        /// Typ badania (W/O/K)
        /// </summary>
        public string B_TypBadania { get; set; } = string.Empty;

        /// <summary>
        /// Data rejestracji (R_Data) - jeśli jest rejestracja
        /// </summary>
        public DateTime? R_Data { get; set; }

        /// <summary>
        /// Status rejestracji (R_Status)
        /// </summary>
        public string R_Status { get; set; } = string.Empty;

        // ? Właściwości pomocnicze dla UI

        public string DataSkierowaniaFormatted => B_DataSkierowania?.ToString("dd.MM.yyyy") ?? "Brak daty";

        public string TypBadaniaFull => B_TypBadania switch
        {
            "W" => "Wstępne",
            "O" => "Okresowe",
            "K" => "Kontrolne",
            _ => B_TypBadania ?? "Brak"
        };

        public string DataRejestracjiFormatted => R_Data?.ToString("dd.MM.yyyy HH:mm") ?? "Brak rejestracji";

        public string StatusDisplay => string.IsNullOrWhiteSpace(R_Status) ? "Brak rejestracji" : R_Status;

        /// <summary>
        /// Czy karta ma przypisaną rejestrację
        /// </summary>
        public bool HasRejestracja => R_Data.HasValue;
    }
}
