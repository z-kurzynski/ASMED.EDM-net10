using System.Windows;

namespace ASMED.EDM.UI.Views.lista_do_faktur;

public partial class FirmaSelectDialog : Window
{
    public FirmaSelectDialog()
    {
        InitializeComponent();
    }

    private void ClearSearch_Click(object sender, RoutedEventArgs e)
    {
        // Clear search placeholder
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void RowChoose_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
