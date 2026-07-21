using ASMED.WPF.Models;
using ASMED.WPF.Views;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using ASMED.WPF.Helpers;
using System.Linq;

namespace ASMED.WPF.ViewModels
{
    public class ListaPacjentowViewModel
    {



        public ObservableCollection<string> FilterTypes { get; } = new() { "All", "Imię", "Nazwisko", "PESEL", "Firma" };
        public ObservableCollection<Pacjent> Pacjenci { get; set; }
        public ObservableCollection<Pacjent> PacjenciFiltered { get; set; } = new();
        public string? SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    FilterPacjenci();
                }
            }
        }
        private string? _searchText;

        public string ?ActiveFilterType
        {
            get => _activeFilterType;
            set
            {
                if (_activeFilterType != value)
                {
                    _activeFilterType = value;
                    FilterPacjenci();
                }
            }
        }
        private string ?_activeFilterType = "All";

        public ICommand ?ClearSearchTextCommand { get; }
        public ICommand ?EditPatientCommand { get; }
        public ICommand ?OpenReferralCommand { get; }
        public ICommand ?RegisterCommand { get; }

        public ListaPacjentowViewModel()
        {
            Pacjenci = new ObservableCollection<Pacjent>();
            EditPatientCommand = new RelayCommand(EditPatient);
            OpenReferralCommand = new RelayCommand(OpenReferral);
            RegisterCommand = new RelayCommand(Register);
            ClearSearchTextCommand = new RelayCommand(_ => { SearchText = string.Empty; });
            LoadPacjenciFromDb();
        }

        private void EditPatient(object? obj)
        {
            if (obj is Pacjent pacjent && pacjent.P_ID > 0)
            {
                var db = new AccessDbContext();
                var rec = db.GetPacjentById(pacjent.P_ID);

                // Jeśli rekord istnieje, otwórz widok edycji pacjenta
                if (rec != null)
                {
                    var mainVM = Application.Current.MainWindow?.DataContext as MainWindowViewModel;
                    var vm = new PacjentDodajViewModel(mainVM)
                    {
                        Imie = rec.Imie,
                        Nazwisko = rec.Nazwisko,
                        PESEL = rec.PESEL,
                        BrakPESEL = rec.BrakPESEL,
                        Plec = rec.Plec,
                        Zawod = rec.Zawod,
                        comments = rec.Uwagi,
                        UlicaNumerDomu = rec.UlicaNumerDomu,
                        KodPocztowy = rec.Kod,
                        ID = rec.ID,
                        P_firma_id = rec.FirmaId,
                        FrazaFirma = rec.FirmaNazwa
                    };

                    // Mapowanie dla pól z autouzupełnianiem
                    vm.WybraneImie = vm.Imie;
                    vm.WybraneNazwisko = vm.Nazwisko;
                    vm.WybranyZawod = vm.Zawod;
                    vm.WybranaUlica = vm.UlicaNumerDomu;
                    vm.WybraneMiasto = vm.Miejscowosc;

                    // Ustaw WybranaFirma po ID (jeśli lista firm już załadowana)
                    if (vm.P_firma_id.HasValue && vm.Firmy != null && vm.Firmy.Count > 0)
                    {
                        var firma = vm.Firmy.FirstOrDefault(f => f.Id == vm.P_firma_id.Value);
                        if (firma != null)
                        {
                            vm.WybranaFirma = firma;
                            vm.FrazaFirma = firma.Name;
                        }
                    }
                    else if (!string.IsNullOrEmpty(vm.FrazaFirma))
                    {
                        // Jeśli nie znaleziono po ID, ustaw nazwę firmy z rekordu (string)
                        vm.FrazaFirma = rec.FirmaNazwa;
                    }

                    // Przełącz widok na edycję pacjenta
                    if (mainVM != null)
                    {
                        mainVM.PacjentWidok = vm;
                    }
                }
            }
        }

        private void OpenReferral(object? obj)
        {
            // Logika otwierania skierowania
        }

        private void Register(object? obj)
        {
            // Logika rejestracji
        }

        private void LoadPacjenciFromDb()
        {
            var db = new Helpers.AccessDbHelper();
            using (var conn = db.GetConnection())
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT P_ID,P_Imie, P_Nazwisko, P_PESEL, P_Firma FROM P_Pacjent";
                    using (var reader = cmd.ExecuteReader())
                    {
                        int lineNumber = 1;
                        while (reader.Read())
                        {
                            Pacjenci.Add(new Pacjent
                            {
                                LineNumber = lineNumber++,
                                P_ID = reader["P_ID"] is int id ? id : int.TryParse(reader["P_ID"].ToString(), out var id2) ? id2 : 0,
                                FirstName = reader["P_Imie"].ToString(),
                                LastName = reader["P_Nazwisko"].ToString(),
                                PESEL = reader["P_PESEL"].ToString(),
                                Company = reader["P_Firma"].ToString()
                            });
                        }
                    }
                }
            }
            FilterPacjenci();
        }

        private void FilterPacjenci()
        {
            PacjenciFiltered.Clear();
            var text = SearchText?.Trim().ToLower() ?? string.Empty;

            // ✅ DODANE: Normalizacja tekstu wyszukiwania
            var normalizedSearch = TextNormalizationHelper.RemovePolishDiacritics(text);

            foreach (var p in Pacjenci)
            {
                bool match = false;
                if (string.IsNullOrEmpty(text))
                {
                    match = true;
                }
                else
                {
                    switch (ActiveFilterType)
                    {
                        case "Imię":
                            match = (p.FirstName ?? "").ToLower().Contains(text) ||
                                   TextNormalizationHelper.ContainsIgnoringDiacritics(p.FirstName ?? "", text);
                            break;
                        case "Nazwisko":
                            match = (p.LastName ?? "").ToLower().Contains(text) ||
                                   TextNormalizationHelper.ContainsIgnoringDiacritics(p.LastName ?? "", text);
                            break;
                        case "PESEL":
                            match = (p.PESEL ?? "").ToLower().Contains(text);
                            break;
                        case "Firma":
                            match = (p.Company ?? "").ToLower().Contains(text) ||
                                   TextNormalizationHelper.ContainsIgnoringDiacritics(p.Company ?? "", text);
                            break;
                        case "All":
                        default:
                            match = (p.FirstName ?? "").ToLower().Contains(text)
                                || TextNormalizationHelper.ContainsIgnoringDiacritics(p.FirstName ?? "", text)
                                || (p.LastName ?? "").ToLower().Contains(text)
                                || TextNormalizationHelper.ContainsIgnoringDiacritics(p.LastName ?? "", text)
                                || (p.PESEL ?? "").ToLower().Contains(text)
                                || (p.Company ?? "").ToLower().Contains(text)
                                || TextNormalizationHelper.ContainsIgnoringDiacritics(p.Company ?? "", text)
                                || p.P_ID.ToString().Contains(text); // ✅ DODANE: Wyszukiwanie po ID
                            break;
                    }
                }
                if (match)
                    PacjenciFiltered.Add(p);
            }
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool>? _canExecute;
        private Action odwswiezListeNaDzien;

        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public RelayCommand(Action odwswiezListeNaDzien)
        {
            this.odwswiezListeNaDzien = odwswiezListeNaDzien;
        }

        public bool CanExecute(object? parameter) => _canExecute == null || _canExecute(parameter);
        public void Execute(object? parameter) => _execute(parameter);
        public event EventHandler? CanExecuteChanged { add { } remove { } }
    }
}
