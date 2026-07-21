using ASMED.WPF.Models;
using System;
using System.Drawing.Printing;
using System.Linq;
using System.Windows;

namespace ASMED.WPF.Views.Dialogs
{
    public partial class CustomPrintDialog : Window
    {
        public PrintSettings? Settings { get; private set; }

        public CustomPrintDialog()
        {
            InitializeComponent();
            LoadPrinters();
            LoadSavedSettings();
        }

        /// <summary>
        /// Ładuje listę dostępnych drukarek
        /// </summary>
        private void LoadPrinters()
        {
            try
            {
                var printers = PrinterSettings.InstalledPrinters;

                foreach (string printer in printers)
                {
                    cmbPrinters.Items.Add(printer);
                }

                // Wybierz domyślną drukarkę
                var defaultPrinter = new PrinterSettings().PrinterName;
                cmbPrinters.SelectedItem = defaultPrinter;

                if (cmbPrinters.SelectedItem == null && cmbPrinters.Items.Count > 0)
                {
                    cmbPrinters.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"LoadPrinters error: {ex.Message}");
                MessageBox.Show($"Błąd ładowania listy drukarek:\n{ex.Message}",
                    "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Ładuje zapisane ustawienia użytkownika
        /// </summary>
        private void LoadSavedSettings()
        {
            try
            {
                var settings = Properties.Settings.Default;

                // Druk dwustronny
                if (settings.PrintDuplexEnabled)
                {
                    chkDuplex.IsChecked = true;

                    if (settings.PrintDuplexMode == "Horizontal")
                        rbDuplexHorizontal.IsChecked = true;
                    else
                        rbDuplexVertical.IsChecked = true;
                }
                else
                {
                    chkDuplex.IsChecked = false;
                }

                // Orientacja
                if (settings.PrintLandscape)
                    rbLandscape.IsChecked = true;
                else
                    rbPortrait.IsChecked = true;

                // Liczba kopii
                if (settings.PrintCopies > 0)
                    txtCopies.Value = settings.PrintCopies;

                // Sortuj kopie
                chkCollate.IsChecked = settings.PrintCollate;

                // Jakość
                cmbQuality.SelectedIndex = settings.PrintQuality;
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"LoadSavedSettings error: {ex.Message}");
            }
        }

        /// <summary>
        /// Zapisuje ustawienia użytkownika
        /// </summary>
        private void SaveSettings()
        {
            if (chkRememberSettings.IsChecked != true)
                return;

            try
            {
                var settings = Properties.Settings.Default;

                settings.PrintDuplexEnabled = chkDuplex.IsChecked == true;
                settings.PrintDuplexMode = rbDuplexHorizontal.IsChecked == true ? "Horizontal" : "Vertical";
                settings.PrintLandscape = rbLandscape.IsChecked == true;
                settings.PrintCopies = (int)(txtCopies.Value ?? 1);
                settings.PrintCollate = chkCollate.IsChecked == true;
                settings.PrintQuality = cmbQuality.SelectedIndex;

                settings.Save();
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"SaveSettings error: {ex.Message}");
            }
        }

        private void CmbPrinters_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (cmbPrinters.SelectedItem == null)
                return;

            try
            {
                var printerName = cmbPrinters.SelectedItem.ToString();
                var printerSettings = new PrinterSettings { PrinterName = printerName };

                // Sprawdź status drukarki
                if (printerSettings.IsValid)
                {
                    txtPrinterStatus.Text = "Status: Gotowa";
                    txtPrinterStatus.Foreground = System.Windows.Media.Brushes.Green;

                    // Sprawdź czy drukarka obsługuje duplex
                    if (!printerSettings.CanDuplex)
                    {
                        chkDuplex.IsEnabled = false;
                        pnlDuplexOptions.IsEnabled = false;
                        txtPrinterStatus.Text = "Status: Gotowa (brak obsługi druku dwustronnego)";
                        txtPrinterStatus.Foreground = System.Windows.Media.Brushes.Orange;
                    }
                    else
                    {
                        chkDuplex.IsEnabled = true;
                        pnlDuplexOptions.IsEnabled = chkDuplex.IsChecked == true;
                    }
                }
                else
                {
                    txtPrinterStatus.Text = "Status: Niedostępna";
                    txtPrinterStatus.Foreground = System.Windows.Media.Brushes.Red;
                }
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"CmbPrinters_SelectionChanged error: {ex.Message}");
                txtPrinterStatus.Text = $"Status: Błąd ({ex.Message})";
                txtPrinterStatus.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        private void ChkDuplex_Checked(object sender, RoutedEventArgs e)
        {
            if (pnlDuplexOptions != null)
                pnlDuplexOptions.IsEnabled = true;
        }

        private void ChkDuplex_Unchecked(object sender, RoutedEventArgs e)
        {
            if (pnlDuplexOptions != null)
                pnlDuplexOptions.IsEnabled = false;
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cmbPrinters.SelectedItem == null)
                {
                    MessageBox.Show("Wybierz drukarkę.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Utwórz ustawienia druku
                Settings = new PrintSettings
                {
                    PrinterName = cmbPrinters.SelectedItem.ToString(),
                    Duplex = GetDuplexMode(),
                    Landscape = rbLandscape.IsChecked == true,
                    Copies = (short)(txtCopies.Value ?? 1),
                    Collate = chkCollate.IsChecked == true,
                    Quality = GetPrintQuality()
                };

                // Zapisz ustawienia jeśli zaznaczono
                SaveSettings();

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"Print_Click error: {ex.Message}");
                MessageBox.Show($"Błąd podczas przygotowywania druku:\n{ex.Message}",
                    "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private Duplex GetDuplexMode()
        {
            if (chkDuplex.IsChecked != true)
                return Duplex.Simplex;

            return rbDuplexHorizontal.IsChecked == true
                ? Duplex.Horizontal
                : Duplex.Vertical;
        }

        private PrintQuality GetPrintQuality()
        {
            return cmbQuality.SelectedIndex switch
            {
                0 => PrintQuality.Normal,
                1 => PrintQuality.High,
                2 => PrintQuality.Draft,
                _ => PrintQuality.Normal
            };
        }

        /// <summary>
        /// Ustawia informacje o dokumencie
        /// </summary>
        public void SetDocumentInfo(string name, int pageCount)
        {
            txtDocumentName.Text = name;
            txtPageCount.Text = pageCount == 1 ? "1 strona" : $"{pageCount} stron";
        }

        private void rbDuplexVertical_Checked(object sender, RoutedEventArgs e)
        {

        }
    }
}
