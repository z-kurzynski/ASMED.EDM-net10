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
                vm.PropertyChanged += Vm_PropertyChanged;

                DataContext = vm;
                Console.WriteLine("✅ BadaniaNewView: DataContext set with DbConnectionFactory");
            }
            else
            {
                // Fallback bez DI (nie powinno się zdarzyć)
                var vm = new BadaniaNewViewModel(null);
                vm.ToggleButtonsResetRequested += ResetToggleButtons;
                vm.PropertyChanged += Vm_PropertyChanged;
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

    private void ClearDates_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not BadaniaNewViewModel vm) return;

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

        // Powiązanie wybranych przycisków z polami statusu w B_Skierowania
        // (Status_Info): "Basic" = Badanie ↔ B_Bad, "Sanitary" = Książeczka ↔ B_ksiazeczka, "HealthClinic" = Urlop ↔ B_Zaswiadczenie,
        // "Laryngologist" ↔ B_laryngolog, "Ophthalmologist" ↔ B_okulista, "EKG" ↔ B_EKG, "Lipidogram" ↔ B_Lipidogram
        var newActiveState = !isActive;
        switch (examinationType)
        {
            case "Basic": vm.SetBadStatus(newActiveState); break;
            case "Sanitary": vm.SetKsiazeczkaStatus(newActiveState); break;
            case "HealthClinic": vm.SetZaswiadczenieStatus(newActiveState); break;
            case "Laryngologist": vm.SetLaryngologStatus(newActiveState); break;
            case "Ophthalmologist": vm.SetOkulistaStatus(newActiveState); break;
            case "EKG": vm.SetEKGStatus(newActiveState); break;
            case "Lipidogram": vm.SetLipidogramStatus(newActiveState); break;
        }
    }

    /// <summary>
    /// Synchronizuje wygląd przycisków powiązanych z polami statusu (Książeczka, Urlop,
    /// Laryngolog, Okulista, EKG, Lipidogram) po załadowaniu ceny z bazy przy wyborze badania z listy.
    /// </summary>
    private void Vm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (DataContext is not BadaniaNewViewModel vm) return;

        switch (e.PropertyName)
        {
            case nameof(BadaniaNewViewModel.Cena1):
                SetToggleButtonVisual(btnNewToggleBasic, vm.Cena1.GetValueOrDefault() > 0);
                break;
            case nameof(BadaniaNewViewModel.Cena2):
                SetToggleButtonVisual(btnNewToggleLaryngologist, vm.Cena2.GetValueOrDefault() > 0);
                break;
            case nameof(BadaniaNewViewModel.Cena3):
                SetToggleButtonVisual(btnNewToggleOphthalmologist, vm.Cena3.GetValueOrDefault() > 0);
                break;
            case nameof(BadaniaNewViewModel.Cena4):
                SetToggleButtonVisual(btnNewToggleSanitary, vm.Cena4.GetValueOrDefault() > 0);
                break;
            case nameof(BadaniaNewViewModel.Cena5):
                SetToggleButtonVisual(btnNewToggleLipidogram, vm.Cena5.GetValueOrDefault() > 0);
                break;
            case nameof(BadaniaNewViewModel.Cena6):
                SetToggleButtonVisual(btnNewToggleEKG, vm.Cena6.GetValueOrDefault() > 0);
                break;
            case nameof(BadaniaNewViewModel.Cena7):
                SetToggleButtonVisual(btnNewToggleHealthClinic, vm.Cena7.GetValueOrDefault() > 0);
                break;
        }
    }

    private static void SetToggleButtonVisual(Button? btn, bool isActive)
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

    }

