using ASMED.WPF.ViewModels;
using ASMED.WPF.ViewModels.Badania;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ASMED.WPF.Views
{
    public partial class BadaniaNewView : UserControl
    {
        private static readonly CultureInfo PlCulture = new CultureInfo("pl-PL");
        public DateTime DataBadania { get; private set; }
        public DateTime DataWaznosci { get; private set; }
        public string ?SelectedWynik { get; private set; }
        public string ?NrKsiegi { get; private set; }
        public object PriceBasicText { get; private set; }

        public BadaniaNewView()
        {
            InitializeComponent();

            var vm = new BadaniaNewViewModel();
            DataContext = vm;

            // Podłącz event resetowania przycisków
            vm.ToggleButtonsResetRequested += ResetNewToggleButtons;

            // ✅ DODANE: Podłącz PropertyChanged dla ustawiania fokusa po akcjach
            vm.PropertyChanged += (s, e) =>
            {
                // Po zapisaniu (PropertyChanged dla Badania/Skierowania)
                if (e.PropertyName == "Skierowania" || e.PropertyName == "Badania")
                {
                    SetFocusOnFilter();
                    // ✅ DODANE: Synchronizuj przyciski po zmianie danych
                    SyncToggleButtonsWithViewModel();
                }
            };

            // Podłącz handlery dla przycisków toggle
            this.Loaded += (s, e) => AttachNewToggleHandlers();

            // ✅ DODANE: Odpinanie eventów przy wyładowaniu
            this.Unloaded += (s, e) =>
            {
                DetachToggleHandlers();
                if (DataContext is BadaniaNewViewModel viewModel)
                {
                    viewModel.ToggleButtonsResetRequested -= ResetNewToggleButtons;
                }
            };

            // Odświeżaj dane po powrocie
            this.Loaded += (s, e) =>
            {
                if (DataContext is BadaniaNewViewModel viewModel)
                {
                    viewModel.RefreshFromDb();
                    ResetNewToggleButtons();
                    // ✅ DODANE: Synchronizuj przyciski z ViewModelem
                    SyncToggleButtonsWithViewModel();
                }

                // ✅ Ustaw fokus po załadowaniu
                SetFocusOnFilter();
            };

            this.IsVisibleChanged += (s, e) =>
            {
                if (this.IsVisible && DataContext is BadaniaNewViewModel viewModel
                    && !viewModel.IsRefreshingAfterSave)
                {
                    viewModel.RefreshFromDb();
                    ResetNewToggleButtons();
                    // ✅ DODANE: Synchronizuj przyciski z ViewModelem (KLUCZOWE dla powrotu z zakładki Edycja)
                    SyncToggleButtonsWithViewModel();

                    // ✅ Ustaw fokus gdy widok staje się widoczny
                    SetFocusOnFilter();
                }
            };

            // ✅ ZMIENIONE: PreviewKeyDown zamiast KeyDown (przechwytuje PRZED kontrolkami potomnymi)
            this.PreviewKeyDown += BadaniaNewView_PreviewKeyDown;
            this.Focusable = true;
        }

        /// <summary>
        /// ✅ ZAKTUALIZOWANA METODA: Obsługa klawiszy funkcyjnych F1-F12
        /// F1 = Odśwież listę
        /// F2 = Wyczyść pole wyszukiwania (focus na filtr)
        /// F11 = Wyczyść formularz
        /// F12 = Zapisz/Modyfikuj badanie
        /// </summary>
        private void BadaniaNewView_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (DataContext is not BadaniaNewViewModel vm) return;

            try
            {
                switch (e.Key)
                {
                    case Key.F1:
                        vm.RefreshCommand?.Execute(null);
                        SetFocusOnFilter();
                        e.Handled = true;
                        break;

                    case Key.F2:
                        vm.FilterText = string.Empty;
                        SetFocusOnFilter();
                        e.Handled = true;
                        break;

                    case Key.F11:
                        vm.ClearCommand?.Execute(null);
                        e.Handled = true;
                        break;

                    case Key.F12:
                        vm.SaveBadanieCommand?.Execute(null);
                        e.Handled = true;
                        break;
                }
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"BadaniaNewView_PreviewKeyDown ERROR: {ex.Message}");
            }
        }

        private void AttachNewToggleHandlers()
        {
            // Znajdź wszystkie przyciski toggle po nazwie i podłącz handler
            var buttons = new[]
            {
                btnNewToggleBasic,
                btnNewToggleLaryngologist,
                btnNewToggleOphthalmologist,
                btnNewToggleSanitary,
                btnNewToggleLipidogram,
                btnNewToggleEKG,
                btnNewToggleHealthClinic,
                btnNewToggleOther
            };

            foreach (var btn in buttons)
            {
                if (btn != null)
                    btn.Click += ToggleNewExamination_Click;
            }
        }

        private void ToggleNewExamination_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            if (DataContext is not BadaniaNewViewModel vm) return;

            // Sprawdź aktualny stan przycisku (po treści)
            var isActive = btn.Content?.ToString()?.StartsWith("✓") ?? false;

            // Pobierz typ badania z Tag
            var examinationType = btn.Tag?.ToString() ?? "";

            if (isActive)
            {
                // Przełącz na NIEAKTYWNE
                btn.Content = "✗ NIEAKTYWNE";
                btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD0D3D6"));

                // ✅ POPRAWIONE: Wyzeruj cenę (null zamiast 0m)
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

        // ✅ ZAKTUALIZOWANA METODA: Resetuj wszystkie przyciski toggle na nieaktywne
        // UWAGA: Ta metoda jest wywoływana z ViewModelu przez event ToggleButtonsResetRequested
        private void ResetNewToggleButtons()
        {
            var buttons = new[]
            {
                btnNewToggleBasic,
                btnNewToggleLaryngologist,
                btnNewToggleOphthalmologist,
                btnNewToggleSanitary,
                btnNewToggleLipidogram,
                btnNewToggleEKG,
                btnNewToggleHealthClinic,
                btnNewToggleOther
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
                SyncToggleButtonsWithViewModel();
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }
        /*
        private void ClearEditForm_xamk()
        {
            DataBadania = DateTime.Now;
            DataWaznosci = DateTime.Now.AddYears(3);
            SelectedWynik = "Pozytywne";
            NrKsiegi = string.Empty;
           // ClearPrices();
            ResetNewToggleButtons();
        }
        */

        public Action ClearPrices { get; set; }

        // Handler dla przycisku "Wyczyść filtr"
        private void ClearFilter_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is BadaniaNewViewModel vm)
            {
                vm.FilterText = string.Empty;
            }

            // ✅ DODANE: Ustaw fokus po wyczyszczeniu
            SetFocusOnFilter();
        }

        // ✅ ZAKTUALIZOWANE: Handler dla przycisku "Lista Badań" - przełącza na zakładkę Edycja Badań
        private void Lista_Badan_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Znajdź MainWindow i przełącz na zakładkę "BadaniaEdit"
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow != null)
                {
                    // Znajdź TabControl i przełącz na zakładkę BadaniaEdit
                    var tabControl = mainWindow.FindName("BadaniaEdit") as Syncfusion.Windows.Tools.Controls.TabItemExt;
                    if (tabControl != null)
                    {
                        tabControl.IsSelected = true;
                    }
                }
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"Lista_Badan_Click error: {ex}");
                MessageBox.Show($"Błąd podczas przełączania zakładki:\n{ex.Message}",
                    "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ✅ DODANA METODA: Odpinanie handlerów
        private void DetachToggleHandlers()
        {
            var buttons = new[]
            {
                btnNewToggleBasic,
                btnNewToggleLaryngologist,
                btnNewToggleOphthalmologist,
                btnNewToggleSanitary,
                btnNewToggleLipidogram,
                btnNewToggleEKG,
                btnNewToggleHealthClinic,
                btnNewToggleOther
            };

            foreach (var btn in buttons)
            {
                if (btn != null)
                    btn.Click -= ToggleNewExamination_Click;
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
                    txtFilterTop?.Focus();
                }
                catch (Exception)
                {
                    // System.Diagnostics.Debug.WriteLine($"BadaniaNewView: ❌ Błąd ustawiania fokusa: {ex.Message}");
                }
            }), System.Windows.Threading.DispatcherPriority.Input);
        }

        /// <summary>
        /// ✅ NOWA METODA: Synchronizuje stan wizualny przycisków Toggle z wartościami Cena1-8 z ViewModelu
        /// ROZWIĄZUJE problem: Przyciski nie resetują się po powrocie z zakładki "Edycja Badań"
        /// </summary>
        private void SyncToggleButtonsWithViewModel()
        {
            if (DataContext is not BadaniaNewViewModel vm) return;

            try
            {
                var buttonMappings = new[]
                {
                    new { Button = btnNewToggleBasic, Cena = vm.Cena1, Type = "Basic" },
                    new { Button = btnNewToggleLaryngologist, Cena = vm.Cena2, Type = "Laryngologist" },
                    new { Button = btnNewToggleOphthalmologist, Cena = vm.Cena3, Type = "Ophthalmologist" },
                    new { Button = btnNewToggleSanitary, Cena = vm.Cena4, Type = "Sanitary" },
                    new { Button = btnNewToggleLipidogram, Cena = vm.Cena5, Type = "Lipidogram" },
                    new { Button = btnNewToggleEKG, Cena = vm.Cena6, Type = "EKG" },
                    new { Button = btnNewToggleHealthClinic, Cena = vm.Cena7, Type = "HealthClinic" },
                    new { Button = btnNewToggleOther, Cena = vm.Cena8, Type = "Other" }
                };

                foreach (var mapping in buttonMappings)
                {
                    if (mapping.Button == null) continue;

                    // Jeśli Cena ma wartość (nie null i > 0) → AKTYWNE
                    // Jeśli Cena jest null lub 0 → NIEAKTYWNE
                    bool shouldBeActive = mapping.Cena.HasValue && mapping.Cena.Value > 0;

                    if (shouldBeActive)
                    {
                        mapping.Button.Content = "✓ AKTYWNE";
                        mapping.Button.Background = Brushes.LightGreen;
                    }
                    else
                    {
                        mapping.Button.Content = "✗ NIEAKTYWNE";
                        mapping.Button.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD0D3D6"));
                    }
                }
            }
            catch
            {
                // błąd synchronizacji przycisków — ignorowany
            }
        }




        /// <summary>
        /// ✅ NOWA METODA: Ustawia filtr na ID skierowania (wywoływana z WizytyViewViewModel)
        /// </summary>
        public void SetFilterByIdSkierowania(int bId)
        {
            try
            {
                if (DataContext is BadaniaNewViewModel vm)
                {
                    vm.FilterText = $"#{bId}";
                    vm.SelectedFilter = "ID";
                    SetFocusOnFilter();
                }
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"❌ SetFilterByIdSkierowania error: {ex.Message}");
            }
        }

        private void btnWyczysc_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
