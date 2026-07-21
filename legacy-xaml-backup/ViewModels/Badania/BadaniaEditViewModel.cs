using ASMED.WPF.Helpers;
using System;
using System.Collections.ObjectModel;

namespace ASMED.WPF.ViewModels.Badania
{
    public class BadaniaEditViewModel : BadaniaBaseViewModel
    {
        // Price fields
        private decimal _priceBasic = 0m;
        private decimal _priceLaryngologist = 0m;
        private decimal _priceOphthalmologist = 0m;
        private decimal _priceSanitary = 0m;
        private decimal _priceLipidogram = 0m;
        private decimal _priceEKG = 0m;
        private decimal _priceHealthClinic = 0m;

        // Formatted properties
        public string PriceBasicText => FormatPrice(_priceBasic);
        public string PriceLaryngologistText => FormatPrice(_priceLaryngologist);
        public string PriceOphthalmologistText => FormatPrice(_priceOphthalmologist);
        public string PriceSanitaryText => FormatPrice(_priceSanitary);
        public string PriceLipidogramText => FormatPrice(_priceLipidogram);
        public string PriceEKGText => FormatPrice(_priceEKG);
        public string PriceHealthClinicText => FormatPrice(_priceHealthClinic);

        private string FormatPrice(decimal v) => v.ToString("N2") + " z�";

        public void SetPriceFields(decimal? basic, decimal? laryngologist, decimal? ophthalmologist, decimal? sanitary, decimal? lipidogram, decimal? ekg, decimal? healthClinic, decimal? other)
        {
            _priceBasic = basic ?? 0m;
            _priceLaryngologist = laryngologist ?? 0m;
            _priceOphthalmologist = ophthalmologist ?? 0m;
            _priceSanitary = sanitary ?? 0m;
            _priceLipidogram = lipidogram ?? 0m;
            _priceEKG = ekg ?? 0m;
            _priceHealthClinic = healthClinic ?? 0m;
            NotifyPriceProperties();
        }

        private void NotifyPriceProperties()
        {
            OnPropertyChanged(nameof(PriceBasicText));
            OnPropertyChanged(nameof(PriceLaryngologistText));
            OnPropertyChanged(nameof(PriceOphthalmologistText));
            OnPropertyChanged(nameof(PriceSanitaryText));
            OnPropertyChanged(nameof(PriceLipidogramText));
            OnPropertyChanged(nameof(PriceEKGText));
            OnPropertyChanged(nameof(PriceHealthClinicText));
        }

        // DataBadania and DataWaznosci properties
        private DateTime? _dataBadania = DateTime.Today;
        public DateTime? DataBadania
        {
            get => _dataBadania;
            set
            {
                if (_dataBadania == value) return;
                _dataBadania = value;
                OnPropertyChanged();
                DataWaznosci = _dataBadania?.AddYears(3);
            }
        }

        private DateTime? _dataWaznosci;
        public DateTime? DataWaznosci
        {
            get => _dataWaznosci;
            set { _dataWaznosci = value; OnPropertyChanged(); }
        }

        // Wynik options for the ComboBox
        public ObservableCollection<string> WynikOptions { get; } = new ObservableCollection<string>();
        private string? _selectedWynik;
        public string? SelectedWynik
        {
            get => _selectedWynik;
            set { _selectedWynik = value; OnPropertyChanged(); }
        }

        private string? _nrKsiegi;
        public string? NrKsiegi
        {
            get => _nrKsiegi;
            set
            {
                if (_nrKsiegi != value)
                {
                    _nrKsiegi = value;
                    OnPropertyChanged(nameof(NrKsiegi));
                }
            }
        }

        private string? _selectedCennik;
        public string? SelectedCennik
        {
            get => _selectedCennik;
            set
            {
                if (_selectedCennik == value) return;
                _selectedCennik = value;
                OnPropertyChanged();
                LoadPricesForSelectedCennik();
            }
        }

        // SelectedWizyta so edit VM can show current selected visit
        private WizytaRecord? _selectedWizyta;
        public WizytaRecord? SelectedWizyta
        {
            get => _selectedWizyta;
            set { _selectedWizyta = value; OnPropertyChanged(); }
        }

        public object SelectedWizywa { get; internal set; }

        public BadaniaEditViewModel()
        {
            WynikOptions.Add("1 - Pozytywne");
            WynikOptions.Add("2 - Negatywne");
            SelectedWynik = WynikOptions.Count > 0 ? WynikOptions[0] : null;
            DataWaznosci = DataBadania?.AddYears(3);
        }

        private void LoadPricesForSelectedCennik()
        {
            _priceBasic = 0m; _priceLaryngologist = 0m; _priceOphthalmologist = 0m; _priceSanitary = 0m;
            _priceLipidogram = 0m; _priceEKG = 0m; _priceHealthClinic = 0m;

            if (string.IsNullOrEmpty(SelectedCennik))
            {
                NotifyPriceProperties();
                return;
            }

            try
            {
                var repo = new WizytyRepository();
                var prices = repo.GetCennikPrices(SelectedCennik);
                if (prices != null)
                {
                    foreach (var kv in prices)
                    {
                        var name = (kv.Key ?? string.Empty).Trim().ToLowerInvariant();
                        var price = kv.Value;
                        if (name.Contains("lekasz") || name.Contains("lekarz") || name.Contains("basic")) _priceBasic = price;
                        else if (name.Contains("laryngolog")) _priceLaryngologist = price;
                        else if (name.Contains("okulista") || name.Contains("okulist")) _priceOphthalmologist = price;
                        else if (name.Contains("ksi") || name.Contains("ksi��eczka") || name.Contains("ksiazeczka")) _priceSanitary = price;
                        else if (name.Contains("lipidogram")) _priceLipidogram = price;
                        else if (name.Contains("ekg")) _priceEKG = price;
                        else if (name.Contains("urlop")) _priceHealthClinic = price;
                    }
                }
            }
            catch { }

            NotifyPriceProperties();
        }

        internal void RefreshFromDb()
        {
            throw new NotImplementedException();
        }

        internal void RefreshBadania()
        {
            throw new NotImplementedException();
        }
    }
}
