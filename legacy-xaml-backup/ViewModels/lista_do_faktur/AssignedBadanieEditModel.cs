using ASMED.WPF.ViewModels;
using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace ASMED.WPF.ViewModels.lista_do_faktur
{
    public class AssignedBadanieEditModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // Patient
        private string? _pImie;
        public string? P_imie { get => _pImie; set { if (_pImie != value) { _pImie = value; OnPropertyChanged(); OnPropertyChanged(nameof(PacjentDisplay)); } } }

        private string? _pNazwisko;
        public string? P_nazwisko { get => _pNazwisko; set { if (_pNazwisko != value) { _pNazwisko = value; OnPropertyChanged(); OnPropertyChanged(nameof(PacjentDisplay)); } } }

        private string? _pZawod;
        public string? P_zawod { get => _pZawod; set { if (_pZawod != value) { _pZawod = value; OnPropertyChanged(); } } }

        // New: PESEL
        private string? _pPesel;
        public string? P_pesel { get => _pPesel; set { if (_pPesel != value) { _pPesel = value; OnPropertyChanged(); OnPropertyChanged(nameof(FormattedPesel)); } } }

        public string PacjentDisplay => (P_imie ?? string.Empty) + " " + (P_nazwisko ?? string.Empty);

        // Firma
        private string? _firmaNazwa;
        public string? FirmaNazwa { get => _firmaNazwa; set { if (_firmaNazwa != value) { _firmaNazwa = value; OnPropertyChanged(); } } }

        // Referral / Skierowanie
        private string? _bTypBadania;
        public string? B_TypBadania { get => _bTypBadania; set { if (_bTypBadania != value) { _bTypBadania = value; OnPropertyChanged(); } } }

        private DateTime? _bDataSkierowania;
        public DateTime? B_DataSkierowania { get => _bDataSkierowania; set { if (_bDataSkierowania != value) { _bDataSkierowania = value; OnPropertyChanged(); OnPropertyChanged(nameof(DataSkierDisplay)); } } }

        private bool? _bKsiazeczka;
        public bool? B_ksiazeczka { get => _bKsiazeczka; set { if (_bKsiazeczka != value) { _bKsiazeczka = value; OnPropertyChanged(); } } }

        private bool? _bZaswiadczenie;
        public bool? B_Zaswiadczenie { get => _bZaswiadczenie; set { if (_bZaswiadczenie != value) { _bZaswiadczenie = value; OnPropertyChanged(); } } }

        // Badanie (from Badanie table)
        private DateTime? _bad_Data;
        public DateTime? Bad_Data { get => _bad_Data; set { if (_bad_Data != value) { _bad_Data = value; OnPropertyChanged(); } } }

        private DateTime? _bad_Data_Do;
        public DateTime? Bad_Data_Do { get => _bad_Data_Do; set { if (_bad_Data_Do != value) { _bad_Data_Do = value; OnPropertyChanged(); } } }

        public string DataSkierDisplay => B_DataSkierowania.HasValue ? B_DataSkierowania.Value.ToString("dd.MM.yyyy") : string.Empty;

        private string? _bad_Wynik;
        public string? Bad_Wynik { get => _bad_Wynik; set { if (_bad_Wynik != value) { _bad_Wynik = value; OnPropertyChanged(); } } }

        private string? _bad_Nr_KS;
        public string? Bad_Nr_KS { get => _bad_Nr_KS; set { if (_bad_Nr_KS != value) { _bad_Nr_KS = value; OnPropertyChanged(); } } }

        private decimal? _bad_Cena1;
        public decimal? Bad_Cena1 { get => _bad_Cena1; set { if (_bad_Cena1 != value) { _bad_Cena1 = value; OnPropertyChanged(); } } }

        private decimal? _bad_Cena2;
        public decimal? Bad_Cena2 { get => _bad_Cena2; set { if (_bad_Cena2 != value) { _bad_Cena2 = value; OnPropertyChanged(); } } }

        private decimal? _bad_Cena3;
        public decimal? Bad_Cena3 { get => _bad_Cena3; set { if (_bad_Cena3 != value) { _bad_Cena3 = value; OnPropertyChanged(); } } }

        private decimal? _bad_Cena4;
        public decimal? Bad_Cena4 { get => _bad_Cena4; set { if (_bad_Cena4 != value) { _bad_Cena4 = value; OnPropertyChanged(); } } }

        private decimal? _bad_Cena5;
        public decimal? Bad_Cena5 { get => _bad_Cena5; set { if (_bad_Cena5 != value) { _bad_Cena5 = value; OnPropertyChanged(); } } }

        private decimal? _bad_Cena6;
        public decimal? Bad_Cena6 { get => _bad_Cena6; set { if (_bad_Cena6 != value) { _bad_Cena6 = value; OnPropertyChanged(); } } }

        private decimal? _bad_Cena7;
        public decimal? Bad_Cena7 { get => _bad_Cena7; set { if (_bad_Cena7 != value) { _bad_Cena7 = value; OnPropertyChanged(); } } }

        private decimal? _bad_Cena8;
        public decimal? Bad_Cena8 { get => _bad_Cena8; set { if (_bad_Cena8 != value) { _bad_Cena8 = value; OnPropertyChanged(); } } }

        private decimal? _bad_Razem;
        public decimal? Bad_Razem { get => _bad_Razem; set { if (_bad_Razem != value) { _bad_Razem = value; OnPropertyChanged(); } } }

        // Identifiers
        private int? _bad_ID;
        public int? Bad_ID { get => _bad_ID; set { if (_bad_ID != value) { _bad_ID = value; OnPropertyChanged(); } } }

        // Formatted PESEL for display
        public string ?FormattedPesel
        {
            get
            {
                if (string.IsNullOrWhiteSpace(P_pesel)) return string.Empty;
                var digits = new string(P_pesel.Where(char.IsDigit).ToArray());
                if (digits.Length >= 11)
                {
                    digits = digits.Substring(0, 11);
                    return digits.Substring(0, 6) + " " + digits.Substring(6, 2) + " " + digits.Substring(8, 3);
                }
                if (digits.Length > 6)
                {
                    var part1 = digits.Substring(0, 6);
                    var rest = digits.Substring(6);
                    if (rest.Length > 2)
                        return part1 + " " + rest.Substring(0, 2) + " " + rest.Substring(2);
                    return part1 + " " + rest;
                }
                return digits;
            }
        }

        protected bool SetProperty<T>(ref T field, T newValue, [CallerMemberName] string? propertyName = null)
        {
            if (!Equals(field, newValue))
            {
                field = newValue;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                return true;
            }

            return false;
        }

        private object selectedFilterLista;

        public object SelectedFilterLista { get => selectedFilterLista; set => SetProperty(ref selectedFilterLista, value); }

        private System.Collections.IEnumerable filterOptionsLista;

        public System.Collections.IEnumerable FilterOptionsLista { get => filterOptionsLista; set => SetProperty(ref filterOptionsLista, value); }

        private string ?filterTextLista;

        public string? FilterTextLista { get => filterTextLista; set => SetProperty(ref filterTextLista, value); }

        private RelayCommand clearFilterCommandLista;
        public ICommand ?ClearFilterCommandLista => clearFilterCommandLista ??= new RelayCommand(ClearFilter);

        private void ClearFilter(object? commandParameter)
        {
        }
    }
}
