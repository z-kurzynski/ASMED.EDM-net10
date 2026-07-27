using System.Windows;
using System.Windows.Controls;

namespace ASMED.EDM.UI.Views.badania;

public partial class BadaniaListaView : UserControl
{
    public BadaniaListaView()
    {
        InitializeComponent();
    }

    private void ClearFilter_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Implement filter clearing
    }

    private void Nowe_Badanie_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Open new examination view
    }

    private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        // TODO: Validate NrKsiegi field
    }

    private void PriceTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        // TODO: Recalculate total price
    }

    private void ToggleExamination_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;

        // TODO: Toggle examination active/inactive state
        var examinationType = button.Tag?.ToString();

        // Update button text and colors
        // Update total price
    }
}
