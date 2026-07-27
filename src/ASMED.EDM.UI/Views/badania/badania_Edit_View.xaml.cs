using System.Windows;
using System.Windows.Controls;

namespace ASMED.EDM.UI.Views.badania;

public partial class BadaniaEditView : UserControl
{
    public BadaniaEditView()
    {
        InitializeComponent();
    }

    private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        // TODO: Implement NrKsiegi validation logic
    }

    private void PriceTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        // TODO: Implement price calculation and total update
        UpdateTotalPrice();
    }

    private void ToggleExamination_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;

        // TODO: Implement examination toggle logic based on button.Tag
        var examinationType = button.Tag?.ToString();

        // Toggle button state (NIEAKTYWNE/AKTYWNE)
        // Update button background color
    }

    private void DeleteBadanie_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Implement examination deletion logic
        // Show confirmation dialog
        // Delete from database
        // Refresh UI
    }

    private void SaveBadanie_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Implement examination save logic
        // Validate all fields
        // Save to database
        // Show confirmation
    }

    private void UpdateTotalPrice()
    {
        // TODO: Calculate and display total price
        // Sum all active examination prices
        // Update lblTotalPrice.Text
    }
}
