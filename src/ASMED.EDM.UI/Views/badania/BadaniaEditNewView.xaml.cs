using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ASMED.EDM.UI.ViewModels.Badania;
using ASMED.EDM.Data.Services;
using Microsoft.Extensions.DependencyInjection;
using Windows.Graphics.Printing.Workflow;

namespace ASMED.EDM.UI.Views.badania;

public partial class BadaniaEditNewView : UserControl
{
    public BadaniaEditNewView()
    {
        Console.WriteLine("🚀 BadaniaEditNewView: Constructor started");
        InitializeComponent();
        Console.WriteLine("🔧 BadaniaEditNewView: InitializeComponent completed");

        if (!DesignerProperties.GetIsInDesignMode(this))
        {
            if (Application.Current is App app && app.Host != null)
            {
                var dbFactory = app.Host.Services.GetService<DbConnectionFactory>();
                var vm = new BadaniaEditNewViewModel(dbFactory);
                vm.PropertyChanged += Vm_PropertyChanged;
                DataContext = vm;
                Console.WriteLine("✅ BadaniaEditNewView: DataContext set with DbConnectionFactory");
            }
            else
            {
                // Fallback bez DI (nie powinno się zdarzyć)
                var vm = new BadaniaEditNewViewModel(null);
                vm.PropertyChanged += Vm_PropertyChanged;
                DataContext = vm;
                Console.WriteLine("⚠️ BadaniaEditNewView: DataContext set WITHOUT DbConnectionFactory");
            }
        }
    }

    /// <summary>
    /// Synchronizuje wygląd przycisków toggle (Przyciski_Wyboru1) ze stanem cen
    /// załadowanych z bazy po wybraniu badania z listy.
    /// </summary>
    private void Vm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (DataContext is not BadaniaEditNewViewModel vm) return;

        switch (e.PropertyName)
        {
            case nameof(BadaniaEditNewViewModel.Cena1):
                SetToggleButtonState(btnNewToggleBasic1, vm.Cena1.GetValueOrDefault() > 0);
                break;
            case nameof(BadaniaEditNewViewModel.Cena2):
                SetToggleButtonState(btnNewToggleLaryngologist1, vm.Cena2.GetValueOrDefault() > 0);
                break;
            case nameof(BadaniaEditNewViewModel.Cena3):
                SetToggleButtonState(btnNewToggleOphthalmologist1, vm.Cena3.GetValueOrDefault() > 0);
                break;
            case nameof(BadaniaEditNewViewModel.Cena4):
                SetToggleButtonState(btnNewToggleSanitary1, vm.Cena4.GetValueOrDefault() > 0);
                break;
            case nameof(BadaniaEditNewViewModel.Cena5):
                SetToggleButtonState(btnNewToggleLipidogram1, vm.Cena5.GetValueOrDefault() > 0);
                break;
            case nameof(BadaniaEditNewViewModel.Cena6):
                SetToggleButtonState(btnNewToggleEKG1, vm.Cena6.GetValueOrDefault() > 0);
                break;
            case nameof(BadaniaEditNewViewModel.Cena7):
                SetToggleButtonState(btnNewToggleHealthClinic1, vm.Cena7.GetValueOrDefault() > 0);
                break;
            case nameof(BadaniaEditNewViewModel.Cena8):
                SetToggleButtonState(btnNewToggleOther1, vm.Cena8.GetValueOrDefault() > 0);
                break;
        }
    }

    private static void SetToggleButtonState(Button? btn, bool isActive)
    {
        if (btn == null) return;

        if (isActive)
        {
            btn.Content = "✓ AKTYWNE";
            btn.Background = Brushes.LightGreen;
        }
        else
        {
            btn.Content = "✗ NIEAKTYWNE";
            btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD0D3D6"));
        }
    }

    private void ClearFilter_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is BadaniaEditNewViewModel vm)
        {
            vm.FilterText = string.Empty;
        }
    }

    private void Lista_Badan_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Navigate to Lista Badań
    }

    private void ClearDates_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not BadaniaEditNewViewModel vm) return;

        if (vm.DataWaznosci == DateTime.MinValue)
        {
            // Drugie wciśnięcie → przywróć DataBadania + ValidityYears
            vm.DataWaznosci = vm.DataBadania.AddYears(vm.ValidityYears);
        }
        else
        {
            // Pierwsze wciśnięcie → ustaw 01.01.0001
            vm.DataWaznosci = DateTime.MinValue;
            if (Data_Waznosci_Bad != null)
                Data_Waznosci_Bad.Value = DateTime.MinValue;
        }
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
        if (DataContext is BadaniaEditNewViewModel vm)
        {
           vm.RecalculateTotalPrice();
        }
    }

    private void ToggleNewExamination_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        if (DataContext is not BadaniaEditNewViewModel vm) return;

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

    private void DeleteBadanie_Click(object sender, RoutedEventArgs e)
    {
        // Handle delete button click
        if (DataContext is BadaniaEditNewViewModel vm)
        {
            
        }
    }
}
