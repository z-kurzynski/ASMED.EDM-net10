using ASMED.WPF.Helpers;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace ASMED.WPF.ViewModels.Badania
{
    public class BadaniePricesViewModel : INotifyPropertyChanged
    {
        private static readonly CultureInfo PlCulture = CultureInfo.GetCultureInfo("pl-PL");

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public decimal? PriceBasic { get => _priceBasic; set { if (_priceBasic != value) { _priceBasic = value; OnPropertyChanged(); OnPropertyChanged(nameof(PriceBasicText)); RecalculateTotal(); } } }
        public decimal? PriceLaryngologist { get => _priceLaryngologist; set { if (_priceLaryngologist != value) { _priceLaryngologist = value; OnPropertyChanged(); OnPropertyChanged(nameof(PriceLaryngologistText)); RecalculateTotal(); } } }
        public decimal? PriceOphthalmologist { get => _priceOphthalmologist; set { if (_priceOphthalmologist != value) { _priceOphthalmologist = value; OnPropertyChanged(); OnPropertyChanged(nameof(PriceOphthalmologistText)); RecalculateTotal(); } } }
        public decimal? PriceSanitary { get => _priceSanitary; set { if (_priceSanitary != value) { _priceSanitary = value; OnPropertyChanged(); OnPropertyChanged(nameof(PriceSanitaryText)); RecalculateTotal(); } } }
        public decimal? PriceLipidogram { get => _priceLipidogram; set { if (_priceLipidogram != value) { _priceLipidogram = value; OnPropertyChanged(); OnPropertyChanged(nameof(PriceLipidogramText)); RecalculateTotal(); } } }
        public decimal? PriceEKG { get => _priceEKG; set { if (_priceEKG != value) { _priceEKG = value; OnPropertyChanged(); OnPropertyChanged(nameof(PriceEKGText)); RecalculateTotal(); } } }
        public decimal? PriceHealthClinic { get => _priceHealthClinic; set { if (_priceHealthClinic != value) { _priceHealthClinic = value; OnPropertyChanged(); OnPropertyChanged(nameof(PriceHealthClinicText)); RecalculateTotal(); } } }
        public decimal? PriceOther { get => _priceOther; set { if (_priceOther != value) { _priceOther = value; OnPropertyChanged(); OnPropertyChanged(nameof(PriceOtherText)); RecalculateTotal(); } } }

        private decimal? _priceBasic, _priceLaryngologist, _priceOphthalmologist, _priceSanitary, _priceLipidogram, _priceEKG, _priceHealthClinic, _priceOther;
        private decimal _total;

        public string PriceBasicText => Format(_priceBasic);
        public string PriceLaryngologistText => Format(_priceLaryngologist);
        public string PriceOphthalmologistText => Format(_priceOphthalmologist);
        public string PriceSanitaryText => Format(_priceSanitary);
        public string PriceLipidogramText => Format(_priceLipidogram);
        public string PriceEKGText => Format(_priceEKG);
        public string PriceHealthClinicText => Format(_priceHealthClinic);
        public string PriceOtherText => Format(_priceOther);
        public string TotalText => string.Format(PlCulture, "{0:N2} zł", _total);

        private string Format(decimal? v) => v.HasValue ? string.Format(PlCulture, "{0:N2} zł", v.Value) : string.Empty;

        public void LoadFromCennik(string bnCennik)
        {
            if (string.IsNullOrWhiteSpace(bnCennik))
            {
                Clear();
                return;
            }
            var repo = new WizytyRepository();
            var prices = repo.GetCennikPrices(bnCennik);
            if (prices == null) { Clear(); return; }

            PriceBasic = TryGet(prices, new[] { "lekarz", "lekasz", "basic" });
            PriceLaryngologist = TryGet(prices, new[] { "laryngolog" });
            PriceOphthalmologist = TryGet(prices, new[] { "okulista", "okulist" });
            PriceSanitary = TryGet(prices, new[] { "ksi", "książeczka", "ksiazeczka" });
            PriceLipidogram = TryGet(prices, new[] { "lipidogram" });
            PriceEKG = TryGet(prices, new[] { "ekg" });
            PriceHealthClinic = TryGet(prices, new[] { "urlop", "urlop (zdrowie)", "healthclinic" });
            PriceOther = TryGet(prices, new[] { "inne", "other" });

            OnPropertyChanged(nameof(PriceBasicText));
            OnPropertyChanged(nameof(PriceLaryngologistText));
            OnPropertyChanged(nameof(PriceOphthalmologistText));
            OnPropertyChanged(nameof(PriceSanitaryText));
            OnPropertyChanged(nameof(PriceLipidogramText));
            OnPropertyChanged(nameof(PriceEKGText));
            OnPropertyChanged(nameof(PriceHealthClinicText));
            OnPropertyChanged(nameof(PriceOtherText));
        }

        public void LoadFromBadanieRecord(AccessDbContext.BadanieRecord bad)
        {
            if (bad == null) { Clear(); return; }
            PriceBasic = bad.Bad_Cena1;
            PriceLaryngologist = bad.Bad_Cena2;
            PriceOphthalmologist = bad.Bad_Cena3;
            PriceSanitary = bad.Bad_Cena4;
            PriceLipidogram = bad.Bad_Cena5;
            PriceEKG = bad.Bad_Cena6;
            PriceHealthClinic = bad.Bad_Cena7;
            PriceOther = bad.Bad_Cena8 ?? bad.Bad_Cena9 ?? bad.Bad_Cena10;
        }

        public void Clear()
        {
            PriceBasic = PriceLaryngologist = PriceOphthalmologist = PriceSanitary = PriceLipidogram = PriceEKG = PriceHealthClinic = PriceOther = null;
            RecalculateTotal();
        }

        private decimal? TryGet(System.Collections.Generic.Dictionary<string, decimal> prices, string[] keys)
        {
            foreach (var k in keys) if (prices.TryGetValue(k, out var v)) return v;
            return null;
        }

        private void RecalculateTotal()
        {
            _total = (PriceBasic ?? 0m) + (PriceLaryngologist ?? 0m) + (PriceOphthalmologist ?? 0m) + (PriceSanitary ?? 0m)
                     + (PriceLipidogram ?? 0m) + (PriceEKG ?? 0m) + (PriceHealthClinic ?? 0m) + (PriceOther ?? 0m);
            OnPropertyChanged(nameof(TotalText));
        }

        // optional helper to parse user input if you plan two-way binding to strings
        public static decimal ParsePriceSafe(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0m;
            var input = s.Replace("zł", "").Replace("zl", "").Trim();
            if (decimal.TryParse(input, System.Globalization.NumberStyles.Number, PlCulture, out var v)) return v;
            if (decimal.TryParse(input, System.Globalization.NumberStyles.Number, CultureInfo.InvariantCulture, out v)) return v;
            return 0m;
        }
    }
}
