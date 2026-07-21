using System;

namespace ASMED.WPF.Models
{
    public class InvoiceItem
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }

        // Typ badania (nowe pole, wyświetlane jako wąska kolumna)
        public string? BadType { get; set; }

        // Prices
        public decimal Total { get; set; }
        public decimal ExaminationPrice { get; set; }
        public decimal LaryngologistPrice { get; set; }
        public decimal OphthalmologistPrice { get; set; }
        public decimal SanitaryPrice { get; set; }
        public decimal OtherPrice { get; set; }
        public decimal LipidogramPrice { get; set; }
        public decimal EKGPrice { get; set; }
        public decimal HealthClinicPrice { get; set; }
    }
}