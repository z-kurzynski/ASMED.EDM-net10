using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Odbc;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ASMED.WPF.Models;
using ASMED.WPF.Helpers;
using ASMED.WPF.Views;

namespace ASMED.WPF.ViewModels
{


    public class FirmaViewModel : INotifyPropertyChanged
    {

        public ICommand ?SaveFirmaCommand { get; }
        public ICommand ?DodajFirmeCommand { get; }
        public ICommand ?EdytujFirmeCommand { get; }
        public ICommand ?PokazUmowyCommand { get; }
        public ICommand ?OdswiezCommand { get; }

        public FirmaViewModel()
        {
            ClearSearchTextCommand = new RelayCommand(_ => { SearchText = string.Empty; });
            SaveFirmaCommand = new RelayCommand<Firma>(SaveFirmaToDb);
            DodajFirmeCommand = new RelayCommand(_ => DodajFirme());
            EdytujFirmeCommand = new RelayCommand<Firma>(EdytujFirme);
            PokazUmowyCommand = new RelayCommand<Firma>(PokazUmowy);
            OdswiezCommand = new RelayCommand(_ => LoadFirmyFromDb());

            Firmy = new ObservableCollection<Firma>();
            LoadFirmyFromDb();
        }

        private void PokazUmowy(Firma? firma)
        {
            if (firma == null) return;

            var window = new Views.firma.UmowyFirmyWindow(firma)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };

            window.ShowDialog();

            // ✅ Odśwież listę firm po zamknięciu okna umów (aktualizacja pól umowa_do, czas_nieokreslon)
            LoadFirmyFromDb();
        }

        private void DodajFirme()
        {
            var window = new FirmaEditWindow
            {
                Owner = System.Windows.Application.Current.MainWindow
            };

            window.ShowDialog();

            // Odśwież listę po zamknięciu okna
            LoadFirmyFromDb();
        }

        private void EdytujFirme(Firma? firma)
        {
            if (firma == null) return;

            var window = new FirmaEditWindow(firma)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };

            window.ShowDialog();

            // Odśwież listę po zamknięciu okna
            LoadFirmyFromDb();
        }

        private void SaveFirmaToDb(Firma? firma)
        {
            System.Windows.MessageBox.Show($"Wywołano SaveFirmaToDb dla firmy: {firma?.Nazwa ?? "(brak nazwy)"}", "Debug", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            // Tu dodaj kod zapisu do bazy
            var db = new AccessDbHelper();
            try
            {
                using (var conn = db.GetConnection())
                {
                    conn.Open();
                    var cmd = new OdbcCommand(@"UPDATE Firma SET
                Cennik = ?,
                Nazwa = ?,
                Miejscowosc = ?,
                Ulica = ?,
                NIP = ?,
                Osoba_kontaktowa = ?,
                Telefon = ?,
                Email = ?,
                FKemail = ?
            WHERE id = ?;", conn);

                    cmd.Parameters.AddWithValue("@Cennik", firma.Cennik ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Nazwa", firma.Nazwa ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Miejscowosc", firma.Miejscowosc ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Ulica", firma.Ulica ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@NIP", firma.NIP ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Osoba_kontaktowa", firma.Osoba_kontaktowa ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Telefon", firma.Telefon ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Email", firma.Email ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@FKemail", firma.FKemail ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@id", firma.id);

                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Błąd zapisu: {ex.Message}", "Błąd", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }

        }

        // INotifyPropertyChanged Implementation
        //---------------------------------
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // public ObservableCollection<Firma> Firmy { get; set; } = new ObservableCollection<Firma>();
        private ObservableCollection<Firma> _firmy = new ObservableCollection<Firma>();
        public ObservableCollection<Firma> Firmy
        {
            get => _firmy;
            set { _firmy = value; OnPropertyChanged(); }
        }

        private ObservableCollection<Firma> _filteredFirmy = new ObservableCollection<Firma>();
        public ObservableCollection<Firma> FilteredFirmy
        {
            get => _filteredFirmy;
            set { _filteredFirmy = value; OnPropertyChanged(); }
        }

        private string ?_searchText;
        public string ?SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); FilterFirmy(); }
        }

        private string ?_activeFilterType = "Nazwa";
        public string ?ActiveFilterType
        {
            get => _activeFilterType;
            set { _activeFilterType = value; OnPropertyChanged(); FilterFirmy(); }
        }

        public ObservableCollection<string> FilterTypes { get; } = new ObservableCollection<string> { "Nazwa", "NIP", "Osoba_kontaktowa" };

        public ICommand ?ClearSearchTextCommand { get; }

        private void LoadFirmyFromDb()
        {
            var db = new AccessDbHelper();
            var firmy = new ObservableCollection<Firma>();
            try
            {
                using (var conn = db.GetConnection())
                {
                    conn.Open();
                    var cmd = new OdbcCommand(@"
                        SELECT id, Cennik, Nazwa, Miejscowosc, Ulica, NIP, Osoba_kontaktowa, Telefon, Email, FKemail, umowa_do, czas_nieokreslon 
                        FROM Firma;", conn);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var f = new Firma
                            {
                                id = reader["id"] != DBNull.Value ? Convert.ToInt32(reader["id"]) : 0,
                                Cennik = reader["Cennik"].ToString(),
                                Nazwa = reader["Nazwa"].ToString(),
                                Miejscowosc = reader["Miejscowosc"].ToString(),
                                Ulica = reader["Ulica"].ToString(),
                                NIP = reader["NIP"].ToString(),
                                Osoba_kontaktowa = reader["Osoba_kontaktowa"].ToString(),
                                Telefon = reader["Telefon"].ToString(),
                                Email = reader["Email"].ToString(),
                                FKemail = reader["FKemail"].ToString(),
                                // ✅ Nowe pola
                                UmowaDo = reader["umowa_do"] != DBNull.Value ? Convert.ToDateTime(reader["umowa_do"]) : (DateTime?)null,
                                CzasNieokreslon = reader["czas_nieokreslon"] != DBNull.Value && Convert.ToBoolean(reader["czas_nieokreslon"])
                            };
                            firmy.Add(f);

                        }
                    }
                }
            }
            catch (Exception)
            {
                // Możesz dodać logowanie lub powiadomienie
            }
            Firmy = firmy;
            FilterFirmy();
        }

        private void FilterFirmy()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                FilteredFirmy = new ObservableCollection<Firma>(Firmy);
                return;
            }
            var lower = SearchText.ToLower();
            switch (ActiveFilterType)
            {
                case "Nazwa":
                    FilteredFirmy = new ObservableCollection<Firma>(Firmy.Where(f =>
                        (f.Nazwa != null && f.Nazwa.ToLower().Contains(lower)) ||
                        TextNormalizationHelper.ContainsIgnoringDiacritics(f.Nazwa ?? "", lower)));
                    break;
                case "NIP":
                    FilteredFirmy = new ObservableCollection<Firma>(Firmy.Where(f => f.NIP != null && f.NIP.ToLower().Contains(lower)));
                    break;
                case "Osoba_kontaktowa":
                    FilteredFirmy = new ObservableCollection<Firma>(Firmy.Where(f =>
                        (f.Osoba_kontaktowa != null && f.Osoba_kontaktowa.ToLower().Contains(lower)) ||
                        TextNormalizationHelper.ContainsIgnoringDiacritics(f.Osoba_kontaktowa ?? "", lower)));
                    break;
                default:
                    FilteredFirmy = new ObservableCollection<Firma>(Firmy);
                    break;
            }
        }
    }
}
