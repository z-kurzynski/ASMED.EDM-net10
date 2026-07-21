using ASMED.WPF.ViewModels;
using System.Windows.Controls;
using System.ComponentModel;
using ASMED.WPF.ViewModels.Badania;
using System.Windows;
using Syncfusion.UI.Xaml.Grid;
using System;
using System.Globalization;
using System.Windows.Media;
using ASMED.WPF.Helpers;

namespace ASMED.WPF.Views
{
    public partial class BadaniaListaView : UserControl
    {
        private static readonly CultureInfo PlCulture = new("pl-PL");

        public BadaniaListaView()
        {
            InitializeComponent();
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                // Use legacy BadaniaViewModel wrapper so list+edit share the same state
                var vm = new BadaniaViewModel();
                DataContext = vm;

                // refresh when shown
                this.Loaded += (s, e) =>
                {
                    try
                    {
                        vm.RefreshFromDb();
                    }
                    catch (Exception ex)
                    {
                        try { NotificationHelper.ShowInfo("RefreshFromDb error", ex.Message); } catch { }
                    }

                    try
                    {
                        vm.RefreshBadania();
                        try { NotificationHelper.ShowInfo("Badania loaded", $"Count = {vm.Badania?.Count ?? 0}"); } catch { }
                        try
                        {
                            if (vm.Badania != null && vm.Badania.Count > 0)
                            {
                                var preview = string.Join(", ", vm.Badania.Take(5).Select(b => $"{b.B_ID}:{b.P_nazwisko}:{(b.Bad_Razem.HasValue ? b.Bad_Razem.Value.ToString("N2") : "-")}"));
                                // System.Diagnostics.Debug.WriteLine("Badania preview: " + preview);
                                try { NotificationHelper.ShowInfo("Badania preview", preview); } catch { }
                            }
                        }
                        catch { }
                    }
                    catch (Exception ex)
                    {
                        try { NotificationHelper.ShowInfo("RefreshBadania error", ex.Message); } catch { }
                    }

                    // Inject the edit view into the right panel and bind to wrapper's Edit VM
                    try
                    {
                        if (Edit_prawa != null)
                        {
                            // Do not add new child here — the editor UI is defined in XAML inside Edit_prawa.
                            // Instead, set the panel's DataContext to the edit VM so XAML bindings use it.
                            Edit_prawa.DataContext = vm.Edit;
                            Edit_prawa.Visibility = Visibility.Visible;
                        }
                    }
                    catch (Exception ex)
                    {
                        try { NotificationHelper.ShowInfo("Inject edit view failed", ex.Message); } catch { }
                    }

                    try
                    {
                        // Force-assign ItemsSource to ensure grid shows data even if binding failed
                        if (Lista_Badan != null && vm.Badania != null)
                        {
                            Lista_Badan.ItemsSource = vm.Badania;
                            try { NotificationHelper.ShowInfo("Debug", "Lista_Badan.ItemsSource assigned from code-behind"); } catch { }
                            // preselect first item for quick visual test
                            if (vm.Badania.Count > 0)
                            {
                                try { vm.SelectedWizyta = vm.Badania[0]; } catch { }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        try { NotificationHelper.ShowInfo("Assign ItemsSource failed", ex.Message); } catch { }
                    }
                };

                this.IsVisibleChanged += (s, e) =>
                {
                    if (this.IsVisible)
                    {
                        try
                        {
                            vm.RefreshFromDb();
                        }
                        catch (Exception ex)
                        {
                            try { NotificationHelper.ShowInfo("RefreshFromDb on Visible error", ex.Message); } catch { }
                        }
                        try
                        {
                            vm.RefreshBadania();
                            try { NotificationHelper.ShowInfo("Badania refreshed on Visible", $"Count = {vm.Badania?.Count ?? 0}"); } catch { }
                        }
                        catch (Exception ex)
                        {
                            try { NotificationHelper.ShowInfo("RefreshBadania on Visible error", ex.Message); } catch { }
                        }
                    }
                };

                // initialize UI state and subscribe to VM changes
                ResetSelectionState();
                if (vm is INotifyPropertyChanged inpc)
                {
                    inpc.PropertyChanged += Vm_PropertyChanged;
                }
            }
        }

        // Handler for 'Nowe Badanie' button (matches BadaniaView behavior)
        private void Nowe_Badanie_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                var main = Application.Current.MainWindow as MainWindow;
                if (main == null) return;

                var badaniaTab = main.FindName("Badania") as System.Windows.Controls.TabItem;
                if (badaniaTab == null) return;

                badaniaTab.Content = new BadaniaView();
            }
            catch { }
        }

        // Parsuje pojedyncze pole cenowe (akceptuje "123,45", "123.45", "123,45 zł", "123")
        private decimal ParsePriceSafe(string? s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return 0m;

            var input = s.Trim();
            // usuń symbol zł jeśli jest
            input = input.Replace("zł", "").Replace("zl", "").Trim();
            // spróbuj parsować w kulturze polskiej
            if (decimal.TryParse(input, System.Globalization.NumberStyles.Number, PlCulture, out var valPl))
                return valPl;
            // fallback: invariant (np. user użył kropki)
            if (decimal.TryParse(input, System.Globalization.NumberStyles.Number, CultureInfo.InvariantCulture, out var valInv))
                return valInv;
            return 0m;
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

        // Obsługa TextChanged dla wszystkich pól cenowych
        private void PriceTextBox_TextChanged(object? sender, TextChangedEventArgs e)
        {
            RecalculateTotal();
        }

        // Przełącza stan badania (aktywne / nieaktywne) i powiązanego TextBoxa
        private void ToggleExamination_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is not Button btn)
                return;

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
                btn.Background = Brushes.LightCoral;
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
            }

            RecalculateTotal();
        }

        // Obsługa przycisku czyszczenia filtru (jeśli pole istnieje)
        private void ClearFilter_Click(object? sender, RoutedEventArgs e)
        {
            if (txtFilterTop != null)
                txtFilterTop.Text = string.Empty;
        }

        private void ResetSelectionState()
        {
            // Set all buttons in Grid_Przyciski to inactive and clear/disable price textboxes
            try
            {
                // Trap: log and mark UI so we can tell ResetSelectionState was invoked
                try
                {
                    // System.Diagnostics.Debug.WriteLine($"ResetSelectionState invoked at {DateTime.Now:O}");
                    if (lblTotalPrice != null)
                    {
                        // visible marker for debugging
                        // lblTotalPrice.Text = "[RESET CALLED]";
                    }
                }
                catch { }

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

        private void Vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (string.Equals(e.PropertyName, "SelectedWizyta", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(e.PropertyName, "Wizyty", StringComparison.OrdinalIgnoreCase))
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    ResetSelectionState();
                    try
                    {
                        if (btnDeleteBadanie != null)
                            btnDeleteBadanie.Visibility = Visibility.Hidden;
                        if (btnSaveBadanie != null)
                            btnSaveBadanie.Label = "Zapisz";

                        var vm = DataContext as BadaniaViewModel;
                        var sel = vm?.SelectedWizyta;
                        if (sel == null) return;

                        int? existingBadId = null;
                        try { existingBadId = sel.B_Badanie_ID is int v ? v : (int?)null; } catch { }

                        if (existingBadId.HasValue && existingBadId.Value > 0)
                        {
                            if (btnDeleteBadanie != null)
                                btnDeleteBadanie.Visibility = Visibility.Visible;
                            if (btnSaveBadanie != null)
                                btnSaveBadanie.Label = "Modyfikuj";

                            try
                            {
                                var db = new AccessDbContext();
                                var bad = db.GetBadanieById(existingBadId.Value);
                                if (bad != null)
                                {
                                    vm.DataBadania = bad.Bad_Data;
                                    vm.DataWaznosci = bad.Bad_Data_Do;
                                    vm.SelectedWynik = bad.Bad_Wynik ?? vm.WynikOptions.FirstOrDefault();
                                    vm.NrKsiegi = bad.Bad_Nr_KS;

                                    if (txtBasicPrice != null) txtBasicPrice.Text = bad.Bad_Cena1.HasValue ? string.Format(PlCulture, "{0:N2}", bad.Bad_Cena1.Value) : string.Empty;
                                    if (txtLaryngologistPrice != null) txtLaryngologistPrice.Text = bad.Bad_Cena2.HasValue ? string.Format(PlCulture, "{0:N2}", bad.Bad_Cena2.Value) : string.Empty;
                                    if (txtOphthalmologistPrice != null) txtOphthalmologistPrice.Text = bad.Bad_Cena3.HasValue ? string.Format(PlCulture, "{0:N2}", bad.Bad_Cena3.Value) : string.Empty;
                                    if (txtSanitaryPrice != null) txtSanitaryPrice.Text = bad.Bad_Cena4.HasValue ? string.Format(PlCulture, "{0:N2}", bad.Bad_Cena4.Value) : string.Empty;
                                    if (txtLipidogramPrice != null) txtLipidogramPrice.Text = bad.Bad_Cena5.HasValue ? string.Format(PlCulture, "{0:N2}", bad.Bad_Cena5.Value) : string.Empty;
                                    if (txtEKGPrice != null) txtEKGPrice.Text = bad.Bad_Cena6.HasValue ? string.Format(PlCulture, "{0:N2}", bad.Bad_Cena6.Value) : string.Empty;
                                    if (txtHealthClinicPrice != null) txtHealthClinicPrice.Text = bad.Bad_Cena7.HasValue ? string.Format(PlCulture, "{0:N2}", bad.Bad_Cena7.Value) : string.Empty;
                                    if (txtOtherPrice != null) txtOtherPrice.Text = bad.Bad_Cena8.HasValue ? string.Format(PlCulture, "{0:N2}", bad.Bad_Cena8.Value) : string.Empty;

                                    Action<Button, TextBox, decimal?> setBtnState = (b, tb, price) =>
                                    {
                                        if (b == null || tb == null) return;
                                        if (price.HasValue && price.Value > 0m)
                                        {
                                            b.Content = "✓ AKTYWNE";
                                            b.Background = Brushes.LightGreen;
                                            tb.IsEnabled = true;
                                        }
                                        else
                                        {
                                            b.Content = "✗ NIEAKTYWNE";
                                            b.Background = (SolidColorBrush)(new BrushConverter().ConvertFromString("#FFD0D3D6"));
                                            tb.IsEnabled = false;
                                        }
                                    };

                                    setBtnState(btnBasic, txtBasicPrice, bad.Bad_Cena1);
                                    setBtnState(btnLaryngologist, txtLaryngologistPrice, bad.Bad_Cena2);
                                    setBtnState(btnOphthalmologist, txtOphthalmologistPrice, bad.Bad_Cena3);
                                    setBtnState(btnSanitary, txtSanitaryPrice, bad.Bad_Cena4);
                                    setBtnState(btnLipidogram, txtLipidogramPrice, bad.Bad_Cena5);
                                    setBtnState(btnEKG, txtEKGPrice, bad.Bad_Cena6);
                                    setBtnState(btnHealthClinic, txtHealthClinicPrice, bad.Bad_Cena7);
                                    setBtnState(btnOther, txtOtherPrice, bad.Bad_Cena8 ?? bad.Bad_Cena9 ?? bad.Bad_Cena10);

                                    if (bad.Bad_Razem.HasValue) lblTotalPrice.Text = string.Format(PlCulture, "{0:N2} zł", bad.Bad_Razem.Value);
                                    else RecalculateTotal();
                                }
                            }
                            catch { }
                        }
                        else
                        {
                            try
                            {
                                vm.DataBadania = sel.Bad_Data;
                                vm.DataWaznosci = sel.Bad_Data_Do;
                                vm.SelectedWynik = sel.Bad_Wynik ?? vm.WynikOptions?.FirstOrDefault();

                                if (!string.IsNullOrEmpty(sel.Firma_Cennik)) vm.SelectedCennik = sel.Firma_Cennik;

                                var repo = new WizytyRepository();
                                var prices = repo.GetCennikPrices(vm.SelectedCennik ?? string.Empty);

                                Func<string[], decimal?> getPrice = keys =>
                                {
                                    foreach (var k in keys)
                                    {
                                        if (prices.TryGetValue(k, out var v)) return v;
                                    }
                                    return null;
                                };

                                var basic = getPrice(new[] { "lekarz", "lekasz", "basic" });
                                var laryng = getPrice(new[] { "laryngolog" });
                                var okulista = getPrice(new[] { "okulista", "okulist" });
                                var ksiazeczka = getPrice(new[] { "ksi", "książeczka", "ksiazeczka" });
                                var lipid = getPrice(new[] { "lipidogram" });
                                var ekg = getPrice(new[] { "ekg" });
                                var urlop = getPrice(new[] { "urlop", "urlop (zdrowie)", "healthclinic" });
                                var other = getPrice(new[] { "inne", "other" });

                                if (lblBasicPriceList != null) lblBasicPriceList.Text = basic.HasValue ? string.Format(PlCulture, "{0:N2} zł", basic.Value) : string.Empty;
                                if (lblLaryngologistPriceList != null) lblLaryngologistPriceList.Text = laryng.HasValue ? string.Format(PlCulture, "{0:N2} zł", laryng.Value) : string.Empty;
                                if (lblOphthalmologistPriceList != null) lblOphthalmologistPriceList.Text = okulista.HasValue ? string.Format(PlCulture, "{0:N2} zł", okulista.Value) : string.Empty;
                                if (lblSanitaryPriceList != null) lblSanitaryPriceList.Text = ksiazeczka.HasValue ? string.Format(PlCulture, "{0:N2} zł", ksiazeczka.Value) : string.Empty;
                                if (lblLipidogramPriceList != null) lblLipidogramPriceList.Text = lipid.HasValue ? string.Format(PlCulture, "{0:N2} zł", lipid.Value) : string.Empty;
                                if (lblEKGPriceList != null) lblEKGPriceList.Text = ekg.HasValue ? string.Format(PlCulture, "{0:N2} zł", ekg.Value) : string.Empty;
                                if (lblHealthClinicPriceList != null) lblHealthClinicPriceList.Text = urlop.HasValue ? string.Format(PlCulture, "{0:N2} zł", urlop.Value) : string.Empty;
                                if (lblOtherPriceList != null) lblOtherPriceList.Text = other.HasValue ? string.Format(PlCulture, "{0:N2} zł", other.Value) : string.Empty;

                                Action<TextBox, Button, decimal?> applyPrice = (tb, btn, price) =>
                                {
                                    bool active = !price.HasValue || price.Value > 0m;
                                    if (tb != null)
                                    {
                                        tb.Text = price.HasValue ? string.Format(PlCulture, "{0:N2}", price.Value) : string.Empty;
                                        tb.IsEnabled = active;
                                    }
                                    if (btn != null)
                                    {
                                        if (active)
                                        {
                                            btn.Content = "✓ AKTYWNE";
                                            btn.Background = Brushes.LightGreen;
                                        }
                                        else
                                        {
                                            btn.Content = "✗ NIEAKTYWNE";
                                            btn.Background = (SolidColorBrush)(new BrushConverter().ConvertFromString("#FFD0D3D6"));
                                        }
                                    }
                                };

                                applyPrice(txtBasicPrice, btnBasic, basic);
                                applyPrice(txtLaryngologistPrice, btnLaryngologist, laryng);
                                applyPrice(txtOphthalmologistPrice, btnOphthalmologist, okulista);
                                applyPrice(txtSanitaryPrice, btnSanitary, ksiazeczka);
                                applyPrice(txtLipidogramPrice, btnLipidogram, lipid);
                                applyPrice(txtEKGPrice, btnEKG, ekg);
                                applyPrice(txtHealthClinicPrice, btnHealthClinic, urlop);
                                applyPrice(txtOtherPrice, btnOther, other);

                                RecalculateTotal();
                            }
                            catch { }
                        }
                    }
                    catch { }
                })
                    );
            }
        }

        private void TextBox_TextChanged(object? sender, TextChangedEventArgs e)
        {

        }

        // Handler obsługujący zapis badania (nowy lub update)
        private void SaveBadanie_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                var vm = DataContext as BadaniaViewModel;
                if (vm == null)
                {
                    MessageBox.Show("Brak kontekstu widoku (DataContext).", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var selected = vm.SelectedWizyta;
                if (selected == null)
                {
                    MessageBox.Show("Proszę wybrać skierowanie, do którego zapisujemy badanie.", "Brak wybranego skierowania", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int? bId = null;
                try { bId = selected.B_ID as int? ?? (selected.B_ID is int bid ? bid : (int?)null); } catch { }

                if (!bId.HasValue)
                {
                    MessageBox.Show("Nie można odczytać identyfikatora skierowania (B_ID).", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                int? existingBadId = null;
                try { existingBadId = selected.B_Badanie_ID as int? ?? (selected.B_Badanie_ID is int x ? x : (int?)null); } catch { }

                var rec = new AccessDbContext.BadanieRecord
                {
                    Bad_S_ID = bId,
                    Bad_bn_cennik = vm.SelectedCennik,
                    Bad_Typ = selected.B_TypBadania,
                    Bad_Data = vm.DataBadania,
                    Bad_Data_Do = vm.DataWaznosci,
                    Bad_Wynik = vm.SelectedWynik?.ToString(),
                    Bad_Cena1 = ParsePriceSafe(txtBasicPrice?.Text),
                    Bad_Cena2 = ParsePriceSafe(txtLaryngologistPrice?.Text),
                    Bad_Cena3 = ParsePriceSafe(txtOphthalmologistPrice?.Text),
                    Bad_Cena4 = ParsePriceSafe(txtSanitaryPrice?.Text),
                    Bad_Cena5 = ParsePriceSafe(txtLipidogramPrice?.Text),
                    Bad_Cena6 = ParsePriceSafe(txtEKGPrice?.Text),
                    Bad_Cena7 = ParsePriceSafe(txtHealthClinicPrice?.Text),
                    Bad_Cena8 = ParsePriceSafe(txtOtherPrice?.Text),
                    Bad_Razem = ParsePriceSafe(lblTotalPrice?.Text),
                    Bad_Nr_KS = vm.NrKsiegi,
                    Bad_END = false
                };

                var db = new AccessDbContext();
                if (existingBadId.HasValue && existingBadId.Value > 0)
                {
                    var ok = db.UpdateBadanie(existingBadId.Value, rec);
                    if (ok)
                    {
                        NotificationHelper.ShowInfo("Badanie zaktualizowane", $"ID = {existingBadId.Value}");
                        try { btnSaveBadanie.Label = " ✏️ Modyfikuj Badanie "; } catch { }
                        btnDeleteBadanie.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        MessageBox.Show("Aktualizacja badania nie powiodła się.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    int newBadId = db.AddBadanie(rec);
                    if (newBadId > 0)
                    {
                        bool linkOk = db.UpdateSkierowanieBadanieId(bId.Value, newBadId);
                        if (linkOk)
                        {
                            NotificationHelper.ShowInfo("Badanie zapisane i przypisane do skierowania", $"Bad_ID = {newBadId}");
                            btnDeleteBadanie.Visibility = Visibility.Visible;
                            try { btnSaveBadanie.Label = " ✏️ Modyfikuj Badanie "; } catch { }
                            try
                            {
                                var refresh = vm.GetType().GetMethod("RefreshFromDb");
                                refresh?.Invoke(vm, null);
                            }
                            catch { }
                        }
                        else
                        {
                            MessageBox.Show("Badanie zapisane, ale nie udało się przypisać go do skierowania.", "Uwaga", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Zapis badania nie powiódł się.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas zapisu badania: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }
}





