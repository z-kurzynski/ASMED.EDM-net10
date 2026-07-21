using ASMED.WPF; // FirmaDto
using ASMED.WPF.Helpers;
using ASMED.WPF.ViewModels.ListaDoFaktur;
using ASMED.WPF.Views.lista_do_faktur;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media; // Dodaj ten using zamiast System.Drawing

namespace ASMED.WPF.Views.lista_do_faktur
{
    public partial class ListaFaktAddView : UserControl
    {
        public IFormatProvider? PlCulture { get; private set; }

        public ListaFaktAddView()
        {
            InitializeComponent();
            // ustaw kulturowe formatowanie liczb (polskie)
            PlCulture = CultureInfo.GetCultureInfo("pl-PL");
            this.Loaded += ListaFaktAddView_Loaded;
            this.Unloaded += ListaFaktAddView_Unloaded;
        }

        private void ListaFaktAddView_Loaded(object? sender, RoutedEventArgs e)
        {
            if (this.DataContext is ListaFaktAddViewModel vm)
            {
                vm.RequestResetSelectionState -= ResetSelectionState;
                vm.RequestResetSelectionState += ResetSelectionState;

                // ✅ NOWE: Inteligentne odświeżanie listy badań przy każdym pokazaniu widoku
                vm.SmartRefreshAvailableBadania();
            }
        }

        private void ListaFaktAddView_Unloaded(object? sender, RoutedEventArgs e)
        {
            if (this.DataContext is ListaFaktAddViewModel vm)
            {
                vm.RequestResetSelectionState -= ResetSelectionState;
            }
        }

        private void OpenFirmaDialog_Click(object sender, RoutedEventArgs e)
        {
            // krótkie logowanie/diagnostyka przez NotificationHelper — usuń po debugowaniu
            NotificationHelper.ShowInfo("Otwieram dialog wyboru firmy...", "Debug");

            var dlg = new FirmaSelectDialog { Owner = Window.GetWindow(this) };
            var result = dlg.ShowDialog();

            // sprawdź czy dialog zwrócił true
            NotificationHelper.ShowInfo($"Dialog result: {result}", "Debug");

            if (result == true)
            {
                var sel = dlg.SelectedFirma; // global::ASMED.WPF.FirmaDto
                if (sel == null)
                {
                    NotificationHelper.ShowWarning("Wybrana firma jest null (SelectedFirma==null).");
                    return;
                }

                // pokaż co mamy w sel.cennik (może być whitespace/newline)
                NotificationHelper.ShowInfo($"Firma: '{sel.Nazwa}' cennik raw: '{sel.Cennik ?? "<null>"}'", "Debug");

                if (DataContext is ListaFaktAddViewModel vm)
                {
                    // Ustawienie firmy w VM
                    vm.SetSelectedFirmaByValues(sel.Id, sel.Nazwa);

                    // Jeżeli firma ma przypisany cennik — normalizuj i ustaw SelectedCennik.
                    var raw = sel.Cennik ?? string.Empty;
                    var cennik = raw.Trim();

                    if (string.IsNullOrEmpty(cennik))
                    {
                        NotificationHelper.ShowWarning("Pole cennik jest puste lub zawiera tylko spacje.");
                        return;
                    }

                    // Jeśli lista StatusOptions nie zawiera takiego cennika, dodaj go na UI thread
                    var exists = vm.StatusOptions.Any(s => string.Equals(s?.Trim(), cennik, StringComparison.CurrentCultureIgnoreCase));
                    if (!exists)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            vm.StatusOptions.Insert(0, cennik);
                        });
                        NotificationHelper.ShowInfo($"Dodano cennik do StatusOptions: '{cennik}'", "Debug");
                    }

                    // ustaw SelectedCennik na oczyszczoną wartość
                    vm.SelectedCennik = cennik;
                    NotificationHelper.ShowInfo($"Ustawiono SelectedCennik = '{cennik}'", "Debug");
                }
            }
            else
            {
                NotificationHelper.ShowInfo("Anulowano wybór firmy (dialog result != true).", "Debug");
            }
        }

        // Wspólna metoda przywracająca zakładkę do świeżo utworzonego ListaDoFakturView
        private void ReturnToListaDoFakturView()
        {
            try
            {
                var main = Application.Current.MainWindow as MainWindow;
                if (main == null) return;

                var tab = main.FindName("ListaDoFaktur") as System.Windows.Controls.TabItem;
                if (tab == null)
                {
                    foreach (var child in LogicalTreeHelper.GetChildren(main))
                    {
                        if (child is System.Windows.Controls.TabControl tc)
                        {
                            foreach (var item in tc.Items)
                            {
                                if (item is System.Windows.Controls.TabItem ti &&
                                    ti.Header?.ToString()?.IndexOf("Lista do Faktur", StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    tab = ti;
                                    break;
                                }
                            }
                        }
                        if (tab != null) break;
                    }
                }

                if (tab == null)
                {
                    MessageBox.Show("Nie znaleziono zakładki 'ListaDoFaktur' w MainWindow.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var fresh = new ASMED.WPF.Views.ListaDoFakturView();
                tab.Content = fresh;

                try
                {
                    if (tab.Parent is TabControl parentTabControl)
                        parentTabControl.SelectedItem = tab;
                }
                catch { }

                try
                {
                    tab.InvalidateMeasure();
                    tab.InvalidateArrange();
                    tab.UpdateLayout();
                }
                catch { }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas powrotu do listy: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenListaDoFaktur_Click(object sender, RoutedEventArgs e)
        {
            ReturnToListaDoFakturView();
        }

        private void PriceTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // optional: validate numeric input; nie zmieniam VM tutaj, tylko chronię przed wyjątkami w UI
            if (sender is TextBox tb)
            {
                // prosty sanity check — usuń nie-numeryczne znaki (opcjonalne)
                // pozostawienie pustego pola jest akceptowalne
            }

            // po każdej zmianie przelicz sumę
            try { RecalculateTotal(); } catch { }
        }

        private void ToggleExamination_Click(object sender, RoutedEventArgs e)
        {
            // Toggle wizualny stanu przycisku (nie wpływa na VM domyślnie)
            if (sender is Button b)
            {
                var btn = b;
                var tag = btn.Tag?.ToString() ?? string.Empty;
                TextBox? target = tag switch
                {
                    "Basic" => txtBasicPrice,
                    "Laryngologist" => txtLaryngologistPrice,
                    "Ophthalmologist" => txtOphthalmologistPrice,
                    "Sanitary" => txtSanitaryPrice,
                    "Lipidogram" => txtLipidogramPrice,
                    "EKG" => txtEKGPrice,
                    "HealthClinic" => txtHealthClinicPrice,
                    "Other" => txtOtherPrice,
                    _ => null
                };
                // Toggle visual state
                var isActive = btn.Content?.ToString()?.StartsWith("✓") ?? false;
                if (isActive)
                {
                    // przełącz na nieaktywne
                    btn.Content = "✗ NIEAKTYWNE";
                    btn.Background = System.Windows.Media.Brushes.LightCoral;
                    if (target != null)
                    {
                        target.IsEnabled = false;
                        // opcjonalnie wyczyść wartość, żeby nie była liczona
                        target.Text = string.Empty;
                    }
                }
                else
                {
                    // przełącz na aktywne
                    btn.Content = "✓ AKTYWNE";
                    btn.Background = Brushes.LightGreen;
                    if (target != null)
                    {
                        target.IsEnabled = true;
                        // jeśli aktywujemy, przepisz wartość z odpowiadającej etykiety cenowej
                        TextBlock? sourceLabel = tag switch
                        {
                            "Basic" => lblBasicPriceList,
                            "Laryngologist" => lblLaryngologistPriceList,
                            "Ophthalmologist" => lblOphthalmologistPriceList,
                            "Sanitary" => lblSanitaryPriceList,
                            "Lipidogram" => lblLipidogramPriceList,
                            "EKG" => lblEKGPriceList,
                            "HealthClinic" => lblHealthClinicPriceList,
                            "Other" => lblOtherPriceList,
                            _ => null
                        };
                        if (sourceLabel != null)
                        {
                            // przepisz tekst z etykiety (np. "80,00 zł") do pola tekstowego
                            var txt = sourceLabel.Text?.Trim() ?? string.Empty;
                            try
                            {
                                // usuń symbol waluty (zł) aby łatwiej modyfikować wartość w TextBoxie
                                txt = txt.Replace("zł", string.Empty, StringComparison.OrdinalIgnoreCase)
                                         .Replace("zl", string.Empty, StringComparison.OrdinalIgnoreCase)
                                         .Trim();
                            }
                            catch
                            {
                                // fallback
                                txt = txt.Replace("zł", "").Replace("ZŁ", "").Replace("zl", "").Replace("ZL", "").Trim();
                            }
                            target.Text = txt;
                        }
                    }
                    RecalculateTotal();
                }
            }
        }

        // Przelicza sumę z pól cenowych i ustawia lblTotalPrice.Text
        private void RecalculateTotal()
        {
            decimal total = 0m;

            // Sprawdź czy elementy istnieją (w razie gdy XAML nie załadowany) i sumuj
            if (txtBasicPrice != null) total += ParsePriceSafe(txtBasicPrice.Text);
            if (txtLaryngologistPrice != null) total += ParsePriceSafe(txtLaryngologistPrice.Text);
            if (txtOphthalmologistPrice != null) total += ParsePriceSafe(txtOphthalmologistPrice.Text);
            if (txtSanitaryPrice != null) total += ParsePriceSafe(txtSanitaryPrice.Text);
            if (txtLipidogramPrice != null) total += ParsePriceSafe(txtLipidogramPrice.Text);
            if (txtEKGPrice != null) total += ParsePriceSafe(txtEKGPrice.Text);
            if (txtHealthClinicPrice != null) total += ParsePriceSafe(txtHealthClinicPrice.Text);
            if (txtOtherPrice != null) total += ParsePriceSafe(txtOtherPrice.Text);

            if (lblTotalPrice != null)
            {
                // Format PL with two decimals and "zł"
                lblTotalPrice.Text = string.Format(PlCulture, "{0:N2} zł", total);
            }
        }

        // Parsuje pojedyncze pole cenowe (akceptuje "123,45", "123.45", "123,45 zł", "123")
        private decimal ParsePriceSafe(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return 0m;

            var input = s.Trim();
            // usuń symbol zł jeśli jest
            input = input.Replace("zł", "").Replace("zl", "").Trim();
            // spróbuj parsować w kulturze polskiej
            if (decimal.TryParse(input, NumberStyles.Number, PlCulture, out var valPl))
                return valPl;
            // fallback: invariant (np. user użył kropki)
            if (decimal.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out var valInv))
                return valInv;
            return 0m;
        }


        private void ResetSelectionState()
        {
            // Set all buttons in Grid_Przyciski to inactive and clear/disable price textboxes
            try
            {
                var buttons = new Button[] { btnBasic, btnLaryngologist, btnOphthalmologist, btnSanitary, btnLipidogram, btnEKG, btnHealthClinic, btnOther };
                foreach (var b in buttons)
                {
                    if (b == null) continue;
                    b.Content = "✗ NIEAKTYWNE";
                    // restore initial inactive background color used in XAML (#FFD0D3D6)
                    b.Background = (SolidColorBrush)(new BrushConverter().ConvertFromString("#FFD0D3D6"));
                    b.Foreground = Brushes.Black;
                }

                var textboxes = new TextBox[] { txtBasicPrice, txtLaryngologistPrice, txtOphthalmologistPrice, txtSanitaryPrice, txtLipidogramPrice, txtEKGPrice, txtHealthClinicPrice, txtOtherPrice };
                foreach (var tb in textboxes)
                {
                    if (tb == null) continue;
                    tb.IsEnabled = false;
                    tb.Text = string.Empty;
                }

                RecalculateTotal();
            }
            catch { }
        }



        // Obsługa podwójnego kliknięcia w wiersz DataGrid: ustaw SelectedAssignedBadanie w VM
        private void dgAssignedBadania_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (DataContext is not ListaFaktAddViewModel vm) return;

                AccessDbContext.AssignedBadanieDto? dto = null;

                // EventSetter na DataGridRow -> sender będzie DataGridRow
                if (sender is DataGridRow row && row.Item is AccessDbContext.AssignedBadanieDto rowDto)
                {
                    dto = rowDto;
                }
                else
                {
                    // fallback: użyj SelectedItem z DataGrid
                    if (this.dgAssignedBadania?.SelectedItem is AccessDbContext.AssignedBadanieDto sel)
                        dto = sel;
                }

                if (dto != null)
                {
                    vm.SelectedAssignedBadanie = dto;

                    // ustaw UI: wypełnij textboxy cenowe, przełącz przyciski i przelicz sumę
                    ApplyDtoPricesToUi(dto);
                }
            }
            catch (Exception)
            {
                // Zaloguj i pokaż minimalny komunikat; szczegóły w Output
                // System.Diagnostics.Debug.WriteLine($"dgAssignedBadania_MouseDoubleClick error: {ex}");
                MessageBox.Show("Błąd podczas próby edycji zaznaczonego rekordu.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Ustawia teksty w TextBoxach cenowych oraz włącza/wyłącza odpowiadające przyciski, potem przelicza sumę.
        private void ApplyDtoPricesToUi(AccessDbContext.AssignedBadanieDto dto)
        {
            try
            {
                if (dto == null)
                {
                    ResetSelectionState();
                    return;
                }

                // lokalny helper
                void Apply(decimal? value, TextBox tb, Button btn, TextBlock label)
                {
                    if (tb == null || btn == null) return;

                    if (value.HasValue && value.Value != 0m)
                    {
                        tb.IsEnabled = true;
                        tb.Text = value.Value.ToString("N2", (IFormatProvider)(PlCulture ?? CultureInfo.CurrentCulture));
                        btn.Content = "✓ AKTYWNE";
                        btn.Background = Brushes.LightGreen;
                        btn.Foreground = Brushes.Black;
                    }
                    else
                    {
                        tb.IsEnabled = false;
                        tb.Text = string.Empty;
                        btn.Content = "✗ NIEAKTYWNE";
                        btn.Background = (SolidColorBrush)(new BrushConverter().ConvertFromString("#FFD0D3D6"));
                        btn.Foreground = Brushes.Black;
                    }

                    // jeśli etykieta cenowa istnieje, zostaw jej text (Price...Text jest bindingiem do VM)
                    // label nie modyfikujemy tu, ale można ustawić jeśli potrzebne:
                    // if (label != null) label.Text = ...;
                }

                Apply(dto.Bad_Cena1, txtBasicPrice, btnBasic, lblBasicPriceList);
                Apply(dto.Bad_Cena2, txtLaryngologistPrice, btnLaryngologist, lblLaryngologistPriceList);
                Apply(dto.Bad_Cena3, txtOphthalmologistPrice, btnOphthalmologist, lblOphthalmologistPriceList);
                Apply(dto.Bad_Cena4, txtSanitaryPrice, btnSanitary, lblSanitaryPriceList);
                Apply(dto.Bad_Cena5, txtLipidogramPrice, btnLipidogram, lblLipidogramPriceList);
                Apply(dto.Bad_Cena6, txtEKGPrice, btnEKG, lblEKGPriceList);
                Apply(dto.Bad_Cena7, txtHealthClinicPrice, btnHealthClinic, lblHealthClinicPriceList);
                Apply(dto.Bad_Cena8, txtOtherPrice, btnOther, lblOtherPriceList);

                // Synchronizacja checkboxów jeśli powiązane z cenami (jeśli używasz bindingów do VM to nie musimy ich tu ustawiać)
                try
                {
                    // jeśli checkboxy istnieją i chcesz ustawić je bezpośrednio (opcjonalne)
                    // ksiązeczka powiązana z Bad_Cena4; urlop z Bad_Cena7
                    if (FindName("IsKsiazeczkaChecked") == null)
                    {
                        // nic nie robimy, VM powinien trzymać state i być powiązany z XAML
                    }
                }
                catch { }

                // przelicz sumę widoku
                RecalculateTotal();
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"ApplyDtoPricesToUi error: {ex}");
            }
        }

        internal void Show()
        {
            throw new NotImplementedException();
        }
    }
}
