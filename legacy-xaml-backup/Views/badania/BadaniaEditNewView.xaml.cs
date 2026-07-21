using ASMED.WPF.Helpers;
using ASMED.WPF.ViewModels.Badania;
using ASMED.WPF.ViewModels.Skierowania;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ASMED.WPF.Views
{
    /// <summary>
    /// Interaction logic for BadaniaEditNewView.xaml
    /// Widok do edycji istniejących badań - niezależny od starego Badania_Edit_View
    /// </summary>
    public partial class BadaniaEditNewView : UserControl
    {
        private static readonly CultureInfo PlCulture = new CultureInfo("pl-PL");

        public BadaniaEditNewView()
        {
            InitializeComponent();

            var vm = new BadaniaEditNewViewModel();
            DataContext = vm;

            // ✅ DODANE: Podłącz event synchronizacji przycisków z cenami
            vm.ToggleButtonsSyncRequested += SyncToggleButtonsWithPrices;

            // ✅ DODANE: Podłącz eventy dla ustawiania fokusa po akcjach
            vm.PropertyChanged += (s, e) =>
            {
                // Po odświeżeniu danych (RefreshFromDb) - ustaw fokus
                if (e.PropertyName == "Skierowania")
                {
                    // System.Diagnostics.Debug.WriteLine("BadaniaEditNewView: PropertyChanged - Skierowania (po odświeżeniu)");
                    SetFocusOnFilter();
                    // ✅ DODANE: Synchronizuj przyciski po zmianie danych
                    SyncToggleButtonsWithPrices();
                }
            };

            // Podłącz handlery dla przycisków toggle
            this.Loaded += (s, e) =>
            {
                AttachEditToggleHandlers();

                // ✅ DODANE: Synchronizuj przyciski z ViewModelem po załadowaniu
                SyncToggleButtonsWithPrices();

                // ✅ DODANE: Ustaw fokus na pole wyszukiwania po załadowaniu
                SetFocusOnFilter();
            };

            // ✅ DODANE: Odpinanie eventów przy Unloaded
            this.Unloaded += (s, e) => DetachToggleHandlers();

            // ✅ DODANE: Ustaw fokus gdy widok staje się widoczny
            this.IsVisibleChanged += (s, e) =>
            {
                if (this.IsVisible)
                {
                    // ✅ KLUCZOWE: Synchronizuj przyciski z ViewModelem (rozwiązuje problem resetowania)
                    SyncToggleButtonsWithPrices();

                    SetFocusOnFilter();
                }
            };

            // ✅ NOWE: Obsługa klawiszy funkcyjnych F1/F2/F11/F12
            this.PreviewKeyDown += BadaniaEditNewView_PreviewKeyDown;
            this.Focusable = true;
        }

        /// <summary>
        /// ✅ NOWA METODA: Obsługa klawiszy funkcyjnych F1/F2/F11/F12
        /// F1 = Odśwież listę
        /// F2 = Wyczyść pole wyszukiwania (focus na filtr)
        /// F11 = Wyczyść formularz edycji
        /// F12 = Zapisz/Modyfikuj badanie
        /// </summary>
        private void BadaniaEditNewView_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (DataContext is not BadaniaEditNewViewModel vm) return;

            try
            {
                switch (e.Key)
                {
                    case Key.F1:
                        // F1 = Odśwież listę
                        // System.Diagnostics.Debug.WriteLine("BadaniaEditNewView: ⌨️ F1 pressed - Odświeżanie listy");
                        vm.RefreshCommand?.Execute(null);
                        SetFocusOnFilter();
                        e.Handled = true;
                        break;

                    case Key.F2:
                        // F2 = Wyczyść pole wyszukiwania + ustaw fokus
                        // System.Diagnostics.Debug.WriteLine("BadaniaEditNewView: ⌨️ F2 pressed - Czyszczenie pola wyszukiwania");
                        vm.FilterText = string.Empty;
                        SetFocusOnFilter();
                        e.Handled = true;
                        break;

                    case Key.F11:
                        // F11 = Wyczyść formularz
                        // System.Diagnostics.Debug.WriteLine("BadaniaEditNewView: ⌨️ F11 pressed - Czyszczenie formularza");
                        vm.ClearCommand?.Execute(null);
                        e.Handled = true;
                        break;

                    case Key.F12:
                        // F12 = Zapisz/Modyfikuj badanie
                        // System.Diagnostics.Debug.WriteLine("BadaniaEditNewView: ⌨️ F12 pressed - Zapisywanie badania");
                        vm.SaveBadanieCommand?.Execute(null);
                        e.Handled = true;
                        break;
                }
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"BadaniaEditNewView_PreviewKeyDown ERROR: {ex.Message}");
            }
        }

        /// <summary>
        /// ✅ NOWA METODA: Przełącza na zakładkę "Zakończ Badanie" (BadaniaNewView)
        /// </summary>
        private void Nowe_Badanie_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // System.Diagnostics.Debug.WriteLine("Nowe_Badanie_Click: Przełączanie na zakładkę BadaniaNew (Zakończ Badanie)...");

                // Znajdź MainWindow
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow == null)
                {
                    // System.Diagnostics.Debug.WriteLine("Nowe_Badanie_Click: ❌ Nie znaleziono MainWindow");
                    MessageBox.Show("Nie można odnaleźć głównego okna aplikacji.",
                        "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // ✅ POPRAWIONE: Znajdź zakładkę BadaniaNew (nie NowaKartaBadan!)
                var badaniaNewTab = mainWindow.FindName("BadaniaNew") as Syncfusion.Windows.Tools.Controls.TabItemExt;
                if (badaniaNewTab != null)
                {
                    badaniaNewTab.IsSelected = true;
                    // System.Diagnostics.Debug.WriteLine("Nowe_Badanie_Click: ✅ Przełączono na zakładkę BadaniaNew (Zakończ Badanie)");
                }
                else
                {
                    // System.Diagnostics.Debug.WriteLine("Nowe_Badanie_Click: ❌ Nie znaleziono zakładki BadaniaNew");
                    MessageBox.Show("Nie można odnaleźć zakładki 'Zakończ Badanie' (BadaniaNew).",
                        "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"Nowe_Badanie_Click ERROR: {ex.Message}");
                MessageBox.Show($"Błąd podczas przełączania zakładki:\n{ex.Message}",
                    "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// ✅ NOWA METODA: Przełącza na zakładkę "Rejestracja" (RejestracjaView)
        /// </summary>
        private void Rejestracja_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // System.Diagnostics.Debug.WriteLine("Rejestracja_Click: Przełączanie na zakładkę Rejestracja...");

                // Znajdź MainWindow
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow == null)
                {
                    // System.Diagnostics.Debug.WriteLine("Rejestracja_Click: ❌ Nie znaleziono MainWindow");
                    MessageBox.Show("Nie można odnaleźć głównego okna aplikacji.",
                        "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // ✅ Znajdź zakładkę Rejestracja
                var rejestracjaTab = mainWindow.FindName("Rejestracja") as Syncfusion.Windows.Tools.Controls.TabItemExt;
                if (rejestracjaTab != null)
                {
                    rejestracjaTab.IsSelected = true;
                    // System.Diagnostics.Debug.WriteLine("Rejestracja_Click: ✅ Przełączono na zakładkę Rejestracja");
                }
                else
                {
                    // System.Diagnostics.Debug.WriteLine("Rejestracja_Click: ❌ Nie znaleziono zakładki Rejestracja");
                    MessageBox.Show("Nie można odnaleźć zakładki 'Rejestracja'.",
                        "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"Rejestracja_Click ERROR: {ex.Message}");
                MessageBox.Show($"Błąd podczas przełączania zakładki:\n{ex.Message}",
                    "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// ✅ NOWA METODA: Ustawia fokus na pole wyszukiwania txtFilterTop
        /// Używana po różnych akcjach: odświeżanie, zapisywanie, czyszczenie
        /// </summary>
        private void SetFocusOnFilter()
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    var searchBox = FindName("txtFilterTop") as System.Windows.Controls.TextBox;
                    if (searchBox != null)
                    {
                        searchBox.Focus();
                        // System.Diagnostics.Debug.WriteLine("BadaniaEditNewView: ✅ Fokus ustawiony na txtFilterTop");
                    }
                    else
                    {
                        // System.Diagnostics.Debug.WriteLine("BadaniaEditNewView: ⚠️ Nie znaleziono txtFilterTop");
                    }
                }
                catch (Exception)
                {
                    // System.Diagnostics.Debug.WriteLine($"BadaniaEditNewView: ❌ Błąd ustawiania fokusa: {ex.Message}");
                }
            }), System.Windows.Threading.DispatcherPriority.Input);
        }

        private void AttachEditToggleHandlers()
        {
            var buttons = new[]
            {
                btnEditToggleBasic,
                btnEditToggleLaryngologist,
                btnEditToggleOphthalmologist,
                btnEditToggleSanitary,
                btnEditToggleLipidogram,
                btnEditToggleEKG,
                btnEditToggleHealthClinic,
                btnEditToggleOther
            };

            foreach (var btn in buttons)
            {
                if (btn != null)
                    btn.Click += ToggleEditExamination_Click;
            }
        }

        // ✅ DODANA METODA: Odpinanie handlerow
        private void DetachToggleHandlers()
        {
            var buttons = new[]
            {
                btnEditToggleBasic,
                btnEditToggleLaryngologist,
                btnEditToggleOphthalmologist,
                btnEditToggleSanitary,
                btnEditToggleLipidogram,
                btnEditToggleEKG,
                btnEditToggleHealthClinic,
                btnEditToggleOther
            };

            foreach (var btn in buttons)
            {
                if (btn != null)
                    btn.Click -= ToggleEditExamination_Click;
            }
        }

        private void ToggleEditExamination_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            if (DataContext is not BadaniaEditNewViewModel vm) return;

            // Sprawdź aktualny stan przycisku (po treści)
            var isActive = btn.Content?.ToString()?.StartsWith("✓") ?? false;

            // Pobierz typ badania z Tag
            var examinationType = btn.Tag?.ToString() ?? "";

            if (isActive)
            {
                // Przełącz na NIEAKTYWNE
                btn.Content = "✗ NIEAKTYWNE";
                btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD0D3D6"));

                // Wyzeruj cenę
                vm.SetCenaForExaminationType(examinationType, null);
            }
            else
            {
                // Przełącz na AKTYWNE
                btn.Content = "✓ AKTYWNE";
                btn.Background = Brushes.LightGreen;

                // Pobierz cenę z cennika i ustaw
                var price = vm.GetPriceForExamination(examinationType);
                vm.SetCenaForExaminationType(examinationType, price);
            }
        }

        /// <summary>
        /// ✅ ZAKTUALIZOWANA METODA: Resetuj wszystkie przyciski toggle na nieaktywne
        /// UWAGA: Ta metoda jest wywoływana z ViewModelu przez event ToggleButtonsResetRequested
        /// </summary>
        private void ResetEdiToggleButtons()
        {
            var buttons = new[]
            {
                btnEditToggleBasic,
                btnEditToggleLaryngologist,
                btnEditToggleOphthalmologist,
                btnEditToggleSanitary,
                btnEditToggleLipidogram,
                btnEditToggleEKG,
                btnEditToggleHealthClinic,
                btnEditToggleOther
            };

            foreach (var btn in buttons)
            {
                if (btn != null)
                {
                    btn.Content = "✗ NIEAKTYWNE";
                    btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD0D3D6"));
                }
            }

            // ✅ DODANE: Po resecie zawsze synchronizuj z ViewModelem (na wypadek gdyby ViewModel miał inne wartości)
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                SyncToggleButtonsWithPrices();
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        /// <summary>
        /// Ustaw stan przycisków toggle na podstawie cen w ViewModelu.
        /// Wywołuj tę metodę po załadowaniu badania z bazy danych.
        /// </summary>
        private void SyncToggleButtonsWithPrices()
        {
            if (DataContext is not BadaniaEditNewViewModel vm) return;

            var buttonsAndPrices = new[]
            {
        (Button: btnEditToggleBasic, Price: vm.Cena1, Type: "Basic"),
        (Button: btnEditToggleLaryngologist, Price: vm.Cena2, Type: "Laryngologist"),
        (Button: btnEditToggleOphthalmologist, Price: vm.Cena3, Type: "Ophthalmologist"),
        (Button: btnEditToggleSanitary, Price: vm.Cena4, Type: "Sanitary"),
        (Button: btnEditToggleLipidogram, Price: vm.Cena5, Type: "Lipidogram"),
        (Button: btnEditToggleEKG, Price: vm.Cena6, Type: "EKG"),
        (Button: btnEditToggleHealthClinic, Price: vm.Cena7, Type: "HealthClinic"),
        (Button: btnEditToggleOther, Price: vm.Cena8, Type: "Other")
    };

            foreach (var item in buttonsAndPrices)
            {
                if (item.Button != null)
                {
                    // Jeśli cena ma wartość > 0 → przycisk AKTYWNY
                    if (item.Price.HasValue && item.Price.Value > 0)
                    {
                        item.Button.Content = "✓ AKTYWNE";
                        item.Button.Background = Brushes.LightGreen;
                        // System.Diagnostics.Debug.WriteLine($"✅ SyncToggleButtons: {item.Type} = AKTYWNE (cena={item.Price})");
                    }
                    else
                    {
                        item.Button.Content = "✗ NIEAKTYWNE";
                        item.Button.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD0D3D6"));
                        // System.Diagnostics.Debug.WriteLine($"⚪ SyncToggleButtons: {item.Type} = NIEAKTYWNE (cena={item.Price})");
                    }
                }
            }
        }



        /// <summary>
        /// Handler dla przycisku "Wyczyść filtr"
        /// </summary>
        private void ClearFilter_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is BadaniaEditNewViewModel vm)
            {
                vm.FilterText = string.Empty;
            }

            // ✅ DODANE: Ustaw fokus po wyczyszczeniu
            SetFocusOnFilter();
        }

        private void DeleteBadanie_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is not BadaniaEditNewViewModel vm)
                {
                    MessageBox.Show("Brak kontekstu widoku (DataContext).", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var selected = vm.SelectedSkierowanie;
                if (selected == null)
                {
                    MessageBox.Show("Nie wybrano badania.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int badanieId = selected.Bad_ID;
                if (badanieId <= 0)
                {
                    MessageBox.Show("Brak powiązanego badania do usunięcia.", "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Potwierdzenie usunięcia
                var result = MessageBox.Show(
                    $"Czy na pewno chcesz usunąć badanie ID={badanieId} dla pacjenta {selected.P_imie} {selected.P_nazwisko}?",
                    "Potwierdzenie usunięcia",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;

                var db = new AccessDbContext();

                // Sprawdź czy badanie istnieje
                var badRecord = db.GetBadanieById(badanieId);
                if (badRecord == null)
                {
                    MessageBox.Show($"Badanie o ID={badanieId} nie istnieje w bazie danych.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                    // Odśwież listę i wyczyść formularz
                    ResetEdiToggleButtons();
                    vm.RefreshFromDb();
                    return;
                }

                // Sprawdź czy badanie powiązane z listą do faktur
                if (badRecord.Bad_L_ID.HasValue && badRecord.Bad_L_ID.Value > 0)
                {
                    MessageBox.Show(
                        "Nie można usunąć badania — jest powiązane z listą do faktur (Bad_L_ID).\nNajpierw usuń powiązanie z listą do faktur.",
                        "Operacja niedozwolona",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Usuń badanie
                bool deleted = db.DeleteBadanie(badanieId);
                if (!deleted)
                {
                    MessageBox.Show("Usuwanie rekordu badania z bazy nie powiodło się.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Odpnij od skierowania (ustaw B_Badanie_ID = 0)
                if (selected.B_ID > 0)
                {
                    try
                    {
                        db.UpdateSkierowanieBadanieId(selected.B_ID, 0);
                    }
                    catch (Exception)
                    {
                        // System.Diagnostics.Debug.WriteLine($"Error unlinking skierowanie: {ex}");
                    }
                }

                // ✅ POPRAWIONE: Resetuj przyciski toggle
                ResetEdiToggleButtons();

                NotificationHelper.ShowInfo("Usunięto badanie", $"Bad_ID = {badanieId}");

                // Odśwież listę badań
                vm.RefreshFromDb();

                // ✅ DODANE: Ustaw fokus po usunięciu
                SetFocusOnFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas usuwania badania: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                // System.Diagnostics.Debug.WriteLine($"DeleteBadanie_Click error: {ex}");
            }
        }

        public static implicit operator BadaniaEditNewView(SkierPacjentaEditViewModel v)
        {
            throw new NotImplementedException();
        }
    }
}

