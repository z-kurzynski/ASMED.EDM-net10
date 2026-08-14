using ASMED.EDM.Printing.Models;
using ASMED.EDM.Printing.Services;
using System.Drawing.Printing;
using System.Windows;

namespace ASMED.EDM.Printing.Views.Dialogs;

/// <summary>
/// Okno wyboru drukarki i ustawień druku (drukarka, duplex, orientacja, kopie, jakość).
/// Port z legacy: ASMED.WPF.Views.Dialogs.CustomPrintDialog.
/// Ustawienia przechowywane przez <see cref="IPrintSettingsStore"/> (zamiast Properties.Settings.Default).
/// </summary>
public partial class CustomPrintDialog : Window
{
    private readonly IPrintSettingsStore _settingsStore;

    public PrintSettings? Settings { get; private set; }

    public CustomPrintDialog() : this(new FilePrintSettingsStore())
    {
    }

    public CustomPrintDialog(IPrintSettingsStore settingsStore)
    {
        _settingsStore = settingsStore;
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

            var defaultPrinter = new PrinterSettings().PrinterName;
            cmbPrinters.SelectedItem = defaultPrinter;

            if (cmbPrinters.SelectedItem == null && cmbPrinters.Items.Count > 0)
            {
                cmbPrinters.SelectedIndex = 0;
            }
        }
        catch (Exception ex)
        {
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
            var settings = _settingsStore.Load();

            if (settings.Duplex != System.Drawing.Printing.Duplex.Simplex)
            {
                chkDuplex.IsChecked = true;

                if (settings.Duplex == System.Drawing.Printing.Duplex.Horizontal)
                    rbDuplexHorizontal.IsChecked = true;
                else
                    rbDuplexVertical.IsChecked = true;
            }
            else
            {
                chkDuplex.IsChecked = false;
            }

            if (settings.Landscape)
                rbLandscape.IsChecked = true;
            else
                rbPortrait.IsChecked = true;

            if (settings.Copies > 0)
                txtCopies.Value = settings.Copies;

            chkCollate.IsChecked = settings.Collate;

            cmbQuality.SelectedIndex = (int)settings.Quality < 0 ? 2 : (int)settings.Quality;
        }
        catch
        {
            // Ignoruj błąd odczytu ustawień - użyj wartości domyślnych z XAML
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
            var settings = new PrintSettings
            {
                PrinterName = cmbPrinters.SelectedItem?.ToString(),
                Duplex = GetDuplexMode(),
                Landscape = rbLandscape.IsChecked == true,
                Copies = (short)(txtCopies.Value ?? 1),
                Collate = chkCollate.IsChecked == true,
                Quality = GetPrintQuality()
            };

            _settingsStore.Save(settings);
        }
        catch
        {
            // Ignoruj błąd zapisu ustawień
        }
    }

    private void CmbPrinters_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (cmbPrinters.SelectedItem == null)
            return;

        try
        {
            var printerName = cmbPrinters.SelectedItem.ToString() ?? string.Empty;
            var printerSettings = new PrinterSettings { PrinterName = printerName };

            if (printerSettings.IsValid)
            {
                txtPrinterStatus.Text = "Status: Gotowa";
                txtPrinterStatus.Foreground = System.Windows.Media.Brushes.Green;

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

            Settings = new PrintSettings
            {
                PrinterName = cmbPrinters.SelectedItem.ToString(),
                Duplex = GetDuplexMode(),
                Landscape = rbLandscape.IsChecked == true,
                Copies = (short)(txtCopies.Value ?? 1),
                Collate = chkCollate.IsChecked == true,
                Quality = GetPrintQuality(),
                DocumentName = txtDocumentName.Text
            };

            SaveSettings();

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Błąd podczas przygotowywania druku:\n{ex.Message}",
                "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private System.Drawing.Printing.Duplex GetDuplexMode()
    {
        if (chkDuplex.IsChecked != true)
            return System.Drawing.Printing.Duplex.Simplex;

        return rbDuplexHorizontal.IsChecked == true
            ? System.Drawing.Printing.Duplex.Horizontal
            : System.Drawing.Printing.Duplex.Vertical;
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
