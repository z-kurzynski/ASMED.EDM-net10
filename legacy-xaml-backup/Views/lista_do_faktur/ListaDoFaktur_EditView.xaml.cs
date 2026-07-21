using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Globalization;
using ASMED.WPF.ViewModels;
using ASMED.WPF.Helpers;

namespace ASMED.WPF.Views
{
    public partial class badania_Edit_View : UserControl
    {
        public event EventHandler? Saved;
        // minimal state for the edit view
        private bool _suppressVmPropertyChange = false;

        public badania_Edit_View()
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"Failed to load ListaDoFaktur_EditView XAML: {ex}");
                MessageBox.Show($"Błąd ładowania widoku ListaDoFaktur_EditView:\n{ex.Message}", "Błąd XAML", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                this.Loaded += ListaDoFaktur_EditView_Loaded;
                this.Unloaded += ListaDoFaktur_EditView_Unloaded;
            }
        }

        // Simple event handlers: keep behavior minimal so VM handles business logic
        private void PriceTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RecalculateTotal();
        }

        private void ToggleExamination_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
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

            // toggle enabled state only; do not copy or modify VM/DB values here
            if (target != null)
            {
                var isEnabled = target.IsEnabled;
                target.IsEnabled = !isEnabled;
                btn.Content = target.IsEnabled ? "✓ AKTYWNE" : "✗ NIEAKTYWNE";
                btn.Background = target.IsEnabled ? System.Windows.Media.Brushes.LightGreen : (System.Windows.Media.SolidColorBrush)(new System.Windows.Media.BrushConverter().ConvertFromString("#FFD0D3D6"));
            }
            RecalculateTotal();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e) { }

        // When Save button clicked in edit view, raise Saved event. VM or parent will perform persistence.
        private void SaveBadanie_Click(object sender, RoutedEventArgs e)
        {
            try { Saved?.Invoke(this, EventArgs.Empty); } catch { }
        }

        // XAML still references SaveBadanie_Click_SaveToDb; keep stub that delegates to minimal Save handler
        private void SaveBadanie_Click_SaveToDb(object sender, RoutedEventArgs e)
        {
            SaveBadanie_Click(sender, e);
        }

        private void DeleteBadanie_Click(object sender, RoutedEventArgs e) { }

        private static readonly CultureInfo PlCulture = new CultureInfo("pl-PL");

        private void ListaDoFaktur_EditView_Loaded(object? sender, RoutedEventArgs e)
        {
            var vm = this.DataContext as ListaDoFakturViewModel;
            if (vm == null) return;
            vm.PropertyChanged -= Vm_PropertyChanged;
            vm.PropertyChanged += Vm_PropertyChanged;

            // initial population from VM-selected DTO (only copy DTO values into UI fields)
            PopulateFromSelected(vm.SelectedAssignedBadanie);
        }

        private void ListaDoFaktur_EditView_Unloaded(object? sender, RoutedEventArgs e)
        {
            var vm = this.DataContext as ListaDoFakturViewModel;
            if (vm == null) return;
            vm.PropertyChanged -= Vm_PropertyChanged;
        }

        private void Vm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // avoid handling property change if we are programmatically updating VM to prevent recursion
            if (_suppressVmPropertyChange) return;

            if (string.Equals(e.PropertyName, "SelectedAssignedBadanie", StringComparison.OrdinalIgnoreCase))
            {
                var vm = this.DataContext as ListaDoFakturViewModel;
                Dispatcher.BeginInvoke(new Action(() => PopulateFromSelected(vm?.SelectedAssignedBadanie)));
            }
        }

        private void PopulateFromSelected(AccessDbContext.AssignedBadanieDto? dto)
        {
            try
            {
                if (dto == null)
                {
                    // clear fields
                    if (txtBasicPrice != null) txtBasicPrice.Text = string.Empty;
                    if (txtLaryngologistPrice != null) txtLaryngologistPrice.Text = string.Empty;
                    if (txtOphthalmologistPrice != null) txtOphthalmologistPrice.Text = string.Empty;
                    if (txtSanitaryPrice != null) txtSanitaryPrice.Text = string.Empty;
                    if (txtLipidogramPrice != null) txtLipidogramPrice.Text = string.Empty;
                    if (txtEKGPrice != null) txtEKGPrice.Text = string.Empty;
                    if (txtHealthClinicPrice != null) txtHealthClinicPrice.Text = string.Empty;
                    if (txtOtherPrice != null) txtOtherPrice.Text = string.Empty;
                    if (lblTotalPrice != null) lblTotalPrice.Text = string.Empty;
                    return;
                }

                // If we have a Bad_ID try to load full record and use its authoritative values
                if (dto.Bad_ID.HasValue)
                {
                    try
                    {
                        var db = new AccessDbContext();
                        var full = db.GetBadanieById(dto.Bad_ID.Value);
                        if (full != null)
                        {
                            // update VM-level fields (if DataContext is VM)
                            var vm = this.DataContext as ListaDoFakturViewModel;
                            try { if (vm != null) { vm.DataBadania = full.Bad_Data; vm.DataWaznosci = full.Bad_Data_Do; vm.SelectedWynik = full.Bad_Wynik; vm.NrKsiegi = full.Bad_Nr_KS; if (!string.IsNullOrEmpty(full.Bad_bn_cennik)) vm.SelectedCennik = full.Bad_bn_cennik; } } catch { }

                            // map prices from full record into UI textboxes
                            if (txtBasicPrice != null) txtBasicPrice.Text = FormatDecimal(full.Bad_Cena1 ?? dto.Bad_Cena1);
                            if (txtLaryngologistPrice != null) txtLaryngologistPrice.Text = FormatDecimal(full.Bad_Cena2 ?? dto.Bad_Cena2);
                            if (txtOphthalmologistPrice != null) txtOphthalmologistPrice.Text = FormatDecimal(full.Bad_Cena3 ?? dto.Bad_Cena3);
                            if (txtSanitaryPrice != null) txtSanitaryPrice.Text = FormatDecimal(full.Bad_Cena4 ?? dto.Bad_Cena4);
                            if (txtLipidogramPrice != null) txtLipidogramPrice.Text = FormatDecimal(full.Bad_Cena5 ?? dto.Bad_Cena5);
                            if (txtEKGPrice != null) txtEKGPrice.Text = FormatDecimal(full.Bad_Cena6 ?? dto.Bad_Cena6);
                            if (txtHealthClinicPrice != null) txtHealthClinicPrice.Text = FormatDecimal(full.Bad_Cena7 ?? dto.Bad_Cena7);
                            if (txtOtherPrice != null) txtOtherPrice.Text = FormatDecimal(full.Bad_Cena8 ?? dto.Bad_Cena8);
                            if (lblTotalPrice != null) lblTotalPrice.Text = (full.Bad_Razem.HasValue ? string.Format(PlCulture, "{0:N2} zł", full.Bad_Razem.Value) : (dto.Bad_Razem.HasValue ? string.Format(PlCulture, "{0:N2} zł", dto.Bad_Razem.Value) : string.Empty));

                            // resolve Firma name from Bad_F_ID when possible and update visible field if exists
                            try
                            {
                                if (full.Bad_F_ID.HasValue)
                                {
                                    using var conn = new AccessDbHelper().GetConnection();
                                    conn.Open();
                                    using var cmd = conn.CreateCommand();
                                    cmd.CommandText = "SELECT Nazwa FROM Firma WHERE id = ?";
                                    var p = cmd.CreateParameter(); p.ParameterName = "@id"; p.Value = full.Bad_F_ID.Value; cmd.Parameters.Add(p);
                                    var res = cmd.ExecuteScalar();
                                    if (res != null && !string.IsNullOrEmpty(res.ToString()))
                                    {
                                        try
                                        {
                                            var vmAssigned = vm?.SelectedAssignedBadanie;
                                            if (vm != null && vmAssigned != null)
                                            {
                                                var name = res.ToString();
                                                // only update if name actually changed to avoid unnecessary PropertyChanged
                                                if (!string.Equals(vmAssigned.FirmaNazwa, name, StringComparison.Ordinal))
                                                {
                                                    try
                                                    {
                                                      _suppressVmPropertyChange = true;
                                                      vmAssigned.FirmaNazwa = name;
                                                      vm.SelectedAssignedBadanie = vmAssigned;
                                                    }
                                                    finally
                                                    {
                                                      _suppressVmPropertyChange = false;
                                                    }
                                                  }
                                            }
                                        }
                                        catch { }
                                    }
                                }
                            }
                            catch { }

                            return;
                        }
                    }
                    catch { }
                }

                // fallback to DTO values when full record not available
                if (txtBasicPrice != null) txtBasicPrice.Text = FormatDecimal(dto.Bad_Cena1);
                if (txtLaryngologistPrice != null) txtLaryngologistPrice.Text = FormatDecimal(dto.Bad_Cena2);
                if (txtOphthalmologistPrice != null) txtOphthalmologistPrice.Text = FormatDecimal(dto.Bad_Cena3);
                if (txtSanitaryPrice != null) txtSanitaryPrice.Text = FormatDecimal(dto.Bad_Cena4);
                if (txtLipidogramPrice != null) txtLipidogramPrice.Text = FormatDecimal(dto.Bad_Cena5);
                if (txtEKGPrice != null) txtEKGPrice.Text = FormatDecimal(dto.Bad_Cena6);
                if (txtHealthClinicPrice != null) txtHealthClinicPrice.Text = FormatDecimal(dto.Bad_Cena7);
                if (txtOtherPrice != null) txtOtherPrice.Text = FormatDecimal(dto.Bad_Cena8);
                if (lblTotalPrice != null) lblTotalPrice.Text = (dto.Bad_Razem.HasValue ? string.Format(PlCulture, "{0:N2} zł", dto.Bad_Razem.Value) : string.Empty);
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"PopulateFromSelected error: {ex}");
            }
        }

        private string FormatDecimal(decimal? d) => d.HasValue ? d.Value.ToString("N2", PlCulture) : string.Empty;
        private string FormatDecimalLabel(decimal? d) => d.HasValue ? d.Value.ToString("N2", PlCulture) + " zł" : string.Empty;

        // Parse price input safely
        private decimal? ParseDecimalSafe(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            var input = s.Trim().Replace("zł", "", StringComparison.OrdinalIgnoreCase).Trim();
            if (decimal.TryParse(input, System.Globalization.NumberStyles.Number, PlCulture, out var d)) return d;
            if (decimal.TryParse(input, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out d)) return d;
            return null;
        }

        // Recalculate total similar to BadaniaView
        private decimal ParsePriceSafeForUi(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
            return 0m;

            var input = s.Trim();
            input = input.Replace("zł", "").Replace("zl", "").Trim();

            if (decimal.TryParse(input, System.Globalization.NumberStyles.Number, PlCulture, out var p))
            return p;

            if (decimal.TryParse(input, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out p))
            return p;

            return 0m;
        }

        private void RecalculateTotal()
        {
            decimal total = 0m;
            if (txtBasicPrice != null) total += ParsePriceSafeForUi(txtBasicPrice.Text);
            if (txtLaryngologistPrice != null) total += ParsePriceSafeForUi(txtLaryngologistPrice.Text);
            if (txtOphthalmologistPrice != null) total += ParsePriceSafeForUi(txtOphthalmologistPrice.Text);
            if (txtSanitaryPrice != null) total += ParsePriceSafeForUi(txtSanitaryPrice.Text);
            if (txtLipidogramPrice != null) total += ParsePriceSafeForUi(txtLipidogramPrice.Text);
            if (txtEKGPrice != null) total += ParsePriceSafeForUi(txtEKGPrice.Text);
            if (txtHealthClinicPrice != null) total += ParsePriceSafeForUi(txtHealthClinicPrice.Text);
            if (txtOtherPrice != null) total += ParsePriceSafeForUi(txtOtherPrice.Text);

            if (lblTotalPrice != null)
            lblTotalPrice.Text = string.Format(PlCulture, "{0:N2} zł", total);
        }
    }
}
