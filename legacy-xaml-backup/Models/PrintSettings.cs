using System.Drawing.Printing;

namespace ASMED.WPF.Models
{
    /// <summary>
    /// Klasa przechowująca ustawienia druku
    /// </summary>
    public class PrintSettings
    {
        /// <summary>
        /// Nazwa wybranej drukarki
        /// </summary>
        public string? PrinterName { get; set; }

        /// <summary>
        /// Tryb druku dwustronnego (Simplex, Vertical, Horizontal)
        /// </summary>
        public Duplex Duplex { get; set; } = Duplex.Simplex;

        /// <summary>
        /// Orientacja pozioma (true) lub pionowa (false)
        /// </summary>
        public bool Landscape { get; set; } = false;

        /// <summary>
        /// Liczba kopii
        /// </summary>
        public short Copies { get; set; } = 1;

        /// <summary>
        /// Sortuj kopie (drukuj kompletne zestawy)
        /// </summary>
        public bool Collate { get; set; } = true;

        /// <summary>
        /// Jakość druku
        /// </summary>
        public PrintQuality Quality { get; set; } = PrintQuality.Normal;

        /// <summary>
        /// Nazwa dokumentu do druku
        /// </summary>
        public string DocumentName { get; set; } = "Dokument";

        /// <summary>
        /// Ścieżka do pliku PDF (jeśli dotyczy)
        /// </summary>
        public string PdfFilePath { get; set; } = string.Empty;

        /// <summary>
        /// Zwraca opis ustawień druku (do logowania)
        /// </summary>
        public override string ToString()
        {
            return $"Drukarka: {PrinterName}, Duplex: {Duplex}, " +
                   $"Orientacja: {(Landscape ? "Pozioma" : "Pionowa")}, " +
                   $"Kopie: {Copies}, Jakość: {Quality}";
        }
    }

    /// <summary>
    /// Jakość druku
    /// </summary>
    public enum PrintQuality
    {
        Draft = -1,    // Robocza (300 dpi)
        Normal = 0,    // Normalna (600 dpi)
        High = 1       // Wysoka (1200 dpi)
    }
}
