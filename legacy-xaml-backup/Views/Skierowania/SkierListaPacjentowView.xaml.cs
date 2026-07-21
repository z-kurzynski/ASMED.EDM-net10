using ASMED.WPF.ViewModels;
using ASMED.WPF.ViewModels.Skierowania;
using ASMED.WPF.Helpers;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ASMED.WPF.Views
{
    public partial class SkierListaPacjentowView : UserControl
    {
        public SkierListaPacjentowView()
        {
            InitializeComponent();
            this.DataContext = new SkierListaPacjentowViewModel();

            // ✅ DODANE: Automatyczne ustawienie fokusa na pole wyszukiwania
            this.Loaded += (s, e) =>
            {
                SetFocusOnFilter();
            };

            this.IsVisibleChanged += (s, e) =>
            {
                if (this.IsVisible)
                {
                    SetFocusOnFilter();
                }
            };

            // ✅ NOWE: Obsługa klawiszy funkcyjnych F1/F2/F11/F12
            this.PreviewKeyDown += SkierListaPacjentowView_PreviewKeyDown;
            this.Focusable = true;
        }

        /// <summary>
        /// ✅ NOWA METODA: Obsługa klawiszy funkcyjnych F1/F2/F11/F12
        /// F1 = Odśwież listę
        /// F2 = Wyczyść pole wyszukiwania (focus na filtr)
        /// </summary>
        private void SkierListaPacjentowView_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (DataContext is not SkierListaPacjentowViewModel vm) return;

            try
            {
                switch (e.Key)
                {
                    case Key.F1:
                        // F1 = Odśwież listę
                        // System.Diagnostics.Debug.WriteLine("SkierListaPacjentowView: ⌨️ F1 pressed - Odświeżanie listy");
                        vm.SearchText = string.Empty;

                        // ✅ Wyczyść również pole tekstowe
                        var searchBox1 = FindName("txtSearchPacjent") as TextBox;
                        if (searchBox1 != null)
                        {
                            searchBox1.Text = string.Empty;
                        }

                        vm.RefreshList();
                        SetFocusOnFilter();
                        e.Handled = true;
                        break;

                    case Key.F2:
                        // F2 = Wyczyść pole wyszukiwania + ustaw fokus
                        // System.Diagnostics.Debug.WriteLine("SkierListaPacjentowView: ⌨️ F2 pressed - Czyszczenie pola wyszukiwania");
                        vm.SearchText = string.Empty;

                        // ✅ POPRAWIONE: Wyczyść również pole tekstowe w UI
                        var searchBox2 = FindName("txtSearchPacjent") as TextBox;
                        if (searchBox2 != null)
                        {
                            searchBox2.Text = string.Empty;
                            // System.Diagnostics.Debug.WriteLine("SkierListaPacjentowView: Pole tekstowe wyczyszczone (F2)");
                        }

                        SetFocusOnFilter();
                        e.Handled = true;
                        break;
                }
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"SkierListaPacjentowView_PreviewKeyDown ERROR: {ex.Message}");
            }
        }

        /// <summary>
        /// ✅ NOWA METODA: Ustawia fokus na pole wyszukiwania txtSearchPacjent
        /// </summary>
        private void SetFocusOnFilter()
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    var searchBox = FindName("txtSearchPacjent") as TextBox;
                    if (searchBox != null)
                    {
                        searchBox.Focus();
                        // System.Diagnostics.Debug.WriteLine("SkierListaPacjentowView: ✅ Fokus ustawiony na txtSearchPacjent");
                    }
                }
                catch (Exception)
                {
                    // System.Diagnostics.Debug.WriteLine($"SkierListaPacjentowView: ❌ Błąd ustawiania fokusa: {ex.Message}");
                }
            }), System.Windows.Threading.DispatcherPriority.Input);
        }

        /// <summary>
        /// ✅ NOWA METODA: Handler dla przycisku X (czyszczenie pola wyszukiwania)
        /// </summary>
        private void ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is SkierListaPacjentowViewModel vm)
                {
                    // System.Diagnostics.Debug.WriteLine("ClearSearch_Click: Czyszczenie pola wyszukiwania");
                    vm.SearchText = string.Empty;

                    // ✅ WAŻNE: Wyczyść również pole tekstowe w UI
                    var searchBox = FindName("txtSearchPacjent") as TextBox;
                    if (searchBox != null)
                    {
                        searchBox.Text = string.Empty;
                        // System.Diagnostics.Debug.WriteLine("ClearSearch_Click: Pole tekstowe wyczyszczone");
                    }

                    SetFocusOnFilter();
                }
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"ClearSearch_Click ERROR: {ex.Message}");
            }
        }

        private void DodajPacjenta_Click(object sender, RoutedEventArgs e)
        {
            if (Application.Current.MainWindow?.DataContext is MainWindowViewModel vm)
            {
                vm.SkierowaniaWidok = new SkierPacjentaEditView();
            }
        }

        // ✅ ZMIENIONA METODA: Przełącza na zakładkę "Karta Badan"
        private void WrocLista_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Znajdź MainWindow i przełącz na zakładkę "Skierowania"
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow != null)
                {
                    // Znajdź TabItemExt o nazwie Skierowania
                    var tabControl = mainWindow.FindName("Skierowania") as Syncfusion.Windows.Tools.Controls.TabItemExt;
                    if (tabControl != null)
                    {
                        tabControl.IsSelected = true;
                    }
                    else
                    {
                        // System.Diagnostics.Debug.WriteLine("WrocLista_Click: Nie znaleziono zakładki Skierowania");
                        MessageBox.Show("Nie można odnaleźć zakładki 'Karta Badan'",
                            "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (System.Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"WrocLista_Click error: {ex}");
                MessageBox.Show($"Błąd podczas przełączania zakładki:\n{ex.Message}",
                    "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Skierowanie_Click(object sender, RoutedEventArgs e)
        {
            if (Application.Current.MainWindow?.DataContext is MainWindowViewModel vm)
            {
                vm.SkierowaniaWidok = new SkierPacjentaView();
            }
        }

        private void RefreshList_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // System.Diagnostics.Debug.WriteLine("RefreshList_Click: Odświeżanie listy pacjentów...");

                // Pobierz ViewModel i odśwież dane
                if (this.DataContext is SkierListaPacjentowViewModel viewModel)
                {
                    // Wyczyść wyszukiwanie
                    viewModel.SearchText = string.Empty;

                    // ✅ Wyczyść również pole tekstowe
                    var searchBox = FindName("txtSearchPacjent") as TextBox;
                    if (searchBox != null)
                    {
                        searchBox.Text = string.Empty;
                    }

                    // Przeładuj dane z bazy
                    viewModel.RefreshList();

                    // System.Diagnostics.Debug.WriteLine($"RefreshList_Click: Lista odświeżona - {viewModel.Pacjenci.Count} pacjentów");

                    // Pokaż powiadomienie
                    //NotificationHelper.ShowNotification("Odświeżanie",
                    //    $"Lista pacjentów została odświeżona. Znaleziono {viewModel.Pacjenci.Count} pacjentów.",
                        //Notifications.Wpf.NotificationType.Success);

                    SetFocusOnFilter();
                }
                else
                {
                    // System.Diagnostics.Debug.WriteLine("RefreshList_Click: Brak ViewModelu");
                    MessageBox.Show("Nie można odświeżyć listy - brak połączenia z danymi.",
                        "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"RefreshList_Click ERROR: {ex.Message}");
                MessageBox.Show($"Błąd odświeżania listy:\n\n{ex.Message}",
                    "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

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
    }
}
