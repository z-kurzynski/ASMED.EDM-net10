using ASMED.WPF.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ASMED.WPF.ViewModels.Skierowania;

namespace ASMED.WPF.Views
{
    public partial class SkierowaniaView : UserControl
    {
        public SkierowaniaView()
        {
            InitializeComponent();
            // Do not set DataContext here; allow hosting DataTemplate or MainWindowViewModel to supply the ViewModel.
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                this.DataContext = new SkierowaniaViewModel();
            }

            // ? Podłącz PropertyChanged dla ustawiania fokusa po akcjach
            this.DataContextChanged += (s, e) =>
            {
                // Odpinaj stary handler
                if (e.OldValue is SkierowaniaViewModel oldVm)
                {
                    oldVm.PropertyChanged -= OnViewModelPropertyChanged;
                }

                // Podłącz nowy handler
                if (this.DataContext is SkierowaniaViewModel newVm)
                {
                    newVm.PropertyChanged += OnViewModelPropertyChanged;
                }
            };

            // When view loaded or becomes visible, try refreshing data if VM provides RefreshFromDb
            this.Loaded += (s, e) =>
            {
                if (this.DataContext is SkierowaniaViewModel vm)
                {
                    try { vm.RefreshFromDb(); } catch { }

                    // Podłącz PropertyChanged jeśli jeszcze nie było
                    vm.PropertyChanged -= OnViewModelPropertyChanged; // Odpinaj aby uniknąć duplikatów
                    vm.PropertyChanged += OnViewModelPropertyChanged;
                }

                // ? Ustaw fokus po załadowaniu
                SetFocusOnFilter();
            };

            this.IsVisibleChanged += (s, e) =>
            {
                if (this.IsVisible && this.DataContext is SkierowaniaViewModel vm)
                {
                    try { vm.RefreshFromDb(); } catch { }

                    // ? Ustaw fokus gdy widok staje się widoczny
                    SetFocusOnFilter();
                }
            };

            // ? NOWE: Obsługa klawiszy funkcyjnych F1/F2/F11/F12
            this.PreviewKeyDown += SkierowaniaView_PreviewKeyDown;
            this.Focusable = true;
        }

        /// <summary>
        /// ? NOWA METODA: Obsługa klawiszy funkcyjnych F1/F2/F11/F12
        /// F1 = Odśwież listę
        /// F2 = Wyczyść pole wyszukiwania (focus na filtr)
        /// F11 = (Reserved - nieużywane w tym widoku)
        /// F12 = (Reserved - nieużywane w tym widoku)
        /// </summary>
        private void SkierowaniaView_PreviewKeyDown(object? sender, KeyEventArgs e)
        {
            if (DataContext is not SkierowaniaViewModel vm) return;

            try
            {
                switch (e.Key)
                {
                    case Key.F1:
                        // F1 = Odśwież listę
                        // System.Diagnostics.Debug.WriteLine("SkierowaniaView: ?? F1 pressed - Odświeżanie listy");
                        vm.RefreshFromDb();
                        SetFocusOnFilter();
                        e.Handled = true;
                        break;

                    case Key.F2:
                        // F2 = Wyczyść pole wyszukiwania + ustaw fokus
                        // System.Diagnostics.Debug.WriteLine("SkierowaniaView: ?? F2 pressed - Czyszczenie pola wyszukiwania");
                        vm.SearchText = string.Empty;
                        SetFocusOnFilter();
                        e.Handled = true;
                        break;
                }
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"SkierowaniaView_PreviewKeyDown ERROR: {ex.Message}");
            }
        }

        /// <summary>
        /// ? NOWA METODA: Ustawia fokus na pole wyszukiwania txtSearchPacjent
        /// Używana po różnych akcjach: odświeżanie, powrót z edycji
        /// </summary>
        private void SetFocusOnFilter()
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    txtSearchPacjent?.Focus();
                    // System.Diagnostics.Debug.WriteLine("SkierowaniaView: ? Fokus ustawiony na txtSearchPacjent");
                }
                catch (Exception)
                {
                    // System.Diagnostics.Debug.WriteLine($"SkierowaniaView: ? Błąd ustawiania fokusa: {ex.Message}");
                }
            }), System.Windows.Threading.DispatcherPriority.Input);
        }

        /// <summary>
        /// ? NOWA METODA: Handler dla PropertyChanged z ViewModelu
        /// Reaguje na zmiany SearchText (czyszczenie) i SkierowaniaFiltered (odświeżenie)
        /// </summary>
        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Po wyczyszczeniu pola wyszukiwania (SearchText = "")
            if (e.PropertyName == "SearchText")
            {
                if (sender is SkierowaniaViewModel vm && string.IsNullOrEmpty(vm.SearchText))
                {
                    // System.Diagnostics.Debug.WriteLine("SkierowaniaView: SearchText wyczyszczony - ustawiam fokus");
                    SetFocusOnFilter();
                }
            }

            // Po odświeżeniu listy
            if (e.PropertyName == "SkierowaniaFiltered" || e.PropertyName == "Skierowania")
            {
                // System.Diagnostics.Debug.WriteLine($"SkierowaniaView: PropertyChanged - {e.PropertyName}");
                SetFocusOnFilter();
            }
        }

        private void RefreshList_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                // System.Diagnostics.Debug.WriteLine("RefreshList_Click: Odświeżanie listy skierowań...");

                // Pobierz ViewModel i odśwież dane
                if (this.DataContext is SkierowaniaViewModel viewModel)
                {
                    // Wyczyść wyszukiwanie
                    viewModel.SearchText = string.Empty;

                    // Przeładuj dane z bazy
                    viewModel.RefreshFromDb();

                    // System.Diagnostics.Debug.WriteLine($"RefreshList_Click: Lista odświeżona - {viewModel.Skierowania.Count} skierowań");

                    // Pokaż powiadomienie
                    //Helpers.NotificationHelper.ShowNotification("Odświeżanie",
                    //    $"Lista skierowań została odświeżona. Znaleziono {viewModel.Skierowania.Count} skierowań.",
                    //    Notifications.Wpf.NotificationType.Success);

                    // ? DODANE: Ustaw fokus po odświeżeniu
                    SetFocusOnFilter();
                }
                else
                {
                    // System.Diagnostics.Debug.WriteLine("RefreshList_Click: Brak ViewModelu");
                    MessageBox.Show("Nie można odświeżyć listy - brak połączenia z danymi.",
                        "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (System.Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"RefreshList_Click ERROR: {ex.Message}");
                MessageBox.Show($"Błąd odświeżania listy:\n\n{ex.Message}",
                    "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// ✅ NOWA METODA: Przełącza na zakładkę "Rejestracja" (RejestracjaView)
        /// </summary>
        private void Rejestracja_Click(object? sender, RoutedEventArgs e)
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
    }
}
