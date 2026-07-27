using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace ASMED.EDM.UI.Views.lista_do_faktur
{
    public partial class ListaFaktAddView : UserControl
    {
        public IFormatProvider? PlCulture { get; private set; }

        public ListaFaktAddView()
        {
            InitializeComponent();
            PlCulture = CultureInfo.GetCultureInfo("pl-PL");
            this.Loaded += ListaFaktAddView_Loaded;
        }

        private void ListaFaktAddView_Loaded(object? sender, RoutedEventArgs e)
        {
            // Placeholder for ViewModel initialization
        }

        private void OpenFirmaDialog_Click(object sender, RoutedEventArgs e)
        {
            // Placeholder for company selection dialog
            var dlg = new FirmaSelectDialog { Owner = Window.GetWindow(this) };
            var result = dlg.ShowDialog();

            if (result == true)
            {
                // TODO: Handle selected firma
            }
        }

        private void OpenListaDoFaktur_Click(object sender, RoutedEventArgs e)
        {
            // Placeholder for navigation back to list view
        }

        private void PriceTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Placeholder for price calculation
            try { RecalculateTotal(); } catch { }
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

            if (target == null) return;

            // Toggle active/inactive state
            var isActive = btn.Content?.ToString()?.Contains("✓") == true;

            if (isActive)
            {
                // Deactivate
                btn.Content = "✗ NIEAKTYWNA";
                btn.Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0xD0, 0xD3, 0xD6));
                target.IsEnabled = false;
                target.Text = "0";
            }
            else
            {
                // Activate
                btn.Content = "✓ AKTYWNA";
                btn.Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x4C, 0xAF, 0x50));
                target.IsEnabled = true;
                target.Focus();
            }

            RecalculateTotal();
        }

        private void RecalculateTotal()
        {
            try
            {
                decimal total = 0;

                decimal ParsePrice(TextBox? tb)
                {
                    if (tb == null || string.IsNullOrWhiteSpace(tb.Text)) return 0;
                    if (decimal.TryParse(tb.Text, NumberStyles.Any, PlCulture, out var val))
                        return val;
                    return 0;
                }

                total += ParsePrice(txtBasicPrice);
                total += ParsePrice(txtLaryngologistPrice);
                total += ParsePrice(txtOphthalmologistPrice);
                total += ParsePrice(txtSanitaryPrice);
                total += ParsePrice(txtLipidogramPrice);
                total += ParsePrice(txtEKGPrice);
                total += ParsePrice(txtHealthClinicPrice);
                total += ParsePrice(txtOtherPrice);

                if (lblTotalPrice != null)
                {
                    lblTotalPrice.Text = total.ToString("N2", PlCulture) + " zł";
                }
            }
            catch
            {
                // Ignore calculation errors
            }
        }

        private void dgAssignedBadania_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Placeholder for editing assigned examination
        }

        private void ResetSelectionState()
        {
            // Placeholder for resetting UI state
        }
    }
}
