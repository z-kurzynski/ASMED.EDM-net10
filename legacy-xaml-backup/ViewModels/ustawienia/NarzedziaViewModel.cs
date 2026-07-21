using ASMED.WPF.Helpers;
using ASMED.WPF.Models;
using ASMED.WPF.Views; // ? DODANE: Dla FirmaView
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace ASMED.WPF.ViewModels
{
    public class NarzedziaViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // ???????????????????????????????????????????????????????
        // ? NOWE: Zarządzanie wyjątkami formatowania
        // ???????????????????????????????????????????????????????

        private string ?_noweSlowo;
        public string ?NoweSlowo
        {
            get => _noweSlowo;
            set
            {
                _noweSlowo = value;
                OnPropertyChanged();
            }
        }

        private string ?_nowyFormatTyp = "UPPERCASE";
        public string ?NowyFormatTyp
        {
            get => _nowyFormatTyp;
            set
            {
                _nowyFormatTyp = value;
                OnPropertyChanged();
            }
        }

        private string ?_nowaKategoria;
        public string ?NowaKategoria
        {
            get => _nowaKategoria;
            set
            {
                _nowaKategoria = value;
                OnPropertyChanged();
            }
        }

        private FormatowanieWyjatekModel _wybranyWyjatek;
        public FormatowanieWyjatekModel WybranyWyjatek
        {
            get => _wybranyWyjatek;
            set
            {
                _wybranyWyjatek = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CzyWybranoWyjatek));
            }
        }

        public bool CzyWybranoWyjatek => WybranyWyjatek != null;

        public ObservableCollection<FormatowanieWyjatekModel> Wyjatki { get; set; } = new();

        // Opcje dla ComboBox FormatTyp
        public ObservableCollection<string> FormatTypyOptions { get; } = new()
        {
            "UPPERCASE",
            "lowercase",
            "Capitalize"
        };

        // Komendy
        public ICommand ?ZaladujWyjatkiCommand { get; }
        public ICommand ?DodajWyjatekCommand { get; }
        public ICommand ?UsunWyjatekCommand { get; }
        public ICommand ?ZastosujDoFirmCommand { get; }

        public NarzedziaViewModel()
        {
            ZaladujWyjatkiCommand = new RelayCommand(_ => ZaladujWyjatki());
            DodajWyjatekCommand = new RelayCommand(_ => DodajWyjatek());
            UsunWyjatekCommand = new RelayCommand(_ => UsunWyjatek(), _ => CzyWybranoWyjatek);
            ZastosujDoFirmCommand = new RelayCommand(_ => ZastosujDoFirm());

            // Załaduj wyjątki przy starcie
            ZaladujWyjatki();
        }

        /// <summary>
        /// Ładuje listę wyjątków formatowania z tabeli FormatowanieTekstu
        /// </summary>
        private void ZaladujWyjatki()
        {
            try
            {
                Wyjatki.Clear();

                var db = new AccessDbHelper();
                using (var conn = db.GetConnection())
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT ID, Slowo, FormatTyp, Kategoria FROM FormatowanieTekstu ORDER BY Slowo";

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Wyjatki.Add(new FormatowanieWyjatekModel
                                {
                                    ID = reader["ID"] is int id ? id : 0,
                                    Slowo = reader["Slowo"]?.ToString() ?? string.Empty,
                                    FormatTyp = reader["FormatTyp"]?.ToString() ?? string.Empty,
                                    Kategoria = reader["Kategoria"]?.ToString() ?? string.Empty
                                });
                            }
                        }
                    }
                }

                // System.Diagnostics.Debug.WriteLine($"[NarzedziaVM] Załadowano {Wyjatki.Count} wyjątków formatowania");
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"[NarzedziaVM] Błąd ładowania wyjątków: {ex.Message}");
                MessageBox.Show($"Błąd ładowania wyjątków:\n\n{ex.Message}",
                    "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Dodaje nowy wyjątek formatowania do bazy
        /// </summary>
        private void DodajWyjatek()
        {
            try
            {
                // Walidacja
                if (string.IsNullOrWhiteSpace(NoweSlowo))
                {
                    MessageBox.Show("Wpisz słowo do dodania",
                        "Walidacja", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(NowyFormatTyp))
                {
                    MessageBox.Show("Wybierz typ formatowania",
                        "Walidacja", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Sprawdź czy wyraz już istnieje
                if (Wyjatki.Any(w => w.Slowo.Equals(NoweSlowo, StringComparison.OrdinalIgnoreCase)))
                {
                    var result = MessageBox.Show($"Wyraz '{NoweSlowo}' już istnieje w bazie.\n\nCzy chcesz go zaktualizować?",
                        "Duplikat", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.No)
                        return;

                    // Zaktualizuj istniejący
                    var istniejacy = Wyjatki.First(w => w.Slowo.Equals(NoweSlowo, StringComparison.OrdinalIgnoreCase));
                    ZaktualizujWyjatek(istniejacy.ID, NoweSlowo, NowyFormatTyp, NowaKategoria);
                    ZaladujWyjatki();
                    return;
                }

                // Dodaj nowy
                var db = new AccessDbHelper();
                using (var conn = db.GetConnection())
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "INSERT INTO FormatowanieTekstu (Slowo, FormatTyp, Kategoria) VALUES (?, ?, ?)";
                        cmd.Parameters.AddWithValue("@Slowo", NoweSlowo);
                        cmd.Parameters.AddWithValue("@FormatTyp", NowyFormatTyp);
                        cmd.Parameters.AddWithValue("@Kategoria", NowaKategoria ?? (object)DBNull.Value);

                        cmd.ExecuteNonQuery();
                    }
                }

                // Wyczyść cache w FirmaEditViewModel
                WyczyscCacheFormatowania();

                MessageBox.Show($"Dodano wyjątek: {NoweSlowo} ? {NowyFormatTyp}",
                    "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);

                // Wyczyść pola i odśwież listę
                NoweSlowo = string.Empty;
                NowaKategoria = string.Empty;
                ZaladujWyjatki();
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"[NarzedziaVM] Błąd dodawania wyjątku: {ex.Message}");
                MessageBox.Show($"Błąd dodawania wyjątku:\n\n{ex.Message}",
                    "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Aktualizuje istniejący wyjątek w bazie
        /// </summary>
        private void ZaktualizujWyjatek(int id, string? slowo, string? formatTyp, string? kategoria)
        {
            try
            {
                var db = new AccessDbHelper();
                using (var conn = db.GetConnection())
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "UPDATE FormatowanieTekstu SET Slowo = ?, FormatTyp = ?, Kategoria = ? WHERE ID = ?";
                        cmd.Parameters.AddWithValue("@Slowo", slowo);
                        cmd.Parameters.AddWithValue("@FormatTyp", formatTyp);
                        cmd.Parameters.AddWithValue("@Kategoria", kategoria ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@ID", id);

                        cmd.ExecuteNonQuery();
                    }
                }

                // Wyczyść cache
                WyczyscCacheFormatowania();

                MessageBox.Show($"Zaktualizowano wyjątek: {slowo} ? {formatTyp}",
                    "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"[NarzedziaVM] Błąd aktualizacji wyjątku: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Usuwa wybrany wyjątek z bazy
        /// </summary>
        private void UsunWyjatek()
        {
            try
            {
                if (WybranyWyjatek == null)
                    return;

                var result = MessageBox.Show($"Czy na pewno usunąć wyjątek '{WybranyWyjatek.Slowo}'?",
                    "Potwierdzenie", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                    return;

                var db = new AccessDbHelper();
                using (var conn = db.GetConnection())
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "DELETE FROM FormatowanieTekstu WHERE ID = ?";
                        cmd.Parameters.AddWithValue("@ID", WybranyWyjatek.ID);

                        cmd.ExecuteNonQuery();
                    }
                }

                // Wyczyść cache
                WyczyscCacheFormatowania();

                MessageBox.Show($"Usunięto wyjątek: {WybranyWyjatek.Slowo}",
                    "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);

                // Odśwież listę
                ZaladujWyjatki();
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"[NarzedziaVM] Błąd usuwania wyjątku: {ex.Message}");
                MessageBox.Show($"Błąd usuwania wyjątku:\n\n{ex.Message}",
                    "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Stosuje formatowanie do wszystkich nazw firm w bazie
        /// </summary>
        private void ZastosujDoFirm()
        {
            try
            {
                var result = MessageBox.Show(
                    "Czy na pewno zastosować reguły formatowania do WSZYSTIKich firm w bazie?\n\n" +
                    "Ta operacja może zająć kilka sekund i zmieni nazwy, miejscowości i ulice wszystkich firm.\n\n" +
                    "UWAGA: Ta operacja nie może być cofnięta!",
                    "Potwierdzenie", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.No)
                    return;

                int przetworzoneFirmy = 0;

                var db = new AccessDbHelper();
                using (var conn = db.GetConnection())
                {
                    conn.Open();

                    // Pobierz wszystkie firmy
                    using (var selectCmd = conn.CreateCommand())
                    {
                        selectCmd.CommandText = "SELECT id, Nazwa, Miejscowosc, Ulica, Osoba_kontaktowa FROM Firma";

                        using (var reader = selectCmd.ExecuteReader())
                        {
                            var firmy = new System.Collections.Generic.List<(int id, string nazwa, string miejscowosc, string ulica, string osoba)>();

                            while (reader.Read())
                            {
                                firmy.Add((
                                    reader["id"] is int id ? id : 0,
                                    reader["Nazwa"]?.ToString(),
                                    reader["Miejscowosc"]?.ToString(),
                                    reader["Ulica"]?.ToString(),
                                    reader["Osoba_kontaktowa"]?.ToString()
                                ));
                            }

                            reader.Close();

                            // Zastosuj formatowanie do każdej firmy
                            foreach (var firma in firmy)
                            {
                                using (var updateCmd = conn.CreateCommand())
                                {
                                    updateCmd.CommandText = @"
                                        UPDATE Firma SET 
                                            Nazwa = ?, 
                                            Miejscowosc = ?, 
                                            Ulica = ?,
                                            Osoba_kontaktowa = ?
                                        WHERE id = ?";

                                    // ? Użyj metody FormatText z FirmaEditViewModel (reflection)
                                    var formatowanaNazwa = FormatTextHelper(firma.nazwa);
                                    var formatowanaMiejscowosc = FormatTextHelper(firma.miejscowosc);
                                    var formatowanaUlica = FormatTextHelper(firma.ulica);
                                    var formatowanaOsoba = FormatTextHelper(firma.osoba);

                                    updateCmd.Parameters.AddWithValue("@Nazwa", formatowanaNazwa ?? (object)DBNull.Value);
                                    updateCmd.Parameters.AddWithValue("@Miejscowosc", formatowanaMiejscowosc ?? (object)DBNull.Value);
                                    updateCmd.Parameters.AddWithValue("@Ulica", formatowanaUlica ?? (object)DBNull.Value);
                                    updateCmd.Parameters.AddWithValue("@Osoba_kontaktowa", formatowanaOsoba ?? (object)DBNull.Value);
                                    updateCmd.Parameters.AddWithValue("@id", firma.id);

                                    updateCmd.ExecuteNonQuery();
                                    przetworzoneFirmy++;
                                }
                            }
                        }
                    }
                }

                // ? NOWE: Odśwież FirmaView po zastosowaniu formatowania
                OdswiezFirmaView();

                MessageBox.Show($"Zastosowano formatowanie do {przetworzoneFirmy} firm.\n\nOperacja zakończona pomyślnie!",
                    "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"[NarzedziaVM] Błąd stosowania formatowania: {ex.Message}");
                MessageBox.Show($"Błąd stosowania formatowania:\n\n{ex.Message}",
                    "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// ? NOWA METODA: Odświeża widok FirmaView po zastosowaniu formatowania
        /// </summary>
        private void OdswiezFirmaView()
        {
            try
            {
                // System.Diagnostics.Debug.WriteLine("[NarzedziaVM] Odświeżanie FirmaView...");

                // Znajdź MainWindow
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow == null)
                {
                    // System.Diagnostics.Debug.WriteLine("[NarzedziaVM] MainWindow is null");
                    return;
                }

                // Znajdź TabControlExt
                var tabControl = FindVisualChild<Syncfusion.Windows.Tools.Controls.TabControlExt>(mainWindow);
                if (tabControl == null)
                {
                    // System.Diagnostics.Debug.WriteLine("[NarzedziaVM] TabControlExt not found");
                    return;
                }

                // Znajdź zakładkę "Firmy" (x:Name="Firmy" lub podobny)
                foreach (var item in tabControl.Items)
                {
                    if (item is Syncfusion.Windows.Tools.Controls.TabItemExt tabItem)
                    {
                        // Sprawdź Name lub Header
                        if (tabItem.Name == "Firmy" ||
                            (tabItem.Header?.ToString()?.Contains("Firmy") ?? false) ||
                            (tabItem.Header?.ToString()?.Contains("Firma") ?? false))
                        {
                            // Znajdź FirmaView w zawartości zakładki
                            if (tabItem.Content is FirmaView firmaView &&
                                firmaView.DataContext is FirmaViewModel firmaVM)
                            {
                                // Wywołaj metodę odświeżania
                                // System.Diagnostics.Debug.WriteLine("[NarzedziaVM] Znaleziono FirmaView, odświeżam...");

                                // Wywołaj LoadFirmyFromDb() przez reflection
                                var loadMethod = typeof(FirmaViewModel).GetMethod("LoadFirmyFromDb",
                                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                                if (loadMethod != null)
                                {
                                    loadMethod.Invoke(firmaVM, null);
                                    // System.Diagnostics.Debug.WriteLine("[NarzedziaVM] ? FirmaView odświeżony");
                                }
                                else
                                {
                                    // System.Diagnostics.Debug.WriteLine("[NarzedziaVM] ?? Nie znaleziono metody LoadFirmyFromDb()");
                                }

                                return;
                            }
                        }
                    }
                }

                // System.Diagnostics.Debug.WriteLine("[NarzedziaVM] ?? Nie znaleziono zakładki Firmy");
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"[NarzedziaVM] Błąd odświeżania FirmaView: {ex.Message}");
            }
        }

        /// <summary>
        /// ? HELPER: Znajduje kontrolkę wizualną typu T w drzewie wizualnym
        /// </summary>
        private static T? FindVisualChild<T>(System.Windows.DependencyObject parent) where T : System.Windows.DependencyObject
        {
            if (parent == null)
                return null;

            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);

                if (child is T result)
                    return result;

                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                    return childOfChild;
            }

            return null;
        }

        /// <summary>
        /// Helper: Stosuje formatowanie tekstu (używa logiki z FirmaEditViewModel)
        /// </summary>
        private string ?FormatTextHelper(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            try
            {
                // ? Wywołaj metodę FormatText z FirmaEditViewModel przez reflection
                var firmaEditType = typeof(FirmaEditViewModel);
                var formatMethod = firmaEditType.GetMethod("FormatText",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (formatMethod != null)
                {
                    var tempInstance = new FirmaEditViewModel();
                    var result = formatMethod.Invoke(tempInstance, new object[] { input });
                    return result?.ToString() ?? input;
                }

                return input;
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"[FormatTextHelper] Błąd: {ex.Message}");
                return input;
            }
        }

        /// <summary>
        /// Czyści statyczny cache w FirmaEditViewModel (przez reflection)
        /// </summary>
        private void WyczyscCacheFormatowania()
        {
            try
            {
                var firmaEditType = typeof(FirmaEditViewModel);
                var cacheField = firmaEditType.GetField("_formatExceptions",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

                if (cacheField != null)
                {
                    cacheField.SetValue(null, null);
                    // System.Diagnostics.Debug.WriteLine("[NarzedziaVM] Wyczyszczono cache formatowania");
                }
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"[NarzedziaVM] Błąd czyszczenia cache: {ex.Message}");
            }
        }
    }
}
