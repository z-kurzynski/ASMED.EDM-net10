using ASMED.WPF.Helpers;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Linq;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Globalization;
using static ASMED.WPF.Helpers.AccessDbContext;

namespace ASMED.WPF.ViewModels
{
    public class FakturaViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private const Visibility collapsed = Visibility.Collapsed;
        private readonly AccessDbContext _db = new AccessDbContext();

        public ObservableCollection<AccessDbContext.FakturaDto> AllFaktura { get; } = new();
        public ObservableCollection<AccessDbContext.FakturaDto> FilteredFaktura { get; } = new();

        public ObservableCollection<string> FilterTypes { get; } = new() { "All", "Firma", "Numer", "NIP", "Lista", "ID" };

        // NOWE: Opcje filtrowania dat
        public ObservableCollection<string> DateFilterOptions { get; } = new()
        {
            "All",
            "Bieżący Miesiąc",
            "Poprzedni Miesiąc",
            "Bieżący Rok",
            "Poprzedni Rok",
            "Wybrany okres"
        };

        private string?_searchText = string.Empty;
        public string?SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged();
                    FilterFaktura();
                }
            }
        }

        private string?_activeFilterType = "All";
        public string?ActiveFilterType
        {
            get => _activeFilterType;
            set
            {
                if (_activeFilterType != value)
                {
                    _activeFilterType = value;
                    OnPropertyChanged();
                    FilterFaktura();
                }
            }
        }

        // NOWE: Wybrany filtr okresu
        private string?_selectedDateFilter = "All";
        public string?SelectedDateFilter
        {
            get => _selectedDateFilter;
            set
            {
                if (_selectedDateFilter != value)
                {
                    _selectedDateFilter = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsCustomDateRangeVisible));
                    FilterFaktura();
                }
            }
        }

        // NOWE: Daty dla "Wybrany okres"
        private DateTime? _filterDateFrom;
        public DateTime? FilterDateFrom
        {
            get => _filterDateFrom;
            set
            {
                if (_filterDateFrom != value)
                {
                    _filterDateFrom = value;
                    OnPropertyChanged();
                    if (SelectedDateFilter == "Wybrany okres")
                        FilterFaktura();
                }
            }
        }

        private DateTime? _filterDateTo;
        public DateTime? FilterDateTo
        {
            get => _filterDateTo;
            set
            {
                if (_filterDateTo != value)
                {
                    _filterDateTo = value;
                    OnPropertyChanged();
                    if (SelectedDateFilter == "Wybrany okres")
                        FilterFaktura();
                }
            }
        }

        // NOWE: Widoczność panelu dat niestandardowych
        public bool IsCustomDateRangeVisible => SelectedDateFilter == "Wybrany okres";

        public ICommand ?ClearSearchTextCommand { get; }
        public ICommand ?ApplyCustomDateFilterCommand { get; }

        // --- pola dla nowej faktury ---
        private int? _selectedFirmaId;
        public int? SelectedFirmaId { get => _selectedFirmaId; set { if (_selectedFirmaId != value) { _selectedFirmaId = value; OnPropertyChanged(); } } }

        private string?_newFirmaName;
        public string? NewFirmaName { get => _newFirmaName; set { if (_newFirmaName != value) { _newFirmaName = value; OnPropertyChanged(); } } }

        private string?_newNumer;
        public string? NewNumer { get => _newNumer; set { if (_newNumer != value) { _newNumer = value; OnPropertyChanged(); } } }

        private DateTime? _newData = DateTime.Today;
        public DateTime? NewData { get => _newData; set { if (_newData != value) { _newData = value; OnPropertyChanged(); } } }

        private decimal? _newKwota;
        public decimal? NewKwota { get => _newKwota; set { if (_newKwota != value) { _newKwota = value; OnPropertyChanged(); } } }

        private string?_newPdfPath;
        public string? NewPdfPath { get => _newPdfPath; set { if (_newPdfPath != value) { _newPdfPath = value; OnPropertyChanged(); } } }

        private decimal _newBadSuma;
        public decimal NewBadSuma { get => _newBadSuma; set { if (_newBadSuma != value) { _newBadSuma = value; OnPropertyChanged(); } } }


        // status: 1-Nowa,2-Lista,3-Zamknięta,4-Anulowana
        private int _newStatus = 1;
        public int NewStatus { get => _newStatus; set { if (_newStatus != value) { _newStatus = value; OnPropertyChanged(); } } }

        public ICommand ?SaveFakturaCommand { get; }

        private RelayCommand<object>? _updateFakturaCommand;
        public ICommand ?UpdateFakturaCommand => _updateFakturaCommand ??= new RelayCommand<object>(UpdateFaktura);

        // ✅ DODANE: Command do usuwania faktury
        private RelayCommand<object>? _deleteFakturaCommand;
        public ICommand ?DeleteFakturaCommand => _deleteFakturaCommand ??= new RelayCommand<object>(DeleteFaktura, CanDeleteFaktura);

        public FakturaViewModel()
        {
            ClearSearchTextCommand = new RelayCommand<object>(_ => { SearchText = string.Empty; });
            ApplyCustomDateFilterCommand = new RelayCommand<object>(_ => FilterFaktura());

            // subskrybuj zmiany kolekcji do przeliczania sum
            FilteredFaktura.CollectionChanged += (_, __) => RecalculateTotals();

            SaveFakturaCommand = new RelayCommand<object>(async _ => await ExecuteSaveFakturaAsync(GetNewBadSuma()), _ => CanSaveFaktura());
        }

        private bool CanSaveFaktura()
        {
            return SelectedFirmaId.HasValue && !string.IsNullOrWhiteSpace(NewNumer);
        }

        private decimal GetNewBadSuma()
        {
            return NewBadSuma;
        }

        private async Task ExecuteSaveFakturaAsync(decimal NewBadSuma)
        {
            try
            {
                // prosta walidacja
                if (!SelectedFirmaId.HasValue)
                {
                    MessageBox.Show("Wybierz firmę.", "Walidacja", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (string.IsNullOrWhiteSpace(NewNumer))
                {
                    MessageBox.Show("Podaj numer faktury.", "Walidacja", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var firmaId = SelectedFirmaId.Value;
                var numer = NewNumer.Trim();
                var data = NewData ?? DateTime.Today;
                var kwota = NewKwota ?? 0m;
                var status = NewStatus;
                var badSuma = this.NewBadSuma;
                var pdf = string.IsNullOrWhiteSpace(NewPdfPath) ? null : NewPdfPath;

                // zapis w tle
                var newId = await Task.Run(() => _db.AddFaktura(firmaId, numer, data, kwota, status, pdf, badSuma));

                if (newId > 0)
                {
                    // MessageBox.Show($"Zapisano fakturę (ID = {newId})", "OK", MessageBoxButton.OK, MessageBoxImage.Information);
                    NotificationHelper.ShowInfo("Faktura zapisana", $"Faktura ID={newId} została zapisana.");

                    // odśwież listę
                    RefreshFromDb();
                    // wyczyść pola edycji
                    // czyścimy firme
                    SelectedFirmaId = null;
                    NewFirmaName = string.Empty;
                    NewNumer = string.Empty;
                    NewData = DateTime.Today;
                    NewKwota = null;
                    NewPdfPath = string.Empty;
                    NewBadSuma = 0m;
                    NewStatus = 1;
                    // czyścimy firme

                }
                else
                {
                    MessageBox.Show("Nie udało się zapisać faktury.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd zapisu faktury: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateFaktura(object? parameter)
        {
            if (parameter is not FakturaDto faktura) return;

            try
            {
                var db = new AccessDbHelper();
                using var conn = db.GetConnection();
                conn.Open();
                using var cmd = conn.CreateCommand();

                cmd.CommandText = @"UPDATE Faktura SET 
            FK_Numer = ?,
            FK_Data = ?,
            FK_Kwota = ?,
            FK_Suma_Bad = ?,
            FK_Saldo = ?
            WHERE FK_ID = ?";

                var p1 = cmd.CreateParameter(); p1.Value = faktura.Numer_Faktury ?? (object)DBNull.Value; cmd.Parameters.Add(p1);
                var p2 = cmd.CreateParameter(); p2.Value = faktura.Data ?? (object)DBNull.Value; cmd.Parameters.Add(p2);
                var p3 = cmd.CreateParameter(); p3.Value = faktura.Kwota.HasValue ? (object)faktura.Kwota.Value : DBNull.Value; cmd.Parameters.Add(p3);
                var p4 = cmd.CreateParameter(); p4.Value = faktura.Kwota_B.HasValue ? (object)faktura.Kwota_B.Value : DBNull.Value; cmd.Parameters.Add(p4);
                var p5 = cmd.CreateParameter(); p5.Value = faktura.Saldo.HasValue ? (object)faktura.Saldo.Value : DBNull.Value; cmd.Parameters.Add(p5);
                var p6 = cmd.CreateParameter(); p6.Value = faktura.Id; cmd.Parameters.Add(p6);

                cmd.ExecuteNonQuery();

                NotificationHelper.ShowInfo("Faktura zaktualizowana", $"ID = {faktura.Id}");
                RefreshFromDb();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas aktualizacji faktury:\n{ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ✅ DODANE: Sprawdza czy faktura może być usunięta (FK_Num_Listy = 0 lub null)
        private bool CanDeleteFaktura(object? parameter)
        {
            if (parameter is not FakturaDto faktura) return false;

            // Faktura może być usunięta tylko gdy nie ma przypisanej listy (Lista = null, "0" lub puste)
            return string.IsNullOrWhiteSpace(faktura.Lista) || faktura.Lista == "0";
        }

        // ✅ DODANE: Metoda usuwająca fakturę
        private void DeleteFaktura(object? parameter)
        {
            if (parameter is not FakturaDto faktura) return;

            try
            {
                // Dodatkowa walidacja
                if (!CanDeleteFaktura(faktura))
                {
                    MessageBox.Show("Nie można usunąć faktury przypisanej do listy badań.\nOdłącz najpierw listę od faktury.",
                        "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Potwierdzenie usunięcia
                var result = MessageBox.Show(
                    $"Czy na pewno usunąć fakturę?\n\nNumer: {faktura.Numer_Faktury}\nFirma: {faktura.Firma}\nKwota: {faktura.Kwota:N2} zł",
                    "Potwierdzenie usunięcia",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes) return;

                var db = new AccessDbHelper();
                using var conn = db.GetConnection();
                conn.Open();
                using var cmd = conn.CreateCommand();

                cmd.CommandText = "DELETE FROM Faktura WHERE FK_ID = ?";
                var p1 = cmd.CreateParameter();
                p1.Value = faktura.Id;
                cmd.Parameters.Add(p1);

                int rows = cmd.ExecuteNonQuery();

                if (rows > 0)
                {
                    NotificationHelper.ShowInfo("Faktura usunięta", $"ID = {faktura.Id}");
                    RefreshFromDb();
                }
                else
                {
                    MessageBox.Show("Nie udało się usunąć faktury.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas usuwania faktury:\n{ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadFromDb()
        {
            throw new NotImplementedException();
        }

        // Pozostała część ViewModelu: Load/Filter/RecalculateTotals (bez zmian)
        public void RefreshFromDb()
        {
            _ = LoadFromDbAsync();
        }

        private async Task LoadFromDbAsync()
        {
            try
            {
                var list = await Task.Run(() => _db.GetFaktury(2000));

                var disp = Application.Current?.Dispatcher;
                if (disp != null && !disp.CheckAccess())
                {
                    disp.Invoke(() =>
                    {
                        AllFaktura.Clear();
                        foreach (var f in list) AllFaktura.Add(f);
                        FilterFaktura();
                    });
                }
                else
                {
                    AllFaktura.Clear();
                    foreach (var f in list) AllFaktura.Add(f);
                    FilterFaktura();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd ładowania faktur: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FilterFaktura()
        {
            FilteredFaktura.Clear();
            var raw = (SearchText ?? string.Empty).Trim();
            var text = raw.ToLowerInvariant();

            // 1. Oblicz zakres dat na podstawie wybranego filtru
            DateTime? dateFrom = null;
            DateTime? dateTo = null;

            switch (SelectedDateFilter)
            {
                case "Bieżący Miesiąc":
                    dateFrom = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                    dateTo = dateFrom.Value.AddMonths(1).AddDays(-1);
                    break;

                case "Poprzedni Miesiąc":
                    dateFrom = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-1);
                    dateTo = dateFrom.Value.AddMonths(1).AddDays(-1);
                    break;

                case "Bieżący Rok":
                    dateFrom = new DateTime(DateTime.Now.Year, 1, 1);
                    dateTo = new DateTime(DateTime.Now.Year, 12, 31);
                    break;

                case "Poprzedni Rok":
                    dateFrom = new DateTime(DateTime.Now.Year - 1, 1, 1);
                    dateTo = new DateTime(DateTime.Now.Year - 1, 12, 31);
                    break;

                case "Wybrany okres":
                    dateFrom = FilterDateFrom;
                    dateTo = FilterDateTo;
                    break;

                case "All":
                default:
                    // Brak filtra daty
                    break;
            }

            // 2. Filtruj faktury
            foreach (var f in AllFaktura)
            {
                // 2a. Filtr daty
                bool dateMatch = true;
                if (dateFrom.HasValue && f.Data.HasValue && f.Data.Value < dateFrom.Value)
                    dateMatch = false;
                if (dateTo.HasValue && f.Data.HasValue && f.Data.Value > dateTo.Value)
                    dateMatch = false;

                if (!dateMatch)
                    continue;

                // 2b. Filtr tekstowy
                bool textMatch = false;
                if (string.IsNullOrEmpty(text))
                {
                    textMatch = true;
                }
                else
                {
                    switch (ActiveFilterType)
                    {
                        case "Firma":
                            textMatch = (f.Firma ?? "").ToLowerInvariant().Contains(text);
                            break;
                        case "Numer":
                            textMatch = (f.Numer_Faktury ?? "").ToLowerInvariant().Contains(text);
                            break;
                        case "NIP":
                            textMatch = (f.NIP ?? "").ToLowerInvariant().Contains(text);
                            break;
                        case "Lista":
                            textMatch = (f.Lista ?? "").ToLowerInvariant().Contains(text);
                            break;
                        case "ID":
                            textMatch = f.Id.ToString().Contains(raw);
                            break;
                        case "All":
                        default:
                            textMatch = (f.Firma ?? "").ToLowerInvariant().Contains(text)
                                || (f.Numer_Faktury ?? "").ToLowerInvariant().Contains(text)
                                || (f.NIP ?? "").ToLowerInvariant().Contains(text)
                                || (f.Lista ?? "").ToLowerInvariant().Contains(text)
                                || f.Id.ToString().Contains(raw);
                            break;
                    }
                }

                if (textMatch)
                    FilteredFaktura.Add(f);
            }

            RecalculateTotals();

            OnPropertyChanged(nameof(FilteredFaktura));
        }

        private decimal _totalKwota;
        public decimal TotalKwota
        {
            get => _totalKwota;
            private set
            {
                if (_totalKwota != value)
                {
                    _totalKwota = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TotalKwotaText));
                }
            }
        }

        private decimal _totalKwotaB;
        public decimal TotalKwotaB
        {
            get => _totalKwotaB;
            private set
            {
                if (_totalKwotaB != value)
                {
                    _totalKwotaB = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TotalKwotaBText));
                }
            }
        }

        private decimal _totalSaldo;
        public decimal TotalSaldo
        {
            get => _totalSaldo;
            private set
            {
                if (_totalSaldo != value)
                {
                    _totalSaldo = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TotalSaldoText));
                }
            }
        }

        public string TotalKwotaText => FormatMoney(TotalKwota);
        public string TotalKwotaBText => FormatMoney(TotalKwotaB);
        public string TotalSaldoText => FormatMoney(TotalSaldo);

        private string FormatMoney(decimal v)
        {
            return string.Format(CultureInfo.GetCultureInfo("pl-PL"), "{0:N2} zł", v);
        }

        private void RecalculateTotals()
        {
            try
            {
                TotalKwota = FilteredFaktura.Sum(f => f.Kwota ?? 0m);
                TotalKwotaB = FilteredFaktura.Sum(f => f.Kwota_B ?? 0m);
                TotalSaldo = FilteredFaktura.Sum(f => f.Saldo ?? 0m);
            }
            catch
            {
                TotalKwota = TotalKwotaB = TotalSaldo = 0m;
            }
        }

        // public helper used przez view-based totals calculation (opcjonalnie)
        public void SetTotalsFromView(decimal kwota, decimal kwotaB, decimal saldo)
        {
            TotalKwota = kwota;
            TotalKwotaB = kwotaB;
            TotalSaldo = saldo;
        }

        // pomocnicze testowe pole
        private string?_testText = "TEST: ViewModel Faktura działa";

        public string?TestText
        {
            get => _testText;
            set
            {
                if (_testText != value)
                {
                    _testText = value;
                    OnPropertyChanged();
                }
            }
        }

        public object NewInvoice { get; private set; }
    }
}
// Koniec pliku FakturaViewModel.cs
