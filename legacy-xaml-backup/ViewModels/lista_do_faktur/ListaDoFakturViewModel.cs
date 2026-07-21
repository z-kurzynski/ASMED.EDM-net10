using ASMED.WPF.Helpers;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.IO;
using ASMED.WPF.Views;
using ASMED.WPF.Services;
using System.Collections.Generic;
using ASMED.WPF.Models;
using ASMED.WPF.ViewModels.lista_do_faktur;
using System.Reflection; // required for BindingFlags and reflection helpers

namespace ASMED.WPF.ViewModels
{
    public class ListaDoFakturViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        // Badanie (from Badanie table)
        private DateTime? _bad_Data;
        public DateTime? Bad_Data { get => _bad_Data; set { if (_bad_Data != value) { _bad_Data = value; OnPropertyChanged(); } } }

        private DateTime? _bad_Data_Do;
        public DateTime? Bad_Data_Do { get => _bad_Data_Do; set { if (_bad_Data_Do != value) { _bad_Data_Do = value; OnPropertyChanged(); } } }



        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private readonly AccessDbContext _db = new AccessDbContext();
        private readonly PdfExportService _pdfService = new PdfExportService();

        public ObservableCollection<AccessDbContext.ListyBadanDto> ListyBadan { get; } = new();

        private AccessDbContext.ListyBadanDto _selectedLista;
        public AccessDbContext.ListyBadanDto? SelectedLista
        {
            get => _selectedLista;
            set
            {
                if (_selectedLista != value)
                {
                    _selectedLista = value;
                    OnPropertyChanged();
                    LoadAssignedBadania();
                }
            }
        }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged();
                    LoadListyBadan();
                }
            }
        }

        public ICommand ClearSearchTextCommand { get; }
        public ICommand DeleteAssignedBadanieCommand { get; }
        public ICommand EditAssignedBadanieCommand { get; }

        // print commands
        public ICommand PrintListaCommand { get; }
        public ICommand PrintPodstawowyCommand { get; }
        public ICommand PrintSzczegolowyCommand { get; }

        private decimal _totalRazem;
        public decimal TotalRazem
        {
            get => _totalRazem;
            private set
            {
                if (_totalRazem != value)
                {
                    _totalRazem = value;
                    OnPropertyChanged();
                }
            }
        }

        // Selected assigned badanie when editing
        private AccessDbContext.AssignedBadanieDto? _selectedAssignedBadanie;
        public AccessDbContext.AssignedBadanieDto? SelectedAssignedBadanie
        {
            get => _selectedAssignedBadanie;
            set
            {
                _selectedAssignedBadanie = value;
                OnPropertyChanged();
                if (_selectedAssignedBadanie == null) return;

                // map into SelectedWizyta (edit model)
                SelectedWizyta = MapAssignedToEditModel(_selectedAssignedBadanie);

                // Try to extract a stored cennik from the DTO using common property names
                string? found = null;
                try
                {
                    var dto = _selectedAssignedBadanie;
                    var props = dto.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    var candidates = new[] { "Bad_bn_cennik", "BadBnCennik", "Firma_Cennik", "FirmaCennik", "bn_cennik", "Cennik" };
                    foreach (var p in props)
                    {
                        if (candidates.Any(c => string.Equals(c, p.Name, StringComparison.OrdinalIgnoreCase)))
                        {
                            var v = p.GetValue(dto) as string;
                            if (!string.IsNullOrEmpty(v)) { found = v; break; }
                        }
                    }
                }
                catch { }

                UpdateCennikOptions();

                if (!string.IsNullOrEmpty(found)) { if (!CennikOptions.Contains(found)) CennikOptions.Insert(0, found); SelectedCennik = found; }

                // Load full BadanieRecord from DB in VM to populate VM-level fields
                try
                {
                    if (_selectedAssignedBadanie.Bad_ID.HasValue)
                    {
                        var bad = _db.GetBadanieById(_selectedAssignedBadanie.Bad_ID.Value);
                        if (bad != null)
                        {
                            DataBadania = bad.Bad_Data;
                            DataWaznosci = bad.Bad_Data_Do;
                            SelectedWynik = bad.Bad_Wynik ?? SelectedWynik;
                            NrKsiegi = bad.Bad_Nr_KS ?? NrKsiegi;

                            if (!string.IsNullOrEmpty(bad.Bad_bn_cennik)) { if (!CennikOptions.Contains(bad.Bad_bn_cennik)) CennikOptions.Insert(0, bad.Bad_bn_cennik); SelectedCennik = bad.Bad_bn_cennik; }

                            try
                            {
                                // copy into DTO
                                _selectedAssignedBadanie.Bad_Cena1 = bad.Bad_Cena1 ?? _selectedAssignedBadanie.Bad_Cena1;
                                _selectedAssignedBadanie.Bad_Cena2 = bad.Bad_Cena2 ?? _selectedAssignedBadanie.Bad_Cena2;
                                _selectedAssignedBadanie.Bad_Cena3 = bad.Bad_Cena3 ?? _selectedAssignedBadanie.Bad_Cena3;
                                _selectedAssignedBadanie.Bad_Cena4 = bad.Bad_Cena4 ?? _selectedAssignedBadanie.Bad_Cena4;
                                _selectedAssignedBadanie.Bad_Cena5 = bad.Bad_Cena5 ?? _selectedAssignedBadanie.Bad_Cena5;
                                _selectedAssignedBadanie.Bad_Cena6 = bad.Bad_Cena6 ?? _selectedAssignedBadanie.Bad_Cena6;
                                _selectedAssignedBadanie.Bad_Cena7 = bad.Bad_Cena7 ?? _selectedAssignedBadanie.Bad_Cena7;
                                _selectedAssignedBadanie.Bad_Cena8 = bad.Bad_Cena8 ?? _selectedAssignedBadanie.Bad_Cena8;
                                _selectedAssignedBadanie.Bad_Razem = bad.Bad_Razem ?? _selectedAssignedBadanie.Bad_Razem;
                                // copy additional fields into DTO so view bindings see DB values
                                _selectedAssignedBadanie.Bad_Data_Do = bad.Bad_Data_Do ?? _selectedAssignedBadanie.Bad_Data_Do;
                                _selectedAssignedBadanie.Bad_Nr_KS = bad.Bad_Nr_KS ?? _selectedAssignedBadanie.Bad_Nr_KS;
                                _selectedAssignedBadanie.Bad_Typ = bad.Bad_Typ ?? _selectedAssignedBadanie.Bad_Typ;

                                // Enrich DTO with patient and skierowanie data when available
                                try
                                {
                                    // helper to set prop by several possible names on DTO
                                    void TrySetDtoProp(object dtoObj, object? val, params string[] names)
                                    {
                                        if (dtoObj == null || val == null) return;
                                        var props = dtoObj.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                                        foreach (var n in names)
                                        {
                                            var p = Array.Find(props, pp => string.Equals(pp.Name, n, StringComparison.OrdinalIgnoreCase));
                                            if (p != null && p.CanWrite)
                                            {
                                                try
                                                {
                                                    var targetType = p.PropertyType;
                                                    object? toSet = val;
                                                    if (val != null && !targetType.IsAssignableFrom(val.GetType()))
                                                    {
                                                        try { toSet = Convert.ChangeType(val, targetType); } catch { toSet = null; }
                                                    }
                                                    if (toSet != null) p.SetValue(dtoObj, toSet);
                                                }
                                                catch { }
                                                break;
                                            }
                                        }
                                    }

                                    // If Bad_P_ID present in bad record, load patient to get PESEL
                                    if (bad.Bad_P_ID.HasValue)
                                    {
                                        try
                                        {
                                            var pac = _db.GetPacjentById(bad.Bad_P_ID.Value);
                                            if (pac != null)
                                            {
                                                TrySetDtoProp(_selectedAssignedBadanie, pac.PESEL, "P_pesel", "Pesel", "PESEL");
                                                if (SelectedWizyta != null) SelectedWizyta.P_pesel = pac.PESEL;
                                            }
                                        }
                                        catch { }
                                    }

                                    // If Bad_S_ID present, load Skierowanie to get referral date / type and flags
                                    if (bad.Bad_S_ID.HasValue)
                                    {
                                        try
                                        {
                                            var sk = _db.GetSkierowanieById(bad.Bad_S_ID.Value);
                                            if (sk != null)
                                            {
                                                TrySetDtoProp(_selectedAssignedBadanie, sk.B_TypBadania, "B_TypBadania", "B_Typ", "B_Typ_Badania", "TypBadania");
                                                TrySetDtoProp(_selectedAssignedBadanie, sk.B_DataSkierowania, "B_DataSkierowania", "DataSkierowania", "B_Data");

                                                if (SelectedWizyta != null)
                                                {
                                                    SelectedWizyta.B_TypBadania = sk.B_TypBadania;
                                                    SelectedWizyta.B_DataSkierowania = sk.B_DataSkierowania;
                                                    SelectedWizyta.B_ksiazeczka = sk.B_książeczka;
                                                    SelectedWizyta.B_Zaswiadczenie = sk.B_Zaswiadczenie;
                                                }
                                            }
                                        }
                                        catch { }
                                    }
                                }
                                catch { }

                                // also copy into SelectedWizyta so UI bound to SelectedWizyta updates
                                if (SelectedWizyta != null)
                                {
                                    SelectedWizyta.Bad_Data = bad.Bad_Data ?? SelectedWizyta.Bad_Data;
                                    SelectedWizyta.Bad_Data_Do = bad.Bad_Data_Do ?? SelectedWizyta.Bad_Data_Do;
                                    SelectedWizyta.Bad_Wynik = bad.Bad_Wynik ?? SelectedWizyta.Bad_Wynik;
                                    SelectedWizyta.Bad_Nr_KS = bad.Bad_Nr_KS ?? SelectedWizyta.Bad_Nr_KS;
                                    SelectedWizyta.Bad_Razem = bad.Bad_Razem ?? SelectedWizyta.Bad_Razem;
                                    SelectedWizyta.Bad_Cena1 = bad.Bad_Cena1 ?? SelectedWizyta.Bad_Cena1;
                                    SelectedWizyta.Bad_Cena2 = bad.Bad_Cena2 ?? SelectedWizyta.Bad_Cena2;
                                    SelectedWizyta.Bad_Cena3 = bad.Bad_Cena3 ?? SelectedWizyta.Bad_Cena3;
                                    SelectedWizyta.Bad_Cena4 = bad.Bad_Cena4 ?? SelectedWizyta.Bad_Cena4;
                                    SelectedWizyta.Bad_Cena5 = bad.Bad_Cena5 ?? SelectedWizyta.Bad_Cena5;
                                    SelectedWizyta.Bad_Cena6 = bad.Bad_Cena6 ?? SelectedWizyta.Bad_Cena6;
                                    SelectedWizyta.Bad_Cena7 = bad.Bad_Cena7 ?? SelectedWizyta.Bad_Cena7;
                                    SelectedWizyta.Bad_Cena8 = bad.Bad_Cena8 ?? SelectedWizyta.Bad_Cena8;
                                }
                            }
                            catch { }

                            OnPropertyChanged(nameof(SelectedAssignedBadanie));
                            OnPropertyChanged(nameof(SelectedWizyta));

                            // Non-intrusive debug: write loaded Badanie properties to debug output
                            try
                            {
                                var badProps = bad.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                                // System.Diagnostics.Debug.WriteLine("Loaded Badanie properties:");
                                foreach (var p in badProps)
                                {
                                    // try { var val = p.GetValue(bad); System.Diagnostics.Debug.WriteLine($"{p.Name} = {val}"); }
                                    // catch { System.Diagnostics.Debug.WriteLine($"{p.Name} = <error>"); }
                                }
                            }
                            catch { }
                        }
                    }
                }
                catch { }
            }
        }

        // Selected Wizyta (edit model) - maps from AssignedBadanieDto
        private AssignedBadanieEditModel? _selectedWizyta;
        public AssignedBadanieEditModel? SelectedWizyta
        {
            get => _selectedWizyta;
            set
            {
                _selectedWizyta = value;
                OnPropertyChanged();
            }
        }

        // Helper mapper from AssignedBadanieDto -> AssignedBadanieEditModel
        private AssignedBadanieEditModel MapAssignedToEditModel(AccessDbContext.AssignedBadanieDto dto)
        {
            var m = new AssignedBadanieEditModel();
            try
            {
                m.Bad_ID = dto.Bad_ID;
                m.Bad_Data = dto.Bad_Data;
                m.Bad_Data_Do = dto.Bad_Data_Do;
                m.Bad_Wynik = dto.Bad_Wynik;
                m.Bad_Razem = dto.Bad_Razem;
                m.Bad_Nr_KS = dto.Bad_Nr_KS;
                m.Bad_Cena1 = dto.Bad_Cena1;
                m.Bad_Cena2 = dto.Bad_Cena2;
                m.Bad_Cena3 = dto.Bad_Cena3;
                m.Bad_Cena4 = dto.Bad_Cena4;
                m.Bad_Cena5 = dto.Bad_Cena5;
                m.Bad_Cena6 = dto.Bad_Cena6;
                m.Bad_Cena7 = dto.Bad_Cena7;
                m.Bad_Cena8 = dto.Bad_Cena8;
                m.P_imie = dto.P_imie;
                m.P_nazwisko = dto.P_nazwisko;
                m.P_zawod = dto.P_zawod;
                m.FirmaNazwa = dto.FirmaNazwa;

                // use reflection to extract optional fields that may have different names in the DTO
                try
                {
                    var props = dto.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    string? TryString(params string[] names)
                    {
                        foreach (var n in names)
                        {
                            var p = Array.Find(props, pp => string.Equals(pp.Name, n, StringComparison.OrdinalIgnoreCase));
                            if (p != null)
                            {
                                var v = p.GetValue(dto);
                                if (v != null) return v.ToString();
                            }
                        }
                        return null;
                    }

                    DateTime? TryDate(params string[] names)
                    {
                        foreach (var n in names)
                        {
                            var p = Array.Find(props, pp => string.Equals(pp.Name, n, StringComparison.OrdinalIgnoreCase));
                            if (p != null)
                            {
                                var v = p.GetValue(dto);
                                if (v is DateTime dt) return dt;
                                if (v != null && DateTime.TryParse(v.ToString(), out var dt2)) return dt2;
                            }
                        }
                        return null;
                    }

                    m.P_pesel = TryString("P_pesel", "Pesel", "PESEL", "Ppesel");
                    m.B_TypBadania = TryString("B_TypBadania", "B_Typ", "B_Typ_Badania", "B_TypBadanie", "TypBadania");
                    m.B_DataSkierowania = TryDate("B_DataSkierowania", "DataSkierowania", "B_Data", "Data_Skierowania");
                }
                catch { }
            }
            catch { }
            return m;
        }

        // Request event for view to open editor for a specific assigned badanie
        public event Action<AssignedBadanieEditModel>? EditAssignedBadanieRequested;

#pragma warning disable CS8618 // Pole niedopuszczające wartości null musi zawierać wartość inną niż null podczas kończenia działania konstruktora. Rozważ dodanie modyfikatora „required” lub zadeklarowanie go jako dopuszczającego wartość null.
        public ListaDoFakturViewModel()
#pragma warning restore CS8618 // Pole niedopuszczające wartości null musi zawierać wartość inną niż null podczas kończenia działania konstruktora. Rozważ dodanie modyfikatora „required” lub zadeklarowanie go jako dopuszczającego wartość null.
        {
            ClearSearchTextCommand = new RelayCommand<object>(_ => { SearchText = string.Empty; });
            DeleteAssignedBadanieCommand = new RelayCommand<object>(DeleteAssignedBadanie);
            EditAssignedBadanieCommand = new RelayCommand<object>(EditAssignedBadanie);

            PrintListaCommand = new RelayCommand<object>(_ => ExecutePrint(ExportType.Lista));
            PrintPodstawowyCommand = new RelayCommand<object>(_ => ExecutePrint(ExportType.Podstawowy));
            PrintSzczegolowyCommand = new RelayCommand<object>(_ => ExecutePrint(ExportType.Szczegolowy));

            LoadListyBadan();

            // load cennik options from DB similar to BadaniaViewModel
            try
            {
                var repo = new WizytyRepository();
                var cenniki = repo.GetCennikOptions();
                if (cenniki != null)
                {
                    foreach (var c in cenniki)
                    {
                        if (!CennikOptions.Contains(c)) CennikOptions.Add(c);
                    }
                }
                // ensure a sensible default present (match legacy default used elsewhere)
                if (!CennikOptions.Contains("Szkoły")) CennikOptions.Insert(0, "Szkoły");
            }
            catch { }
        }

        private void LoadListyBadan()
        {
            try
            {
                ListyBadan.Clear();
                var rows = _db.GetListyBadan(string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim());
                foreach (var r in rows)
                    ListyBadan.Add(r);

                // select first if any
                if (ListyBadan.Any())
                    SelectedLista = ListyBadan.First();
                else
                    SelectedLista = null;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Błąd ładowania listy badań: {ex.Message}", "Błąd", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void LoadAssignedBadania()
        {
            try
            {
                TotalRazem = 0m;

                if (SelectedLista == null || !SelectedLista.Identyfikator.HasValue)
                {
                    // clear if no selection
                    if (SelectedLista != null)
                    {
                        SelectedLista.Badania = new ObservableCollection<AccessDbContext.AssignedBadanieDto>();
                        OnPropertyChanged(nameof(SelectedLista));
                    }
                    return;
                }

                var badania = _db.GetBadaniaForLista(SelectedLista.Identyfikator.Value);
                // assign1-based Lp values
                for (int i = 0; i < badania.Count; i++)
                {
                    badania[i].Lp = i + 1;
                }

                // convert list -> ObservableCollection and assign
                SelectedLista.Badania = new ObservableCollection<AccessDbContext.AssignedBadanieDto>(badania);

                // compute total for Bad_Razem
                TotalRazem = SelectedLista.Badania.Sum(b => b.Bad_Razem ?? 0m);

                // notify that SelectedLista changed so UI updates Badania binding
                OnPropertyChanged(nameof(SelectedLista));
                OnPropertyChanged(nameof(TotalRazem));
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Błąd ładowania badań dla listy: {ex.Message}", "Błąd", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void DeleteAssignedBadanie(object obj)
        {
            // obj expected to be AccessDbContext.AssignedBadanieDto
            if (obj is AccessDbContext.AssignedBadanieDto dto)
            {
                try
                {
                    if (!dto.Bad_ID.HasValue)
                    {
                        MessageBox.Show("Brak identyfikatora badania (Bad_ID). Nie można usunąć powiązania.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var confirm = MessageBox.Show($"Czy na pewno usunąć powiązanie badania (ID = {dto.Bad_ID.Value}) z listą?", "Potwierdź", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (confirm != MessageBoxResult.Yes) return;

                    var ok = _db.UnassignBadanieFromLista(dto.Bad_ID.Value, "@Bad_Fakt");
                    if (ok)
                    {
                        NotificationHelper.ShowInfo("Usunięto powiązanie badania z listą", $"Bad_ID = {dto.Bad_ID.Value}");
                        // refresh assigned items
                        LoadAssignedBadania();
                    }
                    else
                    {
                        MessageBox.Show("Nie udało się usunąć powiązania badania.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Błąd usuwania badania: {ex.Message}", "Błąd", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }

        private void EditAssignedBadanie(object obj)
        {
            if (obj is AccessDbContext.AssignedBadanieDto dto)
            {
                try
                {
                    SelectedAssignedBadanie = dto; // keep DTO around and let setter load full Badanie and update SelectedWizyta
                    // do not override SelectedWizyta here - setter already maps and enriches it
                    EditAssignedBadanieRequested?.Invoke(SelectedWizyta);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Błąd inicjowania edycji badania: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                System.Windows.MessageBox.Show("Edytuj badanie - brak danych wiersza", "Info", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
        }

        // Print handling using PdfExportService
        private void ExecutePrint(ExportType exportType)
        {
            try
            {
                if (SelectedLista == null)
                {
                    MessageBox.Show("Brak wybranej listy do wydruku", "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // map AssignedBadanieDto to InvoiceItem
                var source = SelectedLista.Badania ?? new ObservableCollection<AccessDbContext.AssignedBadanieDto>();
                var items = source
                    .Select(b => new InvoiceItem
                    {
                        FirstName = b.P_imie,
                        LastName = b.P_nazwisko,
                        BadType = b.Bad_Typ, // <- dodane
                        Total = b.Bad_Razem ?? 0m,
                        ExaminationPrice = b.Bad_Cena1 ?? 0m,
                        LaryngologistPrice = b.Bad_Cena2 ?? 0m,
                        OphthalmologistPrice = b.Bad_Cena3 ?? 0m,
                        SanitaryPrice = b.Bad_Cena4 ?? 0m,
                        OtherPrice = b.Bad_Cena8 ?? 0m,
                        LipidogramPrice = b.Bad_Cena5 ?? 0m,
                        EKGPrice = b.Bad_Cena6 ?? 0m,
                        HealthClinicPrice = b.Bad_Cena7 ?? 0m
                    }).ToList();

                // przekaż datę faktury (może być null)
                //DateTime? invoiceDate = SelectedLista.FK_Data;
                DateTime? invoiceDate = SelectedLista?.FK_Data ?? DataWystawienia;

                string path = _pdfService.ExportToPdf(
                    items,
                    (Services.ExportType)exportType,
                    SelectedLista?.FK_Numer ?? string.Empty,
                    SelectedLista?.Nazwa ?? string.Empty,
                    SelectedLista?.FK_Data ?? DataWystawienia // <-- dodano wymagany argument invoiceDate typu DateTime?
                );

                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                {
                    MessageBox.Show("Błąd podczas tworzenia pliku PDF", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // ✅ NOWE: Pobierz email firmy (z domyślnym adresem jeśli brak)
                var emailAddress = _db.GetFirmaEmailById(SelectedLista?.L_Firma_ID);
                var numerFaktury = SelectedLista?.FK_Numer ?? string.Empty;

                var preview = new PdfPreviewWindow();
                preview.LoadFileWithMetadata(path, emailAddress, numerFaktury);
                preview.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd drukowania: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Przycisk do otwierania e-maila z załącznikami
        private void SendEmail_Click(object? sender, RoutedEventArgs e)
        { }

        // Public refresh methods so views can request reload after edits
        public void RefreshFromDb()
        {
            LoadListyBadan();
        }

        public void RefreshAssignedForSelected()
        {
            LoadAssignedBadania();
        }

        // Properties used by the Edit view (mirrors BadaniaView VM surface)
        private DateTime? _dataBadania;
        public DateTime? DataBadania
        {
            get => _dataBadania;
            set { if (_dataBadania != value) { _dataBadania = value; OnPropertyChanged(); } }
        }

        private DateTime? _dataWaznosci;
        public DateTime? DataWaznosci
        {
            get => _dataWaznosci;
            set { if (_dataWaznosci != value) { _dataWaznosci = value; OnPropertyChanged(); } }
        }

        private string? _selectedWynik;
        public string? SelectedWynik
        {
            get => _selectedWynik;
            set { if (_selectedWynik != value) { _selectedWynik = value; OnPropertyChanged(); } }
        }

        private string? _nrKsiegi;
        public string? NrKsiegi
        {
            get => _nrKsiegi;
            set { if (_nrKsiegi != value) { _nrKsiegi = value; OnPropertyChanged(); } }
        }

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



        // Optional option lists for bindings
        public List<string> WynikOptions { get; } = new List<string> { "Pozytywny", "Negatywny", "Brak" };
        // CennikOptions should notify UI when changed -> use ObservableCollection
        public ObservableCollection<string> CennikOptions { get; } = new ObservableCollection<string>(new[] { "Szkoły", "Podstawowy", "Firmowy", "Inny" });

        private bool? _bKsiazeczka;
        public bool? B_Ksiazeczka
        {
            get => _bKsiazeczka;
            set { if (_bKsiazeczka != value) { _bKsiazeczka = value; OnPropertyChanged(); } }
        }

        private bool? _bZaswiadczenie;
        public bool? B_Zaswiadczenie
        {
            get => _bZaswiadczenie;
            set { if (_bZaswiadczenie != value) { _bZaswiadczenie = value; OnPropertyChanged(); } }
        }

        // backing fields for cennik prices
        private decimal? _priceBasic = null;
        private decimal? _priceLaryngologist = null;
        private decimal? _priceOphthalmologist = null;
        private decimal? _priceSanitary = null;
        private decimal? _priceLipidogram = null;
        private decimal? _priceEKG = null;
        private decimal? _priceHealthClinic = null;
        private decimal? _priceOther = null;

        // Formatted properties bound from XAML Grid_Ceny
        public string PriceBasicText => FormatPrice(_priceBasic);
        public string PriceLaryngologistText => FormatPrice(_priceLaryngologist);
        public string PriceOphthalmologistText => FormatPrice(_priceOphthalmologist);
        public string PriceSanitaryText => FormatPrice(_priceSanitary);
        public string PriceLipidogramText => FormatPrice(_priceLipidogram);
        public string PriceEKGText => FormatPrice(_priceEKG);
        public string PriceHealthClinicText => FormatPrice(_priceHealthClinic);
        public string PriceOtherText => FormatPrice(_priceOther);

        public DateTime DataWystawienia { get; private set; }
        public string? WybranaFirmaName { get; internal set; }

        private string FormatPrice(decimal? v)
        {
            if (!v.HasValue) return string.Empty;
            return string.Format(System.Globalization.CultureInfo.GetCultureInfo("pl-PL"), "{0:N2} zł", v.Value);
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
                        if (keys == null || keys.Length == 0) return null;
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

        private void UpdateCennikOptions()
        {
            // ensure currently selected cennik is present in options
            if (!string.IsNullOrEmpty(SelectedCennik) && !CennikOptions.Contains(SelectedCennik))
            {
                CennikOptions.Insert(0, SelectedCennik);
            }
        }
    }
}
