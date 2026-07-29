using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ASMED.EDM.UI.ViewModels.Badania;
using ASMED.EDM.Data.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ASMED.EDM.UI.Views.badania;

public partial class BadaniaNewView : UserControl
{
    public BadaniaNewView()
    {
        Console.WriteLine("🚀 BadaniaNewView: Constructor started");
        InitializeComponent();
        Console.WriteLine("🔧 BadaniaNewView: InitializeComponent completed");

        if (!DesignerProperties.GetIsInDesignMode(this))
        {
            if (Application.Current is App app && app.Host != null)
            {
                var dbFactory = app.Host.Services.GetService<DbConnectionFactory>();
                var vm = new BadaniaNewViewModel(dbFactory);

                // Subskrypcja na event resetowania przycisków toggle
                vm.ToggleButtonsResetRequested += ResetToggleButtons;

                DataContext = vm;
                Console.WriteLine("✅ BadaniaNewView: DataContext set with DbConnectionFactory");
            }
            else
            {
                // Fallback bez DI (nie powinno się zdarzyć)
                var vm = new BadaniaNewViewModel(null);
                vm.ToggleButtonsResetRequested += ResetToggleButtons;
                DataContext = vm;
                Console.WriteLine("⚠️ BadaniaNewView: DataContext set WITHOUT DbConnectionFactory");
            }
        }
    }

    /// <summary>
    /// Resetuje wszystkie przyciski toggle do stanu NIEAKTYWNE (szary, ✗)
    /// Wywoływane przez event ToggleButtonsResetRequested z ViewModelu
    /// </summary>
    private void ResetToggleButtons()
    {
        Console.WriteLine("🔄 ResetToggleButtons - resetting all toggle buttons to NIEAKTYWNE");

        var buttons = new[]
        {
            btnNewToggleBasic,
            btnNewToggleLaryngologist,
            btnNewToggleOphthalmologist,
            btnNewToggleSanitary,
            btnNewToggleLipidogram,
            btnNewToggleEKG,
            btnNewToggleHealthClinic,
            btnNewToggleOther
        };

        foreach (var btn in buttons)
        {
            if (btn != null)
            {
                btn.Content = "✗ NIEAKTYWNE";
                btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD0D3D6"));
            }
        }

        Console.WriteLine("✅ ResetToggleButtons completed");
    }

    private void ClearFilter_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is BadaniaNewViewModel vm)
        {
            vm.FilterText = string.Empty;
        }
    }

    private void Lista_Badan_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Navigate to Lista Badań
    }

    private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        // Event handler for TextBox text changes (e.g., Nr Księgi)
        // Binding handles the update automatically
    }

    private void PriceTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        // Event handler for price TextBox changes
        // Auto-calculate total if needed
        if (DataContext is BadaniaNewViewModel vm)
        {
            vm.RecalculateTotalPrice();
        }
    }

    private void ToggleNewExamination_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        if (DataContext is not BadaniaNewViewModel vm) return;

        // Sprawdź aktualny stan przycisku (po treści)
        var isActive = btn.Content?.ToString()?.StartsWith("✓") ?? false;

        // Pobierz typ badania z Tag
        var examinationType = btn.Tag?.ToString() ?? "";

        if (isActive)
        {
            // Przełącz na NIEAKTYWNE
            btn.Content = "✗ NIEAKTYWNE";
            btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD0D3D6"));

            // Wyzeruj cenę (null)
            vm.SetCenaForExaminationType(examinationType, null);
            Console.WriteLine($"🔴 Toggle OFF: {examinationType}");
        }
        else
        {
            // Przełącz na AKTYWNE
            btn.Content = "✓ AKTYWNE";
            btn.Background = Brushes.LightGreen;

            // Pobierz cenę z cennika i ustaw
            var price = vm.GetPriceForExamination(examinationType);
            vm.SetCenaForExaminationType(examinationType, price);
            Console.WriteLine($"🟢 Toggle ON: {examinationType} = {price:N2} zł");
        }
    }
}
