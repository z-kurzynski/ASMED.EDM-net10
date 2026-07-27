using System.Windows;
using System.Windows.Controls;

namespace ASMED.EDM.UI.Views.badania;

public partial class BadaniaView : UserControl
{
    public BadaniaView()
    {
        InitializeComponent();
    }

    private void ClearFilter_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Implement filter clearing
        if (DataContext is { } viewModel)
        {
            // Clear FilterText property on ViewModel
            // viewModel.FilterText = string.Empty;
        }
    }

    private void Lista_Badan_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Implement Lista Badań navigation
        // Open Lista_Badan view or trigger command
    }
}
