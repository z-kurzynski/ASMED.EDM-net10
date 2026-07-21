using ASMED.WPF.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Input;

namespace ASMED.WPF.ViewModels.Badania
{



    public class BadaniaListViewModel : BadaniaBaseViewModel
    {
        private ObservableCollection<WizytaRecord> _wizyty = new();
        public ObservableCollection<WizytaRecord> Wizyty
        {
            get => _wizyty;
            set { _wizyty = value; OnPropertyChanged(); }
        }

        private ObservableCollection<WizytaRecord> _badania = new();
        public ObservableCollection<WizytaRecord> Badania
        {
            get => _badania;
            set { _badania = value; OnPropertyChanged(); }
        }

        private List<WizytaRecord> _all = new();
        private List<WizytaRecord> _allBadania = new();

        // expose read-only snapshots so wrapper VM może filtrować (przeniesiono logikę filtra do BadaniaViewModel)
        public IReadOnlyList<WizytaRecord> AllWizyty => _all ?? new List<WizytaRecord>();
        public IReadOnlyList<WizytaRecord> AllBadania => _allBadania ?? new List<WizytaRecord>();

        private WizytaRecord? _selectedWizyta;
        public WizytaRecord? SelectedWizyta
        {
            get => _selectedWizyta;
            set
            {
                _selectedWizyta = value;
                OnPropertyChanged();

                var id = _selectedWizyta?.B_ID.ToString() ?? "<null>";
                var cennikRaw = _selectedWizyta?.Firma_Cennik ?? "<null>";
                // System.Diagnostics.Debug.WriteLine($"[VM] SelectedWizyta setter called. B_ID={id}, Firma_Cennik={cennikRaw}");

                UpdateCennikOptionsForSelected();

                // normalizuj/trim tylko do porównania
                var firmaCennik = _selectedWizyta?.Firma_Cennik?.Trim();
                if (!string.IsNullOrEmpty(firmaCennik))
                {
                    // znajdź istniejący element w kolekcji (porównanie wartości)
                    var match = CennikOptions.FirstOrDefault(c => string.Equals(c?.Trim(), firmaCennik, StringComparison.OrdinalIgnoreCase));
                    if (match == null)
                    {
                        // dodaj i użyj tego elementu (ten sam obiekt z kolekcji)
                        CennikOptions.Add(firmaCennik);
                        match = firmaCennik;
                    }

                    // WAŻNE: przypisz dokładny element z CennikOptions, nie nowy string
                    SelectedCennik = match;
                }
                else
                {
                    SelectedCennik = string.Empty;
                }

                // System.Diagnostics.Debug.WriteLine($"[VM] SelectedCennik after assign: '{SelectedCennik}'");
            }
        }

        // --- filter properties & commands ---
        public new ObservableCollection<string> FilterOptions { get; } = new ObservableCollection<string>
        {
            "All", "Imie", "Nazwisko", "ID", "Pesel", "Firma", "Data"
        };

        private string _selectedFilter = "All";
        public string SelectedFilter
        {
            get => _selectedFilter;
            set { if (_selectedFilter != value) { _selectedFilter = value; OnPropertyChanged(); ApplyFilter(); } }
        }

        private string _filterText = string.Empty;
        public string FilterText
        {
            get => _filterText;
            set { if (_filterText != value) { _filterText = value ?? string.Empty; OnPropertyChanged(); ApplyFilter(); } }
        }

        public ICommand ClearFilterCommand { get; }

        // --- Lista-specific (separate backing fields) ---
        private string _filterTextLista = string.Empty;
        public string FilterTextLista
        {
            get => _filterTextLista;
            set
            {
                if (_filterTextLista != value)
                {
                    _filterTextLista = value ?? string.Empty;
                    OnPropertyChanged(nameof(FilterTextLista));
                    ApplyFilterLista();
                }
            }
        }

        private string _selectedFilterLista = "All";
        public string SelectedFilterLista
        {
            get => _selectedFilterLista;
            set
            {
                if (_selectedFilterLista != value)
                {
                    _selectedFilterLista = value ?? "All";
                    OnPropertyChanged(nameof(SelectedFilterLista));
                    ApplyFilterLista();
                }
            }
        }

        // share same options collection (keeps lists consistent)
        public ObservableCollection<string> FilterOptionsLista => FilterOptions;

        public ICommand ClearFilterCommandLista { get; }

        private decimal? _priceBasic;
        private decimal? _priceLaryngologist;
        private decimal? _priceOphthalmologist;
        private decimal? _priceSanitary;
        private decimal? _priceLipidogram;
        private decimal? _priceEKG;
        private decimal? _priceHealthClinic;
        private decimal? _priceOther;


        //public string? SelectedCennik { get; private set; }
        private string? _selectedCennik;
        public string? SelectedCennik
        {

            get => _selectedCennik;
            set
            {
                if (_selectedCennik != value)
                {
                    _selectedCennik = value;
                    OnPropertyChanged();
                    // when selected cennik changes, load its prices so UI bindings update
                    LoadPricesForSelectedCennik();
                }
            }
        }

        public BadaniaListViewModel()
        {
            // inicjalizacja komend czyszczenia filtra
            ClearFilterCommand = new RelayCommand(_ => { FilterText = string.Empty; });
            ClearFilterCommandLista = new RelayCommand(_ => { FilterTextLista = string.Empty; });

            LoadData();
        }

        private void LoadData()
        {
            var repo = new WizytyRepository();
            _all = repo.GetWizyty() ?? new List<WizytaRecord>();
            Wizyty = new ObservableCollection<WizytaRecord>(_all);

            var cenniki = repo.GetCennikOptions();
            if (cenniki != null)
            {
                foreach (var c in cenniki)
                {
                    if (!CennikOptions.Contains(c)) CennikOptions.Add(c);
                }
            }

            if (!CennikOptions.Contains("Szkoły")) CennikOptions.Insert(0, "Szkoły");
        }

        private void UpdateCennikOptionsForSelected()
        {
            if (SelectedWizyta != null)
            {
                var val = SelectedWizyta.Firma_Cennik ?? string.Empty;
                if (!string.IsNullOrEmpty(val) && !CennikOptions.Contains(val))
                {
                    CennikOptions.Add(val);
                }
            }
        }

        public void RefreshFromDb()
        {
            LoadData();
            OnPropertyChanged(nameof(Wizyty));
        }

        public void RefreshBadania()
        {
            try
            {
                var repo = new WizytyRepository();
                _allBadania = repo.GetBadaniaList() ?? new List<WizytaRecord>();
                Badania = new ObservableCollection<WizytaRecord>(_allBadania);
                try
                {
                    var cnt = Badania?.Count ?? 0;
                    NotificationHelper.ShowInfo("Badania: odświeżono", $"Załadowano {cnt} rekordów");
                }
                catch { }
            }
            catch { }
        }

        // --- ApplyFilterLista: filters only Badania (Lista_Badan) using lista-specific properties ---
        private void ApplyFilterLista()
        {
            if (string.IsNullOrWhiteSpace(FilterTextLista))
            {
                Badania = new ObservableCollection<WizytaRecord>(_allBadania ?? new List<WizytaRecord>());
                return;
            }

            var raw = FilterTextLista.Trim();
            var txt = raw.ToLowerInvariant();

            DateTime parsedDate = default;
            bool parsedAsDate = false;
            if (!string.IsNullOrEmpty(txt))
            {
                var pl = CultureInfo.GetCultureInfo("pl-PL");
                parsedAsDate = DateTime.TryParseExact(raw, new[] { "dd-MM-yyyy", "d-M-yyyy", "yyyy-MM-dd", "dd.MM.yyyy", "d.M.yyyy" }, pl, System.Globalization.DateTimeStyles.None, out parsedDate)
                    || DateTime.TryParse(raw, pl, System.Globalization.DateTimeStyles.None, out parsedDate)
                    || DateTime.TryParse(raw, CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out parsedDate);
            }

            IEnumerable<WizytaRecord> sourceBadania = _allBadania ?? new List<WizytaRecord>();
            IEnumerable<WizytaRecord> filteredBadania = sourceBadania;

            if (SelectedFilterLista == "All")
            {
                filteredBadania = sourceBadania.Where(w =>
                    ((w.P_imie ?? string.Empty).ToLowerInvariant().Contains(txt)) ||
                    ((w.P_nazwisko ?? string.Empty).ToLowerInvariant().Contains(txt)) ||
                    ((w.P_pesel ?? string.Empty).ToLowerInvariant().Contains(txt)) ||
                    ((w.Firma_Nazwa ?? string.Empty).ToLowerInvariant().Contains(txt)) ||
                    (parsedAsDate && w.Bad_Data.HasValue && w.Bad_Data.Value.Date == parsedDate.Date) ||
                    w.B_ID.ToString().Contains(raw)
                );
            }
            else
            {
                switch (SelectedFilterLista)
                {
                    case "Imie": filteredBadania = sourceBadania.Where(w => (w.P_imie ?? string.Empty).ToLowerInvariant().Contains(txt)); break;
                    case "Nazwisko": filteredBadania = sourceBadania.Where(w => (w.P_nazwisko ?? string.Empty).ToLowerInvariant().Contains(txt)); break;
                    case "ID": filteredBadania = sourceBadania.Where(w => w.B_ID.ToString().Contains(raw)); break;
                    case "Pesel": filteredBadania = sourceBadania.Where(w => (w.P_pesel ?? string.Empty).ToLowerInvariant().Contains(txt)); break;
                    case "Firma": filteredBadania = sourceBadania.Where(w => (w.Firma_Nazwa ?? string.Empty).ToLowerInvariant().Contains(txt)); break;
                    case "Data":
                        if (parsedAsDate)
                            filteredBadania = sourceBadania.Where(w => w.Bad_Data.HasValue && w.Bad_Data.Value.Date == parsedDate.Date);
                        else
                            filteredBadania = sourceBadania.Where(w => (w.Bad_Data.HasValue ? w.Bad_Data.Value.ToString("dd-MM-yyyy") : string.Empty).ToLowerInvariant().Contains(txt));
                        break;
                    default: filteredBadania = sourceBadania; break;
                }
            }

            Badania = new ObservableCollection<WizytaRecord>(filteredBadania);
        }

        // --- przywrócona metoda ApplyFilter (oryginalna, bez zmiany logiki) ---
        private void ApplyFilter()
        {
            if (string.IsNullOrWhiteSpace(FilterText))
            {
                Badania = new ObservableCollection<WizytaRecord>(_allBadania ?? new List<WizytaRecord>());
                Wizyty = new ObservableCollection<WizytaRecord>(_all);
                return;
            }

            var raw = FilterText.Trim();
            var txt = raw.ToLowerInvariant();

            DateTime parsedDate = default;
            bool parsedAsDate = false;
            if (!string.IsNullOrEmpty(txt))
            {
                var pl = CultureInfo.GetCultureInfo("pl-PL");
                parsedAsDate = DateTime.TryParseExact(raw, new[] { "dd-MM-yyyy", "d-M-yyyy", "yyyy-MM-dd", "dd.MM.yyyy", "d.M.yyyy" }, pl, System.Globalization.DateTimeStyles.None, out parsedDate)
                || DateTime.TryParse(raw, pl, System.Globalization.DateTimeStyles.None, out parsedDate)
                || DateTime.TryParse(raw, CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out parsedDate);
            }

            IEnumerable<WizytaRecord> filteredWizyty = _all;
            if (SelectedFilter == "All")
            {
                filteredWizyty = _all.Where(w =>
                ((w.P_imie ?? string.Empty).ToLowerInvariant().Contains(txt)) ||
                ((w.P_nazwisko ?? string.Empty).ToLowerInvariant().Contains(txt)) ||
                ((w.P_pesel ?? string.Empty).ToLowerInvariant().Contains(txt)) ||
                ((w.Firma_Nazwa ?? string.Empty).ToLowerInvariant().Contains(txt)) ||
                w.B_ID.ToString().Contains(raw));
            }
            else
            {
                switch (SelectedFilter)
                {
                    case "Imie": filteredWizyty = _all.Where(w => (w.P_imie ?? string.Empty).ToLowerInvariant().Contains(txt)); break;
                    case "Nazwisko": filteredWizyty = _all.Where(w => (w.P_nazwisko ?? string.Empty).ToLowerInvariant().Contains(txt)); break;
                    case "ID": filteredWizyty = _all.Where(w => w.B_ID.ToString().Contains(raw)); break;
                    case "Pesel": filteredWizyty = _all.Where(w => (w.P_pesel ?? string.Empty).ToLowerInvariant().Contains(txt)); break;
                    case "Firma": filteredWizyty = _all.Where(w => (w.Firma_Nazwa ?? string.Empty).ToLowerInvariant().Contains(txt)); break;
                    case "Data":
                        if (parsedAsDate)
                            filteredWizyty = _all.Where(w => w.B_DataSkierowania.HasValue && w.B_DataSkierowania.Value.Date == parsedDate.Date);
                        else
                            filteredWizyty = _all.Where(w => (w.B_DataSkierowania.HasValue ? w.B_DataSkierowania.Value.ToString("dd-MM-yyyy") : string.Empty).ToLowerInvariant().Contains(txt));
                        break;
                    default: filteredWizyty = _all; break;
                }
            }

            IEnumerable<WizytaRecord> sourceBadania = _allBadania ?? new List<WizytaRecord>();
            IEnumerable<WizytaRecord> filteredBadania = sourceBadania;
            if (SelectedFilter == "All")
            {
                filteredBadania = sourceBadania.Where(w =>
                ((w.P_imie ?? string.Empty).ToLowerInvariant().Contains(txt)) ||
                ((w.P_nazwisko ?? string.Empty).ToLowerInvariant().Contains(txt)) ||
                ((w.P_pesel ?? string.Empty).ToLowerInvariant().Contains(txt)) ||
                ((w.Firma_Nazwa ?? string.Empty).ToLowerInvariant().Contains(txt)) ||
                (parsedAsDate && w.Bad_Data.HasValue && w.Bad_Data.Value.Date == parsedDate.Date) ||
                w.B_ID.ToString().Contains(raw)
                );
            }
            else
            {
                switch (SelectedFilter)
                {
                    case "Imie": filteredBadania = sourceBadania.Where(w => (w.P_imie ?? string.Empty).ToLowerInvariant().Contains(txt)); break;
                    case "Nazwisko": filteredBadania = sourceBadania.Where(w => (w.P_nazwisko ?? string.Empty).ToLowerInvariant().Contains(txt)); break;
                    case "ID": filteredBadania = sourceBadania.Where(w => w.B_ID.ToString().Contains(raw)); break;
                    case "Pesel": filteredBadania = sourceBadania.Where(w => (w.P_pesel ?? string.Empty).ToLowerInvariant().Contains(txt)); break;
                    case "Firma": filteredBadania = sourceBadania.Where(w => (w.Firma_Nazwa ?? string.Empty).ToLowerInvariant().Contains(txt)); break;
                    case "Data":
                        if (parsedAsDate)
                            filteredBadania = sourceBadania.Where(w => w.Bad_Data.HasValue && w.Bad_Data.Value.Date == parsedDate.Date);
                        else
                            filteredBadania = sourceBadania.Where(w => (w.Bad_Data.HasValue ? w.Bad_Data.Value.ToString("dd-MM-yyyy") : string.Empty).ToLowerInvariant().Contains(txt));
                        break;
                    default: filteredBadania = sourceBadania; break;
                }
            }

            Wizyty = new ObservableCollection<WizytaRecord>(filteredWizyty);
            Badania = new ObservableCollection<WizytaRecord>(filteredBadania);
        }


        private void LoadPricesForSelectedCennik()
        {
            try
            {
                if (string.IsNullOrEmpty(SelectedCennik))
                {
                    _priceBasic = _priceLaryngologist = _priceOphthalmologist = _priceSanitary = _priceLipidogram = _priceEKG = _priceHealthClinic = _priceOther = null;
                }
                else
                {
                    var repo = new WizytyRepository();
                    var prices = repo.GetCennikPrices(SelectedCennik ?? string.Empty);
                    // System.Diagnostics.Debug.WriteLine($"VM.LoadPricesForSelectedCennik: SelectedCennik='{SelectedCennik}', prices.Count={prices?.Count ?? 0}");

                    string Normalize(string s)
                    {
                        if (string.IsNullOrWhiteSpace(s)) return string.Empty;
                        var normalized = s.ToLowerInvariant().Normalize(System.Text.NormalizationForm.FormD);
                        var sb = new System.Text.StringBuilder();
                        foreach (var ch in normalized)
                        {
                            var uc = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch);
                            if (uc != System.Globalization.UnicodeCategory.NonSpacingMark)
                                sb.Append(ch);
                        }
                        return sb.ToString().Normalize(System.Text.NormalizationForm.FormC);
                    }

                    Func<string[], decimal?> getPrice = keys =>
                    {
                        // zabezpieczenie przed null dla 'prices'
                        if (prices == null || keys == null || keys.Length == 0) return null;

                        // try exact match first (case-insensitive)
                        foreach (var k in keys)
                        {
                            if (prices.TryGetValue(k, out var v)) return v;
                        }

                        // fallback: normalized contains match
                        var normKeys = keys.Select(k => Normalize(k)).Where(k => !string.IsNullOrEmpty(k)).ToArray();
                        foreach (var kv in prices)
                        {
                            var nameNorm = Normalize(kv.Key);
                            foreach (var nk in normKeys)
                            {
                                if (nameNorm.Contains(nk)) return kv.Value;
                            }
                        }

                        return null;
                    };

                    _priceBasic = getPrice(new[] { "lekarz", "lekasz", "basic" });
                    _priceLaryngologist = getPrice(new[] { "laryngolog" });
                    _priceOphthalmologist = getPrice(new[] { "okulista", "okulist" });
                    _priceSanitary = getPrice(new[] { "ksi", "książeczka", "ksiazeczka" });
                    _priceLipidogram = getPrice(new[] { "lipidogram" });
                    _priceEKG = getPrice(new[] { "ekg" });
                    _priceHealthClinic = getPrice(new[] { "urlop", "urlop (zdrowie)", "healthclinic" });
                    _priceOther = getPrice(new[] { "inne", "other" });

                    // System.Diagnostics.Debug.WriteLine($"VM.LoadPricesForSelectedCennik: mapped prices Basic={_priceBasic} Laryng={_priceLaryngologist} Okulista={_priceOphthalmologist} Sanitary={_priceSanitary} Lipid={_priceLipidogram} EKG={_priceEKG} Urlop={_priceHealthClinic} Other={_priceOther}");
                }

                // notify UI that formatted texts changed
                OnPropertyChanged(nameof(PriceBasicText));
                OnPropertyChanged(nameof(PriceLaryngologistText));
                OnPropertyChanged(nameof(PriceOphthalmologistText));
                OnPropertyChanged(nameof(PriceSanitaryText));
                OnPropertyChanged(nameof(PriceLipidogramText));
                OnPropertyChanged(nameof(PriceEKGText));
                OnPropertyChanged(nameof(PriceHealthClinicText));
                OnPropertyChanged(nameof(PriceOtherText));
            }
            catch { }
        }

        // helper i publiczne właściwości (dodaj je w klasie, usuń prywatne metody Price...())
        private static string FormatPrice(decimal? price) =>
            price.HasValue ? price.Value.ToString("N2", CultureInfo.GetCultureInfo("pl-PL")) : string.Empty;

        public string PriceBasicText => FormatPrice(_priceBasic);
        public string PriceLaryngologistText => FormatPrice(_priceLaryngologist);
        public string PriceOphthalmologistText => FormatPrice(_priceOphthalmologist);
        public string PriceSanitaryText => FormatPrice(_priceSanitary);
        public string PriceLipidogramText => FormatPrice(_priceLipidogram);
        public string PriceEKGText => FormatPrice(_priceEKG);
        public string PriceHealthClinicText => FormatPrice(_priceHealthClinic);
        public string PriceOtherText => FormatPrice(_priceOther);
    }
}
