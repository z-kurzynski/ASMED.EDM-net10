using ASMED.WPF.Helpers;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using static ASMED.WPF.Helpers.AccessDbContext;

namespace ASMED.WPF.ViewModels.lista_do_faktur
{
    public class ArchiveImportViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private readonly ArchiveImportRepository _archiveRepo;
        private readonly AccessDbContext _dbContext;

        private readonly AccessDbContext _db = new AccessDbContext();

        public ObservableCollection<ArchiveListRecord> ArchiveRecords { get; } = new();

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _statusMessage = "Gotowy do importu";
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (_statusMessage != value)
                {
                    _statusMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _progressText = "";
        public string ProgressText
        {
            get => _progressText;
            set
            {
                if (_progressText != value)
                {
                    _progressText = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _isProcessing = false;
        public bool IsProcessing
        {
            get => _isProcessing;
            set
            {
                if (_isProcessing != value)
                {
                    _isProcessing = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand SearchCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand SelectAllCommand { get; }
        public ICommand DeselectAllCommand { get; }
        public ICommand ImportSelectedCommand { get; }

        public ArchiveImportViewModel()
        {
            _archiveRepo = new ArchiveImportRepository();
            _dbContext = new AccessDbContext();

            SearchCommand = new RelayCommand<object>(_ => LoadArchiveRecords());
            RefreshCommand = new RelayCommand<object>(_ => LoadArchiveRecords());
            SelectAllCommand = new RelayCommand<object>(_ => SelectAll());
            DeselectAllCommand = new RelayCommand<object>(_ => DeselectAll());
            ImportSelectedCommand = new RelayCommand<object>(async _ => await ImportSelected(), _ => !IsProcessing);

            LoadArchiveRecords();
        }

        private void LoadArchiveRecords()
        {
            try
            {
                StatusMessage = "�adowanie danych z archiwum...";
                ArchiveRecords.Clear();

                // Zamiana typ�w rekord�w na w�a�ciwe
                var records = _archiveRepo.GetArchiveListRecords(string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim());

                foreach (var record in records)
                {
                    // Mapowanie ArchiveImportRepository.ArchiveListRecord na AccessDbContext.ArchiveListRecord
                    var mapped = new AccessDbContext.ArchiveListRecord
                    {
                        IsSelected = record.IsSelected,
                        Identyfikator = record.Identyfikator,
                        Lx_ID_Faktura = record.Lx_ID_Faktura,
                        Lx_ID_Firma = record.Lx_ID_Firma,
                        Lx_Data = record.Lx_Data,
                        Lx_ID_Badania = record.Lx_ID_Badania,
                        PacjentDisplay = record.PacjentDisplay,
                        Lx_Faktura = record.Lx_Faktura,
                        Lx_Firma = record.Lx_Firma,
                        Lx_Imie = record.Lx_Imie,
                        Lx_Nazwisko = record.Lx_Nazwisko,
                        Lx_Razem = record.Lx_Razem,
                        Lx_Uwagi = record.Lx_Uwagi,
                        Lx_Cena1 = record.Lx_Cena1,
                        Lx_Cena2 = record.Lx_Cena2,
                        Lx_Cena3 = record.Lx_Cena3,
                        Lx_Cena4 = record.Lx_Cena4,
                        Lx_Cena5 = record.Lx_Cena5,
                        Lx_Cena6 = record.Lx_Cena6,
                        Lx_Cena7 = record.Lx_Cena7,
                        Lx_Cena9 = record.Lx_Cena9,
                        Lx_ID_pacjent = record.Lx_ID_pacjent,
                        Lx_ID_Skierowania = record.Lx_ID_Skierowania
                    };
                    ArchiveRecords.Add(mapped);
                }

                StatusMessage = $"Za�adowano {ArchiveRecords.Count} rekord�w z archiwum";
            }
            catch (Exception ex)
            {
                StatusMessage = "B��d �adowania danych";
                MessageBox.Show($"B��d �adowania archiwum:\n{ex.Message}", "B��d", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SelectAll()
        {
            foreach (var record in ArchiveRecords)
            {
                record.IsSelected = true;
            }
            StatusMessage = $"Zaznaczono wszystkie rekordy ({ArchiveRecords.Count})";
        }

        private void DeselectAll()
        {
            foreach (var record in ArchiveRecords)
            {
                record.IsSelected = false;
            }
            StatusMessage = "Odznaczono wszystkie rekordy";
        }

        private async Task ImportSelected()
        {
            var selected = ArchiveRecords.Where(r => r.IsSelected).ToList();

            if (selected.Count == 0)
            {
                MessageBox.Show("Nie zaznaczono �adnych rekord�w do importu.", "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Potwierdzenie
            var result = MessageBox.Show(
                $"Czy na pewno zaimportowa� {selected.Count} zaznaczonych rekord�w?\n\n" +
                "Ta operacja:\n" +
                "� Utworzy rekordy tabeli Skierowanie\n" +
                "� Utworzy rekordy w tabeli Badanie\n" +
                "� Utworzy listy bada� w tabeli ListyBadan\n" +
                "� Zaktualizuje powi�zania z fakturami\n" +
                "� Oznaczy rekordy w archiwum jako przetworzone",
                "Potwierdzenie importu",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            IsProcessing = true;
            StatusMessage = "Importowanie...";

            try
            {
                await Task.Run(() => ProcessImport(selected));

                // Od�wie� list�
                LoadArchiveRecords();

                MessageBox.Show($"Pomy�lnie zaimportowano {selected.Count} rekord�w!", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"B��d podczas importu:\n{ex.Message}", "B��d", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusMessage = "B��d importu";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private void ProcessImport(List<ArchiveListRecord> records)
        {
            // Grupuj po Lx_ID_Faktura
            var groupedByFaktura = records.GroupBy(r => r.Lx_ID_Faktura).Where(g => g.Key.HasValue);

            int processed = 0;
            int total = groupedByFaktura.Count();

            foreach (var fakturaGroup in groupedByFaktura)
            {
                processed++;
                ProgressText = $"Przetwarzanie faktury {processed}/{total}...";

                try
                {
                    var fakturaId = fakturaGroup.Key.Value;
                    var recordsForFaktura = fakturaGroup.ToList();

                    // ???????????????????????????????????????????????????????
                    // ETAP 1-4: Dodaj badania (z pacjentami i skierowaniami)
                    // ???????????????????????????????????????????????????????
                    var badaniaIds = new List<int>();
                    foreach (var record in recordsForFaktura)
                    {
                        // Mapowanie na typ z ArchiveImportRepository
                        var repoRecord = new ArchiveImportRepository.ArchiveListRecord
                        {
                            Identyfikator = record.Identyfikator,
                            Lx_ID_Faktura = record.Lx_ID_Faktura,
                            Lx_ID_Firma = record.Lx_ID_Firma,
                            Lx_Data = record.Lx_Data,
                            Lx_ID_Badania = record.Lx_ID_Badania,
                            Lx_Faktura = record.Lx_Faktura,
                            Lx_Firma = record.Lx_Firma,
                            Lx_Imie = record.Lx_Imie,
                            Lx_Nazwisko = record.Lx_Nazwisko,
                            Lx_Razem = record.Lx_Razem,
                            Lx_Uwagi = record.Lx_Uwagi,
                            Lx_Cena1 = record.Lx_Cena1,
                            Lx_Cena2 = record.Lx_Cena2,
                            Lx_Cena3 = record.Lx_Cena3,
                            Lx_Cena4 = record.Lx_Cena4,
                            Lx_Cena5 = record.Lx_Cena5,
                            Lx_Cena6 = record.Lx_Cena6,
                            Lx_Cena7 = record.Lx_Cena7,
                            Lx_Cena9 = record.Lx_Cena9,
                            Lx_ID_pacjent = record.Lx_ID_pacjent,
                            Lx_ID_Skierowania = record.Lx_ID_Skierowania
                        };

                        // ? NOWA METODA: ImportArchiveRecord (wykonuje kroki 1-4)
                        int badId = _archiveRepo.ImportArchiveRecord(repoRecord);
                        if (badId > 0)
                        {
                            badaniaIds.Add(badId);
                        }
                    }

                    if (badaniaIds.Count == 0)
                    {
                        // System.Diagnostics.Debug.WriteLine($"ProcessImport: ?? Brak bada� dla faktury {fakturaId}, pomijam...");
                        continue;
                    }

                    // ???????????????????????????????????????????????????????
                    // ETAP 5: Walidacja/Utworzenie FAKTURY
                    // ???????????????????????????????????????????????????????
                    // TODO: Tutaj dodamy walidacj� czy faktura istnieje w tabeli Faktura
                    // Na razie zak�adamy, �e fakturaId jest poprawne

                    // ???????????????????????????????????????????????????????
                    // ETAP 6: Sprawdzenie czy LISTA ju� istnieje dla tej faktury
                    // ???????????????????????????????????????????????????????
                    int? existingListaId = _archiveRepo.CheckIfListaExists(fakturaId);
                    if (existingListaId.HasValue)
                    {
                        // System.Diagnostics.Debug.WriteLine($"ProcessImport: ?? Lista {existingListaId.Value} ju� istnieje dla faktury {fakturaId}!");
                        MessageBox.Show(
                            $"Lista o ID {existingListaId.Value} ju� istnieje dla faktury {fakturaId}.\n\n" +
                            $"Import tej faktury zosta� pomini�ty.",
                            "Uwaga - Lista ju� istnieje",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        continue;
                    }

                    // ???????????????????????????????????????????????????????
                    // ETAP 7: Utworzenie LISTY BADA�
                    // ???????????????????????????????????????????????????????
                    var firstRecord = recordsForFaktura.First();
                    int listaId = _archiveRepo.CreateListaBadan(fakturaId, firstRecord.Lx_ID_Firma, firstRecord.Lx_Data);

                    if (listaId > 0)
                    {
                        // System.Diagnostics.Debug.WriteLine($"ProcessImport: ? Utworzono ListaBadan L_ID={listaId}");

                        // ???????????????????????????????????????????????????????
                        // ETAP 8: Aktualizacja POWI�ZA�
                        // ???????????????????????????????????????????????????????

                        // 8.1: Zaktualizuj Lx_ID_listy w archiwum
                        _archiveRepo.UpdateArchiveWithListaId(fakturaId, listaId);

                        // 8.2: Wylicz sum� i numer faktury dla Bad_Fakt
                        decimal suma = recordsForFaktura.Sum(r => r.Lx_Razem ?? 0);
                        string numerFaktury = firstRecord.Lx_Faktura ?? "";

                        // 8.3: Zaktualizuj Bad_L_ID, Bad_F_ID i Bad_Fakt w tabeli Badanie
                        foreach (var badId in badaniaIds)
                        {
                            _archiveRepo.UpdateBadanieWithListaFakturaAndNumer(badId, listaId, fakturaId, numerFaktury);
                        }

                        // 8.4: Zaktualizuj faktur� (FK_Num_Listy, FK_Suma_Bad, FK_Status)
                        _archiveRepo.UpdateFakturaWithListaSummary(fakturaId, listaId, suma);

                        // 8.5: Oznacz rekordy w archiwum jako przetworzone (Lx_End = True)
                        var identifiers = recordsForFaktura.Select(r => r.Identyfikator).ToList();
                        _archiveRepo.MarkArchiveRecordsAsProcessed(identifiers);

                        // System.Diagnostics.Debug.WriteLine($"ProcessImport: ? Zako�czono przetwarzanie faktury {fakturaId}");
                    }
                    else
                    {
                        // System.Diagnostics.Debug.WriteLine($"ProcessImport: ? Nie uda�o si� utworzy� listy dla faktury {fakturaId}");
                    }
                }
                catch (Exception)
                {
                    // System.Diagnostics.Debug.WriteLine($"ProcessImport ERROR dla faktury {fakturaGroup.Key}: {ex.Message}");
                }
            }

            ProgressText = $"Zako�czono import {processed} faktur";
        }
    }
}
