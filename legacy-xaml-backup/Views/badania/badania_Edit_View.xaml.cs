using ASMED.WPF.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System;
using ASMED.WPF.ViewModels.Badania;
using System.Linq;
using System.Globalization;
using System.Windows.Media;
using Syncfusion.UI.Xaml.Grid;
using ASMED.WPF.Helpers; // dostęp do AccessDbContext

namespace ASMED.WPF.Views
{
    public partial class BadaniaEditView : UserControl
    {
        private static readonly CultureInfo PlCulture = new CultureInfo("pl-PL");

        // keep reference to currently subscribed INotifyPropertyChanged to unsubscribe when DataContext changes
        private INotifyPropertyChanged? _currentDcInpc;

        public BadaniaEditView()
        {
            try
            {
                InitializeComponent();
            }
            catch (System.Exception ex)
            {
                // Log and show a non-blocking message to help diagnose XAML parsing issues
                // System.Diagnostics.Debug.WriteLine($"Failed to load BadaniaEditView XAML: {ex}");
                MessageBox.Show($"Błąd ładowania widoku BadaniaEditView:\n{ex.Message}", "Błąd XAML", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // respond if DataContext is assigned after construction
            this.DataContextChanged += (s, e) =>
            {
                try
                {
                    // unsubscribe previous
                    if (_currentDcInpc != null)
                    {
                        try { _currentDcInpc.PropertyChanged -= Vm_PropertyChanged; } catch { }
                        _currentDcInpc = null;
                    }

                    // attach to new DataContext if it supports INotifyPropertyChanged
                    if (this.DataContext is INotifyPropertyChanged newInpc)
                    {
                        _currentDcInpc = newInpc;
                        _currentDcInpc.PropertyChanged += Vm_PropertyChanged;
                        // populate UI based on current SelectedWizyta
                        try { Vm_PropertyChanged(newInpc, new PropertyChangedEventArgs("SelectedWizyta")); } catch { }
                    }
                    else
                    {
                        // no INotifyPropertyChanged: still reset UI
                        try { ResetSelectionState(); } catch { }
                    }
                }
                catch { }
            };

            // Only create a runtime VM if DataContext was not provided by the caller
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                if (this.DataContext == null)
                {
                    var vm = new BadaniaEditViewModel();
                    DataContext = vm;

                    // ensure controls start in inactive state when view loads data
                    ResetSelectionState();
                    // subscribe to VM property changes
                    if (vm is INotifyPropertyChanged inpc)
                    {
                        inpc.PropertyChanged += Vm_PropertyChanged;
                        _currentDcInpc = inpc;
                    }
                }
                else
                {
                    // If caller provided a DataContext, subscribe to its PropertyChanged if supported
                    if (this.DataContext is INotifyPropertyChanged inpc2)
                    {
                        inpc2.PropertyChanged += Vm_PropertyChanged;
                        _currentDcInpc = inpc2;
                    }
                    // Reset UI to initial state so caller can populate fields
                    ResetSelectionState();

                    // Populate UI once immediately in case DataContext already has SelectedWizyta
                    try { Vm_PropertyChanged(this.DataContext, new PropertyChangedEventArgs("SelectedWizyta")); } catch { }
                }

                // when view is loaded or becomes visible, optionally refresh VM data if needed
                this.Loaded += (s, e) => { try { /* optionally load data into edit VM */ } catch { } };
                this.IsVisibleChanged += (s, e) => { if (this.IsVisible) { try { /* optionally refresh */ } catch { } } };
            }
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
            if (decimal.TryParse(input, NumberStyles.Number, PlCulture, out var valPl))
                return valPl;
            // fallback: invariant (np. user użył kropki)
            if (decimal.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out var valInv))
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

        private void Vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // if SelectedWizyta changed in VM, reset UI controls same as when selection changed in grid
            if (string.Equals(e.PropertyName, "SelectedWizyta", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(e.PropertyName, "Wizyty", StringComparison.OrdinalIgnoreCase))
            {
                // invoke on UI thread to reset controls when selection or list changes
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    ResetSelectionState();

                    try
                    {
                        // Default: hide Delete button and set Save button label to simple "Zapisz"
                        if (btnDeleteBadanie != null)
                            btnDeleteBadanie.Visibility = Visibility.Hidden;
                        if (btnSaveBadanie != null)
                            btnSaveBadanie.Label = "Zapisz";

                        // If the selected row already has a linked Badanie, show Delete and set Save to Modify
                        var vm = DataContext as BadaniaEditViewModel;
                        var sel = vm?.SelectedWizyta;
                        if (sel != null)
                        {
                            int? existingBadId = null;
                            try { existingBadId = sel.B_Badanie_ID is int v ? v : (int?)null; } catch { }
                            if (existingBadId.HasValue && existingBadId.Value >0)
                            {
                                if (btnDeleteBadanie != null)
                                    btnDeleteBadanie.Visibility = Visibility.Visible;
                                if (btnSaveBadanie != null)
                                    btnSaveBadanie.Label = "Modyfikuj";
                            }

                            // Populate price fields from SelectedWizyta (DTO contains Bad_Cena1..Bad_Cena8)
                            try
                            {
                                void ApplyPrice(TextBox tb, decimal? price, Button btn)
                                {
                                    if (tb == null || btn == null) return;
                                    if (price.HasValue && price.Value !=0m)
                                    {
                                        tb.IsEnabled = true;
                                        tb.Text = price.Value.ToString("N2", PlCulture);
                                        btn.Content = "✓ AKTYWNE";
                                        btn.Background = Brushes.LightGreen;
                                    }
                                    else
                                    {
                                        tb.IsEnabled = false;
                                        tb.Text = string.Empty;
                                        btn.Content = "✗ NIEAKTYWNE";
                                        btn.Background = (SolidColorBrush)(new BrushConverter().ConvertFromString("#FFD0D3D6"));
                                    }
                                }

                                ApplyPrice(txtBasicPrice, sel.Bad_Cena1, btnBasic);
                                ApplyPrice(txtLaryngologistPrice, sel.Bad_Cena2, btnLaryngologist);
                                ApplyPrice(txtOphthalmologistPrice, sel.Bad_Cena3, btnOphthalmologist);
                                ApplyPrice(txtSanitaryPrice, sel.Bad_Cena4, btnSanitary);
                                ApplyPrice(txtLipidogramPrice, sel.Bad_Cena5, btnLipidogram);
                                ApplyPrice(txtEKGPrice, sel.Bad_Cena6, btnEKG);
                                ApplyPrice(txtHealthClinicPrice, sel.Bad_Cena7, btnHealthClinic);
                                ApplyPrice(txtOtherPrice, sel.Bad_Cena8, btnOther);

                                // update total label
                                RecalculateTotal();
                            }
                            catch { }
                        }
                    }
                    catch { }
                }));
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
                var vm = DataContext as BadaniaEditViewModel;
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

                // B_ID ze skierowania (Bad_S_ID ma wskazywać na to B_ID)
                int? bId = null;
                try { bId = selected.B_ID as int? ?? (selected.B_ID is int bid ? bid : (int?)null); } catch { }

                if (!bId.HasValue)
                {
                    MessageBox.Show("Nie można odczytać identyfikatora skierowania (B_ID).", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Jeśli istnieje już powiązane badanie (w skierowaniu), spróbujemy je odczytać
                int? existingBadId = null;
                try { existingBadId = selected.B_Badanie_ID as int? ?? (selected.B_Badanie_ID is int x ? x : (int?)null); } catch { }

                // Determine patient and firm IDs from the selected object (supports different selected record types)
                int? ExtractIntProperty(object obj, params string[] names)
                {
                    if (obj == null) return null;
                    var props = obj.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    string Normalize(string s) => new string((s ?? string.Empty).Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
                    var targetNames = names.Select(n => Normalize(n)).ToArray();
                    foreach (var p in props)
                    {
                        var n = Normalize(p.Name);
                        if (targetNames.Contains(n))
                        {
                            try
                            {
                                var v = p.GetValue(obj);
                                if (v is int iv) return iv;
                                if (v != null && int.TryParse(v.ToString(), out var parsed)) return parsed;
                            }
                            catch { }
                        }
                    }
                    // not found
                    return null;
                }

                var patientId = ExtractIntProperty(selected, "P_ID", "B_Pacjent_ID", "PId", "PacjentId");
                var firmaId = ExtractIntProperty(selected, "Firma_id", "B_Firma_ID", "FirmaId", "id");

                // Zbuduj rekord BadanieRecord z wartości z UI/VM
                var rec = new AccessDbContext.BadanieRecord
                {
                    Bad_S_ID = bId,
                    Bad_P_ID = patientId,
                    Bad_F_ID = firmaId,
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
                if (existingBadId.HasValue && existingBadId.Value >0)
                {
                    // Update only price-related fields and cennik to avoid overwriting other data
                    var priceUpdate = new AccessDbContext.BadanieRecord
                    {
                        Bad_Cena1 = rec.Bad_Cena1,
                        Bad_Cena2 = rec.Bad_Cena2,
                        Bad_Cena3 = rec.Bad_Cena3,
                        Bad_Cena4 = rec.Bad_Cena4,
                        Bad_Cena5 = rec.Bad_Cena5,
                        Bad_Cena6 = rec.Bad_Cena6,
                        Bad_Cena7 = rec.Bad_Cena7,
                        Bad_Cena8 = rec.Bad_Cena8,
                        Bad_Razem = rec.Bad_Razem,
                        Bad_bn_cennik = rec.Bad_bn_cennik
                    };

                    var ok = db.UpdateBadanie(existingBadId.Value, priceUpdate);
                    if (ok)
                    {
                        NotificationHelper.ShowInfo("Badanie zaktualizowane", $"ID = {existingBadId.Value}");
                        // zmień label przycisku na modyfikuj
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
                    // dodanie nowego badania
                    int newBadId = db.AddBadanie(rec);
                    if (newBadId >0)
                    {
                        // zlinkuj badanie do skierowania
                        bool linkOk = db.UpdateSkierowanieBadanieId(bId.Value, newBadId);
                        if (linkOk)
                        {
                            NotificationHelper.ShowInfo("Badanie zapisane i przypisane do skierowania", $"Bad_ID = {newBadId}");
                            // ustaw widoczność/usługi UI
                            btnDeleteBadanie.Visibility = Visibility.Visible;
                            try { btnSaveBadanie.Label = " ✏️ Modyfikuj Badanie "; } catch { }
                            // Jeśli ViewModel udostępnia metodę odświeżenia, wywołaj ją (opcjonalne)
                            try
                            {
                                // jeśli VM ma RefreshFromDb lub podobne
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

        // Prosty handler dla przycisku usuń (tu tylko demonstracyjnie czyścimy powiązanie; nie usuwamy rekordu Badanie)
        private void DeleteBadanie_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                var vm = DataContext as BadaniaEditViewModel;
                if (vm == null || vm.SelectedWizyta == null)
                {
                    MessageBox.Show("Brak wybranego skierowania.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int? bId = null;
                try { bId = vm.SelectedWizyta.B_ID as int? ?? (vm.SelectedWizyta.B_ID is int bid ? bid : (int?)null); } catch { }

                int? existingBadId = null;
                try { existingBadId = vm.SelectedWizyta.B_Badanie_ID as int? ?? (vm.SelectedWizyta.B_Badanie_ID is int x ? x : (int?)null); } catch { }

                if (!bId.HasValue || !existingBadId.HasValue)
                {
                    MessageBox.Show("Brak powiązanego badania do usunięcia.", "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Usuń powiązanie w B_Skierowania (ustaw NULL) — funkcja UpdateSkierowanieBadanieId przyjmuje int, więc możemy ustawić 0 lub dodać metodę do ustawienia NULL.
                // Tutaj ustawimy 0 (może w DB reprezentowane jako NULL zależnie od schematu) — lepiej byłoby mieć dedykowaną metodę do NULL.
                var db = new AccessDbContext();
                bool ok = db.UpdateSkierowanieBadanieId(bId.Value, 0);
                if (ok)
                {
                    NotificationHelper.ShowInfo("Usunięto powiązanie badania ze skierowaniem", $"B_ID = {bId}");
                    btnDeleteBadanie.Visibility = Visibility.Hidden;
                    try { btnSaveBadanie.Label = " ✏️ Zapisz Badanie "; } catch { }
                }
                else
                {
                    MessageBox.Show("Nie udało się usunąć powiązania.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas usuwania powiązania badania: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Lista_Badan_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                // Find main window and its Badania tab, then replace content with BadaniaListaView
                var main = Application.Current.MainWindow as MainWindow;
                if (main == null) return;

                var badaniaTab = main.FindName("Badania") as System.Windows.Controls.TabItem;
                if (badaniaTab == null) return;

                // replace the content with the list view
                var listView = new BadaniaListaView();
                badaniaTab.Content = listView;
            }
            catch { }
        }
    }
}
