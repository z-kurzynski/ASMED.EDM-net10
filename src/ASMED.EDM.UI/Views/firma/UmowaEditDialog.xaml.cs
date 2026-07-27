using System.Windows;

namespace ASMED.EDM.UI.Views.Firma;

public partial class UmowaEditDialog : Window
{
    public UmowaEditDialog()
    {
        InitializeComponent();
    }

    private void ChkCzyTerminowa_Changed(object sender, RoutedEventArgs e)
    {
        // Event handler placeholder
    }

    private void BtnZapisz_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void BtnAnuluj_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
