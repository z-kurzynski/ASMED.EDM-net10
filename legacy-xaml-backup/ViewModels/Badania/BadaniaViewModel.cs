using ASMED.WPF.Helpers;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Globalization;
using System.Linq;
using System.Collections.Generic;

namespace ASMED.WPF.ViewModels.Badania
{
    // Legacy compatibility view model - delegates to specialized VMs
    public class BadaniaViewModel : BadaniaBaseViewModel
    {
        private readonly BadaniaListViewModel _listVm = new BadaniaListViewModel();
        private readonly BadaniaEditViewModel _editVm = new BadaniaEditViewModel();

        public BadaniaListViewModel List => _listVm;
        public BadaniaEditViewModel Edit => _editVm;

        public ObservableCollection<WizytaRecord> Wizyty { get => _listVm.Wizyty; set => _listVm.Wizyty = value; }
        public ObservableCollection<WizytaRecord> Badania { get => _listVm.Badania; set => _listVm.Badania = value; }

        public WizytaRecord? SelectedWizyta { get => _listVm.SelectedWizyta; set => _listVm.SelectedWizyta = value; }

        public string? SelectedCennik { get => _editVm.SelectedCennik; set => _editVm.SelectedCennik = value; }

        // Expose edit properties for back-compat
        public DateTime? DataBadania { get => _editVm.DataBadania; set => _editVm.DataBadania = value; }
        public DateTime? DataWaznosci { get => _editVm.DataWaznosci; set => _editVm.DataWaznosci = value; }
        public ObservableCollection<string> WynikOptions => _editVm.WynikOptions;
        public string? SelectedWynik { get => _editVm.SelectedWynik; set => _editVm.SelectedWynik = value; }
        public string? NrKsiegi { get => _editVm.NrKsiegi; set => _editVm.NrKsiegi = value; }
        public object SelectedWiza { get; internal set; }

        // Filter properties moved here (BadaniaView obsługuje UI filtra)
        private string ?_filterText = string.Empty;
        public string ?FilterText
        {
            get => _filterText;
            set
            {
                if (_filterText != value)
                {
                    _filterText = value ?? string.Empty;
                    OnPropertyChanged(nameof(FilterText));
                    ApplyFilter();
                }
            }
        }

        private string ?_selectedFilter = "All";
        public string ?SelectedFilter
        {
            get => _selectedFilter;
            set
            {
                if (_selectedFilter != value)
                {
                    _selectedFilter = value ?? "All";
                    OnPropertyChanged(nameof(SelectedFilter));
                    ApplyFilter();
                }
            }
        }

        // Options for the ComboBox in the view
        public new ObservableCollection<string> FilterOptions { get; } = new ObservableCollection<string> { "All", "Imie", "Nazwisko", "ID", "Pesel", "Firma", "Data" };

        public BadaniaViewModel()
        {
            // forward cennik options already loaded in list vm into base CennikOptions
            foreach (var c in _listVm.CennikOptions)
            {
                if (!CennikOptions.Contains(c)) CennikOptions.Add(c);
            }

            // Also populate edit VM's CennikOptions so editor bindings (when bound directly to vm.Edit)
            // will see the same options immediately.
            foreach (var c in _listVm.CennikOptions)
            {
                if (!_editVm.CennikOptions.Contains(c)) _editVm.CennikOptions.Add(c);
            }

            // subscribe to edit VM property changes so wrapper can forward notifications
            _editVm.PropertyChanged += (s, e) =>
            {
                // map edit vm property names to wrapper names where appropriate
                switch (e.PropertyName)
                {
                    case nameof(_editVm.DataBadania): OnPropertyChanged(nameof(DataBadania)); break;
                    case nameof(_editVm.DataWaznosci): OnPropertyChanged(nameof(DataWaznosci)); break;
                    case nameof(_editVm.SelectedWynik): OnPropertyChanged(nameof(SelectedWynik)); break;
                    case nameof(_editVm.NrKsiegi): OnPropertyChanged(nameof(NrKsiegi)); break;
                    case nameof(_editVm.WynikOptions): OnPropertyChanged(nameof(WynikOptions)); break;
                    // price texts can be observed via Edit.Price... bindings so no need to forward those names
                    default: break;
                }
            };

            // synchronize selection and forward property changes from list VM
            _listVm.PropertyChanged += (s, e) =>
            {
                // forward collection/property notifications so bindings to wrapper update
                if (e.PropertyName == nameof(_listVm.Badania)) OnPropertyChanged(nameof(Badania));
                if (e.PropertyName == nameof(_listVm.Wizyty)) OnPropertyChanged(nameof(Wizyty));
                if (e.PropertyName == nameof(_listVm.SelectedWizyta))
                {
                    // update wrapper selection notification
                    OnPropertyChanged(nameof(SelectedWizyta));

                    // sync selected wizyta into edit VM so editor shows current selection
                    try
                    {
                        _editVm.SelectedWizyta = _listVm.SelectedWizyta;

                        // also ensure wrapper CennikOptions contains current company's cennik
                        var compCennik = _listVm.SelectedWizyta?.Firma_Cennik;
                        if (!string.IsNullOrEmpty(compCennik))
                        {
                            if (!CennikOptions.Contains(compCennik)) CennikOptions.Add(compCennik);

                            if (!_editVm.CennikOptions.Contains(compCennik)) _editVm.CennikOptions.Add(compCennik);
                            _editVm.SelectedCennik = compCennik;
                        }
                    }
                    catch { }
                }

                // ensure wrapper also propagates selected cennik change when list selection changes
                if (e.PropertyName == nameof(_listVm.SelectedWizyta))
                {
                    SelectedCennik = _listVm.SelectedWizyta?.Firma_Cennik ?? SelectedCennik;
                    OnPropertyChanged(nameof(SelectedCennik));
                }
            };
        }

        // Public wrappers
        public void RefreshFromDb() => _listVm.RefreshFromDb();
        public void RefreshBadania() => _listVm.RefreshBadania();

        // Filtrowanie – skopiowano i przeniesiono logikę z BadaniaListViewModel.ApplyFilter
        private void ApplyFilter()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(FilterText))
                {
                    // restore full sets
                    _listVm.Wizyty = new ObservableCollection<WizytaRecord>(_listVm.AllWizyty ?? new List<WizytaRecord>());
                    _listVm.Badania = new ObservableCollection<WizytaRecord>(_listVm.AllBadania ?? new List<WizytaRecord>());
                    OnPropertyChanged(nameof(Wizyty));
                    OnPropertyChanged(nameof(Badania));
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

                IEnumerable<WizytaRecord> allW = _listVm.AllWizyty ?? new List<WizytaRecord>();
                IEnumerable<WizytaRecord> allB = _listVm.AllBadania ?? new List<WizytaRecord>();

                IEnumerable<WizytaRecord> filteredWizyty = allW;
                if (SelectedFilter == "All")
                {
                    filteredWizyty = allW.Where(w =>
                        ((w.P_imie ?? string.Empty).ToLowerInvariant().Contains(txt)) ||
                        TextNormalizationHelper.ContainsIgnoringDiacritics(w.P_imie ?? "", txt) ||
                        ((w.P_nazwisko ?? string.Empty).ToLowerInvariant().Contains(txt)) ||
                        TextNormalizationHelper.ContainsIgnoringDiacritics(w.P_nazwisko ?? "", txt) ||
                        ((w.P_pesel ?? string.Empty).ToLowerInvariant().Contains(txt)) ||
                        ((w.Firma_Nazwa ?? string.Empty).ToLowerInvariant().Contains(txt)) ||
                        TextNormalizationHelper.ContainsIgnoringDiacritics(w.Firma_Nazwa ?? "", txt) ||
                        w.B_ID.ToString().Contains(raw));
                }
                else
                {
                    switch (SelectedFilter)
                    {
                        case "Imie": 
                            filteredWizyty = allW.Where(w => (w.P_imie ?? string.Empty).ToLowerInvariant().Contains(txt) ||
                                                             TextNormalizationHelper.ContainsIgnoringDiacritics(w.P_imie ?? "", txt)); 
                            break;
                        case "Nazwisko": 
                            filteredWizyty = allW.Where(w => (w.P_nazwisko ?? string.Empty).ToLowerInvariant().Contains(txt) ||
                                                             TextNormalizationHelper.ContainsIgnoringDiacritics(w.P_nazwisko ?? "", txt)); 
                            break;
                        case "ID": filteredWizyty = allW.Where(w => w.B_ID.ToString().Contains(raw)); break;
                        case "Pesel": filteredWizyty = allW.Where(w => (w.P_pesel ?? string.Empty).ToLowerInvariant().Contains(txt)); break;
                        case "Firma": 
                            filteredWizyty = allW.Where(w => (w.Firma_Nazwa ?? string.Empty).ToLowerInvariant().Contains(txt) ||
                                                             TextNormalizationHelper.ContainsIgnoringDiacritics(w.Firma_Nazwa ?? "", txt)); 
                            break;
                        case "Data":
                            if (parsedAsDate)
                                filteredWizyty = allW.Where(w => w.B_DataSkierowania.HasValue && w.B_DataSkierowania.Value.Date == parsedDate.Date);
                            else
                                filteredWizyty = allW.Where(w => (w.B_DataSkierowania.HasValue ? w.B_DataSkierowania.Value.ToString("dd-MM-yyyy") : string.Empty).ToLowerInvariant().Contains(txt));
                            break;
                        default: filteredWizyty = allW; break;
                    }
                }

                IEnumerable<WizytaRecord> filteredBadania = allB;
                if (SelectedFilter == "All")
                {
                    filteredBadania = allB.Where(w =>
                        ((w.P_imie ?? string.Empty).ToLowerInvariant().Contains(txt)) ||
                        TextNormalizationHelper.ContainsIgnoringDiacritics(w.P_imie ?? "", txt) ||
                        ((w.P_nazwisko ?? string.Empty).ToLowerInvariant().Contains(txt)) ||
                        TextNormalizationHelper.ContainsIgnoringDiacritics(w.P_nazwisko ?? "", txt) ||
                        ((w.P_pesel ?? string.Empty).ToLowerInvariant().Contains(txt)) ||
                        ((w.Firma_Nazwa ?? string.Empty).ToLowerInvariant().Contains(txt)) ||
                        TextNormalizationHelper.ContainsIgnoringDiacritics(w.Firma_Nazwa ?? "", txt) ||
                        (parsedAsDate && w.Bad_Data.HasValue && w.Bad_Data.Value.Date == parsedDate.Date) ||
                        w.B_ID.ToString().Contains(raw)
                    );
                }
                else
                {
                    switch (SelectedFilter)
                    {
                        case "Imie": 
                            filteredBadania = allB.Where(w => (w.P_imie ?? string.Empty).ToLowerInvariant().Contains(txt) ||
                                                              TextNormalizationHelper.ContainsIgnoringDiacritics(w.P_imie ?? "", txt)); 
                            break;
                        case "Nazwisko": 
                            filteredBadania = allB.Where(w => (w.P_nazwisko ?? string.Empty).ToLowerInvariant().Contains(txt) ||
                                                              TextNormalizationHelper.ContainsIgnoringDiacritics(w.P_nazwisko ?? "", txt)); 
                            break;
                        case "ID": filteredBadania = allB.Where(w => w.B_ID.ToString().Contains(raw)); break;
                        case "Pesel": filteredBadania = allB.Where(w => (w.P_pesel ?? string.Empty).ToLowerInvariant().Contains(txt)); break;
                        case "Firma": 
                            filteredBadania = allB.Where(w => (w.Firma_Nazwa ?? string.Empty).ToLowerInvariant().Contains(txt) ||
                                                              TextNormalizationHelper.ContainsIgnoringDiacritics(w.Firma_Nazwa ?? "", txt)); 
                            break;
                        case "Data":
                            if (parsedAsDate)
                                filteredBadania = allB.Where(w => w.Bad_Data.HasValue && w.Bad_Data.Value.Date == parsedDate.Date);
                            else
                                filteredBadania = allB.Where(w => (w.Bad_Data.HasValue ? w.Bad_Data.Value.ToString("dd-MM-yyyy") : string.Empty).ToLowerInvariant().Contains(txt));
                            break;
                        default: filteredBadania = allB; break;
                    }
                }

                _listVm.Wizyty = new ObservableCollection<WizytaRecord>(filteredWizyty);
                _listVm.Badania = new ObservableCollection<WizytaRecord>(filteredBadania);

                OnPropertyChanged(nameof(Wizyty));
                OnPropertyChanged(nameof(Badania));
            }
            catch
            {
                // ciche pominięcie błędów filtra – nie przerywamy działania UI
            }
        }

        // Keep INotify pattern for compatibility
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
